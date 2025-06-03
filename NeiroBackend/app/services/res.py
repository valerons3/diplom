import os
import time
import torch
import torch.nn as nn
import numpy as np
import scipy.io
from scipy.ndimage import zoom
import datetime
from app.models.rabbitData import ProcessStatus

def find_arr_in_matfile(matfile):
    """Находит первый numpy массив в .mat файле"""
    for key in matfile.keys():
        if isinstance(matfile[key], np.ndarray):
            return matfile[key]
    raise ValueError("No numpy array found in .mat file")

def zoom_preprocess():
    """Создает функцию для увеличения изображения"""
    def image_zoom(torch_tensor):
        return torch.from_numpy(zoom(torch_tensor, (2, 2), order=1))
    return image_zoom

def normalize_data(data):
    """Нормализует данные в диапазон [0, 1]"""
    min_val = torch.min(data)
    max_val = torch.max(data)
    return (data - min_val) / (max_val - min_val + 1e-8)

class BottleNeck(nn.Module):
    def __init__(self, in_channels):
        super().__init__()
        self.conv1 = nn.Conv2d(in_channels, in_channels, kernel_size=1)
        self.conv2 = nn.Conv2d(in_channels, in_channels, kernel_size=3, padding='same')
        
    def forward(self, x):
        identity = x
        x = torch.tanh(self.conv1(x))
        x = torch.tanh(self.conv2(x))
        x = torch.tanh(self.conv1(x))
        return identity + x

class DenseBlock(nn.Module):
    def __init__(self, in_channels):
        super().__init__()
        self.bottle = BottleNeck(in_channels)
        self.conv = nn.Conv2d(in_channels, 24, kernel_size=3, padding='same', padding_mode='circular')
        self.bn = nn.BatchNorm2d(in_channels)
        
    def forward(self, x):
        x = torch.cat([x, x], dim=1)
        x = torch.relu(self.bn(x))
        x = self.bottle(x)
        x = torch.relu(self.bn(x))
        return self.conv(x)

class FeatureExtractor(nn.Module):
    def __init__(self):
        super().__init__()
        self.conv1 = nn.Conv2d(1, 96, kernel_size=7, padding='same', padding_mode='circular')
        self.dense_block1 = DenseBlock(192)
        self.dense_block2 = DenseBlock(48)
        
    def forward(self, x):
        x = torch.relu(self.conv1(x))
        x1 = self.dense_block1(x)
        x2 = self.dense_block2(x1)
        x2 = x2 + x1
        x3 = self.dense_block2(x2)
        x3 = x3 + x2 + x1
        x4 = self.dense_block2(x3)
        x4 = x4 + x3 + x2 + x1
        x5 = self.dense_block2(x4)
        x5 = x5 + x4 + x3 + x2 + x1
        x6 = self.dense_block2(x5)
        return x6

class ComponentFilter(nn.Module):
    def __init__(self, in_channels, out_channels):
        super().__init__()
        self.bn = nn.BatchNorm2d(in_channels)
        self.conv = nn.Conv2d(in_channels, out_channels, kernel_size=3, padding='same', padding_mode='circular')
        
    def forward(self, x):
        return self.conv(torch.relu(self.bn(x)))

class ComponentFilterNoBatch(nn.Module):
    def __init__(self, in_channels, out_channels):
        super().__init__()
        self.conv = nn.Conv2d(in_channels, out_channels, kernel_size=3, padding='same', padding_mode='circular')
        
    def forward(self, x):
        return self.conv(torch.relu(x))

class PhaseFilter(nn.Module):
    def __init__(self):
        super().__init__()
        self.comp1 = ComponentFilter(24, 24)
        self.comp2 = ComponentFilter(24, 24)
        self.comp21 = ComponentFilterNoBatch(24, 24)
        self.comp3 = ComponentFilter(24, 1)
        
    def forward(self, x):
        x = self.comp1(x)
        x = self.comp2(x)
        x = self.comp2(x)
        x = self.comp21(x)
        x = self.comp21(x)
        x = self.comp21(x)
        return self.comp3(x)

class NeuralNetwork(nn.Module):
    def __init__(self):
        super().__init__()
        self.features = FeatureExtractor()
        self.phase = PhaseFilter()
        
    def forward(self, x):
        x = normalize_data(x)
        x = self.features(x)
        return self.phase(x)

def unwrap_phase_ddn(filePath: str):
    """Основная функция для развертывания фазы"""
    try:
        # Инициализация модели
        model = NeuralNetwork().to("cpu")
        
        # Загрузка весов
        weights_path = "./app/services/PulNoise.pth"
        state_dict = torch.load(weights_path, map_location='cpu')
        
        # Обработка ключей для моделей, сохраненных с DataParallel
        if all(k.startswith('module.') for k in state_dict.keys()):
            state_dict = {k.replace('module.', ''): v for k, v in state_dict.items()}
        
        state_dict = {k.replace('FeatureExtractor.', 'features.'): v for k, v in state_dict.items()}
        state_dict = {k.replace('features.dense_block_1.', 'features.dense_block1.'): v for k, v in state_dict.items()}
        state_dict = {k.replace('features.dense_block_2.', 'features.dense_block2.'): v for k, v in state_dict.items()}
        state_dict = {k.replace('PhaseFilter.component_1.', 'phase.comp1.'): v for k, v in state_dict.items()}
        state_dict = {k.replace('PhaseFilter.component_2.', 'phase.comp2.'): v for k, v in state_dict.items()}
        state_dict = {k.replace('PhaseFilter.component_2_1.', 'phase.comp21.'): v for k, v in state_dict.items()}
        state_dict = {k.replace('PhaseFilter.component_3.', 'phase.comp3.'): v for k, v in state_dict.items()}
        #state_dict = {k.replace('features.dense_block1.bottle.', 'features.dense_block1.bn.'): v for k, v in state_dict.items()}
        
        print(f'[DICT] {state_dict.keys()}')

        model.load_state_dict(state_dict, strict=False)
        model.eval()

        # Загрузка данных
        mat_data = scipy.io.loadmat(filePath)
        np_array = find_arr_in_matfile(mat_data)
        tensor_data = torch.from_numpy(np_array).float()
        
        # Препроцессинг
        if tensor_data.dim() == 2:
            tensor_data = tensor_data.unsqueeze(0).unsqueeze(0)
        elif tensor_data.dim() == 3:
            tensor_data = tensor_data.unsqueeze(1)
            
        if tensor_data.size(2) == 256:
            zoom_fn = zoom_preprocess()
            tensor_data = zoom_fn(tensor_data.squeeze()).unsqueeze(0).unsqueeze(0)

        # Обработка
        start_time = time.time()
        with torch.no_grad():
            output = model(tensor_data)
        processing_time = datetime.timedelta(seconds=time.time() - start_time)

        return {
            "status": ProcessStatus.SUCCESS,
            "processing_time": str(processing_time),
            "content": output.squeeze().numpy(),
        }
        
    except Exception as e:
        print(f'[ERROR] {e}')
        return {
            "status": ProcessStatus.FAILED,
            "processing_time": "0:00:00",
            "content": None,
            "error": str(e)
        }