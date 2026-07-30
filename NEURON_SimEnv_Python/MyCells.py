# -*- coding: utf-8 -*-

"""
MyCells.py — A class for defining cell types in the NEURON simulation environment.

@author: ncn-neuron
"""

from neuron import h #, gui
from neuron.units import ms, mV
import numpy as np 

h.celsius = 37.0

class MyPyramidal:
    
    def __init__(self, gid, x, y, z, spike_detector=True, tau_decay_NMDA=148.5):
        self._gid = gid
        self.cell_type = "exc"
        self.pulse_impedance = 40.0e6 # Ω 
        self._setup_morphology()
        self._setup_biophysics()
        self.x = 0.0
        self.y = 0.0
        self.z = 0.0
        self.well = None
        h.define_shape()
        self._set_position(x, y, z)
        
        self.tau_decay_NMDA = tau_decay_NMDA
        
        if spike_detector:
            self._spike_detector = h.NetCon(self.soma(0.5)._ref_v, None, sec=self.soma)
            self.spike_times = h.Vector()
            self._spike_detector.record(self.spike_times)
        
        self._ncs       = []
        self._exc_syns  = []
        self._inh_syns  = []
        
        self.output_gids = np.array([], dtype=int)  # gids of output cells
        
    def _save_currents(self):
        self.soma_i_k = h.Vector().record(self.soma(0.5)._ref_ik)
        self.soma_i_na = h.Vector().record(self.soma(0.5)._ref_ina)
        self.soma_i_pas = h.Vector().record(self.soma(0.5)._ref_i_pas)
        
    def _save_voltage(self):
        self.soma_v = h.Vector().record(self.soma(0.5)._ref_v)
        
    def _get_voltage(self):
        return self.soma_v

    def remove_connections(self):
        # Destroy all NetCon objects
        for nc in self._ncs:
            nc = None  # This deletes the NetCon object
        self._ncs = []  # Clear the list of connections
    
    
    def _setup_morphology(self):
        self.soma = h.Section(name='soma', cell=self)
        self.all = [self.soma]
        self.soma.L = 20.0
        self.soma.diam = 20.0
        
        
    def _set_position(self, x, y, z):
        for sec in self.all:
            for i in range(sec.n3d()):
                sec.pt3dchange(i,
                               x - self.x + sec.x3d(i),
                               y - self.y + sec.y3d(i),
                               z - self.z + sec.z3d(i),
                              sec.diam3d(i))
        self.x, self.y, self.z = x, y, z
        
    def _get_position(self):
        return self.x, self.y, self.z
        
    def _setup_biophysics(self):
        for sec in self.all:
            sec.Ra = 150.0  # Axial resistance in Ω * cm
            sec.cm = 1.0    # Membrane capacitance in μF / cm^2  
                        
        self.soma.insert('pas')            
        self.soma.insert('HH2')    
        # self.soma.insert('CaIntraCellDyn')
        # self.soma.insert('iCaL')
        # self.soma.insert('iKCa')
        
        for seg in self.soma:            
            seg.pas.g = 0.0001  # Passive conductance in S/cm^2
            seg.pas.e = -65    # Leak reversal potential mV
            
            seg.HH2.gnabar  = 0.12  # 0.12 (S/cm^2)
            seg.HH2.gkbar   = 0.036  # 0.036 (S/cm^2)
            seg.HH2.vtraub  = -55.0  # (mV) 
            
            # seg.CaIntraCellDyn.cai_tau  = 300 # (ms)  100
            # seg.iKCa.gbar               = 0.005 # (S/cm2)
            # seg.iCaL.pcabar             = 0.00001 # (cm/s)	
        
        self.noise_exc          = h.ExpSyn(self.soma(0.5))
        self.noise_exc.tau      = 3.0 * ms          # Synapse time constant
        self.noise_exc.e        = 0.0 * mV          # Synapse reversal potential (0.0 mV => excitatory synapse)
         
        self.noise_inh          = h.ExpSyn(self.soma(0.5))
        self.noise_inh.tau      = 6.0 * ms          
        self.noise_inh.e        = -80.0 * mV        # Synapse reversal potential ( <-70mV --> inhibitory )
    
        
        # # Simulates a slow afterhyperpolarization using ExpSyn
        # self.ahp_syn            = h.ExpSyn(self.soma(0.5))
        # self.ahp_syn.tau        = 1000 * ms   # ms — controls AHP decay
        # self.ahp_syn.e          = -90.0       # mV — K⁺-like reversal potential
    
        # # NetCon triggers AHP conductance after spikes
        # self.nc_ahp             = h.NetCon(self.soma(0.5)._ref_v, 
        #                                     self.ahp_syn, sec=self.soma)
        # self.nc_ahp.threshold   = 0     # Spike detection threshold
        # self.nc_ahp.delay       = 0     # No delay
        # self.nc_ahp.weight[0]   = 5e-5
    
        # # Optional: store references for later analysis or modification
        # self._iAHP_components   = (self.ahp_syn, self.nc_ahp)
        
        # Add stimulation synapse that goes with DynamicPulseInjector class
        self.stim_syn           = h.ExpSyn(self.soma(0.5))
        self.stim_syn.tau       = 0.2
        self.stim_syn.e         = 0 
    
    def add_exc_synapse(self, conn_type="AMPA"):
        """
        For values in hippocampus check "Reconstruction of the Hippocampus" (Romani et al. 2022)
        """
        if conn_type == "AMPA":        
            syn_exc            = h.AMPA_DynSyn(self.soma(0.5))
            syn_exc.tau_decay  = 3.0 * ms               # Decay time constant
            syn_exc.tau_rec    = 671.0 * ms             # Recovery time constant (depression) 
            syn_exc.tau_fac    = 17.0 * ms              # Relaxation time constant (facilitation)
            syn_exc.U1         = 0.5   
            syn_exc.e          = 0.0 * mV               # Synapse reversal potential (0.0 mV => excitatory synapse)
        elif conn_type == "NMDA":  
            """
            For values check "Data-driven integration of hippocampal CA1 synaptic 
            physiology in silico" (Ecker et al. 2020)
            """
            syn_exc            = h.NMDA_DynSyn(self.soma(0.5))
            syn_exc.tau_rise   = 3.9 * ms                   
            syn_exc.tau_decay  = self.tau_decay_NMDA * ms   
            syn_exc.e          = 0.0 * mV   
            # No plasticity:
            syn_exc.tau_rec    = 671.0 * ms   
            syn_exc.tau_fac    = 0.1 * ms 
            syn_exc.U1         = 1
        
        self._exc_syns.append(syn_exc)
        
        return self._exc_syns[-1]
        
    def add_inh_synapse(self):
        # For ranges check "Coding of Temporal Information by Activity-Dependent Synapses" (Fuhrmann et al. 2002)
        # For values in hippocampus check "Reconstruction of the Hippocampus" (Romani et al. 2022)
        syn_inh            = h.GABAa_DynSyn(self.soma(0.5)) 
        syn_inh.tau_decay  = 5.94 * ms              
        syn_inh.tau_rec    = 965.0 * ms             
        syn_inh.tau_fac    = 8.6 * ms               
        syn_inh.U1         = 0.16
        syn_inh.e          = -80.0 * mV             
        
        self._inh_syns.append(syn_inh)
        
        return self._inh_syns[-1]
        
    
    def __repr__(self):
        return 'Pyramidal[{}]'.format(self._gid)
    
    
