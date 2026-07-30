# -*- coding: utf-8 -*-
"""
MyElectrode.py — A class for defining virtual electrodes
and stimulation mechanisms for a NEURON simulation environment.

@author: ncn-neuron
"""

from neuron import h #, gui
import numpy as np 
import matplotlib.pyplot as plt

from scipy import signal
from scipy.signal import welch

class DynamicPulseInjector:
    def __init__(self, target_synapse, weight=0.1, delay=0):
        """
        Parameters:
        - target_synapse: NEURON synaptic mechanism (e.g., ExpSyn)
        - weight: synaptic weight
        - delay: optional delay added to each spike (ms)
        """
        self.nc = h.NetCon(None, target_synapse)
        self.nc.weight[0] = weight
        self.delay = delay
        self.injection_times = []

    def inject_at(self, t_ms):
        """
        Schedules a spike injection at absolute simulation time t_ms.
        """
        t_inj = t_ms + self.delay
        h.cvode.event(t_inj, lambda: self.nc.event(t_inj))
        self.injection_times.append(t_inj)

    def inject_now(self):
        """
        Injects a spike immediately at current simulation time.
        """
        t_now = float(h.t) + self.delay
        h.cvode.event(t_now, lambda: self.nc.event(t_now))
        self.injection_times.append(t_now)
        
    def inject_train_at(self, start_ms, n_pulses=5, frequency=100):
        """
        Injects a train of n_pulses at `frequency` Hz starting from start_ms.
        
        Parameters:
        - start_ms: start time of the train (absolute time, in ms)
        - n_pulses: number of spikes to inject
        - frequency: frequency in Hz (default: 100 Hz = 10 ms interval)
        """
        isi = 1000.0 / frequency  # interspike interval in ms
        for i in range(n_pulses):
            t_pulse = start_ms + i * isi
            self.inject_at(t_pulse)

class MyElectrode:
    
    def __init__(self, gid, x, y, z=0, radius=15, border_radius=100, impedance=80000, sampling_frequency=10000):
        self._gid = gid
        self.x = 0.0
        self.y = 0.0
        self.z = 0.0
        self.radius = 0.0
        self.border_radius = 0.0
        self.impedance = 0.0
        self.sampling_frequency = 0.0
        self.distances = []
        self.nearby_cells_gids = []
        self.field_potentials = []
        self.filtered_field_potentials = []
        self.threshold = 5 * np.sqrt((4 * 1.380649e-23 * (37 + 273.15) * impedance * sampling_frequency/2))
        self.spike_indices = []
        self.spike_sorted_id = []
        self.bursts = []
        self.dynamic_injectors = {}
        
        self._set_position(x, y, z)
        self._set_radius(radius)
        self._set_border_radius(border_radius)
        self._set_impedance(impedance)
        self._set_sampling_frequency(sampling_frequency)
        
    def _set_position(self, x, y, z):
        self.x, self.y, self.z = x, y, z
        
    def _get_position(self):
        return self.x, self.y, self.z
    
    def _set_radius(self, radius):
        self.radius = radius
        
    def _get_radius(self):
        return self.radius
    
    def _set_border_radius(self, border_radius):
        self.border_radius = border_radius
        
    def _get_border_radius(self):
        return self.border_radius
    
    def _set_impedance(self, impedance):
        self.impedance = impedance
        
    def _get_impedance(self):
        return self.impedance
    
    def _set_sampling_frequency(self, sampling_frequency):
        self.sampling_frequency = sampling_frequency
        
    def _get_sampling_frequency(self):
        return self.sampling_frequency
        
    def _get_threshold(self):
        return self.threshold
    
    def get_distances(self, cells):
        self.distances = []
        for cell in cells:
            euclidean_dist = np.sqrt((cell.x - self.x)**2 + (cell.y - self.y)**2)
            self.distances.append(euclidean_dist)
                
        return self.distances
    
    def get_nearby_cells(self, cells):
        if self.distances == []:
            self.get_distances(cells)
        
        self.nearby_cells_gids = [index for index, value in enumerate(self.distances) if value <= self.border_radius]
        
        for gid in self.nearby_cells_gids:
            if gid not in self.dynamic_injectors:
                self.dynamic_injectors[gid] = DynamicPulseInjector(cells[gid].stim_syn)

        return self.nearby_cells_gids
        
    def print_nearby_cells(self, cells, excCells, inhCells):
        x, y, z = self._get_position()
        dist = self.get_distances(cells)
        cells_within = [i for i, v  in enumerate(dist) if v <= self.radius]
        cells_effect = [i for i, v  in enumerate(dist) if v <= self.radius + self.border_radius] 
        exc_cells_within = len([c for c in cells_within if c in excCells])
        inh_cells_within = len([c for c in cells_within if c in inhCells])    
        exc_cells_effect = len([c for c in cells_effect if c in excCells])
        inh_cells_effect = len([c for c in cells_effect if c in inhCells])
        print('Electrode {} @({},{}) - {}({}) exc + {}({}) inh'.format(self._gid, self.x, self.y, 
                                                                       exc_cells_effect,
                                                                       exc_cells_within,
                                                                       inh_cells_effect,
                                                                       inh_cells_within))
        
        
