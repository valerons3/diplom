import asyncio
import uvicorn
from app.services.rabbitmqConsumerService import consume
from app.api import app
from app.config.config import CONFIG  

async def main():
    """Запуск FastAPI и RabbitMQ параллельно"""
    rabbit_task = asyncio.create_task(consume())

    # Берем конфиг из файла
    fastapi_host = CONFIG["FastAPI"]["Host"]
    fastapi_port = CONFIG["FastAPI"]["Port"]

    config = uvicorn.Config(app, host=fastapi_host, port=fastapi_port)
    server = uvicorn.Server(config)
    fastapi_task = server.serve()

    print(f"🚀 FastAPI запущен на http://{fastapi_host}:{fastapi_port}")
    
    await asyncio.gather(rabbit_task, fastapi_task)


if __name__ == "__main__":
    loop = asyncio.get_event_loop()
    loop.run_until_complete(main())