class MyInterneuron:
    
    def __init__(self, gid, x, y, z, spike_detector=True):
        self._gid = gid
        self.cell_type = "inh"
        self.pulse_impedance = 135.0e6 # Ω
        self._setup_morphology()
        self._setup_biophysics()
        self.x = 0.0
        self.y = 0.0
        self.z = 0.0
        self.well = None
        h.define_shape()
        self._set_position(x, y, z)
        
        if spike_detector:
            self._spike_detector = h.NetCon(self.soma(0.5)._ref_v, None, sec=self.soma)
            self.spike_times = h.Vector()
            self._spike_detector.record(self.spike_times)
            
        
        self._ncs       = []
        self._exc_syns  = []
        self._inh_syns  = []
        
        self.output_gids = np.array([], dtype=int)
        
    def _save_currents(self):
        self.soma_i_k = h.Vector().record(self.soma(0.5)._ref_ik)
        self.soma_i_na = h.Vector().record(self.soma(0.5)._ref_ina)
        self.soma_i_pas = h.Vector().record(self.soma(0.5)._ref_i_pas)
    
    def _save_voltage(self):
        self.soma_v = h.Vector().record(self.soma(0.5)._ref_v)
        
    def _get_voltage(self):
        return self.soma_v

    def remove_connections(self):
        # Destroy all NetCon objects
        for nc in self._ncs:
            nc = None  # This deletes the NetCon object
        self._ncs = []  # Clear the list of connections
    
    
    def _setup_morphology(self):
        self.soma = h.Section(name='soma', cell=self)
        self.all = [self.soma]
        self.soma.L = 15.0
        self.soma.diam = 15.0
        
        
    def _set_position(self, x, y, z):
        for sec in self.all:
            for i in range(sec.n3d()):
                sec.pt3dchange(i,
                               x - self.x + sec.x3d(i),
                               y - self.y + sec.y3d(i),
                               z - self.z + sec.z3d(i),
                              sec.diam3d(i))
        self.x, self.y, self.z = x, y, z
        
    def _get_position(self):
        return self.x, self.y, self.z
        
    def _setup_biophysics(self):
        for sec in self.all:
            sec.Ra = 150.0  # Axial resistance in Ω * cm
            sec.cm = 1.0    # Membrane capacitance in μF / cm^2  
                        
        self.soma.insert('pas')            
        self.soma.insert('HH2')
        
        for seg in self.soma:
            
            # From https://modeldb.science/128559?tab=2&file=WDR-Model/interneuron.hoc
            seg.pas.g = 4.2e-5  # Passive conductance in S/cm^2
            seg.pas.e = -65.0    # Leak reversal potential mV
            
            seg.HH2.gnabar  = 0.08  # (S/cm^2)
            seg.HH2.gkbar   = 0.02 # (S/cm^2)
            seg.HH2.vtraub  = -55.0 # (mV) 
        
        
        self.noise_exc          = h.ExpSyn(self.soma(0.5))
        self.noise_exc.tau      = 3.0 * ms          
        self.noise_exc.e        = 0.0 * mV          
            
        self.noise_inh          = h.ExpSyn(self.soma(0.5))
        self.noise_inh.tau      = 6.0 * ms          
        self.noise_inh.e        = -80.0 * mV        
        
        self.stim_syn           = h.ExpSyn(self.soma(0.5))
        self.stim_syn.tau       = 0.2               
        self.stim_syn.e         = 0
        
    
    def add_exc_synapse(self, conn_type="AMPA"):
        """
        For values in hippocampus check "Reconstruction of the Hippocampus" (Romani et al. 2022)
        """
        if conn_type == "AMPA":        
            syn_exc            = h.AMPA_DynSyn(self.soma(0.5))
            syn_exc.tau_decay  = 4.12 * ms              # Decay time constant
            syn_exc.tau_rec    = 410.0 * ms             # Recovery time constant (depression) (Markram et al. 1998)
            syn_exc.tau_fac    = 10.0 * ms              # Relaxation time constant (facilitation) (Markram et al. 1998)
            syn_exc.U1         = 0.23                   # Step increase in U_SE
            syn_exc.e          = 0.0 * mV               # Synapse reversal potential (0.0 mV => excitatory synapse)
            
        elif conn_type == "NMDA":  
            """
            For values check "Data-driven integration of hippocampal CA1 synaptic 
            physiology in silico" (Ecker et al. 2020)
            """
            syn_exc            = h.NMDA_DynSyn(self.soma(0.5))
            syn_exc.tau_rise   = 3.9 * ms     # Rise time constant
            syn_exc.tau_decay  = 148.5 * ms   # Decay time constant
            syn_exc.e          = 0.0 * mV   
            # No plasticity:
            syn_exc.tau_rec    = 410.0 * ms   # 671.0
            syn_exc.tau_fac    = 0.1 * ms 
            syn_exc.U1         = 1
        
        self._exc_syns.append(syn_exc)
        
        return self._exc_syns[-1]
    
        
    def add_inh_synapse(self):
        syn_inh            = h.GABAa_DynSyn(self.soma(0.5))
        syn_inh.tau_decay  = 2.67 * ms          
        syn_inh.tau_rec    = 930.0 * ms         
        syn_inh.tau_fac    = 1.6 * ms           
        syn_inh.U1         = 0.16               
        syn_inh.e          = -80.0 * mV         
        
        self._inh_syns.append(syn_inh)
        
        return self._inh_syns[-1]
        
    def __repr__(self):
        return 'Interneuron[{}]'.format(self._gid)