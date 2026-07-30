# -*- coding: utf-8 -*-
"""
NetBuilder.py — A collection of functions for building and 
manipulating neuronal networks in a NEURON simulation environment.

@author: ncn-neuron
"""
from neuron import h
import numpy as np
from scipy.stats import multivariate_normal
import matplotlib.pyplot as plt
import math


#%% Building Functions

def place_cells_uniform(cells, centers, sides, seed, cells_per_well=None):
    '''
    places the cells in a 2D plane with uniform distribution across multiple wells.

    Args:
        cells (list): List of cell objects (must have _set_position method).
        centers (list of tuples/lists): Coordinates for the center of each well.
        sides (list of tuples/lists): Side lengths [side_x, side_y] for each well.
        seed (int): Random seed for reproducibility.
        cells_per_well (list of int): Number of cells to allocate to each well.
    '''
    np.random.seed(seed)
    nCells = np.shape(cells)[0]
    
    all_pos = []
    current_cell_index = 0

    if len(centers) == 1:
        cells_per_well = [nCells]
    elif cells_per_well is None or sum(cells_per_well) != nCells:
        raise ValueError("When using multiple centers, 'cells_per_well' must be provided and must sum up to the total number of cells.")

    
    for w in range(len(centers)):
        n_current_well = cells_per_well[w]
        xx = np.random.uniform(centers[w][0] - sides[w][0] / 2, centers[w][0] + sides[w][0] / 2, n_current_well).reshape([n_current_well, 1])
        yy = np.random.uniform(centers[w][1] - sides[w][1] / 2, centers[w][1] + sides[w][1] / 2, n_current_well).reshape([n_current_well, 1])
        pos_well = np.concatenate((xx, yy), axis=1)
        
        for i in range(n_current_well):
            global_cell_index = current_cell_index + i
            # Use the position generated for this specific cell in this well
            cells[global_cell_index]._set_position(pos_well[i, 0], pos_well[i, 1], 0)
        
        all_pos.append(pos_well)
        current_cell_index += n_current_well
        
    final_pos = np.concatenate(all_pos, axis=0) if all_pos else np.array([])
        
    return cells, final_pos



def place_cells_circle(cells, center, max_radius):    
    ''' places the cells in a 2D plane with uniform distribution ''' 
    nCells = np.shape(cells)[0]
    radius = np.random.uniform(0, max_radius, nCells)
    angles = np.random.uniform(0, 2*np.pi, nCells)
    xx = (np.cos(angles)*radius + center[0]).reshape([nCells, 1])
    yy = (np.sin(angles)*radius + center[1]).reshape([nCells, 1])
    pos = np.concatenate((xx, yy), axis = 1)
    for c in range(nCells):
        cells[c]._set_position(pos[c,0],  pos[c,1], 0 ) 
    return cells, pos


def place_cells_neyman_scott(cells, centers, sides, cells_per_well=None,
                             n_clusters=20, cluster_spread=50.0, 
                             min_dist=10.0, max_attempts=1000, seed=None):
    ''' 
    Places cells in a 2D plane with CLUSTERED distribution, 
    ensuring no overlaps (min_dist) and staying within bounds.
    ''' 
    np.random.seed(seed)
    nCells = np.shape(cells)[0]

    if len(centers) == 1:
        cells_per_well = [nCells]
    elif cells_per_well is None or sum(cells_per_well) != nCells:
        raise ValueError("When using multiple centers, 'cells_per_well' must be provided and must sum up to the total number of cells.")

    
    for w in range(len(centers)):
        n_current_well = cells_per_well[w]
        
        # Define Boundaries based on center and sides
        x_min = centers[w][0] - sides[w][0] / 2
        x_max = centers[w][0] + sides[w][0] / 2
        y_min = centers[w][1] - sides[w][1] / 2
        y_max = centers[w][1] + sides[w][1] / 2
        
        # Generate invisible 'Parent' cluster centers
        parents_x = np.random.uniform(x_min, x_max, n_clusters)
        parents_y = np.random.uniform(y_min, y_max, n_clusters)
        
        valid_positions = []
        
        # Iteratively place each cell
        for i in range(n_current_well):
            placed = False
            attempts = 0
            
            while not placed and attempts < max_attempts:
                attempts += 1
                
                # Pick a random cluster parent
                parent_idx = np.random.randint(0, n_clusters)
                
                # Generate candidate around parent (Gaussian)
                cx = parents_x[parent_idx] + np.random.normal(0, cluster_spread)
                cy = parents_y[parent_idx] + np.random.normal(0, cluster_spread)
                
                # --- Boundary Check ---
                if cx < x_min or cx > x_max or cy < y_min or cy > y_max:
                    continue
                    
                # --- Overlap Check ---
                if len(valid_positions) == 0:
                    valid_positions.append([cx, cy])
                    placed = True
                else:
                    # Calculate distance to all previously placed cells
                    # We use a temporary numpy array for fast distance calculation
                    existing_pos = np.array(valid_positions)
                    dist = np.sqrt((existing_pos[:,0] - cx)**2 + (existing_pos[:,1] - cy)**2)
                    
                    if np.min(dist) >= min_dist:
                        valid_positions.append([cx, cy])
                        placed = True

            if not placed:
                print(f"Warning: Could not place cell {i} (Space too crowded).")
                valid_positions.append(valid_positions[-1] if valid_positions else [centers[w][0], centers[w][1]])

        pos = np.array(valid_positions)
        for c in range(nCells):
            cells[c]._set_position(pos[c, 0], pos[c, 1], 0)
        
    return cells, pos

