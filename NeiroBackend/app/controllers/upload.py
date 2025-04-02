import os
from fastapi import FastAPI
from fastapi import APIRouter, HTTPException
from fastapi.responses import FileResponse
from app.services.fileservice import BASE_DIR

router = APIRouter()

@router.get("/fileshare/upload")
async def download_file(userID: str, processID: str, fileName: str):
    file_path = os.path.join(BASE_DIR, userID, processID, 'Result', fileName)

    if not os.path.exists(file_path):
        raise HTTPException(status_code=404, detail="File not found")
    
    return FileResponse(file_path, media_type="application/octet-stream", filename=fileName)