#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Test_Baseline.py — Quick 1-minute simulated run of the baseline
on a biophysical NEURON network environment.

@author: ncn-neuron
"""

import os
import sys
from pathlib import Path
import platform  

import time
import numpy as np
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

from environments.BaseEnvironment import BaseEnvironmentConfig, BaseEnvironment

#%% Environment Configuration
# -----------------------------------------------------------------------------

env_config = BaseEnvironmentConfig(
    get_cell_spikes=True,
    spike_sorting=True,
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


env = BaseEnvironment(env_config)
fig0 = env.grid.plot_grid_and_network(env.cells)
output_plot = os.path.join(script_dir, "figures/Env_Grid.png")
plt.savefig(output_plot, dpi=300)
fig1 = env.grid.plot_degree_maps(env.cells, env.all_conns)
output_plot = os.path.join(script_dir, "figures/Env_Grid_Hubs.png")
plt.savefig(output_plot, dpi=300)
# fig2 = env.grid.plot_connectivty(env.cells, env.conn_weights)
# output_plot = os.path.join(script_dir, "figures/Env_Grid_Connectivty.png")
# plt.savefig(output_plot, dpi=300)

#%% Run Baseline
# -----------------------------------------------------------------------------
duration_s = 30

# stim_electrode = [1,2,3,4]
# stim_electrode = [4]
stim_electrode = None

env.reset()
start = time.time()

if duration_s > 30:
    env.run_for(duration_s, stim_frequency=0.5, stim_amplitude=0.8, number_pulses=5,
                     stim_electrode=stim_electrode, save_path=None)
else:
    env.run_baseline(duration_s, stim_frequency=0.5, stim_amplitude=0.8, number_pulses=5,
                     stim_electrode=stim_electrode, save_path=None, buffer_duration=10)
    
end = time.time()
runtime = end - start
print(f"Runtime: {runtime:.3f} seconds")

#%% Plots
# -----------------------------------------------------------------------------
import MyUtils

MyUtils.set_pub_style()

# Get burst times
min_bursting_electrodes = 0.3 * env.nElectrodes
all_electrode_spikes = [np.array(spikes) for spikes in env.all_electrode_spikes]
if type(stim_electrode) != list: stim_electrode = [stim_electrode]

all_electrode_spikes_tuple = []
for e in range(env.nElectrodes):  
    electrode_spikes = list([t for t in all_electrode_spikes[e]])  
    for spike in electrode_spikes:
        all_electrode_spikes_tuple.append((e, spike))

bursts, active_electrodes = MyUtils.detect_network_bursts_v1(
    all_electrode_spikes_tuple,
    env.nElectrodes, 
    min_bursting_electrodes, 
    3, 
    20, 
    200)

excColor = 'blue'  # Example color value for excitatory
inhColor = 'red'  # Example color value for inhibitory
color_array = []
for cell_ids in env.all_electrode_spikes_sorted:
    electrode_colors = []
    for cell_id in cell_ids:
        if cell_id in env.excCells:
            electrode_colors.append(excColor)
        elif cell_id in env.inhCells:
            electrode_colors.append(inhColor)
        else:
            electrode_colors.append('black')
    color_array.append(electrode_colors)

fig1 = MyUtils.plot_electrode_raster(
    env.nElectrodes, 
    env.all_electrode_spikes, 
    [0, h.t/1000], 
    env.stim_times_s, 
    bursts=bursts, 
    show_bursts=True, 
    color_spikes=color_array)
output_plot = os.path.join(script_dir, "figures/Baseline_Raster_SpikeSorted.png")
plt.savefig(output_plot, dpi=300)

fig2 = MyUtils.plot_cell_raster(
    env.excCells, 
    env.inhCells, 
    env.all_electrode_spikes, 
    env.all_cell_spikes, 
    [0, h.t/1000])
output_plot = os.path.join(script_dir, "figures/Baseline_Raster.png")
plt.savefig(output_plot, dpi=300)

cells_firing_rates = []
exc_isi = []
inh_isi = []
for c, cell_spikes in enumerate(env.all_cell_spikes):
    basal_cell_spikes = [s for s in cell_spikes if s>0 and s<2000]  
    cells_firing_rates.append(len(basal_cell_spikes)/2)
    
    if c in env.excCells:
        exc_isi += list(np.diff(basal_cell_spikes))
    else:
        inh_isi += list(np.diff(basal_cell_spikes))
    
cells_firing_rates = np.array(cells_firing_rates)

# Plot histograms
plt.figure(figsize=(10, 6))
plt.hist(cells_firing_rates[env.excCells], bins=20, alpha=0.7, label="Excitatory", color="blue")
plt.xlabel("Firing Rate (Hz)", fontsize=16, weight="bold")
plt.ylabel("Frequency", fontsize=16, weight="bold")
plt.legend()
plt.grid(False)
plt.tight_layout()
output_plot = os.path.join(script_dir, "figures/Baseline_FR_Exc.png")
plt.savefig(output_plot, dpi=300)
plt.show()

plt.figure(figsize=(10, 6))
plt.hist(cells_firing_rates[env.inhCells], bins=20, alpha=0.7, label="Inhibitory", color="red")
plt.xlabel("Firing Rate (Hz)", fontsize=16, weight="bold")
plt.ylabel("Frequency", fontsize=16, weight="bold")
plt.legend()
plt.grid(False)
plt.tight_layout()
output_plot = os.path.join(script_dir, "figures/Baseline_FR_Inh.png")
plt.savefig(output_plot, dpi=300)
plt.show()
    

electrode = env.grid.electrodes[3]
electrode.detect_bursts(200)
fig5 = electrode.plot_field_potentials(filtered=True, thresholds=True)
output_plot = os.path.join(script_dir, "figures/Baseline_Elec_Trace.png")
plt.savefig(output_plot, dpi=300)

# Estimate the IFR
FS = 100
ifr_time, ifr_rate = MyUtils.estimate_ifr(
    all_electrode_spikes, 
    fs=FS, 
    tau_rise=0.010, # s
    tau_decay=0.050  # s
)

# Plot
fig, (ax1, ax2) = plt.subplots(2, sharex=True, figsize=(15,5))

MyUtils.plot_electrode_raster(
    env.nElectrodes, 
    all_electrode_spikes, 
    [0, h.t/1000], 
    env.stim_times_s, 
    bursts=bursts, 
    show_bursts=True, 
    color_spikes=None,
    ax=ax1)

# Plot the IFR trace
ax2.plot(ifr_time, ifr_rate, color='#1f77b4', linewidth=2)
plt.xlim(0, duration_s)
plt.xlabel('Time (s)', weight='bold')
plt.ylabel('IFR (Hz)', weight='bold')
plt.grid(False)
ax2.spines['top'].set_visible(False)
ax2.spines['right'].set_visible(False)
ax2.spines['bottom'].set_linewidth(2)
ax2.spines['left'].set_linewidth(2)
plt.tight_layout()
output_plot = os.path.join(script_dir, "figures/Baseline_Raster_with_IFR.png")
plt.savefig(output_plot, dpi=300)
plt.show()


#%%

print("Starting Oscillator Replay with Period Adaptation...")

# 1. PREPARE DATA
# Flatten all electrode spikes into a single sorted timeline (ms)
# The oscillator reacts to the aggregate network activity
all_spikes_flat = []
for elec_spikes in env.all_electrode_spikes:
    all_spikes_flat.extend(elec_spikes)
all_spikes_flat = np.sort(all_spikes_flat)

# 2. CONFIGURATION (Matching C# / Worker Parameters)
dt_ms = 0.1             # Simulation step in ms
dt_sec = dt_ms / 1000.0 # Step in seconds (0.0001s)

# Initial Period guess (e.g., 1.0s or based on first few bursts if available)
T_period = 1.0          
w = 2 * np.pi / T_period

# Oscillator State
x_now = 0.0
x_pre = 0.0
x_pre_pre = 0.0
osctr_value = 0.0       # x_dot (Velocity)
gain = 0.01

# IFR Kernel Init
tau_rise = 0.010
tau_decay = 0.050
kernel_rise = 0.0
kernel_decay = 0.0
kernel_C = 1.0 / (tau_decay - tau_rise)

# NIBI Tracking
nibi_buffer = [1000.0] * 5 # Initialize with 1s (in ms)
burst_cursor = 0           # To track which burst we are approaching

# Trace Storage
trace_time = []
trace_ifr = []
trace_osc_x = []
trace_osc_dot = []
trace_energy = [] # NEW: Energy Trace
trace_T = []      # To verify period adaptation

# 3. REPLAY LOOP
# Iterate through the entire simulation time
total_duration_ms = h.t
current_time_ms = 0.0
spike_cursor = 0
total_spikes = len(all_spikes_flat)


while current_time_ms < total_duration_ms:
    
    # --- A. PERIOD ADAPTATION LOGIC ---
    # Check if we just entered a new burst (Start time)
    # Bursts list is [start, end]. We use start time to calculate NIBI from prev end.
    if burst_cursor < len(bursts):
        burst_start = bursts[burst_cursor][0]
        
        if current_time_ms >= burst_start:
            # Calculate NIBI if it's not the very first burst
            if burst_cursor > 0:
                prev_start = bursts[burst_cursor-1][0]
                new_nibi_ms = burst_start - prev_start
                
                # Update Buffer
                nibi_buffer.pop(0)
                nibi_buffer.append(new_nibi_ms)
                
                # Calculate New Period T (Median)
                median_nibi_ms = np.median(nibi_buffer)
                if median_nibi_ms < 100: median_nibi_ms = 100 # Safety floor
                
                T_period = median_nibi_ms / 1000.0 # Convert to seconds
                
                # Update Oscillator Params
                w = 2 * np.pi / T_period
                k_scaling = w 
                
            burst_cursor += 1
            
    
    # --- B. Count spikes in window ---
    new_spikes = 0
    while spike_cursor < total_spikes and all_spikes_flat[spike_cursor] < (current_time_ms + dt_ms):
        new_spikes += 1
        spike_cursor += 1
    
    # C. Update IFR Kernels (using seconds for time constants)
    kernel_rise -= kernel_rise * (dt_sec / tau_rise)
    kernel_decay -= kernel_decay * (dt_sec / tau_decay)
    kernel_rise += new_spikes * kernel_C
    kernel_decay += new_spikes * kernel_C
    
    # Calculate FireRate (approx Hz)
    # Scaling factor might need tuning depending on total electrode count
    fireRate_Hz = (kernel_decay - kernel_rise) * gain
    
    # C. Update Oscillator (Difference Equation)
    # Note: k = w (scaling factor)
    k_scaling = w 
    
    # --- D. Update Oscillator ---
    denom = (1 + w * dt_sec + (dt_sec**2) * (w**2))
    num = (k_scaling * fireRate_Hz * (dt_sec**2) + 
           2 * x_pre - x_pre_pre + 
           w * dt_sec * x_pre)
    
    x_now = num / denom
    
    # Calculate Velocity (x_dot)
    osctr_value = (x_now - x_pre) / dt_sec 
    
    # Shift history
    x_pre_pre = x_pre
    x_pre = x_now
    
    # --- E. Calculate Energy ---
    # Energy proportional to Amplitude^2 + Velocity^2
    # Note: Scaling might be needed for plotting, but raw is fine for checking trend
    energy = (x_now**2) + (osctr_value**2)
    
    # --- F. Store Data ---
    # Downsample slightly for plotting speed (e.g., every 1ms) if needed
    if int(current_time_ms * 10) % 10 == 0: # Every 1ms
        trace_time.append(current_time_ms / 1000.0)
        trace_ifr.append(fireRate_Hz)
        trace_osc_x.append(x_now)
        trace_osc_dot.append(osctr_value)
        trace_energy.append(energy)
        trace_T.append(T_period)
    
    current_time_ms += dt_ms


fig, axes = plt.subplots(4, 1, figsize=(12, 12), sharex=True)

# 1. Raster & Bursts
axes[0].set_title("Network Activity & Burst Detection")
axes[0].eventplot([all_spikes_flat/1000.0], color='black', linewidths=0.5, alpha=0.5)
# Overlay detected bursts ranges
for b in bursts:
    axes[0].axvspan(b[0]/1000.0, b[1]/1000.0, color='red', alpha=0.2)
axes[0].set_ylabel("Spikes")

# 2. IFR & Adapted Period
ax2 = axes[1]
ax2.plot(trace_time, trace_ifr, color='orange', label="IFR (Hz)")
ax2.set_ylabel("Firing Rate (Hz)")
ax2.legend(loc="upper left")
ax2.grid(True, alpha=0.3)

# Overlay T on secondary axis
ax2b = ax2.twinx()
ax2b.plot(trace_time, trace_T, color='gray', linestyle='--', linewidth=1.5, label="Adapted T (s)")
ax2b.set_ylabel("Period T (s)", color='gray')
ax2b.set_ylim([0, 2])
ax2b.legend(loc="upper right")

# 3. Oscillator State
axes[2].plot(trace_time, trace_osc_x, color='blue', linewidth=2, label="Position (x)")
axes[2].plot(trace_time, trace_osc_dot, color='green', linestyle='--', alpha=0.5, label="Velocity (x_dot)")
axes[2].axhline(0, color='black', linewidth=0.5)
axes[2].set_ylabel("Oscillator State")
axes[2].legend(loc="upper right")
axes[2].grid(True, alpha=0.3)

# 4. Oscillator Energy
axes[3].plot(trace_time, trace_energy, color='purple', linewidth=2, label="Energy (x^2 + dot_x^2)")
axes[3].set_ylabel("Energy")
axes[3].set_xlabel("Time (s)")
axes[3].legend(loc="upper right")
axes[3].grid(True, alpha=0.3)

plt.tight_layout()
plt.show()


#%% Plot Electrode

# all_electrode_spikes = np.empty(nElectrodes, dtype=object)
# all_electrode_spikes[...] = [[] for _ in range(all_electrode_spikes.shape[0])]

electrodes = env.grid.electrodes

# for e, electrode in enumerate(electrodes):
# #     # electrode._set_border_radius(10)
# #     # electrode.get_nearby_cells(cells)
# #     # electrode.calculate_field_potentials(cells, 0, 1000, snr=10)
# #     # electrode.update_threshold(threshold_sigma)
# #     # electrode.calculate_field_potentials(cells, 0, int(duration_sec*1000 / h.dt), snr=10)
# #     # electrode.calculate_field_potentials(cells, 46500, 46800, snr=10)

#     # electrode.threshold = 4e-6
    
#     electrode.calculate_field_potentials(env.cells, 0, int(h.t/h.dt))
#     spike_indices = electrode.detect_spikes(min_spike_interval, [])
#     electrode.detect_bursts(20, 3)
    
#     for spike in spike_indices:
#         all_electrode_spikes[e].append(int(spike*h.dt))
    
electrode = electrodes[8]

# # electrode.detect_bursts(250)
# # electrode.plot_welch()
electrode.plot_spikes_and_bursts(filtered=False)
# electrode.plot_spectrogram()
# electrode.plot_wavelet_transform()
# electrode.plot_wavelet_power_spectrum()

#%%

import json

filename = "seed=5_seedNoise=42_elecSpikes"

# Function to recursively convert NumPy arrays to Python lists
def convert_nested_arrays_to_lists(nested):
    if isinstance(nested, np.ndarray):  # Base case: convert array to list
        return nested.tolist()
    elif isinstance(nested, list):  # Recursive case: process each element
        return [convert_nested_arrays_to_lists(item) for item in nested]
    else:  # If it's neither a list nor an array, leave it as is
        return nested
    
class NumpyEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, (np.integer, np.int64)):  # Handle NumPy integers
            return int(obj)
        elif isinstance(obj, (np.floating, np.float64)):  # Handle NumPy floats
            return float(obj)
        elif isinstance(obj, np.ndarray):  # Handle NumPy arrays
            return obj.tolist()
        return super().default(obj)


all_electrode_spikes = env.all_electrode_spikes
all_electrode_spikes = convert_nested_arrays_to_lists(all_electrode_spikes)

# # Convert npz data to a dictionary and save as JSON
# data_dict = {key: npz_file[key].tolist() for key in npz_file.keys()}

data_dict = {"allElectrodeSpikes": all_electrode_spikes}

json_data = json.dumps(data_dict, cls=NumpyEncoder)

with open(filename + ".json", "w") as json_file:
    json.dump(json_data, json_file)

print("Saved data to data.json")