def prune_connections(cells, source_ids, target_ids, avg_conns, sigma, hub_strength=0, hub_mode='out'):
    ''' 
    Args:
        hub_mode (str): 'out' = Create Driver cells (High Out-Degree). Best for bursts.
                        'in'  = Create Integrator cells (High In-Degree).
                        'both' = A mix (not implemented in this simple snippet, defaults to out).
    '''
    
    connectivity_matrix = np.ones((len(source_ids), len(target_ids)))
    prune_prob_matrix   = np.zeros((len(source_ids), len(target_ids)))
    
    # We need weights for Sources (Out-degree) or Targets (In-degree)
    if hub_strength > 0:
        weights_source = np.ones(len(source_ids))
        weights_target = np.ones(len(target_ids))
        
        if hub_mode == 'out':
            weights_source = np.random.lognormal(mean=0, sigma=hub_strength, size=len(source_ids))
        elif hub_mode == 'in':
            weights_target = np.random.lognormal(mean=0, sigma=hub_strength, size=len(target_ids))
    else:
        weights_source = np.ones(len(source_ids))
        weights_target = np.ones(len(target_ids))


    pos_source = np.array([[c.x, c.y, c.z] for c in [cells[i] for i in source_ids]])
    pos_target = np.array([[c.x, c.y, c.z] for c in [cells[i] for i in target_ids]])  

    # shapes: (N_src, 1, 3) - (1, N_tgt, 3) -> (N_src, N_tgt, 3) -> norm -> (N_src, N_tgt)
    diff = pos_source[:, np.newaxis, :] - pos_target[np.newaxis, :, :]
    dist_matrix = np.linalg.norm(diff, axis=2)
    base_prob_matrix = dist_matrix / sigma # base probability based on Gaussian distance
    
    # Divide prob by weights. High weight = Low Prune Prob = Connection kept.
    # reshape weights_source to (N, 1) to broadcast across rows
    # reshape weights_target to (1, N) to broadcast across columns
    prune_prob_matrix = base_prob_matrix / (weights_source[:, np.newaxis] * weights_target[np.newaxis, :])
    
    # Handle Self-Connections (Diagonal)
    for i, s_id in enumerate(source_ids):
        if s_id in target_ids:
            j = target_ids.index(s_id)
            connectivity_matrix[i, j] = 0
            prune_prob_matrix[i, j] = 0

    
    # Normalize and Prune
    total_conns = len(source_ids) * len(target_ids)
    target_conns = int(len(source_ids) * avg_conns)    
    prune_matrix_normalized = prune_prob_matrix / np.sum(prune_prob_matrix)
    flattened_conn = connectivity_matrix.flatten()
    flattened_prob = prune_matrix_normalized.flatten()    
    n_to_prune = int(np.sum(flattened_conn) - target_conns)    
    if n_to_prune > 0:
        prune_indices = np.random.choice(
            np.arange(flattened_conn.size), 
            size=n_to_prune, 
            replace=False, 
            p=flattened_prob
        )
        flattened_conn[prune_indices] = 0
    
    return flattened_conn.reshape(connectivity_matrix.shape)

