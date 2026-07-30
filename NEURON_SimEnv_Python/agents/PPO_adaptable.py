#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
PPO_adaptable.py — Implements the Proximal Policy Optimization (PPO)
reinforcement learning algorithm with actor-critic architecture,
action masking, and support for customizable training configurations.

@author: ncn-neuron
"""

import torch
import torch.nn as nn
import torch.optim as optim
import numpy as np
import torch.nn.init as init
import random
import os
from datetime import datetime


class Actor(nn.Module):
    def __init__(self,
                 input_size: int,
                 hidden_sizes,
                 output_size: int,
                 n_hidden: int | None = None):
        super().__init__()

        # ------------- Build hidden stack -------------
        if isinstance(hidden_sizes, int):
            if n_hidden is None:
                raise ValueError("When `hidden_sizes` is an int "
                                 "you must pass `n_hidden`.")
            hidden_sizes = [hidden_sizes] * n_hidden

        layers = []
        in_features = input_size
        for h in hidden_sizes:
            layers.append(nn.Linear(in_features, h))
            layers.append(nn.Mish())          
            in_features = h

        self.hidden = nn.Sequential(*layers)  # single container

        # ------------- Output layer -------------
        self.out = nn.Linear(in_features, output_size)
        self.softmax = nn.Softmax(dim=-1)

        # ------------- Init weights -------------
        self.apply(self._init_weights)

        self.log_probs          = []
        self.loss               = []

    # -------------------------------------------------
    def forward(self, x, mask=None):
        x = self.hidden(x)
        logits = self.out(x)

        if mask is not None:                  # mask invalid actions
            logits = logits.masked_fill(mask == 0, float("-inf"))

        return self.softmax(logits)

    # -------------------------------------------------
    @staticmethod
    def _init_weights(m):
        if isinstance(m, nn.Linear):
            init.xavier_normal_(m.weight)
            if m.bias is not None:
                init.constant_(m.bias, 0.0)
                
     
class Critic(nn.Module):
    def __init__(self,
                 input_size: int,
                 hidden_sizes,
                 output_size: int,
                 n_hidden: int | None = None):
        super().__init__()

        # ---- build hidden stack -------------------------------------------
        if isinstance(hidden_sizes, int):
            if n_hidden is None:
                raise ValueError("If hidden_sizes is int you must set n_hidden.")
            hidden_sizes = [hidden_sizes] * n_hidden

        layers = []
        in_f = input_size
        for h in hidden_sizes:
            layers += [nn.Linear(in_f, h), nn.Mish()]
            in_f = h

        self.hidden = nn.Sequential(*layers)
        self.q_head = nn.Linear(in_f, output_size)
        
        self.Q              = []
        self.Q_next         = []
        self.advantages     = []
        self.values         = []
        self.loss           = []

        self.apply(self._init_weights)

    # ----------------------------------------------------------------------
    def forward(self, x):
        x = self.hidden(x)
        return self.q_head(x)   # shape: (batch, nActions)

    @staticmethod
    def _init_weights(m):
        if isinstance(m, nn.Linear):
            init.xavier_normal_(m.weight)
            init.constant_(m.bias, 0.0)

class PPO:
    def __init__(self, agent_params):
        
        self.input_size         = agent_params["input_size"]
        self.hidden_layer_size  = agent_params["hidden_layer_size"]
        self.output_size        = agent_params["output_size"]

        # Action masking
        self.mask_logits        = np.ones(self.output_size, dtype=bool)
        for e in agent_params["ignore_electrodes"]:
            self.mask_logits[e] = False
        self.mask_logits = torch.tensor(self.mask_logits, dtype=bool)
        
        self.actor              = Actor(self.input_size, self.hidden_layer_size, self.output_size, n_hidden=1)
        self.critic             = Critic(self.input_size, self.hidden_layer_size, self.output_size, n_hidden=1)   
        
        self.gamma              = agent_params["gamma"]
        self.weight_entropy     = agent_params["weight_entropy"]
        self.clip_epsilon       = agent_params["clip_epsilon"]
        
        self.actor_lr           = agent_params["actor_lr"]
        self.critic_lr          = agent_params["critic_lr"]
        
        self.actor_optimizer = optim.Adam(self.actor.parameters(), lr=self.actor_lr)
        self.critic_optimizer = optim.Adam(self.critic.parameters(), lr=self.critic_lr)
        
    def get_electrode_to_stimulate(self, state, exploration=False):
        
        with torch.no_grad():
            probs = self.actor(torch.tensor(state, dtype=torch.float32), mask=self.mask_logits)        
        
        probs       = probs.numpy()           
        log_probs   = np.log(probs + 1e-10)
        
        if exploration:        
            random.seed(None)
            stim_electrode = np.random.choice(range(len(probs)), p=probs)
        else:
            stim_electrode = np.argmax(probs)
                
        return stim_electrode, log_probs[stim_electrode]

    def update(self, states, actions, old_log_probs, rewards, next_states, dones,
               optimizer_step=True, print_info=False):
        
        states_tensor       = torch.tensor(np.array(states)).float()
        actions_tensor      = torch.tensor(actions, dtype=torch.long)
        rewards_tensor      = torch.tensor(rewards).float()
        next_states_tensor  = torch.tensor(np.array(next_states)).float()
        dones_tensor        = torch.tensor(dones, dtype=torch.float32)
        
        with torch.no_grad():
            all_Q       = self.critic(states_tensor)
            probs_curr  = self.actor(states_tensor)
            V_curr      = torch.sum(all_Q * probs_curr, dim=1)
        
            if self.gamma > 0:
                all_Q_next  = self.critic(next_states_tensor)        
                probs_next  = self.actor(next_states_tensor)         
                V_next      = torch.sum(all_Q_next * probs_next, dim=1)      
                targets     = rewards_tensor + self.gamma * (1 - dones_tensor) * V_next.detach()      
            else:
                targets = rewards_tensor  
            
        advantages = targets - V_curr
        advantages = (advantages - advantages.mean()) / (advantages.std() + 1e-8)

        # Actor update
        probs = self.actor(states_tensor)
        dist = torch.distributions.Categorical(probs)
        log_probs = dist.log_prob(actions_tensor)

        ratio = torch.exp(log_probs - torch.tensor(old_log_probs, dtype=torch.float32))
        surr1 = ratio * advantages
        surr2 = torch.clamp(ratio, 1.0 - self.clip_epsilon, 1.0 + self.clip_epsilon) * advantages
        policy_loss = -torch.min(surr1, surr2).mean()
        entropy = dist.entropy().mean()

        if optimizer_step:
            self.actor_optimizer.zero_grad()
            (policy_loss - self.weight_entropy * entropy).backward()
            self.actor_optimizer.step() 

        # Critic update
        all_Q_pred = self.critic(states_tensor)
        Q_pred = all_Q_pred[range(len(actions_tensor)), actions_tensor]

        value_loss = nn.MSELoss()(Q_pred, targets)

        if optimizer_step:
            self.critic_optimizer.zero_grad()
            value_loss.backward()
            self.critic_optimizer.step() 
            
        if print_info:
            print(f"Batch: {len(states)} | Actor Loss: {policy_loss.item():.4f} | Critic Loss: {value_loss.item():.4f}")
            print(f"Mean Advantage: {advantages.mean().item():.4f}, Std: {advantages.std(unbiased=False).item():.4f}")
            print(f"Entropy: {entropy.item():.4f}")
            
        self.actor.loss.append(policy_loss.item())        
        self.critic.loss.append(value_loss.item())
            
    def save_agent(self, save_path=None):
        
        if save_path is None:
            save_path = "rl/trained_PPO/"
            os.makedirs(save_path, exist_ok=True)
        
        filename = save_path + "{}.pth".format(datetime.now().strftime("%Y%m%d_%H%M%S"))
        torch.save({'actor_state_dict': self.actor.state_dict(),
                    'critic_state_dict': self.critic.state_dict(),
                    'actor_optimizer_state_dict': self.actor_optimizer.state_dict(),
                    'critic_optimizer_state_dict': self.critic_optimizer.state_dict()
                    }, filename)
        
    def load_agent(self, filename):
        checkpoint = torch.load(filename)
        self.actor.load_state_dict(checkpoint['actor_state_dict'])
        self.critic.load_state_dict(checkpoint['critic_state_dict'])
            