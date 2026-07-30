TITLE Ornstein-Uhlenbeck Gaussian Current Noise
:
: Injects stochastic Gaussian current based on the Ornstein-Uhlenbeck (OU) process.
:
: Models intrinsic membrane noise or fluctuating input from distant sources.
: Compatible with fixed and variable time step integration (e.g., CVODE).
:
: The injected current follows:
:     dx/dt = -x/tau + sqrt(2 * sigma^2 / tau) * N(0,1)
:     i(t) = x + mu
:
: PARAMETERS:
:     mu     - mean injected current (nA)
:     sigma  - standard deviation (nA)
:     tau    - correlation time constant (ms)
:
: This implementation assumes additive current noise, independent of membrane potential.
: 
: Created by Eduardo Carvalho, 2025
: Based on standard OU formulations in computational neuroscience (e.g., Destexhe et al.)

NEURON {
    POINT_PROCESS OU_Noise
    RANGE mu, sigma, tau, i
    NONSPECIFIC_CURRENT i
}

UNITS {
    (nA) = (nanoamp)
}

PARAMETER {
    mu = 0     (nA)    : Mean current
    sigma = 0.2 (nA)   : Noise amplitude
    tau = 5    (ms)    : Correlation time (OU time constant)
}

ASSIGNED {
    i (nA)
    dt (ms)
}

STATE {
    x
}

INITIAL {
    x = 0
}

BREAKPOINT {
    SOLVE state METHOD euler
    i = x + mu
}

DERIVATIVE state {
    x' = (-x + normrand(0, sigma * sqrt(2 / tau))) / tau
}