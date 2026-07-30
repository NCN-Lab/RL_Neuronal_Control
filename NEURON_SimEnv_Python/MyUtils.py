# -*- coding: utf-8 -*-
"""
MyUtils.py — A collection of utility functions for analyzing
and processing simulation data in a NEURON-based microelectrode
array environment.

@author: ncn-neuron
"""

import numpy as np
from numpy import unravel_index
import matplotlib.pyplot as plt
import matplotlib as mpl
from matplotlib.patches import Rectangle
import seaborn as sns
from collections import Counter

import time
import datetime as dt

from scipy.signal import welch, find_peaks
from scipy.signal import convolve
from scipy.stats import shapiro
from scipy.stats import probplot, lognorm



def calcProcessTime(start_time, cur_iter, max_iter):

    t_elapsed = time.time() - start_time
    t_estimated = (t_elapsed/cur_iter)*(max_iter)

    finish_time = start_time + t_estimated
    finish_time = dt.datetime.fromtimestamp(finish_time).strftime("%H:%M:%S")

    left_time = t_estimated-t_elapsed  # in seconds

    return (int(t_elapsed), int(left_time), finish_time)


def find_coincident_spikes(all_electrode_spikes, precision_ms=1.0):
    """
    Finds time points where 2 or more electrodes spike at the same time.
    
    Parameters:
    - all_electrode_spikes: list of np.arrays, each containing spike times for an electrode
    - precision_ms: time resolution to consider two spikes simultaneous (in ms)

    Returns:
    - coincident_times: list of time points (rounded) where 2 or more electrodes fired
    """
    # Flatten all spike times with rounding for resolution tolerance
    all_times = []
    for spikes in all_electrode_spikes:
        rounded_times = np.round(np.array(spikes) / precision_ms) * precision_ms
        all_times.extend(rounded_times)

    # Count how many times each time appears
    counts = Counter(all_times)

    # Keep times that appear in 2 or more electrodes
    coincident_times = [t for t, c in counts.items() if c >= 2]

    return np.array(sorted(coincident_times))

def detect_bursts(spikes, max_burst_isi, min_num_spikes=3):  
    
    bursts = []
    num_random_spikes = 0
            
    # Detect bursts (clusters of spikes within the minimum interval)
    if len(spikes) > 0:
        current_burst = [spikes[0]]
        
        for i in range(1, len(spikes)):
            if spikes[i] - spikes[i - 1] <= max_burst_isi:
                current_burst.append(spikes[i])
            else:
                if len(current_burst) >= min_num_spikes:
                    bursts.append((current_burst[0], current_burst[-1]))
                else:
                    num_random_spikes += len(current_burst)
                current_burst = [spikes[i]]
        
        # Add the last burst
        if len(current_burst) >= min_num_spikes:
            bursts.append((current_burst[0], current_burst[-1]))
        else:
            num_random_spikes += len(current_burst)
            
    return bursts, num_random_spikes

def detect_network_bursts_v1(spikes, nElectrodes, min_active_electrodes, min_spikes_per_electrode, max_isi, min_ibi):
    # Sort spikes by time
    spikes = sorted(spikes, key=lambda x: x[1])  # Assuming each spike is a tuple (electrode, spike_time)
    
    burst_start_time = None
    burst_electrodes = []
    burst_electrodes_unique = set()
    burst_intervals = []
    burst_active_electrodes = []
    
    last_time = -np.inf
    last_spike_in_burst = False
    last_electrode = None
    for electrode, spike_time in spikes:
        isi = spike_time - last_time
        
        # Check if this spike is part of an ongoing burst
        if isi < max_isi:
            
            # Record burst start time
            if burst_start_time is None:
                burst_start_time = last_time
                burst_electrodes_unique.add(last_electrode)
                burst_electrodes.append(last_electrode)
                
            burst_electrodes_unique.add(electrode)
            burst_electrodes.append(electrode)
            last_spike_in_burst = True
                
        else:
            # If the burst meets the criteria, record the burst interval
            if (
                sum([burst_electrodes.count(x) >= min_spikes_per_electrode for x in burst_electrodes_unique]) >= min_active_electrodes
            ):  
                if (
                    len(burst_intervals) > 0  
                    and burst_start_time - burst_intervals[-1][1] < min_ibi
                ):
                    burst_active_electrodes[-1] = list(set(burst_active_electrodes[-1] + list(burst_electrodes_unique)))
                    burst_intervals[-1] = ((burst_intervals[-1][0], last_time))
                else:
                    burst_active_electrodes.append(list(burst_electrodes_unique))
                    burst_intervals.append((burst_start_time, last_time))
            
           
            burst_start_time = None
            burst_electrodes = []
            burst_electrodes_unique = set()
            last_spike_in_burst = False
            
        last_time = spike_time
        last_electrode = electrode
    
    # print(burst_start_time)
    # print(len(burst_electrodes))
    
    # Add last burst
    if last_spike_in_burst:
        if (
            sum([burst_electrodes.count(x) >= min_spikes_per_electrode for x in burst_electrodes_unique]) >= min_active_electrodes
        ):  
            if (
                len(burst_intervals) > 0  
                and burst_start_time - burst_intervals[-1][1] < min_ibi
            ):
                burst_active_electrodes[-1] = list(set(burst_active_electrodes[-1] + list(burst_electrodes_unique)))
                burst_intervals[-1] = ((burst_intervals[-1][0], last_time))
            else:
                burst_active_electrodes.append(list(burst_electrodes_unique))
                burst_intervals.append((burst_start_time, last_time))
    
    return burst_intervals, burst_active_electrodes