#%% Stimulation Functions
# =============================================================================
        
    def generate_pulses(self, cells, start, end, frequency, pulse_duration, electrode_potential, number_pulses=[]):
        
        # Check if start time comes before end time
        if start > end:
            return []
        
        if frequency == 0:
            frequency = 1e-9
            
        period = 1000.0 / frequency
        pulse_on = np.arange(start, end, period)
        
        if number_pulses != [] and len(pulse_on) > number_pulses:
            pulse_on = pulse_on[:number_pulses]
        
        # Electrode potential should be in V
        tissue_potential = electrode_potential / 100 # Loss of potential in the metal-tissue interface (Joucla & Yvert, 2009)    
        current_injected = (electrode_potential - tissue_potential) / self.impedance # A
        
        sigma_ext = 0.3e-6  # Extracellular conductivity (S/μm)
        
        distances = self.get_distances(cells)        
        for c in self.get_nearby_cells(cells):            
            # These values are defined in MyCells
            if cells[c].cell_type == 'inh':
                cell_radius     = 7.5 # (μm)
            else:
                cell_radius     = 10 # (μm)
            
            
            distance_cell = np.sqrt(distances[c]**2 + cell_radius**2)
            
            E = current_injected / (4 * np.pi * sigma_ext * distance_cell**2) # V/μm
            phi_m = 3/2 * E * cell_radius # V
            
            # Uses explicit Z_pulse derived from high-frequency C_m shorting
            I_cell = phi_m / cells[c].pulse_impedance  # A
            
            # Convert injected current (A) over duration (s) to weight (S or µS) using ohm's law:
            #   Q = I * Δt   →   G = Q / [(Vm - Erev) * τ]
            pulse_q = I_cell * (pulse_duration * 1e-3)   # in Coulombs
            weight = (pulse_q / ( 65*1e-3 * cells[c].stim_syn.tau*1e-3)) * 1e6  # convert S to µS
            
            self.dynamic_injectors[c].nc.weight[0] = weight

            for t in pulse_on:
                self.dynamic_injectors[c].inject_at(t)

        return pulse_on
    

