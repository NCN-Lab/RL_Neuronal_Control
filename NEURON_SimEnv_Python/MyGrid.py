# -*- coding: utf-8 -*-
"""
MyGrid.py — A class for defining a square grid of virtual electrodes
within a NEURON simulation environment.

@author: ncn-neuron
"""

from MyElectrode import MyElectrode
import matplotlib.pyplot as plt
from matplotlib.colors import ListedColormap

import random
import numpy as np


class MyGrid:
    
    def __init__(self, gid, total_electrodes, electrode_radius=15, electrode_spacing=200, impedance_lim=[60000, 90000], sampling_freq=10000, seed=None):
        
        self._gid = gid
        self.total_electrodes = total_electrodes
        self.electrodes = []       
        self._create_grid(total_electrodes, electrode_radius, electrode_spacing, impedance_lim, sampling_freq, seed)
        
        
        
    def _create_grid(self, total_electrodes, electrode_radius, electrode_spacing, impedance_lim, sampling_freq, seed):
        random.seed(seed)
        
        # Calculate the number of electrodes per side of the square grid
        num_electrodes_per_side = int(total_electrodes ** 0.5)
        electrodes = []

        # Generate electrode coordinates for the square grid
        for i in range(num_electrodes_per_side):
            for j in range(num_electrodes_per_side):
                x = (i - (num_electrodes_per_side - 1) / 2) * electrode_spacing
                y = (j - (num_electrodes_per_side - 1) / 2) * electrode_spacing
                
                random_impedance = random.randint(impedance_lim[0], impedance_lim [1])
                electrodes.append(MyElectrode(len(electrodes)+1, x, y, 0, radius=electrode_radius, impedance=random_impedance, sampling_frequency=sampling_freq))
        
        self.electrodes = electrodes
        
