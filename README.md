# General Information

This repository contains the code supporting the manuscript

"Closed-loop control of in vitro neuronal activity using reinforcement learning after in silico pre-training"

Eduardo Carvalho, José Mateus, Ricardo Pinto, Miguel Aroso, & Paulo Aguiar (*)

(*) Correspondence to: eduardoc@i3s.up.pt, pauloaguiar@i3s.up.pt

Preprint: https://doi.org/10.64898/2026.07.13.738298

---

***The following computational tools are provided:***

***a)*** A biophysically detailed computational model to recreate the activity of primary hippocampal neuronal networks, developed in NEURON simulation environment.
This model allows RL agents for neuronal activity control to be pre-trained in silico.
Code is available in folder ***NEURON_SimEnv_Python***. You need NEURON simulation environment installed ( https://www.neuronsimulator.org/ ) to run this code. Example code is provided (see specific Readme.md file), with execution time of a few minutes.

***b)*** A C# application for real-time control of the MCS-MultiChannel Systems MEA2100 electrophysiology hardware
The MCS-MultiChannel Systems MEA2100 is one of the most widely used in vitro MEA electrophysiology hardware. The application we developed allows real-time closed-loop control of the system. Namely, it provides a versatile environment where neuronal activity is continuously monitored and stimulation parameters are adapted accordingly. Its usefulness goes well beyond the context of this manuscript.
Code is available in folder ***MEA_ClosedLoop_Control_C#***. Naturally, you need an MCS MEA2100 system to test this application.

***Both folders contain a specific Readme.md file with detailed instructions on how to run the code and examples.***


After peer-review, these applications/code will be publically available at the NCN Lab's GitHub page: https://github.com/NCN-Lab?tab=repositories

As it is common practice in all our publications with a computational component, ***all data, models and code will be openly available and in accordance with FAIR data principles***.

---

Questions/Support regarding installing and running the code: NCN Lab, pauloaguiar@i3s.up.pt