def plot_degree_distribution(matrix):
    """
    Plots the distribution of incoming (In-Degree) and outgoing (Out-Degree) 
    connections from the environment's connectivity matrix.
    """

    # Calculate Degrees
    # Sum of each column = Total inputs TO a specific cell
    in_degrees = np.sum(matrix, axis=0) 
    
    # Sum of each row = Total outputs FROM a specific cell
    out_degrees = np.sum(matrix, axis=1)

    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(14, 5))

    # --- In-Degree (Target) ---
    ax1.hist(in_degrees, bins=30, color='#1f77b4', edgecolor='black', alpha=0.7)
    ax1.set_title("In-Degree Distribution\n(Inputs per Cell)", weight='bold')
    ax1.set_xlabel("Number of Incoming Connections")
    ax1.set_ylabel("Count (Cells)")
    mean_in = np.mean(in_degrees)
    ax1.axvline(mean_in, color='red', linestyle='dashed', linewidth=1, label=f'Mean: {mean_in:.1f}')
    ax1.legend()

    # --- Out-Degree (Source) ---
    ax2.hist(out_degrees, bins=30, color='#ff7f0e', edgecolor='black', alpha=0.7)
    ax2.set_title("Out-Degree Distribution\n(Outputs per Cell)", weight='bold')
    ax2.set_xlabel("Number of Outgoing Connections")
    ax2.set_ylabel("Count (Cells)")

    # Styling
    for ax in [ax1, ax2]:
        ax.spines['top'].set_visible(False)
        ax.spines['right'].set_visible(False)
        ax.grid(axis='y', alpha=0.3)

    plt.tight_layout()
    plt.show()
    
def set_connecting_cells_gauss(pos, source_ids, target_ids, sigma, nConns_per_cell, seed):
    ''' Returns a matrix with the ids of the cells (columns) connected to each cell (lines). 
    The probability of connecting with the neighboring cell is defined by a 2D Gaussian centered in each cell with sigma "sigma"
    NOTE: This function does not specify whether the connections are inputs (convergent connectivity) or 
    outputs(divergent connectivity)'''    
    nCells = len(source_ids)
    cell_Conns = np.zeros((nCells,nConns_per_cell))
    
    for n, c in enumerate(source_ids):
        aux_target_ids = [i for i in target_ids if i != c]
        cell_Conns[n,:] = connect_gauss_dist(pos[aux_target_ids,:], pos[c,:], sigma, nConns_per_cell, aux_target_ids, seed)          
    return cell_Conns.astype(int)


def connect_gauss_dist(all_target_positions, source_position, sigma, nConns_per_cell, all_target_cells_ids, seed):
    ''' Returns the ids of target cells connected to the source cell. Prob of connections follow a gaussian distribution
    with distance, with sigma [sigma, sigma] (by now is 2D.....not sure if i'll need 3D...')
    '''
    np.random.seed(seed)
    
    # Get probability of connecting to each neuron
    probs = multivariate_normal(mean = source_position, cov=[sigma, sigma], seed=seed).pdf(all_target_positions)
    norm_prob = probs/sum(probs) # normalize gaussian probability density to SUM = 1
        
    # Choose random connection according to probability:         
    conn_target_cells_ids = np.random.choice(all_target_cells_ids, nConns_per_cell, p=norm_prob, replace = False )        
    return conn_target_cells_ids


def distance_between_cells(cell_1, cell_2):
    ''' Calculates 3D distance between cell_1 and cell_2'''    
    dist = math.sqrt( (cell_1.x - cell_2.x)**2 + (cell_1.y - cell_2.y)**2 + (cell_1.z - cell_2.z)**2 )
    return dist


def set_cell_synapses_linear_decay(source_cell, target_cells, conn_weight, syn_vel):
    ''' 
    sets the synapse between source_cell and target cells, with weight conn_weight and velocity syn_vel in um/us
    source_cell: type MyCell
    tagert_cell: list of MyCell's --> NOTE: if you only have 1 target cell do a list of 1 element: [target_cell]
    
    all synapses from source_cell to target_cell have the same weigth, but different delays depending on the distance
    '''
    nTargets = len(target_cells)
    for i in range(nTargets):
        dist = distance_between_cells(source_cell, target_cells[i])
        conn_delay = syn_vel * dist
        
        connect_cell_pair(source_cell, target_cells[i], conn_weight, conn_delay)


def connect_cell_pair(source, target, conn_weight, conn_delay):
    ''' Create synapse to connect cell pair source-target, with weight conn_weight and delay conn_delay'''
    
    if source.cell_type == 'exc':
        
        if target.cell_type == 'inh':
            target.add_exc_synapse(conn_type='AMPA')
            nc = h.NetCon(source.soma(0.5)._ref_v, target._exc_syns[-1], sec=source.soma)
            nc.weight[0] = conn_weight # 1e-99
            nc.delay = conn_delay
            source._ncs.append(nc) 
                 
        else:
            target.add_exc_synapse(conn_type='AMPA')
            nc = h.NetCon(source.soma(0.5)._ref_v, target._exc_syns[-1], sec=source.soma)
            nc.weight[0] = conn_weight # 1e-99
            nc.delay = conn_delay
            source._ncs.append(nc)  
        
            target.add_exc_synapse(conn_type='NMDA')
            nc = h.NetCon(source.soma(0.5)._ref_v, target._exc_syns[-1], sec=source.soma)
            nc.weight[0] = conn_weight / 1.645 # 1.645
            nc.delay = conn_delay
            source._ncs.append(nc)      
    else:    
        target.add_inh_synapse()
        nc = h.NetCon(source.soma(0.5)._ref_v, target._inh_syns[-1], sec=source.soma)
        nc.weight[0] = -conn_weight
        nc.delay = conn_delay
        source._ncs.append(nc)  



