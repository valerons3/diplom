import os
import asyncio
import shutil
import aiofiles
import aiohttp
import ssl
import scipy
import numpy as np
import scipy.io

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

async def save_img(np_image, path):
    cs = plt.contourf(np_image, levels=100)
    plt.colorbar(cs)
    plt.savefig(path)

async def WriteResultFile(userID, processID, fileName, content):
    newFileName = f'Result{fileName}'
    fullPath = os.path.join(BASE_DIR, str(userID), str(processID), "Result", newFileName)

    os.makedirs(os.path.dirname(fullPath), exist_ok=True)
    scipy.io.savemat(fullPath, {"content": content})#сохраняет с ключом content 

    fullPathImg = os.path.join(BASE_DIR, str(userID), str(processID), "Result", 'Phase.png')
    
    await save_img(content, fullPathImg)
    
    return newFileName

async def DeleteUserProcessDirectory(userID, processID):
    fullPath = os.path.join(BASE_DIR, userID, processID)

    if not os.path.exists(fullPath):
        return  

    loop = asyncio.get_running_loop()
    await loop.run_in_executor(None, shutil.rmtree, fullPath)
