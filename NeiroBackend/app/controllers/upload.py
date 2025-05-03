import os
import shutil
from fastapi import APIRouter, HTTPException
from fastapi.responses import StreamingResponse
from app.services.fileservice import BASE_DIR
        
router = APIRouter()

def file_stream_and_cleanup(file_path: str, process_dir: str):
    try:
        with open(file_path, "rb") as f:
            yield from iter(lambda: f.read(4096), b'')
    finally:
        try:
            pass
            #if os.path.exists(process_dir):
            #    shutil.rmtree(process_dir)  
        except OSError as e:
            print(f"Cleanup failed: {e}")

@router.get("/fileshare/upload")
async def download_file(userID: str, processID: str, fileName: str):
    file_path = os.path.join(BASE_DIR, userID, processID, 'Result', fileName)
    process_dir = os.path.join(BASE_DIR, userID, processID)  

    if not os.path.exists(file_path):
        raise HTTPException(status_code=404, detail="File not found")

    return StreamingResponse(
        file_stream_and_cleanup(file_path, process_dir),
        media_type="application/octet-stream",
        headers={
            "Content-Disposition": f'attachment; filename="{fileName}"'
        }
    )
