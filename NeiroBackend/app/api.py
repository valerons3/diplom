from fastapi import FastAPI
from app.controllers.upload import router as upload_router  

app = FastAPI()

app.include_router(upload_router)