def connect_cells(cells, cell_Conns, conn_weigths, conn_delays, type='div'):   
    ''' Connect cells according to connections in cell_Conns, with weights conn_weights and delays
    conn_delays, with Divergent (connections are outputs) or Convergent (connections are inputs) connectivity
    
    cell_Conns: [N_cells, N_conns_per_cell] matrix defining which neurons (along columns) connect to each neuron (along lines)    
    '''
    nCells_to_connect = len(cell_Conns)
    
    for i in range(nCells_to_connect):        
        cell_i = cells[ i ]
        
        conn_ind = np.where(cell_Conns[i,:] == 1)[0]
        for j in conn_ind:                  
           cell_j = cells[ j ]
           
           if type == 'div':
               source = cell_i
               target = cell_j
           elif type == 'conv':
               source = cell_j
               target = cell_i
           else:
               print('Unknown Connectivity Type!')
               
           connect_cell_pair(source, target, conn_weigths[i,j], conn_delays[i,j])
  
    
  
    
#%% Plot Functions 
# =============================================================================

       
def plot_cells(pos, cells_colors = [[1, 0, 0]], ax = None):
    '''Plots all cells in the positions pos with colors cell_colors. 
       pos is a numpy array [nCells x 3] with the positions of each cell
       cells_colors is a numpy array [nCells x 3] or [nCells x 4] with the colors of each cell'''
    nCells = pos.shape[0]    
    
    if ax == None:
        fig1, ax = plt.subplots()
        
    for c in range(nCells):
        
        if len(cells_colors) > 1:
            color = cells_colors[c,:]
        else:
            color = cells_colors
            
        ax.scatter(pos[c,0], pos[c,1], s=10, color = color) 


def plot_cells_conns(pos, cell_Conns, nCells_to_connect, nConns_per_cell, conn_type, cells_colors, alpha=1):
    nCells = pos.shape[0]    
    
    fig, ax = plt.subplots(figsize=(10,6), dpi=150)
        
    for c in range(nCells):
        ax.scatter(pos[c,0], pos[c,1], s=10, color = cells_colors[c,:]) 
        ax.axis('equal')  
        
    #ax.xlim(min_pos-0.05*max_pos, max_pos+0.05*max_pos)
    #ax.ylim(min_pos-0.05*max_pos, max_pos+0.05*max_pos)
    
    
    for c_from in range(nCells_to_connect ):
        x_from = pos[c_from,0]
        y_from = pos[c_from,1]    
    
        ax.scatter(x_from, y_from, s = 20, color = cells_colors[c_from, :])
        
        for c_to in range(nConns_per_cell):
            conn_to = int(cell_Conns[c_from,c_to])
            x_to = pos[conn_to,0]
            y_to = pos[conn_to,1]    
        
            ax.plot([x_from, x_to], [y_from, y_to], color = cells_colors[c_from, :])


    if conn_type == 'conv':
        ax.set_title('Convergent - Connections are cell inputs')
    else:
        ax.set_title('Divergent - Connections are cell outputs') 
        
    return fig
  
    
def plot_conn_distances(dist_exc, dist_inh, bin_width=25):
    
      plt.rc('font', size=18, weight='bold')

      fig, ax = plt.subplots(figsize=(12,6))
      bins    = np.arange(0, np.max(dist_exc+dist_inh)+bin_width, bin_width)
      ax.hist(dist_exc, bins, alpha=0.5, color='blue', label='Excitatory')
      ax.hist(dist_inh, bins,  alpha=0.5, color='red', label='Inhibitory')
      plt.xlabel('Distance ($\mu$m)',  weight='bold')
      plt.ylabel('Number of connections',  weight='bold')
      plt.legend(loc='upper right')
      ax.spines['top'].set_visible(False)
      ax.spines['right'].set_visible(False)
      ax.spines['bottom'].set_linewidth(2)
      ax.spines['left'].set_linewidth(2)
      
      return fig
      