import os
import time
import torch
import numpy as np
import torch.nn as nn
import pandas as pd
import matplotlib.pyplot as plt
import datetime

import asyncio
import random
import os
from enum import IntEnum
from app.models.rabbitData import ProcessStatus
from scipy.ndimage import zoom
from torch.utils.data import DataLoader, Dataset
from pathlib import Path
import scipy

def zoom_preprocess():
    def image_zoom(torch_tensor):
        return torch.from_numpy(zoom(torch_tensor, (2, 2), order=1))
    return image_zoom


class BatchnormBlock(nn.Module):
    def __init__(self, out_channels):
        super().__init__()
        self.batch_norm = nn.Sequential(
            nn.ReLU()
        )
    
    def forward(self, x):
        return self.batch_norm(x)

class ResBlock(nn.Module):
    def __init__(self, in_channels, out_channels):
        super().__init__()
        self.batch_nrom = BatchnormBlock(in_channels)
        self.identity_conv = nn.Conv2d(in_channels, out_channels, kernel_size=1, padding='same', dilation=1)
        self.conv_3_d1 = nn.Conv2d(in_channels, out_channels, kernel_size=3, padding='same', dilation=1)
        self.conv_5_d1 = nn.Conv2d(out_channels, out_channels, kernel_size=5, padding='same', dilation=1)
        self.conv_3_d2 = nn.Conv2d(out_channels, out_channels, kernel_size=3, padding='same', dilation=2)
        
        
        
    def forward(self, x, pre_activation=False):
        x = x.float() 
        res_x = self.identity_conv(x)
        if pre_activation:
            x = self.batch_nrom(x)
        x = self.conv_3_d1(x)
        x = self.batch_nrom(x)
        x = self.conv_5_d1(x)
        x = self.batch_nrom(x)
        x = self.conv_3_d2(x)
        return x + res_x

class DownBlock(nn.Module):
    def __init__(self):
        super().__init__()
        self.down = nn.Sequential(
            nn.MaxPool2d(kernel_size=(2, 2)),
            nn.MaxPool2d(kernel_size=(2, 2))
        )
    
    def forward(self, x):
        return self.down(x)

class UpBlock(nn.Module):
    def __init__(self, in_channels, out_channels):
        super().__init__()
        self.conv_block = ResBlock(in_channels+in_channels//2, out_channels)
        self.up_sample = nn.Sequential(
            nn.Upsample(scale_factor=2),
            nn.Upsample(scale_factor=2),
        )
        
    def forward(self, down_input, skip_input):
        x = self.up_sample(down_input)
        x = torch.cat([x, skip_input], dim=1)
        return self.conv_block(x, pre_activation=True)
        
class ResUnet(nn.Module):
    def __init__(self):
        super().__init__()
        self.conv_block_1 = ResBlock(1, 16)
        self.down_block_1 = DownBlock()
        self.conv_block_2 = ResBlock(16, 32)
        self.down_block_2 = DownBlock()
        self.conv_block_3 = ResBlock(32, 64)
        self.down_block_3 = DownBlock()
        
        self.bridge = ResBlock(64, 128)

        self.up_block_3 = UpBlock(128, 64)
        self.up_block_2 = UpBlock(64, 32)
        self.up_block_1 = UpBlock(32, 16)
        
        self.last_conv = nn.Conv2d(16, 1, kernel_size=1, padding='same', dilation=1)
        
    
    def forward(self, x):
        
        x_conv_1 = self.conv_block_1(x)
        x_down_1 = self.down_block_1(x_conv_1)
        
        x_conv_2 = self.conv_block_2(x_down_1, pre_activation=True)
        x_down_2 = self.down_block_2(x_conv_2)
        
        x_conv_3 = self.conv_block_3(x_down_2, pre_activation=True)
        x_down_3 = self.down_block_3(x_conv_3)
        
        x_bridge = self.bridge(x_down_3, pre_activation=True)
        
        x_up_3 = self.up_block_3(x_bridge, x_conv_3)
        x_up_2 = self.up_block_2(x_up_3, x_conv_2)
        x_up_1 = self.up_block_1(x_up_2, x_conv_1)
        
        x_last_conv = self.last_conv(x_up_1)

        return x_last_conv

        
def find_arr_in_matfile(matfile):
    for key in matfile.keys():
        if type(matfile[key]) == np.ndarray:
            return matfile[key]
    

def unwrap_phase(filePath: str):
    model = ResUnet().to("cpu")
    w_dict = torch.load("./app/services/resunet.pt", weights_only=True, map_location=torch.device('cpu'))
    # print(f"w-dict\n{w_dict.keys()}")
    # w_dict = {k.partition('module.')[2]:w_dict[k] for k in w_dict.keys()}


    model.load_state_dict(w_dict)
    model.eval()

    matfile = scipy.io.loadmat(filePath)
    np_phase = find_arr_in_matfile(matfile)
    zoom_prep = zoom_preprocess()

    torch_phase = torch.from_numpy(np_phase)
    
    if torch_phase.size()[0] == 256:
        torch_phase = zoom_prep(torch_phase)
    

    start_time = time.time()
    with torch.no_grad():
        # удаляет все фиктивные измерения массива (512, 512, 1) --> (512, 512)
        torch_phase = torch_phase.squeeze()
        torch_phase = torch_phase.unsqueeze(0).unsqueeze(0)
        res = model(torch_phase)

    processing_time = time.time() - start_time
    processing_time = datetime.timedelta(seconds=processing_time)
    # в content будет numpy массив 512 на  512, чтобы проверить размерность -- res["content"].shape
    return {
        "status": ProcessStatus.SUCCESS,
        "processing_time": str(processing_time),
        "content": res.squeeze().numpy(),
    }