def detect_network_bursts_v2(burst_masks, min_bursting_electrodes, min_ibi, resolution_ms):
    
    # Get total number of electrodes bursting at every given moment
    electrodes_in_burst = np.sum(burst_masks, axis=0)
    
    # Get periods where the total number of bursting electrodes is equal or higher than minimum
    network_burst_mask = electrodes_in_burst >= min_bursting_electrodes
    
    sequences = []
    start_index = None

    for i, value in enumerate(network_burst_mask):
        if value == 1:
            if start_index is None:
                start_index = i
        elif value == 0 and start_index is not None:
            if len(sequences) > 0 and resolution_ms*(i-1) - sequences[-1][1]*resolution_ms < min_ibi:
                sequences[-1][1] = resolution_ms*(i-1)
            else:
                sequences.append([resolution_ms*start_index, resolution_ms*(i - 1)])
            start_index = None

    # Check if there's a sequence of ones ending at the end of the array
    if start_index is not None:
        if len(sequences) > 0 and resolution_ms*(len(network_burst_mask) - 1) - sequences[-1][1]*resolution_ms < min_ibi:
            sequences[-1][1] = resolution_ms*(len(network_burst_mask) - 1)
        else:
            sequences.append([resolution_ms*start_index, resolution_ms*(len(network_burst_mask) - 1)])
    
    return sequences

def calculate_ibi(burst_intervals):
    ibi = burst_intervals[1:, 0] - burst_intervals[:-1, 1]
    return ibi

def calculate_burst_duration(burst_intervals):
    burst_durations = burst_intervals[:, 1] - burst_intervals[:, 0]
    return burst_durations


def calculate_synchrony(rel_spike_times, dt, total_time_s, kernel, min_FR_active, plot_binaries=False):
    # Get active electrodes
    total_spikes = np.array([len(spikes) for spikes in rel_spike_times])
    mask_active = total_spikes / total_time_s > min_FR_active
    active_spike_times = [rel_spike_times[i] for i in range(len(mask_active)) if mask_active[i]]

    # Generate binary signal for each active electrode
    num_active_electrodes = len(active_spike_times)
    binary_signal = np.zeros((num_active_electrodes, int(total_time_s * 1000) + 1))

    for e in range(num_active_electrodes):
        time_vector = np.arange(0, total_time_s, dt)

        # Create a binary time series from spike times
        spike_train = np.zeros_like(time_vector)
        spike_indices = np.round(np.array(active_spike_times[e])).astype(int)
        spike_train[spike_indices-1] = 1

        # Convolve the spike train with the square wave kernel
        convolved_signal = np.convolve(spike_train, kernel, mode='same')

        # Binarize the convolved signal
        binary_signal[e, :] = convolved_signal > 0

    if plot_binaries:
        plt.figure()
        for e in range(num_active_electrodes):
            plt.plot(np.arange(0, len(binary_signal[e, :])) / 1000, e + binary_signal[e, :] / 2)
        plt.xlabel('Time (s)')
        plt.ylabel('Electrode')
        plt.show()

    # Calculate the synchrony measure χ
    network_activity = np.mean(binary_signal, axis=0)
    var_network_activity = np.var(network_activity)
    var_electrode_activity = np.var(binary_signal, axis=1)
    chi = var_network_activity / np.mean(var_electrode_activity)

    return chi

