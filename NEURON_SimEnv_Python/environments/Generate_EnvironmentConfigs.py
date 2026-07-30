#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Generate_EnvironmentConfigs.py — Generate a number of 
environment configurations for the NEURON network environment.

@author: ncn-neuron
"""

from environments.BaseEnvironment import BaseEnvironmentConfig

import numpy as np
from datetime import datetime

def main():
    
    path = "paper1/results/in_silico/"
    
    np.random.seed(None)
    
    # Parameters
    nElectrodes     = 9
    nCells          = 200 # 356 for 25 electrodes
    sides           = [(750, 750)] # (1000, 1000) for 25 electrodes
    nWorkers        = 1000
    seed_offset     = 0
    env_configs     =  []
    for i in range(nWorkers):
        # Pick random values from each distribution  
        # random_seed             = i // 10 + 5
        random_seed             = i + seed_offset
        # random_noise_seed       = np.random.randint(1, 10000)
        random_noise_seed       = i + seed_offset
        # random_tau_noise        = np.round(4 + 1*np.random.random(), 1)
        random_tau_noise        = 4.3
        random_tau_decay_NMDA   = np.random.randint(75, 126)
        # random_tau_decay_NMDA   = 150
        
        config = BaseEnvironmentConfig(seed=random_seed,
                                       NOISE_SEED=random_noise_seed,
                                       tau_noise=random_tau_noise,
                                       tau_decay_NMDA=random_tau_decay_NMDA,
                                       nCells=nCells,
                                       sides=sides,
                                       nElectrodes=nElectrodes)
        
        
        env_configs.append(config)  
    
    
    np.savez(path + "configs/{}.npz".format(datetime.now().strftime("%Y%m%d_%H%M%S")),
             env_configs=env_configs)
    

        
if __name__ == "__main__":
    main()