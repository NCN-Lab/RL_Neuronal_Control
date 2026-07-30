# -*- coding: utf-8 -*-
"""
Worker.py — Implementation of the Worker class for parallel
reinforcement learning training in a simulated neural network environment.

@author: ncn-neuron
"""

from neuron import h
import torch
import torch.multiprocessing as mp
from torch.utils.data import DataLoader, TensorDataset
import numpy as np
import psutil
import os 
from datetime import datetime
import random

import MyUtils
from environments.NbControlEnvironment import NbControlEnvironment
from agents.PPO_adaptable import PPO

# Define Worker class
class Worker(mp.Process):
    def __init__(self, env_params, task_params, worker_id, mode="random", save_dir="", env_class=NbControlEnvironment):
        super(Worker, self).__init__()
        
        torch.set_num_threads(1)
        
        # Use the provided environment class (defaults to NbControlEnvironment)
        self.env            = env_class(env_params, task_params)             
        self.worker_id      = worker_id
        
        # Mode-specific parameters
        self.mode = mode.lower()  # "random", "specialist", "generalist", "efficient_random"
        self.task_params = task_params
        
        if self.mode == "random": 
            self.stim_probability = task_params["stim_probability"]
        
        # RL-specific parameters
        if self.mode in ["specialist", "generalist"]:
            agent_params = {
                 'input_size':                  1 + self.env.nElectrodes,
                 'hidden_layer_size':           task_params["hidden_layer_size"],
                 'output_size':                 len(task_params["action_space_electrodes"]),
                 'actor_lr':                    task_params["actor_lr"], 
                 'critic_lr':                   task_params["critic_lr"],  
                 'gamma':                       task_params["gamma"],
                 'weight_entropy':              task_params["weight_entropy"],
                 'clip_epsilon':                task_params["clip_epsilon"],
                 'ignore_electrodes':           task_params["ignore_electrodes"]
                }

            self.local_agent = PPO(agent_params)
            
        self.action_space_electrodes    = task_params["action_space_electrodes"]
        self.save_dir                   = save_dir
        
        self.buffer_transitions = []
        self.episode_batches    = []
        
        # Tracking
        self.step_counter                   = 0
        self.current_episode                = 0
        self.num_stimulations               = 0
        self.abort_training                 = False
        
    def run(self, nSteps=2560, steps_per_update=512, batch_size=64, nEpochs=10, update_agent=True, nEpisodes=None):
        if nEpisodes != None:
            self.nEpisodes = nEpisodes
        else:
            self.nEpisodes = 1e10
            
        self.train(nSteps=nSteps, steps_per_update=steps_per_update, batch_size=batch_size, 
                   nEpochs=nEpochs, exploration=True, update_agent=update_agent, 
                   save_run=True, save_agent=True)
        return 1
    
    def train(self, nSteps=2560, steps_per_update=512, batch_size=64, nEpochs=10, exploration=True, 
              update_agent=True, save_run=True, save_agent=False):
        
        self.nSteps             = nSteps
        self.steps_per_update   = steps_per_update
        self.batch_size         = batch_size
        self.nEpochs            = nEpochs
        
        if self.save_dir == "":
            run_path = self.save_dir + "runs/" + self.mode + "/"
        else:
            run_path = self.save_dir
            
        if self.mode in ["specialist", "generalist"]:
            if exploration:
                run_path = run_path + "exploration/"
            else:
                run_path = run_path + "exploitation/"
                       
        # Standard Environment Initialization via reset()
        state = self.env.reset()
        if state is None:
            print('Worker {} does not contain a valid environment.'.format(self.worker_id))
            return 0
        
        while self.step_counter < self.nSteps:
            self.current_episode += 1
            self.run_episode(exploration, update_agent=update_agent, save_agent=save_agent)
            
            if self.current_episode == self.nEpisodes:
                break
            
        if save_run and not self.abort_training:    
            os.makedirs(run_path, exist_ok=True)
            filename = run_path + "seed={}_seedNoise={}_EpDuration={}ms_{}.npz"
            filename = filename.format(self.env.seed, self.env.NOISE_SEED, 
                                       self.env.step_duration_ms, 
                                       datetime.now().strftime("%Y%m%d_%H%M%S"))
            self.save_data(filename)
            
        return 1
            
    def run_episode(self, exploration, update_agent=True, save_agent=False):
        
        episode_data = {'s': [], 'a': [], 'log_probs': [], 'r': [], 's_prime': []}
        
        if self.abort_training:
            self.episode_batches.append(episode_data)
            return
        
        # Use soft_reset to start a new episode on the continuous biological timeline
        # This allows environment config changes mid-run without losing the NB warmup history
        state = self.env.soft_reset()
        if state is None:
            # Fallback to hard reset if no state exists
            print('Soft reset failed. Hard reset initialized...')
            state = self.env.reset()
        
        self.num_stimulations   = 0
        
        # Standard RL Interaction Loop
        while not self.env.done:
            
            # 1. Action Selection
            log_prob = None
            if self.mode in ["specialist", "generalist"]:
                action, log_prob = self.local_agent.get_electrode_to_stimulate(state, exploration=exploration) 
            elif self.mode == "random":
                if np.random.random() > self.stim_probability:                
                    action = 0
                else:
                    index = np.random.randint(0, len(self.action_space_electrodes))
                    action = self.action_space_electrodes[index]
            else:
                action = 0
            
            if action > 0:
                self.num_stimulations += 1
                
            # 2. Environment Step
            next_state, reward, done, info = self.env.step(action)
            
            if next_state is None:
                break
                
            self.step_counter += 1
            
            # Track episode stats
            episode_data['s'].append(state)
            episode_data['a'].append(action)                
            episode_data['r'].append(reward)
            episode_data['s_prime'].append(next_state)
            if log_prob is not None:
                episode_data['log_probs'].append(log_prob)
            
            # 3. Store Transition and Rollout Value Updates
            if self.mode in ["specialist", "generalist"]:
                self.buffer_transitions.append((state, action, log_prob, reward, next_state, int(done)))
                
                with torch.no_grad():
                    states_tensor   = torch.tensor(np.array(state)).float()
                    Q               = self.local_agent.critic(states_tensor)
                    probs           = self.local_agent.actor(states_tensor)  
                    
                self.local_agent.critic.values.append(Q.numpy())
                self.local_agent.actor.log_probs.append(np.log(probs.numpy() + 1e-10))
                
            # End of training check
            if self.step_counter >= self.nSteps:
                break
                
            if self.env.current_step == self.env.max_num_steps:
                self.abort_training = True
                self.env.done = True
                
            # 4. Agent Rollout Update
            if self.mode in ["specialist", "generalist"] and update_agent:
                if len(self.buffer_transitions) == self.steps_per_update:
                    self._update_agent(save_agent)
                    
            state = next_state
        
        self.episode_batches.append(episode_data)
        
    def _update_agent(self, save_agent=False):
        """Batch update of the PPO agent based on the rollout buffer"""
        train_transitions = []
        for i, transition in enumerate(self.buffer_transitions):
            s, a, lp, r, sp, d = transition
            train_transitions.append((s, a, lp, r, sp, d))
            
        for epoch in range(self.nEpochs):
            random.shuffle(train_transitions)
            for i in range(0, len(train_transitions), self.batch_size):
                batch = train_transitions[i:i+self.batch_size]  
                states, actions, log_probs, rewards, next_states, dones = zip(*batch)
        
                self.local_agent.update(
                                    np.array(states),
                                    np.array(actions),
                                    np.array(log_probs),
                                    np.array(rewards),
                                    np.array(next_states),
                                    dones)
        
        # Reset rollout buffers
        self.buffer_transitions   = []
        
        if self.mode == "specialist" and save_agent:
            os.makedirs(self.save_dir + "checkpoints/", exist_ok=True)
            save_path = self.save_dir + f"checkpoints/seed={self.env.seed}_"                        
            self.local_agent.save_agent(save_path=save_path)

    def save_data(self, filename):
        data_to_save = {
            "seed"                          : self.env.seed,
            "CELL_CONN_SEED"                : self.env.CELL_CONN_SEED,
            "CELL_PLACE_SEED"               : self.env.CELL_PLACE_SEED,
            "CELL_TYPE_SEED"                : self.env.CELL_TYPE_SEED,
            "NOISE_SEED"                    : self.env.NOISE_SEED,
            "nCells"                        : self.env.nCells,
            "sides"                         : self.env.sides,
            "fraction_inh"                  : self.env.fraction_inh,
            "excCells"                      : self.env.excCells,
            "inhCells"                      : self.env.inhCells,
            "tau_noise"                     : self.env.tau_noise,
            "weight_noise"                  : self.env.weight_noise,
            "noise_randomness"              : self.env.noise_randomness,
            "wee"                           : self.env.wee,
            "wei"                           : self.env.wei,
            "wie"                           : self.env.wie, 
            "sigma_exc"                     : self.env.sigma_exc,
            "sigma_inh"                     : self.env.sigma_inh,
            "nConns_exc"                    : self.env.nConns_exc,
            "nConns_inh"                    : self.env.nConns_inh,
            "dist_exc"                      : self.env.dist_exc,
            "dist_inh"                      : self.env.dist_inh,
            "nElectrodes"                   : self.env.nElectrodes,
            "electrode_radius"              : self.env.electrodes_radius,
            "electrode_spacing"             : self.env.spacing,
            "impedance_lim"                 : self.env.impedance_lim,
            "burst_method"                  : self.env.burst_method,
            "min_spike_interval_ms"         : self.env.min_spike_interval_ms, 
            "all_cell_spikes"               : self.env.all_cell_spikes,
            "all_electrode_spikes"          : self.env.all_electrode_spikes,
            "conn_weights"                  : self.env.conn_weights,
            "threshold_sigma"               : self.env.threshold_sigma,
            "action_space_electrodes"       : self.env.action_space_electrodes,
            "ignore_electrodes"             : self.env.ignore_electrodes,
            "nSteps"                        : self.nSteps,
            "step_duration_ms"              : self.env.step_duration_ms,
            "stim_frequency"                : self.env.stim_frequency,
            "stim_amplitude"                : self.env.stim_amplitude,
            "pulse_duration"                : self.env.pulse_duration,
            "avg_nibi_ms"                   : np.mean(self.env.buffer_nibi_ms) if len(self.env.buffer_nibi_ms) else None,
            "episode_batches"               : self.episode_batches,
            "training_aborted"              : self.abort_training,
            "stim_times_s"                  : self.env.stim_times_s,
            "total_duration_s"              : h.t/1000
            }
        
        if self.mode in ["specialist", "generalist"]:
            data_to_save["hidden_layer_size"]   = self.task_params["hidden_layer_size"]           
            data_to_save["actor_lr"]            = self.task_params["actor_lr"]
            data_to_save["critic_lr"]           = self.task_params["critic_lr"]            
            data_to_save["steps_per_update"]    = self.steps_per_update
            data_to_save["batch_size"]          = self.batch_size
            data_to_save["nEpochs"]             = self.nEpochs
            data_to_save["gamma"]               = self.task_params["gamma"]
            data_to_save["weight_entropy"]      = self.task_params["weight_entropy"]
            data_to_save["clip_epsilon"]        = self.task_params["clip_epsilon"]
            data_to_save["actor_losses"]        = self.local_agent.actor.loss
            data_to_save["actor_log_probs"]     = self.local_agent.actor.log_probs
            data_to_save["critic_losses"]       = self.local_agent.critic.loss
            data_to_save["critic_values"]       = self.local_agent.critic.values
            data_to_save["actor_state_dict"]            = self.local_agent.actor.state_dict()
            data_to_save["critic_state_dict"]           = self.local_agent.critic.state_dict(),
            data_to_save["actor_optimizer_state_dict"]  = self.local_agent.actor_optimizer.state_dict()
            data_to_save["critic_optimizer_state_dict"] = self.local_agent.critic_optimizer.state_dict()

        np.savez(filename, data=data_to_save)