def estimate_ifr(list_of_spike_arrays_ms, fs=1000, tau_rise=0.005, tau_decay=0.020,
                 start_time_s=0.0, end_time_s=None):
    """
    Estimates the Network Instantaneous Firing Rate (IFR) using a normalized 
    double-exponential kernel and efficient convolution.

    Args:
        list_of_spike_arrays (list of numpy arrays): A list where each element 
                                                    is an array of spike times (in seconds) 
                                                    for a single electrode.
        fs (int): Sampling frequency for the IFR time-series (Hz). 
        tau_rise (float): Rise time constant of the double-exponential (seconds).
        tau_decay (float): Decay time constant of the double-exponential (seconds).
        start_time_s (float): Start time of the desired window (seconds).
        end_time_s (float): End time of the desired window (seconds).

    Returns:
        tuple: (ifr_time, ifr_rate) - The time vector and the estimated IFR (Hz).
    """
    
    if not list_of_spike_arrays_ms:
        print("Error: Input list of spike arrays is empty.")
        return np.array([0]), np.array([0])
        
    all_electrode_spikes_ms = np.concatenate(list_of_spike_arrays_ms)
    all_electrode_spikes = all_electrode_spikes_ms / 1000.0
    
    if len(all_electrode_spikes) == 0:
        print("Error: Pooled spike list is empty.")
        return np.array([start_time_s]), np.array([0])
    
    # Sort the combined spikes
    all_electrode_spikes = np.sort(all_electrode_spikes)

    if end_time_s is not None:
        all_electrode_spikes = all_electrode_spikes[all_electrode_spikes < end_time_s]
        
    all_electrode_spikes = all_electrode_spikes[all_electrode_spikes >= start_time_s]
    all_electrode_spikes -= start_time_s
    
    if len(all_electrode_spikes) == 0:
        print(f"Warning: No spikes found between {start_time_s}s and {end_time_s}s.")
        return np.array([start_time_s]), np.array([0])

    max_time = all_electrode_spikes[-1]
    
    if end_time_s is not None:
        window_duration = end_time_s - start_time_s
        n_bins = int(np.ceil(window_duration * fs))
    else:
        n_bins = int(np.ceil(max_time * fs))
        
    binned_spikes, _ = np.histogram(all_electrode_spikes, bins=n_bins, range=(0, window_duration if end_time_s is not None else max_time))
    kernel_duration = int(5 * tau_decay * fs)
    if kernel_duration < 1: kernel_duration = 1
    kernel_time = np.arange(kernel_duration) / fs
    C = 1.0 / (tau_decay - tau_rise)
    kernel = C * (np.exp(-kernel_time / tau_decay) - np.exp(-kernel_time / tau_rise))
    kernel_template = kernel * (1.0 / fs)

    ifr_rate = convolve(binned_spikes, kernel_template, mode='full')
    ifr_rate = ifr_rate[:n_bins]
    time_vector = np.arange(n_bins) / fs
    time_vector += start_time_s 
    
    return time_vector, ifr_rate

def estimate_individual_ifr(list_of_spike_arrays_ms, fs=1000, tau_rise=0.005, tau_decay=0.020, 
                            start_time_s=0.0, end_time_s=None): 
    """ 
    Estimates the Instantaneous Firing Rate (IFR) for *each* electrode individually 
    using a normalized double-exponential kernel.

    Args: 
        list_of_spike_arrays_ms (list of numpy arrays): A list where each element 
                                                        is an array of spike times (in milliseconds) 
                                                        for a single electrode. 
        fs (int): Sampling frequency for the IFR time-series (Hz).  
        tau_rise (float): Rise time constant (seconds). 
        tau_decay (float): Decay time constant (seconds). 
        start_time_s (float): Start time of the desired window (seconds). 
        end_time_s (float): End time of the desired window (seconds). 

    Returns: 
        tuple: (ifr_time, ifr_matrix) 
            - ifr_time: 1D array of time points.
            - ifr_matrix: 2D array of shape (n_electrodes, n_time_points) containing IFR in Hz.
    """ 
     
    if not list_of_spike_arrays_ms: 
        print("Error: Input list of spike arrays is empty.") 
        return np.array([0]), np.array([[0]]) 
    
    n_electrodes = len(list_of_spike_arrays_ms)

    if end_time_s is None:
        max_times = [np.max(arr)/1000.0 if len(arr) > 0 else 0 for arr in list_of_spike_arrays_ms]
        max_time_global = max(max_times) if max_times else 0
        
        window_duration = max_time_global - start_time_s + 0.1 
    else:
        window_duration = end_time_s - start_time_s

    # Ensure valid duration
    if window_duration <= 0:
        print("Error: Invalid time window duration.")
        return np.array([start_time_s]), np.zeros((n_electrodes, 1))

    n_bins = int(np.ceil(window_duration * fs))
    
    kernel_duration = int(5 * tau_decay * fs)
    if kernel_duration < 1: kernel_duration = 1
    kernel_time = np.arange(kernel_duration) / fs
    C = 1.0 / (tau_decay - tau_rise)
    kernel = C * (np.exp(-kernel_time / tau_decay) - np.exp(-kernel_time / tau_rise))
    # Normalize so the convolution sum represents Hz (events per second)
    kernel_template = kernel * (1.0 / fs)

    ifr_matrix = np.zeros((n_electrodes, n_bins))
    
    for i, spike_array_ms in enumerate(list_of_spike_arrays_ms):
        
        spikes_s = spike_array_ms / 1000.0
        
        if end_time_s is not None:
             spikes_s = spikes_s[spikes_s < end_time_s]
        
        spikes_s = spikes_s[spikes_s >= start_time_s]
        spikes_s -= start_time_s
        
        if len(spikes_s) == 0:
            continue # Matrix is already zeros, so just skip to next electrode
            
        binned_spikes, _ = np.histogram(spikes_s, bins=n_bins, range=(0, window_duration))
        rate_trace = convolve(binned_spikes, kernel_template, mode='full')
        rate_trace = rate_trace[:n_bins]
        ifr_matrix[i, :] = rate_trace * fs 

    time_vector = np.arange(n_bins) / fs
    time_vector += start_time_s 
     
    return time_vector, ifr_matrix

