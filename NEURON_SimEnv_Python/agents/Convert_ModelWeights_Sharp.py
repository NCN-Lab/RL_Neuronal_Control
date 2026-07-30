# -*- coding: utf-8 -*-
"""
Convert_ModelWeights_Sharp.py — Converts
neural network model weights from .pth to JSON format
for PyTorch-based training or deployment.

@author: ncn-neuron
"""

import torch
from torch.utils.data import Dataset

import numpy as np
from json import JSONEncoder
import json
    
filename = "generalist_PPO_hidden=1_size=32" 

# Load the file
file = checkpoint = torch.load(filename + ".pth")

actor_state_dict = file["actor_state_dict"]
critic_state_dict = file["critic_state_dict"]

class EncodeTensor(JSONEncoder,Dataset):
    def default(self, obj):
        if isinstance(obj, torch.Tensor):
            return obj.cpu().detach().numpy().tolist()
        return super(EncodeTensor, self).default(obj)


data_dict = {"actor_state_dict": actor_state_dict, "critic_state_dict": critic_state_dict}

json_data = json.dumps(data_dict, cls=EncodeTensor)

with open(filename + ".json", "w") as json_file:
    json.dump(json_data, json_file)

print("Saved data to data.json")
