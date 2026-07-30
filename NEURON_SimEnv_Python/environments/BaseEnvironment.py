#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
BaseEnvironment.py — A configuration-driven 
class for building and running NEURON-based microelectrode array
simulation environments.

@author: ncn-neuron
"""

from neuron.units import mV, ms
from neuron import h #, gui

from MyCells import MyPyramidal, MyInterneuron
from MyGrid import MyGrid
import NetBuilder
import numpy as np
import matplotlib as mpl
import random
import os   
from datetime import datetime

h.load_file('stdrun.hoc')

class BaseEnvironmentConfig:
    
    def __init__(self, **kwargs):
        
        # ===== DEFAULT PARAMETERS =====
        
        self.get_cell_spikes    = False
        self.spike_sorting      = False

        # Network parameters
        self.nCells             = 200
        self.sides              = [(750, 750)]
        self.nClusters          = 8
        self.cluster_spread     = 100 # um
        self.min_cell_dist      = 10 # diam Pyr
        self.fraction_inh       = 0.05 # 0.05
        self.weight_noise       = 15e-5
        self.tau_noise          = 4.3
        self.noise_randomness   = 1
        self.seed               = 5
        self.NOISE_SEED         = 42
        
        # Connectivity parameters (assessed by calibrating maximal synaptic conductances)
        self.sigma_exc          = 1e4
        self.sigma_inh          = 100
        self.nConns_exc         = 10
        self.nConns_inh         = 20
        self.wee                = 97e-5
        self.wei                = 870e-5
        self.wie                = -1345e-5
        self.wii                = 0
        self.tau_decay_NMDA     = 148.5
        self.hub_strength_exc   = 0
        self.hub_strength_inh   = 0
        self.hub_mode           = 'out' # 'in' / 'out' / 'both'
        
        # Electrode parameters
        self.nElectrodes        = 9
        self.electrodes_radius  = 15
        self.spacing            = 200
        self.impedance_lim      = [50000, 70000]
        self.sampling_freq      = 10000

        # Spike detection parameters
        self.threshold_sigma        = 5
        self.min_spike_interval_ms  = 3
        self.blanking_ms            = 6

        # Override with any provided parameters
        for key, value in kwargs.items():
            setattr(self, key, value)

    def __repr__(self):
        params = {key: value for key, value in self.__dict__.items()}
        return f"{self.__class__.__name__}({params})"
    
    
    



class BaseEnvironment:
    
    def __init__(self, config):        
        super(BaseEnvironment, self).__init__()
        
        # Initial setup using the provided config
        self._apply_config_and_setup(config)
        
        return
    
    def _apply_config_and_setup(self, config):
        """Internal method to handle configuration application and object creation."""
        # Update attributes from the config object
        for key, value in config.__dict__.items():
            setattr(self, key, value)
        
        # Global NEURON configuration
        h.dt = 0.1 * ms
        h.celsius = 37.0
        
        # Seeds
        self.CELL_CONN_SEED     = 5 + self.seed
        self.CELL_PLACE_SEED    = 42 + self.seed
        self.CELL_TYPE_SEED     = 100 + self.seed
        
        # Spike detection parameters
        self.min_spike_interval_samples  = self.min_spike_interval_ms * 1/h.dt    
        
        # Spiking variables
        self.all_cell_spikes = np.empty(self.nCells, dtype=object)
        self.all_cell_spikes[...] = [[] for _ in range(self.all_cell_spikes.shape[0])]        
        self.all_electrode_spikes = np.empty(self.nElectrodes, dtype=object)
        self.all_electrode_spikes[...] = [[] for _ in range(self.all_electrode_spikes.shape[0])]        
        self.all_electrode_spikes_sorted = np.empty(self.nElectrodes, dtype=object)
        self.all_electrode_spikes_sorted[...] = [[] for _ in range(self.all_electrode_spikes_sorted.shape[0])]        
        
        # Define excitatory and inhibitory cells
        self._setup_cells()
        
        # Connect cells
        self._setup_connectivity()
        
        # Generate spontaneous activity        
        self._generate_noise(self.excCells, self.NOISE_SEED)
        # self._generate_noise(range(self.nCells), self.NOISE_SEED)
        
        # Insert electrodes
        random.seed(self.CELL_PLACE_SEED)
        self.stim_times_s = []
        self.grid = MyGrid(0, self.nElectrodes, self.electrodes_radius, self.spacing, 
                           self.impedance_lim, self.sampling_freq, seed=self.CELL_PLACE_SEED)

        
        # Enable recording of membrane voltage of cells near the electrodes (<100um)
        for e in self.grid.electrodes:
            for c in e.get_nearby_cells(self.cells):
                self.cells[c]._save_voltage()
                self.cells[c]._save_currents()
                
        # Initialize run
        h.finitialize()
        
    
    def _setup_cells(self):
        
        random.seed(self.CELL_TYPE_SEED)
        self.inhCells = random.sample(range(0, self.nCells), int(float(self.nCells) * self.fraction_inh))
        self.inhCells.sort()
        self.excCells = list(set(range(0, self.nCells)).difference(self.inhCells))

        self.cells                  = []
        self.cells_colors           = np.zeros([self.nCells, 4])
        viridis                     = mpl.colormaps['jet']
        self.cells_colors[1:,:]     = viridis(0)
        for c in range(self.nCells):
            if c in self.inhCells:
                self.cells.append(MyInterneuron(c, 0, 0, 0, spike_detector=self.get_cell_spikes))
                self.cells_colors[c,:] = [1, 0, 0, 1] # red
            else:
                self.cells.append(MyPyramidal(c, 0, 0, 0, spike_detector=self.get_cell_spikes, tau_decay_NMDA=self.tau_decay_NMDA))
                self.cells_colors[c,:] = [0, 0, 1, 1] # blue

        self.center = [(0, 0)]            
        [self.cells, self.pos] = NetBuilder.place_cells_uniform(self.cells, self.center, self.sides, self.CELL_PLACE_SEED)
        # [self.cells, self.pos] = NetBuilder.place_cells_neyman_scott(self.cells, self.center, self.sides, n_clusters=self.nClusters, 
        #                                                              cluster_spread=self.cluster_spread, min_dist=self.min_cell_dist, 
        #                                                              seed=self.CELL_PLACE_SEED)
    
    def _setup_connectivity(self):        
        connectivity_matrix       = np.ones((self.nCells,self.nCells))        
        
        # Remove autapses
        np.fill_diagonal(connectivity_matrix, 0)        
        
        # Remove inh-inh connections (if weight == 0)
        if self.wii == 0:
            connectivity_matrix[np.ix_(self.inhCells,self.inhCells)] = 0
            connectivity_matrix[np.ix_(self.inhCells,self.excCells)] = NetBuilder.prune_connections(self.cells, self.inhCells, self.excCells, self.nConns_inh, self.sigma_inh, self.hub_strength_inh, self.hub_mode)
        else:
            connectivity_matrix[np.ix_(self.inhCells,range(self.nCells))]   = NetBuilder.prune_connections(self.cells, self.inhCells, range(self.nCells), self.nConns_inh, self.sigma_inh, self.hub_strength_inh, self.hub_mode)
        
        connectivity_matrix[np.ix_(self.excCells,range(self.nCells))]   = NetBuilder.prune_connections(self.cells, self.excCells, range(self.nCells), self.nConns_exc, self.sigma_exc, self.hub_strength_exc, self.hub_mode)
        
        self.all_conns = connectivity_matrix
        
        # # Check the result
        # remaining_connections = np.sum(connectivity_matrix)        
        # print(f"Final remaining connections: {remaining_connections}")
            
        # Weights
        self.conn_weights    = np.zeros(np.shape(self.all_conns)) 
        for c in range(self.nCells):
            conn_ind = np.where(self.all_conns[c,:] == 1)[0]
            if c in self.inhCells:
                for conn in conn_ind:
                    if conn in self.excCells:
                        self.conn_weights[c,conn] = self.wie
                    else:
                        self.conn_weights[c,conn] = self.wii

            else:        
                for conn in conn_ind:
                    if conn in self.excCells:
                        self.conn_weights[c,conn] = self.wee
                    else:
                        self.conn_weights[c,conn] = self.wei
                    
        # Delays
        self.dist_exc       = []
        self.dist_inh       = []
        self.conn_delays    = np.zeros(np.shape(self.all_conns))
        for c in range(self.nCells):
            conn_ind = np.where(self.all_conns[c,:] == 1)[0]
            for conn in conn_ind:
                dist = NetBuilder.distance_between_cells(self.cells[c], self.cells[conn])
                if c in self.excCells:
                    self.dist_exc.append(dist)
                else:
                    self.dist_inh.append(dist)
                random.seed(c)
                self.conn_delays[c,conn] = dist / 1000 + float(random.uniform(0.3, 0.5)) * ms # um/ms + synaptic delay
                
                
               
        NetBuilder.connect_cells(self.cells, self.all_conns, self.conn_weights, self.conn_delays, 'div')        
        # NetBuilder.plot_conn_distances(self.dist_exc, self.dist_inh)
        # NetBuilder.plot_degree_distribution(self.all_conns)
    
    def _generate_noise(self, cells_id, seed):
        
        random.seed(seed)
        
        ns = []
        ncs = []
        for c in cells_id:
            ns.append(h.NetStim())   
            ns[-1].interval = self.tau_noise
            ns[-1].number = 1e9
            ns[-1].noise = self.noise_randomness
            ns[-1].start = float(random.randint(1, 10))
            ns[-1].seed(random.randint(0,10000))
            nc = h.NetCon(ns[-1], self.cells[c].noise_exc)
            nc.weight[0] = self.weight_noise
            ncs.append(nc)
            
            # # Adding inh noise increases number of objects and makes simulation slower
            # ns.append(h.NetStim())   
            # ns[-1].interval = self.tau_noise * 2.5
            # ns[-1].number = 1e9
            # ns[-1].noise = self.noise_randomness
            # ns[-1].start = float(random.randint(1, 10))
            # ns[-1].seed(random.randint(0,10000))
            # nc = h.NetCon(ns[-1], self.cells[c].noise_inh)
            # nc.weight[0] = self.weight_noise * 1.5
            # ncs.append(nc)
            
        self.ns     = ns
        self.ncs    = ncs
        
        
    def reset(self, config=None):
        """Reset the environment state and return the initial observation."""
        if config is not None:
            # Re-run the full setup with the new configuration
            self._apply_config_and_setup(config)
        else:
            # Standard reset logic (e.g., just clearing spike buffers)
            self.stim_times_s = []
            
            self.all_cell_spikes = np.empty(self.nCells, dtype=object)
            self.all_cell_spikes[...] = [[] for _ in range(self.all_cell_spikes.shape[0])]        
            self.all_electrode_spikes = np.empty(self.nElectrodes, dtype=object)
            self.all_electrode_spikes[...] = [[] for _ in range(self.all_electrode_spikes.shape[0])]
            self.all_electrode_spikes_sorted = np.empty(self.nElectrodes, dtype=object)
            self.all_electrode_spikes_sorted[...] = [[] for _ in range(self.all_electrode_spikes_sorted.shape[0])]        
            
            h.finitialize()
        return    
    
    def run_baseline(self, total_duration_s, stim_electrode=None, stim_frequency=1, stim_amplitude=0.8, 
                     number_pulses=[], save_path=None, buffer_duration=0.2):
        
        electrodes = self.grid.electrodes
        
        tstop = int(total_duration_s * 1000)
        
        if stim_electrode != None: 
            if type(stim_electrode) != list: stim_electrode = [stim_electrode]
            for elec in stim_electrode:
                stim_times = electrodes[elec-1].generate_pulses(self.cells,
                                                          h.dt,  tstop, 
                                                          stim_frequency, 0.2, stim_amplitude, number_pulses)
            
            self.stim_times_s = [t / 1000 for t in stim_times]
                
        
        for i in range(int(total_duration_s / buffer_duration)):
            self.run_for(buffer_duration)  
            
            
        if save_path is not None: 
            os.makedirs(save_path, exist_ok=True)
            filename = save_path + "seed={}_seedNoise={}_{}.npz"
            filename = filename.format(self.seed, self.NOISE_SEED, 
                                       datetime.now().strftime("%Y%m%d_%H%M%S"))
            self.save_data(filename)
        
        return
    
    def run_for(self, duration_s, stim_electrode=None, stim_frequency=1, stim_amplitude=0.8, 
                number_pulses=[], save_path=None):
        
        electrodes = self.grid.electrodes
        
        tstart  = round(h.t, 1)
        tstop   = round(h.t, 3) + duration_s * 1000
        
        nSteps = int(duration_s * 1000 / h.dt)
        
        rel_stim_times = []
        if stim_electrode != None: 
            if type(stim_electrode) != list: stim_electrode = [stim_electrode]
            for elec in stim_electrode:
                stim_times = electrodes[elec-1].generate_pulses(self.cells,
                                                          tstart + h.dt,  tstop, 
                                                          stim_frequency, 0.2, stim_amplitude, number_pulses)
            
                
            rel_stim_times += [t-tstart for t in stim_times]
            self.stim_times_s += [t / 1000 for t in stim_times]
        
        
        for i in range(nSteps):    
            h.fadvance()
        
        # h.continuerun(tstop)
    
        rel_cell_spikes = None
        if self.get_cell_spikes:
            rel_cell_spikes = np.empty(self.nCells, dtype=object)
            rel_cell_spikes = [[] for _ in range(rel_cell_spikes.shape[0])]           
            for c, cell in enumerate(self.cells):        
                for spike in list(cell.spike_times):
                    self.all_cell_spikes[c].append(spike)
                    rel_cell_spikes[c].append(spike-tstart)
                cell.spike_times.resize(0)
                
            
        for e, electrode in enumerate(electrodes):
            electrode.calculate_field_potentials(self.cells, 0, int(duration_s * 1000 / h.dt))
            spike_indices = electrode.detect_spikes(self.min_spike_interval_ms, rel_stim_times, self.blanking_ms, sorting=self.spike_sorting, cells_spikes=rel_cell_spikes)
            
            for spike in spike_indices:
                self.all_electrode_spikes[e].append(tstart + spike*h.dt)
                
            if self.spike_sorting:
                self.all_electrode_spikes_sorted[e] += electrode.spike_sorted_id
        
        
        for e in electrodes:
            for c in e.get_nearby_cells(self.cells):
                self.cells[c].soma_v.resize(0)
                self.cells[c].soma_i_na.resize(0)
                self.cells[c].soma_i_k.resize(0)
                self.cells[c].soma_i_pas.resize(0)
                
        if save_path is not None: 
            os.makedirs(save_path, exist_ok=True)
            filename = save_path + "seed={}_seedNoise={}_{}.npz"
            filename = filename.format(self.seed, self.NOISE_SEED, 
                                       datetime.now().strftime("%Y%m%d_%H%M%S"))
            self.save_data(filename)
            
        
    
    @staticmethod
    def run_baseline_static(config, total_duration_s, save_path=None):
        instance = BaseEnvironment(config)
        return instance.run_baseline(total_duration_s, save_path=save_path)
    
    def save_data(self, filename):

        np.savez(filename, 
                 seed                            = self.seed,
                 CELL_CONN_SEED                  = self.CELL_CONN_SEED,
                 CELL_PLACE_SEED                 = self.CELL_PLACE_SEED,
                 CELL_TYPE_SEED                  = self.CELL_TYPE_SEED,
                 NOISE_SEED                      = self.NOISE_SEED,
                 nCells                          = self.nCells,
                 sides                           = self.sides,
                 fraction_inh                    = self.fraction_inh,
                 excCells                        = self.excCells,
                 inhCells                        = self.inhCells,
                 tau_noise                       = self.tau_noise,
                 weight_noise                    = self.weight_noise,
                 noise_randomness                = self.noise_randomness,
                 wee                             = self.wee,
                 wei                             = self.wei,
                 wie                             = self.wie, 
                 tau_decay_NMDA                  = self.tau_decay_NMDA,
                 hub_mode                        = self.hub_mode,
                 hub_strength_exc                = self.hub_strength_exc,
                 hub_strength_inh                = self.hub_strength_inh,
                 sigma_exc                       = self.sigma_exc,
                 sigma_inh                       = self.sigma_inh,
                 nConns_exc                      = self.nConns_exc,
                 nConns_inh                      = self.nConns_inh,
                 dist_exc                        = self.dist_exc,
                 dist_inh                        = self.dist_inh,
                 nElectrodes                     = self.nElectrodes,
                 electrode_radius                = self.electrodes_radius,
                 electrode_spacing               = self.spacing,
                 impedance_lim                   = self.impedance_lim,
                 min_spike_interval_ms           = self.min_spike_interval_ms, 
                 all_cell_spikes                 = self.all_cell_spikes,
                 all_electrode_spikes            = self.all_electrode_spikes,
                 conn_weights                    = self.conn_weights,
                 threshold_sigma                 = self.threshold_sigma,
                 total_duration_s                = h.t/1000)
        