#%% Recording Functions
# =============================================================================
    
    def calculate_field_potentials(self, cells, start, end, bandwidth = 5000):        
        sigma_ext = 0.3e-6  # Extracellular conductivity (S/μm)
        
        distances = self.get_distances(cells)
        
        simple_potentials = np.zeros(end-start)
        for c in self.get_nearby_cells(cells):
            # These values are defined in MyCells
            if cells[c].cell_type == 'inh':
                cell_area       = 4*np.pi*(7.5e-4)**2 # cm²
                C_membrane      = 1.0e-6 # (F/cm²)
                cell_z          = 7.5 # (μm)
            else:
                cell_area       = 4*np.pi*(10e-4)**2 # cm²
                C_membrane      = 1.0e-6 # (F/cm²)
                cell_z          = 10 # (μm)
            
            
            distance_cell = np.sqrt(distances[c]**2 + cell_z**2) # μm
            
            V = np.array(list(cells[c].soma_v)[start:end])       # mV       
            V_diff = np.concatenate((np.array([0]), np.diff(V))) # mV
            
            
            i_Na = np.array(list(cells[c].soma_i_na)[start:end])    # mA/cm²
            i_K = np.array(list(cells[c].soma_i_k)[start:end])      # mA/cm²
            i_pas = np.array(list(cells[c].soma_i_pas)[start:end])  # mA/cm²
            
            I_membrane = C_membrane * V_diff / h.dt * 1000 + i_Na + i_K + i_pas # mA/cm²
            I_out = cell_area * I_membrane * 1e-3  # μA
            
            
            # For realistic decay, check: https://pmc.ncbi.nlm.nih.gov/articles/PMC2186261
            distance_exponent = 1.6
            
            simple_potentials += I_out / (4 * np.pi * sigma_ext * distance_cell**distance_exponent)
            
        
        # Constants
        k = 1.380649e-23  # Boltzmann constant (J/K)
        T = 37 + 273.15   # Temperature in Kelvin
        
        # Calculate noise power
        noise_power = 4 * k * T * self.impedance * bandwidth
        
        # Generate random noise samples
        num_samples = len(simple_potentials) 
        noise_samples = np.random.normal(0, np.sqrt(noise_power), num_samples)
        
        # Add noise to electrode measurements
        self.field_potentials = np.add(simple_potentials, noise_samples)
        self.filtered_field_potentials = []
        
    def update_threshold(self, sigma=5):
        if len(self.filtered_field_potentials) == 0:
            self.filter_field_potentials()        
        self.threshold = sigma * np.sqrt(np.mean(self.filtered_field_potentials**2))
    
    def filter_field_potentials(self, low_cutoff=300, high_cutoff=3000, sampling_freq=10000):
        
        freq_range = np.array([low_cutoff, high_cutoff]) / (sampling_freq/2)
        b, a = signal.butter(4, freq_range, 'bandpass')
        self.filtered_field_potentials = signal.filtfilt(b, a, self.field_potentials)
        
        return self.filtered_field_potentials
    
    def detect_spikes(self, min_spike_interval_ms, stim_times, blanking_ms=5, sorting=False, cells_spikes=None):
        if len(self.filtered_field_potentials) == 0:
            self.filter_field_potentials()
        signal = self.filtered_field_potentials
        
        # Detect crossings from below to above threshold
        abs_signal = np.abs(signal)
        crossings = np.where((abs_signal[1:] >= self.threshold) & (abs_signal[:-1] < self.threshold))[0] + 1
        temp_spike_indices = crossings
        
        # Remove spike indices within the blanking period after stimulation times
        valid_spike_indices = []
        for spike in temp_spike_indices:  
            if not ((spike * 1000/self.sampling_frequency - stim_times >= 0) & (spike * 1000/self.sampling_frequency - stim_times <= blanking_ms)).any():
                valid_spike_indices.append(spike)
            # else:
            #     print("Spike removed @ {}ms".format(spike/10))

        # Remove spikes within the minimum spike interval
        count = 1    
        while count < len(valid_spike_indices):
            if (valid_spike_indices[count] - valid_spike_indices[count - 1]) * 1000/self.sampling_frequency < min_spike_interval_ms:
                if np.abs(signal[valid_spike_indices[count]]) > np.abs(signal[valid_spike_indices[count-1]]):
                    valid_spike_indices = np.delete(valid_spike_indices, count-1)
                else:
                    valid_spike_indices = np.delete(valid_spike_indices, count)
            else:
                count += 1
        
        self.spike_indices = valid_spike_indices
        
        if sorting:
            sorted_id = []
            for spike_time in valid_spike_indices:
                min_distance = 2.0
                assigned_cell = None
                
                for c in self.nearby_cells_gids: 
                    if self.distances[c] > 50:
                        continue
                    
                    if len(cells_spikes[c]) == 0:
                        continue
                    closest_spike_time = cells_spikes[c][np.abs((np.array(cells_spikes[c]) - spike_time * 1000/self.sampling_frequency)).argmin()]
                    distance = closest_spike_time - spike_time * 1000/self.sampling_frequency
                    
                    # Update the assigned cell if this is the closest match
                    if np.abs(distance) < min_distance:
                        min_distance = np.abs(distance)
                        assigned_cell = c
                
                sorted_id.append(assigned_cell)
                
            self.spike_sorted_id = sorted_id
        
        return self.spike_indices
    
    def detect_bursts(self, max_burst_isi, min_num_spikes=3):  
        
        self.bursts = []
                
        # Detect bursts (clusters of spikes within the minimum interval)
        if len(self.spike_indices) > 0:
            current_burst = [self.spike_indices[0]]
            
            for i in range(1, len(self.spike_indices)):
                if self.spike_indices[i] - self.spike_indices[i - 1] <= max_burst_isi:
                    current_burst.append(self.spike_indices[i])
                else:
                    if len(current_burst) >= min_num_spikes:
                        self.bursts.append([current_burst[0], current_burst[-1]])
                    current_burst = [self.spike_indices[i]]
            
            # Add the last burst
            if len(current_burst) >= min_num_spikes:
                self.bursts.append([current_burst[0], current_burst[-1]])
                
        return self.bursts 

