from enum import IntEnum
from dataclasses import dataclass
from uuid import UUID
from typing import Optional
import json

class ProcessStatus(IntEnum):
    PROCESSING = 0
    SUCCESS = 1
    FAILED = 2

@dataclass
class RabbitData:
    UserID: UUID
    ProcessID: UUID
    ProcessingTime: Optional[int] = None  
    Status: ProcessStatus = ProcessStatus.PROCESSING  
    ProcessMethod: Optional[str] = None  
    DownloadLink: Optional[str] = None
    ImageDownloadLink: Optional[str] = None

    @staticmethod
    def from_json(json_str: str) -> "RabbitData":
        data = json.loads(json_str)

        return RabbitData(
            UserID=UUID(data["UserID"]),
            ProcessID=UUID(data["ProcessID"]),
            ProcessingTime=data.get("ProcessingTime"),
            Status=ProcessStatus(data["Status"]) if "Status" in data else ProcessStatus.PROCESSING,
            ProcessMethod=data.get("ProcessMethod"),
            DownloadLink=data.get("DownloadLink"),
            ImageDownloadLink=data.get("ImageDownloadLink")
        )
    
    def to_json(self) -> str:
        return json.dumps({
            "UserID": str(self.UserID),
            "ProcessID": str(self.ProcessID),
            "ProcessingTime": self.ProcessingTime,
            "Status": self.Status.value,
            "ProcessMethod": self.ProcessMethod,  
            "DownloadLink": self.DownloadLink,
            "ImageDownloadLink": self.ImageDownloadLink
        })