import aio_pika
import json
import uuid
from app.config.config import CONFIG
from app.models.rabbitData import RabbitData

async def send_message_to_queue(rabbit_data: RabbitData):
    """Отправка сообщения в очередь RabbitMQ"""
    
    rabbit_config = CONFIG["RabbitMQ"]
    
    # Формируем соединение с RabbitMQ
    connection = await aio_pika.connect_robust(
        f"amqp://{rabbit_config['Username']}:{rabbit_config['Password']}@"
        f"{rabbit_config['Host']}:{rabbit_config['Port']}/{rabbit_config['VirtualHost']}"
    )
    
    async with connection:
        # Создаем канал
        channel = await connection.channel()
        
        queue = await channel.declare_queue(rabbit_config["SenderQueue"], durable=True)
        
        # Преобразуем модель RabbitData в JSON
        message_body = rabbit_data.to_json()
        
        await channel.default_exchange.publish(
            aio_pika.Message(body=message_body.encode('utf-8')),
            routing_key=rabbit_config["SenderQueue"]
        )