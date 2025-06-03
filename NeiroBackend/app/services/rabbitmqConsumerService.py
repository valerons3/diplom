import asyncio
import aio_pika
import os
import json
from urllib.parse import unquote
from app.config.config import CONFIG
from app.models.rabbitData import RabbitData
from app.services.fileservice import DownloadAndWriteFile
from app.services.fileservice import WriteResultFile
from app.services.fileservice import extract_filename
from app.services.resUnet import unwrap_phase
from app.services.res import unwrap_phase_ddn
from app.models.rabbitData import ProcessStatus
from app.services.rabbitmqProducer import send_message_to_queue


async def process_message(message: aio_pika.IncomingMessage):
    """Обрабатывает полученное сообщение из очереди"""
    async with message.process():
        
        # Декодирование тела сообщения как UTF-8
        body = message.body.decode('utf-8')
        data = RabbitData.from_json(body)

        # Декодируем URL в поле DownloadLink
        decoded_link = unquote(data.DownloadLink)

        # Переход к скачиванию и записи файла
        fileName = extract_filename(decoded_link)
        inputFilePath = await DownloadAndWriteFile(data.UserID, data.ProcessID, fileName, decoded_link)

        # Процесс обработки нейросетью

        if data.ProcessMethod == 'neural':
            result = unwrap_phase(inputFilePath)
            if result['status'] == ProcessStatus.SUCCESS:
                newFileName = await WriteResultFile(data.UserID, data.ProcessID, fileName, result['content'], result["contentInputFile"])
                downloadLink = f'{CONFIG["FileShare"]["BaseURL"]}userID={data.UserID}&processID={data.ProcessID}&fileName={newFileName}'
                resultImageDownloadLink = f'{CONFIG["FileShare"]["BaseURL"]}userID={data.UserID}&processID={data.ProcessID}&fileName=ResultPhase.png'
                inputImageDownloadLink = f'{CONFIG["FileShare"]["BaseURL"]}userID={data.UserID}&processID={data.ProcessID}&fileName=InputPhase.png'
                rabbitData = RabbitData(
                UserID=data.UserID,
                ProcessID=data.ProcessID,
                Status=ProcessStatus.SUCCESS,
                ProcessMethod=data.ProcessMethod,
                DownloadLink=downloadLink,
                ResultImageDownloadLink=resultImageDownloadLink,
                InputImageDownloadLink=inputImageDownloadLink,
                ProcessingTime=result["processing_time"]
                )
                await send_message_to_queue(rabbitData)

            if result['status'] == ProcessStatus.FAILED:
                rabbitData = RabbitData(
                UserID=data.UserID,
                ProcessID=data.ProcessID,
                Status=ProcessStatus.FAILED,
                ProcessMethod=data.ProcessMethod,
                DownloadLink=None,
                ProcessingTime=None
                )
                await send_message_to_queue(rabbitData)

        if data.ProcessMethod == 'phase-denoising':
            result = unwrap_phase_ddn(inputFilePath)
            if result['status'] == ProcessStatus.SUCCESS:
                newFileName = await WriteResultFile(data.UserID, data.ProcessID, fileName, result['content'], result["contentInputFile"])
                downloadLink = f'{CONFIG["FileShare"]["BaseURL"]}userID={data.UserID}&processID={data.ProcessID}&fileName={newFileName}'
                resultImageDownloadLink = f'{CONFIG["FileShare"]["BaseURL"]}userID={data.UserID}&processID={data.ProcessID}&fileName=ResultPhase.png'
                inputImageDownloadLink = f'{CONFIG["FileShare"]["BaseURL"]}userID={data.UserID}&processID={data.ProcessID}&fileName=InputPhase.png'
                rabbitData = RabbitData(
                UserID=data.UserID,
                ProcessID=data.ProcessID,
                Status=ProcessStatus.SUCCESS,
                ProcessMethod=data.ProcessMethod,
                DownloadLink=downloadLink,
                ResultImageDownloadLink=resultImageDownloadLink,
                InputImageDownloadLink=inputImageDownloadLink,
                ProcessingTime=result["processing_time"]
                )
                await send_message_to_queue(rabbitData)

            if result['status'] == ProcessStatus.FAILED:
                rabbitData = RabbitData(
                UserID=data.UserID,
                ProcessID=data.ProcessID,
                Status=ProcessStatus.FAILED,
                ProcessMethod=data.ProcessMethod,
                DownloadLink=None,
                ProcessingTime=None
                )
                await send_message_to_queue(rabbitData)


async def consume():
    """Основной процесс потребителя сообщений"""
    rabbit_config = CONFIG["RabbitMQ"]
    
    connection = await aio_pika.connect_robust(
        f"amqp://{rabbit_config['Username']}:{rabbit_config['Password']}@"
        f"{rabbit_config['Host']}:{rabbit_config['Port']}/{rabbit_config['VirtualHost']}"
    )

    async with connection:
        channel = await connection.channel()
        queue = await channel.declare_queue(rabbit_config["ReceiverQueue"], durable=True)


        async for message in queue:
            await process_message(message)
