TITLE Ornstein-Uhlenbeck Conductance Noise
:
: Injects biophysically realistic synaptic noise using an Ornstein-Uhlenbeck (OU) process.
:
: The fluctuating conductance is governed by:
:     dg/dt = -g/tau + sqrt(2 * sigma^2 / tau) * N(0,1)
:     i(t) = g(t) * (v - e)
:
: PARAMETERS:
:     g_mean - mean synaptic conductance (nS)
:     g_std  - standard deviation (nS)
:     tau    - correlation time constant (ms)
:     e      - synaptic reversal potential (mV)
:
: This mechanism mimics excitatory or inhibitory background activity
: and supports shunting effects via voltage-dependent current injection.
:
: Suitable for in vivo-like balanced background input models.
:
: Created by Eduardo Carvalho, 2025
: Inspired by conductance noise models from Destexhe & Pare (1999)

NEURON {
    POINT_PROCESS OU_Conductance
    RANGE g, g_mean, g_std, tau, e, i
    NONSPECIFIC_CURRENT i
}

UNITS {
    (nS) = (nanosiemens)
    (mV) = (millivolt)
    (nA) = (nanoamp)
}

PARAMETER {
    g_mean = 0.0 (nS)    : Mean conductance
    g_std  = 0.5 (nS)    : Standard deviation (noise amplitude)
    tau    = 5.0 (ms)    : Correlation time constant
    e      = 0.0 (mV)    : Reversal potential
}

ASSIGNED {
    v (mV)
    i (nA)
    g (nS)
    dt (ms)
}

STATE {
    x (nS)
}

INITIAL {
    x = 0
}

BREAKPOINT {
    SOLVE state METHOD euler
    g = x + g_mean
    i = g * (v - e)
}

DERIVATIVE state {
    x' = (-x + normrand(0, g_std * sqrt(2 / tau))) / tau
}