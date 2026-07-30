"""
NbControlEnvironment.py — A configuration-driven 
class for building and running NEURON-based microelectrode array
simulation environments for network burst control tasks.

@author: ncn-neuron
"""

import numpy as np
from neuron import h
import MyUtils
from environments.BaseEnvironment import BaseEnvironment

class NbControlEnvironment(BaseEnvironment):
    """
    RL Environment Wrapper for Network Burst Control.
    """
    def __init__(self, config, task_params):
        super(NbControlEnvironment, self).__init__(config)
        
        self.task_params = task_params
        
        # Stimulation parameters
        self.stim_frequency      = task_params["stim_frequency"]
        self.stim_amplitude      = task_params["stim_amplitude"]
        self.pulse_duration      = task_params["pulse_duration"]
        self.action_space_electrodes = task_params["action_space_electrodes"]
        self.ignore_electrodes       = task_params["ignore_electrodes"]
        self.step_duration_ms        = task_params["step_duration_ms"]
        self.max_num_steps           = task_params["max_num_steps"]
        
        # Burst detection parameters
        self.burst_method                   = task_params["burst_method"]
        self.min_spike_interval_ms          = task_params["min_spike_interval_ms"]
        self.max_network_burst_isi_ms       = task_params["max_network_burst_isi_ms"]
        self.min_spikes_per_electrode       = task_params["min_spikes_per_electrode"]
        self.min_ibi_ms                     = task_params["min_ibi_ms"]
        self.min_ratio_active_electrodes    = task_params["min_ratio_active_electrodes"]
        
        self.reset_rl_buffers()

    def reset_rl_buffers(self):
        """Resets variables related to the RL loop's state tracking."""
        self.buffer_bursts                  = []
        self.buffer_nibi_ms                 = []
        self.buffer_weighted_electrodes     = []
        self.off_previous_burst             = True
        self.done                           = False
        self.state                          = None
        self.current_step                   = 0

    def _advance_until_valid_state(self):
        """Advances the biological simulation until the environment yields a valid state."""
        nSimulationSteps = int(self.step_duration_ms / h.dt)
        max_warmup_steps = int(3 * 60 * 1000 / self.step_duration_ms) # Limit to 3 min max
        warmup_step = 0
        
        while self.state is None:            
            tstart  = round(h.t, 1)
            
            for i in range(nSimulationSteps):
                h.fadvance()
                
            for e, electrode in enumerate(self.grid.electrodes):
                electrode.calculate_field_potentials(self.cells, 0, int(self.step_duration_ms / h.dt))
                spike_indices = electrode.detect_spikes(self.min_spike_interval_ms, [])
                         
                for spike in spike_indices:
                    self.all_electrode_spikes[e].append(tstart + spike*h.dt)
                    
            for e in self.grid.electrodes:
                for c in e.get_nearby_cells(self.cells):
                    self.cells[c].soma_v.resize(0)
                    self.cells[c].soma_i_na.resize(0)
                    self.cells[c].soma_i_k.resize(0)
                    self.cells[c].soma_i_pas.resize(0)
                    
            self.state = self.get_state()
            
            warmup_step += 1
            if warmup_step >= max_warmup_steps:
                print('Environment failed to find a valid state during simulation advancement.')
                break

    def soft_reset(self, new_task_params=None):
        """
        Starts a new RL episode on the same biological timeline.
        Resets the step counter and done flag, but retains spike history, 
        network burst buffers, and simulation time. 
        """
                
        if new_task_params is not None:
            self.task_params = new_task_params
            self.stim_frequency      = new_task_params.get("stim_frequency", self.stim_frequency)
            self.stim_amplitude      = new_task_params.get("stim_amplitude", self.stim_amplitude)
            self.pulse_duration      = new_task_params.get("pulse_duration", self.pulse_duration)
            self.action_space_electrodes = new_task_params.get("action_space_electrodes", self.action_space_electrodes)
            self.ignore_electrodes       = new_task_params.get("ignore_electrodes", self.ignore_electrodes)
            self.step_duration_ms        = new_task_params.get("step_duration_ms", self.step_duration_ms)
            self.max_num_steps           = new_task_params.get("max_num_steps", self.max_num_steps)
            self.burst_method                   = new_task_params.get("burst_method", self.burst_method)
            self.min_spike_interval_ms          = new_task_params.get("min_spike_interval_ms", self.min_spike_interval_ms)
            self.max_network_burst_isi_ms       = new_task_params.get("max_network_burst_isi_ms", self.max_network_burst_isi_ms)
            self.min_spikes_per_electrode       = new_task_params.get("min_spikes_per_electrode", self.min_spikes_per_electrode)
            self.min_ibi_ms                     = new_task_params.get("min_ibi_ms", self.min_ibi_ms)
            self.min_ratio_active_electrodes    = new_task_params.get("min_ratio_active_electrodes", self.min_ratio_active_electrodes)
            
        self.current_step = 0
        self.done = False
        
        # Advance simulation if the previous episode ended and state was reset to None
        if self.state is None:
            self._advance_until_valid_state()
            
        return self.state

    def reset(self, new_config=None, reset_buffers=True):
        """
        Resets the environment to an initial valid state.
        Runs a warmup period until the first valid observation is generated.
        Returns the initial state observation.
        """
        
        # Make sure the NB ended before resetting
        self._advance_until_valid_state()
        
        old_ht = h.t
        
        # Save buffers if we're retaining history across a hard reset
        if not reset_buffers:
            old_buffer_bursts = [[b[0] - old_ht, b[1] - old_ht] for b in self.buffer_bursts]
            old_buffer_nibi_ms = list(self.buffer_nibi_ms)
            old_buffer_weighted_electrodes = list(self.buffer_weighted_electrodes)
            old_off_previous_burst = self.off_previous_burst
            
            # Save previous spikes to ensure continuous burst detection
            old_spikes = []
            for e in range(self.nElectrodes):
                old_spikes.append([t - old_ht for t in self.all_electrode_spikes[e] if t >= old_ht - 1e9])
            
            # Save previous stim_times (note: stim_times_s is in seconds, old_ht is in ms)
            old_stim_times = []
            if hasattr(self, 'stim_times_s'):
                old_stim_times = [t - (old_ht / 1000.0) for t in self.stim_times_s if t >= (old_ht / 1000.0) - 1e6]
        
        # Call base reset to apply new config (which resets h.t and clears spike arrays)
        super(NbControlEnvironment, self).reset(config=new_config)
        
        if reset_buffers:
            self.reset_rl_buffers()
        else:
            # Restore the offset buffers to sync with the new h.t = 0
            self.buffer_bursts = old_buffer_bursts
            self.buffer_nibi_ms = old_buffer_nibi_ms
            self.buffer_weighted_electrodes = old_buffer_weighted_electrodes
            self.off_previous_burst = old_off_previous_burst
            self.done = False
            self.current_step = 0
            self.stim_times_s = old_stim_times
            
            # Restore offset spikes into the newly initialized arrays
            for e in range(self.nElectrodes):
                self.all_electrode_spikes[e].extend(old_spikes[e])
        
        self._advance_until_valid_state()
        
        return self.state

    def step(self, action):
        """
        Takes an action, advances the simulation, and returns next_state, reward, done, info.
        """
        tstart  = round(h.t, 1)
        tstop   = int(h.t + self.step_duration_ms)
        nSteps  = int(self.step_duration_ms / h.dt)
        
        rel_stim_times = []
        if action > 0:
            # Generate pulses
            stim_times = self.grid.electrodes[
                self.action_space_electrodes[action]-1
            ].generate_pulses(self.cells,
                              tstart + h.dt,  
                              tstop, 
                              self.stim_frequency,
                              self.pulse_duration,
                              self.stim_amplitude)
            rel_stim_times = [t for t in stim_times]            
            self.stim_times_s += [t / 1000 for t in stim_times]

        for i in range(nSteps):
            h.fadvance()
            
        self.current_step += 1
        
        for e, electrode in enumerate(self.grid.electrodes):
            electrode.calculate_field_potentials(self.cells, 0, int(self.step_duration_ms / h.dt))
            spike_indices = electrode.detect_spikes(self.min_spike_interval_ms, rel_stim_times, self.blanking_ms)
                     
            for spike in spike_indices:
                self.all_electrode_spikes[e].append(tstart + spike*h.dt)
                
        # Reset vectors
        for e in self.grid.electrodes:
            for c in e.get_nearby_cells(self.cells):
                self.cells[c].soma_v.resize(0)
                self.cells[c].soma_i_na.resize(0)
                self.cells[c].soma_i_k.resize(0)
                self.cells[c].soma_i_pas.resize(0)

        next_state = self.get_state()
        
        if self.current_step >= self.max_num_steps:
            self.done = True
            
        reward = self.calculate_reward(self.state, action, next_state)
        
        # Update current state to next state
        if self.done:
            self.state = None            
        else:
            self.state = next_state
        
        return next_state, reward, self.done, {}

    def get_state(self):
        """Calculates and returns the current environment observation."""
        if not self.off_previous_burst:
            start = self.buffer_bursts[-1][0] - self.max_network_burst_isi_ms
        else:
            start = h.t - self.step_duration_ms      
            
        min_bursting_electrodes = self.min_ratio_active_electrodes * self.nElectrodes
          
        if self.burst_method == "v1":
            all_electrode_spikes_tuple = []
            for e in range(self.nElectrodes):  
                electrode_spikes = list([t for t in self.all_electrode_spikes[e] if t >= start])  
                for spike in electrode_spikes:
                    all_electrode_spikes_tuple.append((e, spike))
            
            network_burst_intervals, active_electrodes = MyUtils.detect_network_bursts_v1(
                all_electrode_spikes_tuple,
                self.nElectrodes, 
                min_bursting_electrodes, 
                self.min_spikes_per_electrode, 
                self.max_network_burst_isi_ms, 
                self.min_ibi_ms)
            
        elif self.burst_method =="v2":        
            resolution_ms = 1
            electrode_burst_masks = np.zeros((self.nElectrodes, int((h.t - start) /resolution_ms)))
            for e in range(self.nElectrodes):
                electrode_spikes = [t-start for t in self.all_electrode_spikes[e] if t >= start]
                bursts, num_random_spikes = MyUtils.detect_bursts(
                    electrode_spikes, self.max_network_burst_isi_ms, self.min_spikes_per_electrode)
                
                temp_mask = np.zeros(int((h.t - start) / resolution_ms))
                for b in bursts:
                    temp_mask[int(np.floor(b[0])):int(np.floor(b[1]))] = 1
                electrode_burst_masks[e,:] = temp_mask
    
            network_burst_intervals = MyUtils.detect_network_bursts_v2(
                electrode_burst_masks, min_bursting_electrodes, self.min_ibi_ms, 
                resolution_ms)
            
            for b in network_burst_intervals:
                b[0] += start
                b[1] += start
        
        if len(network_burst_intervals) > 0:
            for b in network_burst_intervals:  
                if not self.off_previous_burst:
                    self.buffer_bursts.pop()
                    self.buffer_weighted_electrodes.pop()    
                else:
                    self.done = True
                    
                    if len(self.buffer_nibi_ms) == 0:
                        self.buffer_nibi_ms.append(b[0])
                    else:
                        self.buffer_nibi_ms.append(b[0] - self.buffer_bursts[-1][1])
                        
                self.buffer_bursts.append(b) 
                weighted_electrodes = np.zeros(self.nElectrodes)
                for e in range(self.nElectrodes):               
                    weighted_electrodes[e] = sum([np.exp(-np.log(2)/10*(t-b[0])) for t in self.all_electrode_spikes[e] if t >= b[0] and t <= b[0]+100])
                self.buffer_weighted_electrodes.append(weighted_electrodes) 
                    
            # Check if burst is over
            if self.buffer_bursts[-1][1] <= h.t - self.min_ibi_ms:
                self.off_previous_burst = True 
            else:
                self.off_previous_burst = False 
                
                if self.state is None:
                    return None
                else:     
                    self.elapsed_time_since_burst = h.t - self.buffer_bursts[-2][1]   
                    rel_elapsed_time = self.elapsed_time_since_burst / np.median(self.buffer_nibi_ms[:-1])
                    state = self.state.copy()
                    state[0] = rel_elapsed_time
                    return state        
        
        if len(self.buffer_bursts) > 4 and self.off_previous_burst:   
            # Remove oldest NB from buffers
            if len(self.buffer_bursts) > 5:
                self.buffer_nibi_ms.pop(0)
                self.buffer_bursts.pop(0)
                self.buffer_weighted_electrodes.pop(0)
            
            # Calculate relative elapsed time from median NIBI
            self.elapsed_time_since_burst = h.t - self.buffer_bursts[-1][1]
            median_nibi = np.median(self.buffer_nibi_ms)
            rel_elapsed_time = self.elapsed_time_since_burst / median_nibi  
            
            # Calculate relative weights per electrode
            all_weighted_electrodes = sum(self.buffer_weighted_electrodes)            
            for e in self.ignore_electrodes:
                all_weighted_electrodes[e-1] = 0                
            rel_weighted_electrodes = all_weighted_electrodes / np.sum(all_weighted_electrodes)
            
            state = np.hstack((rel_elapsed_time, rel_weighted_electrodes)) 
            return state
        else:
            return None

    def calculate_reward(self, state, action, next_state):   
        """Calculates step reward based on environment state changes."""
        if action > 0:
            if len(self.buffer_bursts) > 0:
                delta = self.buffer_bursts[-1][0] - (h.t - self.step_duration_ms)
                if delta >= 0 and delta < 100:                
                    reward = 1             
                else: 
                    reward = -0.25
            else:
                reward = -0.25
        else:
            if self.done:     
                reward = -1
            else:
                reward = 0.25
        return reward
