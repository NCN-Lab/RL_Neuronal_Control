# -*- coding: utf-8 -*-
"""
Convert_ModelWeights_json_to_pth.py — Converts
neural network model weights from JSON to .pth format
for PyTorch-based training or deployment.

@author: ncn-neuron
"""

import torch
import json
import numpy as np


filename = "generalist_PPO_hidden=1_size=32"

# Load the JSON file
with open(filename + ".json", "r") as json_file:
    raw_json_string = json.load(json_file)
    data_dict = json.loads(raw_json_string)

def dict_to_tensor(d):
    for key, value in d.items():
        if isinstance(value, list):
            d[key] = torch.tensor(value)
        elif isinstance(value, dict):
            d[key] = dict_to_tensor(value)
    return d

# Convert lists back to tensors for both actor and critic
reconstructed_data = {
    "actor_state_dict": dict_to_tensor(data_dict["actor_state_dict"]),
    "critic_state_dict": dict_to_tensor(data_dict["critic_state_dict"])
}

# Save back to .pth format
output_path = filename + "_recovered.pth"
torch.save(reconstructed_data, output_path)

print(f"Successfully recovered model weights to {output_path}")
