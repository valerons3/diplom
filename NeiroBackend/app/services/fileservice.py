import os
import asyncio
import shutil
import aiofiles
import aiohttp
import ssl
import scipy
import numpy as np
import scipy.io
import matplotlib
from matplotlib import pyplot as plt
from urllib.parse import urlparse, parse_qs

BASE_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../uploads"))

def extract_filename(url: str) -> str:
    parsed_url = urlparse(url)

    query_params = parse_qs(parsed_url.query)
    file_name = query_params.get('fileName', [None])[0]

    if file_name:
        return file_name

    path_parts = parsed_url.path.split("/")
    if path_parts:
        return path_parts[-1]  

    return None  


def create_ssl_context():
    ssl_context = ssl.create_default_context()
    ssl_context.check_hostname = False
    ssl_context.verify_mode = ssl.CERT_NONE
    return ssl_context

async def DownloadAndWriteFile(userID, processID, file_name, url):
    if not file_name:
        raise ValueError("Не удалось извлечь имя файла из URL.")
    
    async with aiohttp.ClientSession() as session:
        async with session.get(url) as response:
            if response.status == 200:
                content = await response.read()  
                return await WriteInputFile(str(userID), str(processID), file_name, content)
            else:
                raise Exception(f"Ошибка скачивания {url}: {response.status}")

async def WriteInputFile(userID, processID, fileName, content):
    fullPath = os.path.join(BASE_DIR, userID, processID, "Input", fileName)

    os.makedirs(os.path.dirname(fullPath), exist_ok=True)

    async with aiofiles.open(fullPath, "wb") as file:
        await file.write(content)
    return fullPath

def isColorbar(ax):
         """Guesses if an Axes instance is home to a colorbar."""
         if not ax.get_xticks() and not ax.get_yticks():
             if not ax.get_xticklabels() and not ax.get_yticklabels():
                 if not ax.get_xlabel() and not ax.get_ylabel():
                     xmin, xmax = ax.get_xlim()
                     ymin, ymax = ax.get_ylim()
                     if (xmin, xmax) == (0, 1) and (ymin, ymax) == (0, 1):
                         return True
         return False

async def save_img(np_image, path):
    fig, ax = plt.subplots()  
    cs = ax.contourf(np_image, levels=100)
    fig.colorbar(cs, ax=ax)
    fig.savefig(path)
    plt.close(fig) 

async def WriteResultFile(userID, processID, fileName, content, contentInputImage):
    newFileName = f'Result{fileName}'
    fullPath = os.path.join(BASE_DIR, str(userID), str(processID), "Result", newFileName)

    os.makedirs(os.path.dirname(fullPath), exist_ok=True)
    scipy.io.savemat(fullPath, {"content": content})#сохраняет с ключом content 

    fullResultPathImg = os.path.join(BASE_DIR, str(userID), str(processID), "Result", 'ResultPhase.png')
    fullInputPathImg = os.path.join(BASE_DIR, str(userID), str(processID), "Result", 'InputPhase.png')
    
    await save_img(content, fullResultPathImg)
    await save_img(contentInputImage, fullInputPathImg)
    
    return newFileName

async def DeleteUserProcessDirectory(userID, processID):
    fullPath = os.path.join(BASE_DIR, userID, processID)

    if not os.path.exists(fullPath):
        return  

    loop = asyncio.get_running_loop()
    await loop.run_in_executor(None, shutil.rmtree, fullPath)