#%% Plot Functions 
# =============================================================================
    
    def plot_field_potentials(self, filtered=True, thresholds=False):
        if filtered:
            if len(self.filtered_field_potentials) == 0:
                self.filter_field_potentials()
            signal = self.filtered_field_potentials
        else:
            signal = self.field_potentials
            
        plt.rc('font', size=18, weight='bold')
        
        fig, ax = plt.subplots(figsize=(12,6))
        ax.plot([i/self.sampling_frequency for i in range(len(signal))], signal * 1e6, color='black')
        if thresholds:
            threshold = self.threshold * 1e6
            ax.axhline( threshold, linewidth=2, color='r' )
            ax.axhline( -threshold, linewidth=2, color='r' )
        plt.xlabel('Time (s)', weight='bold')
        plt.ylabel('Amplitude (\u03BCV)', weight='bold')
        # plt.title('Recorded Extracellular Field Potential @({},{})'.format(self.x, self.y))
        ax.grid(False)
        ax.spines['top'].set_visible(False)
        ax.spines['right'].set_visible(False)
        ax.spines['bottom'].set_linewidth(2)
        ax.spines['left'].set_linewidth(2)
        
        return fig
        
    def plot_welch(self):
        freq, Pxx_den = welch(self.field_potentials, self.sampling_frequency, nperseg=4096, scaling='spectrum')
        
        plt.rc('font', size=18, weight='bold')
        
        fig, ax = plt.subplots(figsize=(12,6))
        ax.plot(freq, np.abs(Pxx_den))
        plt.xlabel('Frequency (Hz)', weight='bold')
        plt.ylabel('PSD (V**2/Hz)', weight='bold')
        plt.title('PSD @({},{})'.format(self.x, self.y))
        plt.xlim(0, 3000)
        ax.spines['top'].set_visible(False)
        ax.spines['right'].set_visible(False)
        ax.spines['bottom'].set_linewidth(2)
        ax.spines['left'].set_linewidth(2)
        
    def plot_spikes(self, filtered=True):
        if filtered:
            if len(self.filtered_field_potentials) == 0:
                self.filter_field_potentials()
            signal = self.filtered_field_potentials
        else:
            signal = self.field_potentials
        
        plt.rc('font', size=18, weight='bold')
            
        # Plot the signal with spikes and bursts
        fig, ax = plt.subplots(figsize=(12, 6))
        ax.plot([i/self.sampling_frequency for i in range(len(signal))], signal*1e6, label="Signal")
        if len(self.spike_indices) > 0:
            ax.plot(np.array(self.spike_indices)*1/self.sampling_frequency, signal[self.spike_indices]*1e6, 'ro', label="Spikes")
        min_lim, max_lim = plt.ylim()
        plt.title('Pseudo-Extracellular Signal with Spike Detection @({},{})'.format(self.x,self.y))
        plt.xlabel("Time (s)", weight='bold')
        plt.ylabel('Amplitude (\u03BCV)', weight='bold')
        hand, labl = ax.get_legend_handles_labels()
        indexes = np.unique(labl, return_index=True)[1]
        ax.legend([labl[index] for index in sorted(indexes)])
        ax.grid(False)
        ax.spines['top'].set_visible(False)
        ax.spines['right'].set_visible(False)
        ax.spines['bottom'].set_linewidth(2)
        ax.spines['left'].set_linewidth(2)

    def plot_spikes_and_bursts(self, filtered=True):
        if filtered:
            if len(self.filtered_field_potentials) == 0:
                self.filter_field_potentials()
            signal = self.filtered_field_potentials
        else:
            signal = self.field_potentials
            
            
        plt.rc('font', size=18, weight='bold')
        
        # Plot the signal with spikes and bursts
        fig, ax = plt.subplots(figsize=(12, 6))
        ax.plot([i/self.sampling_frequency for i in range(len(signal))], signal*1e6, label="Signal")
        if len(self.spike_indices) > 0:
            ax.plot(np.array(self.spike_indices)*1/self.sampling_frequency, signal[self.spike_indices]*1e6, 'ro', label="Spikes")
        min_lim, max_lim = plt.ylim()
        for burst in self.bursts:
            ax.axvspan(burst[0]/self.sampling_frequency, burst[-1]/self.sampling_frequency, alpha=0.2, color='red',
                        label="Bursts")
            # if len(burst) > 2:
            #     ax.axvspan(burst[0]/self.sampling_frequency, burst[-1]/self.sampling_frequency, alpha=0.2, color='red',
            #                 label="Bursts")
        plt.title('Pseudo-Extracellular Signal with Spike and Burst Detection @({},{})'.format(self.x,self.y))
        plt.xlabel("Time (s)", weight='bold')
        plt.ylabel('Amplitude (\u03BCV)', weight='bold')
        hand, labl = ax.get_legend_handles_labels()
        indexes = np.unique(labl, return_index=True)[1]
        ax.legend([labl[index] for index in sorted(indexes)])
        ax.grid(False)
        ax.spines['top'].set_visible(False)
        ax.spines['right'].set_visible(False)
        ax.spines['bottom'].set_linewidth(2)
        ax.spines['left'].set_linewidth(2)
        