#%% Plot Functions 
# =============================================================================
    
    def plot_grid_and_network(self, cells, highlight_exc=[], highlight_inh=[]):
        
        plt.rc('font', size=14, weight='bold')
        
        fig, ax = plt.subplots(figsize=(8,4), dpi=150)
        for e in self.electrodes:
            circle = plt.Circle(e._get_position(), 
                                radius=e._get_radius(),
                                facecolor='#b2b2b2', edgecolor='#666666', 
                                linewidth=0.5)
            plt.gca().add_artist(circle)
              
        for c in cells:
            if c._gid in highlight_exc: 
                ax.scatter(c.x, c.y, marker='v', s=10, color='orange')                
            elif c._gid in highlight_inh:
                ax.scatter(c.x, c.y, s=10, color='cyan')
            elif c.cell_type == "inh":
                ax.scatter(c.x, c.y, s=10, color='#c00000', 
                           edgecolor='#550000', linewidths=0.5) 
            else:
                ax.scatter(c.x, c.y, marker='v', s=10, color='#0070c0', 
                           edgecolor='#000080', linewidths=0.5)
                 

        x0,x1 = ax.get_xlim()
        y0,y1 = ax.get_ylim()
        ax.set_facecolor('#f8e9ec')
        ax.set_aspect(abs(x1-x0)/abs(y1-y0))
        ax.set_xlabel(u'\u03bcm', weight='bold')
        ax.set_ylabel(u'\u03bcm', weight='bold')

        # Add legend manually with only one entry for each cell type
        legend_handles = [plt.Line2D([0], [0], marker='v', color='w', 
                                     markerfacecolor='#0070c0', markeredgecolor='#000080',
                                     markersize=10, markeredgewidth=0.5, label='Excitatory'),
                          plt.Line2D([0], [0], marker='o', color='w', 
                                     markerfacecolor='#c00000', markeredgecolor='#550000', 
                                     markersize=10, markeredgewidth=0.5, label='Inhibitory')]

        ax.legend(handles=legend_handles, loc='center left', bbox_to_anchor=(1, 0.5))
        
        ax.spines['top'].set_linewidth(2)
        ax.spines['right'].set_linewidth(2)
        ax.spines['bottom'].set_linewidth(2)
        ax.spines['left'].set_linewidth(2)
        
        plt.tight_layout()
        
        return fig
    
    def plot_connectivty(self, cells, conn_weights):
        
        plt.rc('font', size=14, weight='bold')
        
        fig, ax = plt.subplots(figsize=(8,4), dpi=150)
              
        for c in cells:
            if c.cell_type == "inh":
                ax.scatter(c.x, c.y, s=10, color='#c00000', 
                           edgecolor='#550000', linewidths=0.5) 
            else:
                ax.scatter(c.x, c.y, marker='v', s=10, color='#0070c0', 
                           edgecolor='#000080', linewidths=0.5)
        
        for c_from in range(conn_weights.shape[0]):
            x_from = cells[c_from].x
            y_from = cells[c_from].y 
            
            for c_to in range(conn_weights.shape[1]):
                
                x_to = cells[c_to].x
                y_to = cells[c_to].y   
                
                if cells[c_from].cell_type == "inh":
                    color_conn = '#c00000'
                    alpha      = 0.03
                else:
                    color_conn = '#000080'
                    alpha      = 0.002
            
                ax.plot([x_from, x_to], [y_from, y_to], color = color_conn, alpha=alpha)
                 

        x0,x1 = ax.get_xlim()
        y0,y1 = ax.get_ylim()
        ax.set_facecolor('#f8e9ec')
        ax.set_aspect(abs(x1-x0)/abs(y1-y0))
        ax.set_xlabel(u'\u03bcm', weight='bold')
        ax.set_ylabel(u'\u03bcm', weight='bold')

        # Add legend manually with only one entry for each cell type
        legend_handles = [plt.Line2D([0], [0], marker='v', color='w', 
                                     markerfacecolor='#0070c0', markeredgecolor='#000080',
                                     markersize=10, markeredgewidth=0.5, label='Excitatory'),
                          plt.Line2D([0], [0], marker='o', color='w', 
                                     markerfacecolor='#c00000', markeredgecolor='#550000', 
                                     markersize=10, markeredgewidth=0.5, label='Inhibitory')]

        ax.legend(handles=legend_handles, loc='center left', bbox_to_anchor=(1, 0.5))
        
        ax.spines['top'].set_linewidth(2)
        ax.spines['right'].set_linewidth(2)
        ax.spines['bottom'].set_linewidth(2)
        ax.spines['left'].set_linewidth(2)
        
        plt.tight_layout()
        
        return fig
    

    def plot_degree_maps(self, cells, connectivity_matrix):
        """
        Plots the electrode grid and cells, coloring cells by their In-Degree and Out-Degree.
        Based on the styling of MyGrid.plot_grid_and_network.
        
        Args:
            grid (MyGrid): The grid instance containing electrodes.
            cells (list): List of cell objects (must have .x and .y attributes).
            connectivity_matrix (np.array): Connectivity matrix (Rows=Source, Cols=Target).
        """
        
        # Calculate Degrees
        #       Sum columns = In-Degree (Inputs), Sum rows = Out-Degree (Outputs)
        in_degree = np.sum(connectivity_matrix, axis=0)
        out_degree = np.sum(connectivity_matrix, axis=1)
        
        # Extract cell positions
        cell_x = [c.x for c in cells]
        cell_y = [c.y for c in cells]
    
        plt.rc('font', size=14, weight='bold')
        fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 5), dpi=150)
        
        # Helper to draw the specific style
        def draw_subplot(ax, degrees, title, cmap):
            for e in self.electrodes:
                circle = plt.Circle(e._get_position(), 
                                    radius=e._get_radius(),
                                    facecolor='#b2b2b2', edgecolor='#666666', 
                                    linewidth=0.5)
                ax.add_artist(circle)
                
            # Draw Cells (Colored by Degree)
            # Using scatter with 'c' argument for mapping values to colors
            sc = ax.scatter(cell_x, cell_y, c=degrees, s=20, cmap=cmap, 
                            edgecolor='none', alpha=0.9)
            
            # Styling and Limits
            ax.autoscale()
            x0, x1 = ax.get_xlim()
            y0, y1 = ax.get_ylim()
            
            ax.set_facecolor('#f8e9ec')
            ax.set_aspect(abs(x1-x0)/abs(y1-y0))
            
            ax.set_xlabel(u'\u03bcm', weight='bold')
            ax.set_ylabel(u'\u03bcm', weight='bold')
            ax.set_title(title, weight='bold', pad=15)
            
            ax.spines['top'].set_linewidth(2)
            ax.spines['right'].set_linewidth(2)
            ax.spines['bottom'].set_linewidth(2)
            ax.spines['left'].set_linewidth(2)
            
            cbar = plt.colorbar(sc, ax=ax, fraction=0.046, pad=0.04)
            cbar.set_label('Number of Connections', weight='bold')
            cbar.outline.set_linewidth(2)
    
        draw_subplot(ax1, in_degree, "In-Degree (Inputs/Integrators)", 'viridis')
        draw_subplot(ax2, out_degree, "Out-Degree (Outputs/Drivers)", 'magma')
    
        plt.tight_layout()
        return fig


    def plot_cell_density(self, cells, radius=40, radius_n=20):
        
        all_x = [c.x for c in cells]
        all_y = [c.y for c in cells]
        
        cells_coordinates = np.array([[c.x, c.y] for c in cells])
        
        # Create grid for contour plot
        x = np.linspace(int(np.min(all_x)-10), int(np.max(all_x)+10), int(0.5*(np.max(all_x) - np.min(all_x) + 20)))
        y = np.linspace(int(np.min(all_y)-10), int(np.max(all_y)+10), int(0.5*(np.max(all_y) - np.min(all_y) + 20)))
        xx, yy = np.meshgrid(x, y)

        overall_density = np.zeros(xx.shape)

        # Compute density around each electrode
        all_masks = []
        for electrode in self.electrodes:
            distances = np.sqrt((xx - electrode.x)**2 + (yy - electrode.y)**2)
            
            # Create a mask for points within the radius
            mask = distances <= radius
            all_masks.append(mask) 

            # Calculate cell counts within the radius for this electrode
            for i, j in zip(*np.where(mask)):
                grid_point      = np.array([xx[i, j], yy[i, j]])
                cell_distances  = np.sqrt(np.sum((cells_coordinates - grid_point)**2, axis=1))
                
                neighbouring_cells  = cells_coordinates[cell_distances <= radius_n]
                density             = len(neighbouring_cells) / (np.pi * radius_n**2)

                overall_density[i, j] += density


        # Normalize the density for better contrast
        norm_density = overall_density / overall_density.max()
        
        plt.rc('font', size=14, weight='bold')        
        plt.figure(figsize=(10, 8))
        cmap = plt.cm.viridis
        new_cmap = cmap(np.linspace(0, 1, 256))
        new_cmap[0, -1] = 0
        new_cmap[1:, -1] = 1
        transparent_cmap = ListedColormap(new_cmap)

        overall_mask    = np.logical_or.reduce(all_masks) 
        masked_density  = np.ma.masked_where(overall_mask == False, norm_density)

        contour = plt.contourf(xx, yy, masked_density, cmap=transparent_cmap, levels=8, alpha=0.8)

        for e in self.electrodes:
            circle = plt.Circle(e._get_position(), 
                                radius=e._get_radius(),
                                color='black', alpha=.3)
            plt.gca().add_artist(circle)

        cb = plt.colorbar(contour)
        cb.set_label(label="Normalized Cell Density", weight='bold')
        plt.title("Cell Density Contours Around Electrodes", weight='bold')
        plt.xlabel("X Coordinate (µm)", weight='bold')
        plt.ylabel("Y Coordinate (µm)", weight='bold')
        # plt.legend()
        plt.show()