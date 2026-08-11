# General Information

This repository contains the code supporting the manuscript:

> **"Closed-loop control of in vitro neuronal activity using reinforcement learning after in silico pre-training"**
> 
> Eduardo Carvalho, José Mateus, Ricardo Pinto, Miguel Aroso, & Paulo Aguiar (*)
> 
> (*) Correspondence to: eduardoc@i3s.up.pt, pauloaguiar@i3s.up.pt
> 
> **Preprint:** https://doi.org/10.64898/2026.07.13.738298  
> **Raw Data (Zenodo):** [https://doi.org/10.5281/zenodo.21891175](https://doi.org/10.5281/zenodo.21891175)

---
## Highlights: Video Demonstration & Runnable Examples
* **Closed-Loop Demo Video:** A video recording demonstrating the C# desktop application in action during real-time closed-loop hardware control (showing live signal plotting, artifact suppression, online spike detection) is available at:
  👉 **[MEA_ClosedLoop_Control_C#/GUI_ClosedLoop.mp4](./MEA_ClosedLoop_Control_C%23/GUI_ClosedLoop.mp4)**
* **Python Simulation Examples:** Ready-to-run Python examples (execution time of a few minutes) for simulating hippocampal neuronal networks and evaluating pre-trained DRL agents are provided in:
  👉 **[NEURON_SimEnv_Python/examples/](./NEURON_SimEnv_Python/examples)**  
  *(See [NEURON_SimEnv_Python/README.md](./NEURON_SimEnv_Python/README.md) for instructions on running `Test_Baseline.py` and `Test_Generalist.py`)*
---

## 🛠️ Computational Tools & Repository Structure

The repository is organized into two primary computational packages, each containing a dedicated `README.md` file with detailed instructions:

```
RL_Neuronal_Control/
├── NEURON_SimEnv_Python/          # Biophysical computational model (NEURON & Python) for in silico network simulation & RL agent pre-training
│   ├── agents/                    # DRL agent implementations (PPO) & PyTorch <-> TorchSharp weight converters
│   ├── environments/              # RL environment wrappers for NEURON biological simulations
│   ├── trainers/                  # Parallel multiprocessing rollout & training worker orchestrator
│   ├── examples/                  # Example scripts for baseline network tests and pre-trained agent evaluation
│   ├── figures/                   # Output visualization plots (rasters, firing rates, LFP traces)
│   └── mods/                      # NMODL source files (.mod) for synaptic dynamics and ion channels
│
└── MEA_ClosedLoop_Control_C#/     # Real-time closed-loop hardware control application (C# & WinForms)
    ├── GUI/                       # Windows Forms user interface for real-time visualization & hardware control
    ├── MCS_Devices/               # MCS MEA2100 hardware API wrapper, SALPA artifact suppression & spike detection
    ├── RL/                        # TorchSharp DRL engine for real-time closed-loop PPO inference
    ├── ElectrodeFiles/            # MEA electrode layout & pinout mapping configurations
    └── GUI_ClosedLoop.mp4         # Video demonstration of the C# application GUI operating in real-time closed-loop mode
```

---

### 📂 Detailed Folder Descriptions

#### 1. [NEURON_SimEnv_Python](./NEURON_SimEnv_Python)
A biophysically detailed computational model to recreate the activity of primary hippocampal neuronal networks, developed in the **NEURON simulation environment**. This model allows RL agents for neuronal activity control to be pre-trained *in silico*.

* **[agents/](./NEURON_SimEnv_Python/agents)**: Contains Deep RL agent implementations (adaptable PPO agent with action masking) and scripts to serialize PyTorch model weights to JSON format for compatibility with the C#/TorchSharp engine (and vice-versa).
* **[environments/](./NEURON_SimEnv_Python/environments)**: Custom RL environments wrapping NEURON network simulations, handling electrode stimulation, action execution, spike observation, and network burst control reward logic.
* **[trainers/](./NEURON_SimEnv_Python/trainers)**: Multiprocessing parallel rollout worker (`Worker.py`) for collecting experience rollouts across multiple CPU cores.
* **[examples/](./NEURON_SimEnv_Python/examples)**: Example scripts demonstrating baseline network simulation (`Test_Baseline.py`) and evaluating pre-trained generalist PPO agents (`Test_Generalist.py`). Execution time is a few minutes.
* **[figures/](./NEURON_SimEnv_Python/figures)**: Directory housing generated output figures, including raster plots, firing rate distributions, network hub connectivity maps, and LFP traces.
* **[mods/](./NEURON_SimEnv_Python/mods)**: NMODL (`.mod`) biological mechanism definitions (Hodgkin-Huxley ionic channels, AMPA/NMDA/GABA dynamic synapses, intracellular calcium dynamics, and background noise generators).

> **Prerequisite:** Requires the NEURON simulation environment ([https://www.neuronsimulator.org/](https://www.neuronsimulator.org/)). For full installation and execution details, refer to [NEURON_SimEnv_Python/README.md](./NEURON_SimEnv_Python/README.md).

---

#### 2. [MEA_ClosedLoop_Control_C#](./MEA_ClosedLoop_Control_C%23)
A C# application for real-time closed-loop control of the **MCS (Multi Channel Systems) MEA2100** electrophysiology hardware. It continuously monitors neuronal activity and adapts electrical stimulation parameters in real-time.

* **[GUI/](./MEA_ClosedLoop_Control_C%23/GUI)**: Windows Forms frontend application built with ScottPlot for real-time electrophysiology data graphing, electrode mapping display, hardware configuration, and closed-loop monitoring.
* **[MCS_Devices/](./MEA_ClosedLoop_Control_C%23/MCS_Devices)**: Hardware abstraction library managing high-frequency USB data streaming (`MeaDacq.cs`), real-time SALPA electrical artifact suppression (`SalpaCleaner.cs`), online spike detection (`SpikeDetector.cs`), and STG stimulation triggers (`Stimulator.cs`).
* **[RL/](./MEA_ClosedLoop_Control_C%23/RL)**: Reinforcement Learning inference engine utilizing **TorchSharp** (`PPO.cs`) and dedicated background worker threads (`Worker.cs`) for low-latency closed-loop control during live experiments.
* **[ElectrodeFiles/](./MEA_ClosedLoop_Control_C%23/ElectrodeFiles)**: Asset directory containing MEA layout definitions and pinout maps (e.g., 60-well and 252-well plate formats).
* **[GUI_ClosedLoop.mp4](./MEA_ClosedLoop_Control_C%23/GUI_ClosedLoop.mp4)**: Video recording demonstrating the C# desktop application interface during real-time closed-loop operation, featuring live signal streaming, spike detection.

> **Prerequisite:** Requires an MCS MEA2100 electrophysiology system for hardware execution. For setup, assembly, and dependency guidelines, see [MEA_ClosedLoop_Control_C#/README.md](./MEA_ClosedLoop_Control_C%23/README.md).

---

## 📊 Data Availability & Open Science

* **Raw Data (Zenodo):** All raw experimental and simulation datasets are archived on Zenodo: [https://doi.org/10.5281/zenodo.21891175](https://doi.org/10.5281/zenodo.21891175)
* **Source Code:** Following peer-review, all software tools and updates will be publicly maintained on the NCN Lab GitHub: [https://github.com/NCN-Lab](https://github.com/NCN-Lab).
* In accordance with NCN Lab practice, **all data, computational models, and code are open and adhere to FAIR data principles**.

---

## 📧 Support & Contact

For questions or assistance regarding software installation, setup, or running example code:
* **NCN Lab**: [eduardoc@i3s.up.pt](mailto:eduardoc@i3s.up.pt) | [pauloaguiar@i3s.up.pt](mailto:pauloaguiar@i3s.up.pt)

