#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Test_Generalist.py — Simulated run of the pre-trained Generalist PPO Agent
using the Worker orchestration class with plotting.

@author: ncn-neuron
"""

import os
import sys
from pathlib import Path
import platform  

import time
import numpy as np
import torch
import matplotlib.pyplot as plt

from neuron import h

# Cross-platform loading of NEURON mechanisms (Windows uses .dll, Linux/macOS uses .so)
script_dir = Path(__file__).resolve().parent if '__file__' in globals() else Path.cwd()
if not hasattr(h, "AMPA_DynSyn"):
    if platform.system() == "Windows":
        dll_path = script_dir / "nrnmech.dll"
        if dll_path.exists():
            h.nrn_load_dll(str(dll_path))
    else:
        so_path = script_dir / "mods/x86_64/libnrnmech.so"
        if so_path.exists():
            h.nrn_load_dll(str(so_path))

from environments.BaseEnvironment import BaseEnvironmentConfig
from trainers.Worker import Worker


#%% Environment & Task Configurations
# -----------------------------------------------------------------------------
step_duration_ms = 200
max_num_steps    = 300

env_config = BaseEnvironmentConfig(
    get_cell_spikes=True,
    spike_sorting=False,
    tau_noise=4.5,
    nCells=200,
    nElectrodes=9,
    sides=[(750, 750)],
    nClusters=8,
    cluster_spread=100,
    min_cell_dist=10,
    NOISE_SEED=42,
    seed=5,
    fraction_inh=0.05,
    nConns_exc=10,
    nConns_inh=20,
    sigma_exc=1e4,
    sigma_inh=100,
    wee=97e-5,
    wei=870e-5,
    wie=-1345e-5,
    wii=0,
    weight_noise=15e-5,
    tau_decay_NMDA=148.5,
    hub_strength_exc=0,
    hub_strength_inh=0,
    hub_mode='out'
)

task_params = {
    'stim_frequency':              1e-9,
    'stim_amplitude':              0.4,
    'pulse_duration':              0.2,
    'action_space_electrodes':     list(range(0, 10)),
    'ignore_electrodes':           [],
    'hidden_layer_size':           32,
    'actor_lr':                    3e-3,
    'critic_lr':                   1e-4,
    'gamma':                       0,
    'weight_entropy':              0.001,
    'clip_epsilon':                0.2,
    'step_duration_ms':            step_duration_ms,
    'max_num_steps':               max_num_steps,
    'burst_method':                "v1",
    'min_spike_interval_ms':       3,
    'max_network_burst_isi_ms':    20,
    'min_spikes_per_electrode':    3,
    'min_ibi_ms':                  200,
    'min_ratio_active_electrodes': 0.3
}


#%% Instantiate Worker & Load Agent Weights
# -----------------------------------------------------------------------------
model_path = "generalist_PPO_hidden=1_size=32.pth"

print("Initializing Worker in 'generalist' mode...")
worker = Worker(env_params=env_config, task_params=task_params, worker_id=0, mode="generalist")

if os.path.exists(model_path):
    print(f"Loading pre-trained Generalist agent weights from: {model_path}")
    worker.local_agent.load_agent(model_path)
else:
    print(f"Warning: Model weights file not found at {model_path}. Running with random/uninitialized agent weights.")


#%% Execute Generalist Testing via Worker
# -----------------------------------------------------------------------------
print(f"\nExecuting simulation run ({max_num_steps} steps @ {step_duration_ms} ms/step)...")
worker.run(nSteps=max_num_steps, update_agent=False)

print("\nGeneralist agent simulation completed successfully.")


#%% Extract Simulation Data & Plot Run
# -----------------------------------------------------------------------------
import MyUtils

MyUtils.set_pub_style()

all_actions = []
all_rewards = []

for ep_data in worker.episode_batches:
    all_actions.extend(ep_data['a'])
    all_rewards.extend(ep_data['r'])

all_actions = np.array(all_actions)
all_rewards = np.array(all_rewards)
total_stims = np.count_nonzero(all_actions)
total_reward = np.sum(all_rewards)
total_sim_time_s = h.t / 1000.0

print("\n==================================================================")
print("GENERALIST SIMULATION SUMMARY")
print("==================================================================")
print(f"Total Episodes (NBs): {len(worker.episode_batches)}")
print(f"Total Step Count   : {len(all_actions)} steps")
print(f"Biological Time    : {total_sim_time_s:.2f} seconds")
print(f"Total Stims        : {total_stims} electrical pulses triggered")
print(f"Recorded Stim Times: {len(worker.env.stim_times_s)} pulses recorded")
print(f"Cumulative Reward  : {total_reward:.4f}")
print("==================================================================")


fig, (ax1, ax2) = plt.subplots(2, 1, figsize=(15, 5))

# Subplot 1: Electrode Spike Raster & Accurately Timed Stimulation Events
MyUtils.plot_electrode_raster(
    nElectrodes=worker.env.nElectrodes,
    all_electrode_spikes=worker.env.all_electrode_spikes,
    time_window=[0, total_sim_time_s],
    stim_times=worker.env.stim_times_s,
    show_bursts=False,
    ax=ax1
)
ax1.set_title('Generalist Agent Run — MEA Electrode Raster & IFR')


# Estimate the IFR
FS = 10
all_electrode_spikes = [np.array(spikes) for spikes in worker.env.all_electrode_spikes]
ifr_time, ifr_rate = MyUtils.estimate_ifr(
    all_electrode_spikes, 
    fs=FS, 
    tau_rise=0.010, # s
    tau_decay=0.050  # s
)

ax2.plot(ifr_time, ifr_rate, color='#1f77b4', linewidth=2)
plt.xlim(0, total_sim_time_s)
plt.xlabel('Time (s)')
plt.ylabel('IFR (Hz)')
plt.grid(False)
ax2.spines['top'].set_visible(False)
ax2.spines['right'].set_visible(False)
ax2.spines['bottom'].set_linewidth(2)
ax2.spines['left'].set_linewidth(2)

plt.tight_layout()
output_plot = os.path.join(script_dir, "figures/Generalist_Worker_Run_Result.png")
plt.savefig(output_plot, dpi=300)
print(f"\nSummary plot saved to: {output_plot}")
plt.show()