def calculate_dominant_frequency(rel_spike_times, bin_size, total_time_s, frequency_window_Hz, plot_result):
    # Create temporal resolution edges
    bin_edges = np.arange(0, total_time_s + bin_size, bin_size)
    bin_counts = np.zeros(len(bin_edges) - 1)

    # Iterate over each element in the list
    for spikes in rel_spike_times:
        bin_counts += np.histogram(spikes, bins=bin_edges)[0]

    IFR = bin_counts / bin_size / len(rel_spike_times)

    # Compute the Power Spectral Density using Welch's method
    N = 2 * (len(IFR) // 4)
    f, pxx = welch(IFR, nperseg=N, noverlap=N//2, fs=1/bin_size)

    # Normalize the power spectral density
    normalized_pxx = pxx / np.sum(pxx)
    peaks, properties = find_peaks(normalized_pxx)
    peak_frequencies = f[peaks]

    if len(peaks) > 0:
        dominant_magnitude = normalized_pxx[peaks].max()
        index_max = np.argmax(normalized_pxx[peaks])
        dominant_frequency = peak_frequencies[index_max]
    else:
        dominant_frequency = np.nan

    # Calculate relative power
    if np.isnan(dominant_frequency):
        rel_power = np.nan
    else:
        low_freq = max(0, dominant_frequency - frequency_window_Hz)
        high_freq = min(dominant_frequency + frequency_window_Hz, f[-1])
        rel_power = bandpower(normalized_pxx, f, [low_freq, high_freq]) / bandpower(normalized_pxx, f)

    # Check if the frequency is truly dominant
    if rel_power < 5 * (high_freq - low_freq) / f[-1]:
        dominant_frequency = np.nan

    # Plot results if required
    if plot_result:
        plt.figure(figsize=(10, 6))
        
        plt.subplot(2, 1, 1)
        plt.plot(bin_edges[:-1], IFR)
        plt.xlabel('Time (s)')
        plt.ylabel('Instantaneous Firing Rate (Hz)')
        
        plt.subplot(2, 1, 2)
        plt.plot(f, normalized_pxx)
        plt.xlabel('Frequency (Hz)')
        plt.ylabel('Power Spectral Density')
        plt.title('Power Spectral Density Estimate')
        if not np.isnan(dominant_frequency):
            plt.plot(dominant_frequency, dominant_magnitude, 'ro')
        plt.tight_layout()
        plt.show()

    return dominant_frequency, rel_power

def bandpower(psd, freqs, band=None):
    if band is not None:
        freq_mask = np.logical_and(freqs >= band[0], freqs <= band[1])
        return np.sum(psd[freq_mask])
    else:
        return np.sum(psd)


def plot_ibi_distribution(ibi, bins=20, log_scale=True):
    
    if log_scale:
        # Generate logarithmically spaced bin edges
        bins = np.logspace(np.log10(min(ibi)), np.log10(max(ibi)), num=bins)
        stat, p_value = shapiro(np.log(ibi))
        print(f'Statistic: {stat}, p-value: {p_value}')
        
        if p_value > 0.05:
            print('The IBI data appears to be log-normally distributed.')
        else:
            print('The IBI data does not appear to be log-normally distributed.')
    
    fig = plt.figure(figsize=(12, 6))
    plt.subplot(1, 2, 1)
    # plt.hist(ibi, bins=bins, edgecolor='black')
    sns.histplot(ibi, bins=bins, kde=True, edgecolor='black')
    plt.title('Inter-Burst Interval (IBI) Distribution')
    plt.xlabel('Inter-Burst Interval (s)')
    plt.ylabel('Burst Probability')
    if log_scale: plt.xscale('log')
    
    # Q-Q plot
    s = 0.95 # shape parameter
    plt.subplot(1, 2, 2)
    _, (slope, intercept, r) = probplot(ibi, dist=lognorm, sparams=(s,), plot=plt)
    plt.title(f'Q-Q Plot for Log-Normal Distribution\nSlope: {slope:.2f}, Intercept: {intercept:.2f}, R-squared: {r**2:.2f}')
    plt.xlabel('Theoretical Quantiles')
    plt.ylabel('Sample Quantiles')
    
    plt.tight_layout()

    return fig
    
    
def plot_burst_duration_distribution(burst_durations, bins=20, log_scale=True):
    
    if log_scale:
        # Generate logarithmically spaced bin edges
        bins = np.logspace(np.log10(min(burst_durations)), np.log10(max(burst_durations)), num=bins)
        stat, p_value = shapiro(np.log(burst_durations))
        print(f'Statistic: {stat}, p-value: {p_value}')
        
        if p_value > 0.05:
            print('The BD data appears to be log-normally distributed.')
        else:
            print('The BD data does not appear to be log-normally distributed.')
    
    fig = plt.figure(figsize=(12, 6))        
    plt.subplot(1, 2, 1)
    sns.histplot(burst_durations, bins=bins, kde=False, edgecolor='black')
    plt.title('Burst Duration Distribution')
    plt.xlabel('Burst duration (s)')
    plt.ylabel('Burst Probability')
    if log_scale: plt.xscale('log')
    
    # Q-Q plot
    s = 0.95 # shape parameter
    plt.subplot(1, 2, 2)
    _, (slope, intercept, r) = probplot(burst_durations, dist=lognorm, sparams=(s,), plot=plt)
    plt.title(f'Q-Q Plot for Log-Normal Distribution\nSlope: {slope:.2f}, Intercept: {intercept:.2f}, R-squared: {r**2:.2f}')
    plt.xlabel('Theoretical Quantiles')
    plt.ylabel('Sample Quantiles')
    
    plt.tight_layout()

    return fig

def plot_cell_raster(excCells, inhCells, all_electrode_spikes, all_cell_spikes, time_window):
    
    plt.rc('font', size=18, weight='bold')
    
    nCells              = len(excCells) + len(inhCells)
    cells_colors        = np.zeros([nCells, 4])
    viridis             = mpl.colormaps['jet']
    cells_colors[1:,:]  = viridis(0)
    for c in range(nCells):
        if c in inhCells:
            cells_colors[c,:] = [1, 0, 0, 1] # red
        else:
            cells_colors[c,:] = [0, 0, 1, 1] # blue
            
            
    fig, ax = plt.subplots(figsize=(15,2.5))
    count = 0
    total_spikes = 0
    for c in list(excCells) + list(inhCells):
        spike_times = [t for t in all_cell_spikes[c] if t >= time_window[0]*1000 and t < time_window[1]*1000]
        if len(spike_times) > 0:
            total_spikes = total_spikes + len(spike_times)
            ax.vlines([i / 1000 for i in spike_times], count-0.5, count+0.5, color = cells_colors[c, :])
        count += 1
    plt.xlabel('Time (s)', weight='bold')
    plt.ylabel('Cell', weight='bold')
    plt.xlim(time_window)
    
    ax.spines['top'].set_visible(False)
    ax.spines['right'].set_visible(False)
    # ax.spines['bottom'].set_visible(False)
    # ax.spines['left'].set_visible(False)
    
    ax.spines['bottom'].set_linewidth(2)
    ax.spines['left'].set_linewidth(2)
    
    plt.tight_layout()
    
    return fig

def plot_electrode_raster(nElectrodes, all_electrode_spikes, time_window, 
                          stim_times=[], bursts=[], show_bursts=True,
                          color_spikes=None, color_stims=None, ax=None,
                          linewidth=0.5):
    
    plt.rc('font', size=18, weight='bold')
    
    if ax is None: 
        fig, ax = plt.subplots(figsize=(15,2.5))
    count = 1
    total_spikes = 0
    electrode_spike_counter = np.zeros(nElectrodes)
    nActiveElectrodes = 0
    indxActiveElectrodes = []
    
    # Handle colors if provided
    if color_stims == None:
        c_stims = ['#ff7f00'] * len(stim_times)  # Default color is black
    elif isinstance(color_stims, str):
        c_stims = [color_stims] * len(stim_times)  # Default color is black
    else:
        c_stims = color_stims

    for e in range(nElectrodes):
        
        ax.hlines(count-0.5, time_window[0], time_window[1], linewidth=0.5, color='black')
        
        spike_times = [t for t in all_electrode_spikes[e] if t >= time_window[0]*1000 and t < time_window[1]*1000]
        electrode_spike_counter[e] = len(spike_times)
        
        if color_spikes == None:
            c_spikes = ['black'] * len(spike_times)  # Default color is black
        elif isinstance(color_spikes, str):
            c_spikes = [color_spikes] * len(spike_times)  # Default color is black
        else:
            c_spikes = [color_spikes[e][i] for i, t in enumerate(all_electrode_spikes[e]) if t >= time_window[0]*1000 and t < time_window[1]*1000]


        if len(spike_times) > 0:
            if len(spike_times) > (time_window[1] - time_window[0]) / 10:
                nActiveElectrodes += 1
                indxActiveElectrodes.append(e)
            
            # Plot each spike with its corresponding color
            for t, color in zip(spike_times, c_spikes):
                ax.vlines(t / 1000, count - 0.5, count + 0.5, color=color, linewidth=linewidth)
            
            total_spikes += len(spike_times)
        count += 1
    
    ax.set_xlabel('Time (s)', weight='bold')
    ax.set_ylabel('Electrode', weight='bold')
    ax.set_xlim(time_window)
    ax.set_ylim([0.5, nElectrodes+0.5])
    ax.set_yticks([i for i in range(1,nElectrodes+1,nElectrodes//5 + 1)])
    
    if len(stim_times) > 0:
        ax.scatter(stim_times, [nElectrodes + 1] * len(stim_times), color=c_stims, marker='v', label='Stimulation', zorder=10)
        ax.set_ylim([0.5, nElectrodes+1.5])
        
    if show_bursts:
        for burst in bursts:
            ax.axvspan(burst[0]/1000, burst[-1]/1000, alpha=0.4, color='#3690c0')
            
    ax.spines['top'].set_visible(False)
    ax.spines['right'].set_visible(False)
    # ax.spines['bottom'].set_visible(False)
    # ax.spines['left'].set_visible(False)
    
    ax.spines['bottom'].set_linewidth(2)
    ax.spines['left'].set_linewidth(2)
    
    plt.tight_layout()
    
    if 'fig' in locals():
        return ax, fig # Return both if created internally
    else:
        return ax # Only return the axes object


def plot_electrode_raster_scheme(nElectrodes, all_electrode_spikes, time_window, 
                          stim_times=[], bursts=[], show_bursts=True,
                          color_spikes=None, color_stims=None):
    
    plt.rc('font', size=18, weight='bold')
    
    fig, ax = plt.subplots(figsize=(15,2.5))
    count = 1
    total_spikes = 0
    electrode_spike_counter = np.zeros(nElectrodes)
    nActiveElectrodes = 0
    indxActiveElectrodes = []
    
    n_alpha = 0.3
    elec_colors = ['#003c30', '#01665e', '#35978f', '#80cdc1',
               '#f6e8c3', '#dfc27d', '#bf812d', '#8c510a', '#543005']
    
    # Handle colors if provided
    if color_stims == None:
        c_stims = ['#ff7f00'] * len(stim_times)  # Default color is black
    elif isinstance(color_stims, str):
        c_stims = [color_stims] * len(stim_times)  # Default color is black
    else:
        c_stims = color_stims

    for e in range(nElectrodes):
        
        ax.hlines(count-0.5, time_window[0], time_window[1], linewidth=0.5, color='black')
        
        spike_times = [t for t in all_electrode_spikes[e] if t >= time_window[0]*1000 and t < time_window[1]*1000]
        electrode_spike_counter[e] = len(spike_times)
        
        # Handle colors if provided
        if color_spikes == None:
            c_spikes = ['black'] * len(spike_times)  # Default color is black
        elif isinstance(color_spikes, str):
            c_spikes = [color_spikes] * len(spike_times)  # Default color is black
        else:
            c_spikes = [color_spikes[e][i] for i, t in enumerate(all_electrode_spikes[e]) if t >= time_window[0]*1000 and t < time_window[1]*1000]


        if len(spike_times) > 0:
            if len(spike_times) > (time_window[1] - time_window[0]) / 10:
                nActiveElectrodes += 1
                indxActiveElectrodes.append(e)

            for t in spike_times:
                color = (0, 0, 0, 1.0)  # default black
                t0 = None
                for burst in bursts:
                    if burst[0] <= t <= burst[1]:
                        t0 = burst[0]
                        break

                if t0 is not None:
                    delta = (t - t0)
                    alpha = n_alpha + (1 - n_alpha) * np.exp(-np.log(2) / 20 * delta)
                    alpha = np.clip(alpha, n_alpha, 1.0)
                    base_color = mpl.colors.to_rgba(elec_colors[e])
                    color = tuple(np.append(base_color[:3], alpha))

                ax.vlines(t / 1000, count - 0.5, count + 0.5, color=color)

            total_spikes += len(spike_times)
        count += 1
    
    plt.xlabel('Time (s)', weight='bold')
    plt.ylabel('Electrode', weight='bold')
    plt.xlim(time_window)
    plt.ylim([0.5, nElectrodes+0.5])
    ax.set_yticks([i for i in range(1,nElectrodes+1,nElectrodes//5 + 1)])
    
    if len(stim_times) > 0:
        ax.scatter(stim_times, [nElectrodes + 1] * len(stim_times), color=c_stims, marker='v', label='Stimulation', zorder=10)
        plt.ylim([0.5, nElectrodes+1.5])
        
    if show_bursts:
        for burst in bursts:
            ax.axvspan(burst[0]/1000, burst[-1]/1000, alpha=0.4, color='#3690c0')
            
    ax.spines['top'].set_visible(False)
    ax.spines['right'].set_visible(False)
    # ax.spines['bottom'].set_visible(False)
    # ax.spines['left'].set_visible(False)
    
    ax.spines['bottom'].set_linewidth(2)
    ax.spines['left'].set_linewidth(2)
    
    plt.tight_layout()
    
    return fig

def detect_convergence(smoothed_signal, threshold=1.8, window=100, std_tol=0.005):
    for i in range(window, len(smoothed_signal)):
        window_data = smoothed_signal[i-window:i]
        # if np.all(window_data > threshold):
        if np.all(window_data > threshold) and np.std(window_data) < std_tol:
            return i  # conservative convergence point
    return None

def plot_electrode_grid_visits(nElectrodes, electrode_counter, task):
    
    grid_counter = []
    for i in range(int(np.sqrt(nElectrodes))):
        row_counter = []
        for j in range(int(np.sqrt(nElectrodes))):
            row_counter.append(electrode_counter[int(np.sqrt(nElectrodes)*j + i + 1)])
        grid_counter.append(row_counter)

    grid_counter = np.array(grid_counter)

    cmap = plt.cm.viridis
    grid_counter_colors  = cmap((grid_counter-np.min(grid_counter))/(np.unique(grid_counter)[-1]-np.min(grid_counter)))
    min_indx = unravel_index(grid_counter.argmin(), grid_counter.shape)
    no_stim_counter_color = cmap((electrode_counter[0]-np.min(grid_counter))/(np.unique(grid_counter)[-1]-np.min(grid_counter)))


    if task != "MFR":
        grid_counter_colors[min_indx] = 1,0,0,1
        grid_counter[min_indx] = 0
        
    grid_counter_colors[grid_counter == 0] = 0,0,0,0.7

    fig, ax = plt.subplots()
    ax.imshow(grid_counter_colors)
    # Loop over data dimensions and create text annotations.
    for i in range(int(np.sqrt(nElectrodes))):
        for j in range(int(np.sqrt(nElectrodes))):
            ax.text(j, i, int(grid_counter[i, j]),
                           ha="center", va="center", color="w", weight='bold')
    
    ax.set_title("Frequency of visit")
    fig.tight_layout()
    
    # Draw the square outside the grid
    square = Rectangle((-2.5, 2.5), 1, 1, fill=True, edgecolor=None, 
                       facecolor=no_stim_counter_color, linewidth=2)
    ax.add_patch(square)
    ax.text(-2, 3, int(electrode_counter[0]),
                   ha="center", va="center", color="w", weight='bold')
    
    # Highlight most visited square
    max_indx = unravel_index(grid_counter.argmax(), grid_counter.shape)
    highlight = Rectangle((max_indx[1]-0.5, max_indx[0]-0.5), 1, 1, linewidth=2, edgecolor='r', facecolor='none')
    ax.add_patch(highlight)
    
    # Set axis limits to include the square
    ax.set_xlim(-2.5, np.sqrt(nElectrodes)-0.5)
    ax.set_ylim(-0.5, np.sqrt(nElectrodes)-0.5)
    ax.set_frame_on(False)
    plt.xticks([])
    plt.yticks([])
    
    return fig
    
def plot_electrode_grid_mean_states(nElectrodes, electrode_counter, electrode_mean_state, task):
    
    grid_counter = []
    grid_mean_state = []
    for i in range(int(np.sqrt(nElectrodes))):
        row_counter = []
        row_state = []
        for j in range(int(np.sqrt(nElectrodes))):
            row_counter.append(electrode_counter[int(np.sqrt(nElectrodes)*j + i + 1)])
            row_state.append(electrode_mean_state[int(np.sqrt(nElectrodes)*j + i + 1)])
        grid_counter.append(row_counter)
        grid_mean_state.append(row_state)
    
    grid_counter = np.array(grid_counter)
    grid_mean_state = np.array(grid_mean_state)

    cmap = plt.cm.viridis
    min_indx = unravel_index(grid_counter.argmin(), grid_counter.shape)
    
    if np.max(grid_mean_state) != np.min(grid_mean_state):
        grid_mean_state_colors  = cmap((grid_mean_state-np.min(grid_mean_state))/(np.max(grid_mean_state)-np.min(grid_mean_state)))
        no_stim_mean_state_color = cmap((electrode_mean_state[0]-np.min(grid_mean_state))/(np.max(grid_mean_state)-np.min(grid_mean_state)))
    else:
        grid_mean_state_colors  = cmap((grid_mean_state-np.min(grid_mean_state))/1)
        no_stim_mean_state_color = cmap((electrode_mean_state[0]-np.min(grid_mean_state))/1)
    
    if task != "MFR":
        grid_counter[min_indx] = 0
        grid_mean_state_colors[min_indx] = 1,0,0,1
        grid_mean_state[min_indx] = np.nan

    grid_mean_state_colors[grid_counter == 0] = 0,0,0,0.7
    grid_mean_state[grid_counter == 0] = np.nan

    fig, ax = plt.subplots()
    ax.imshow(grid_mean_state_colors)
    # Loop over data dimensions and create text annotations.
    for i in range(int(np.sqrt(nElectrodes))):
        for j in range(int(np.sqrt(nElectrodes))):
            ax.text(j, i, round(grid_mean_state[i, j],2),
                           ha="center", va="center", color="w", weight='bold')

    ax.set_title("Mean Target Response")
    fig.tight_layout()
    # Draw the square outside the grid
    square = Rectangle((-2.5, 2.5), 1, 1, fill=True, edgecolor=None, 
                       facecolor=no_stim_mean_state_color, linewidth=2)
    ax.add_patch(square)
    ax.text(-2, 3, round(electrode_mean_state[0],2),
                   ha="center", va="center", color="w", weight='bold')
    
    # Highlight most visited square
    max_indx = unravel_index(grid_counter.argmax(), grid_counter.shape)
    highlight = Rectangle((max_indx[1]-0.5, max_indx[0]-0.5), 1, 1, linewidth=2, edgecolor='r', facecolor='none')
    ax.add_patch(highlight)

    # Set axis limits to include the square
    ax.set_xlim(-2.5, np.sqrt(nElectrodes)-0.5)
    ax.set_ylim(-0.5, np.sqrt(nElectrodes)-0.5)
    ax.set_frame_on(False)
    plt.xticks([])
    plt.yticks([])
    
    return fig


def set_pub_style():
    """Sets standard matplotlib parameters for publication-quality figures."""
    plt.rcParams.update({
        'font.size': 14,
        'font.family': 'sans-serif',
        'font.sans-serif': ['Arial', 'DejaVu Sans', 'Liberation Sans'],
        'font.weight': 'bold',
        'axes.labelweight': 'bold',
        'axes.titleweight': 'bold',
        'axes.linewidth': 2,            # Thicker spines
        'xtick.major.width': 2,
        'xtick.major.size': 8,
        'xtick.minor.width': 1.7,
        'xtick.minor.size': 6,
        'ytick.major.width': 2,
        'ytick.major.size': 8,
        'ytick.minor.width': 1.7,
        'ytick.minor.size': 6,
        'xtick.direction': 'out',
        'ytick.direction': 'out',
        'figure.figsize': (6, 5),       # Standardized size
        'svg.fonttype': 'none',         # Text as text, not paths
        'legend.frameon': False,        # Clean legends
        'legend.fontsize': 12
    })