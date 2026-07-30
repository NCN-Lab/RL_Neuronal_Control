TITLE Hippocampal HH channels
:
: Fast Na+ and K+ currents responsible for action potentials
: Iterative equations
:
: Equations modified by Traub, for Hippocampal Pyramidal cells, in:
: Traub & Miles, Neuronal Networks of the Hippocampus, Cambridge, 1991
:
: range variable vtraub adjust threshold
:
: Written by Alain Destexhe, Salk Institute, Aug 1992
:
: Modifications by Arthur Houweling for use in MyFirstNEURON
: Modifications by Paulo Aguiar: vh changed from 5 to 6 - NOT ANYMORE: vh=5 as originally set

INDEPENDENT {t FROM 0 TO 1 WITH 1 (ms)}

NEURON {
	SUFFIX HH2
	USEION na READ ena WRITE ina
	USEION k READ ek WRITE ik
	RANGE gnabar, gkbar, vtraub
	RANGE m_inf, h_inf, n_inf
	RANGE tau_m, tau_h, tau_n
	RANGE m_exp, h_exp, n_exp
	RANGE ik, ina 
}


UNITS {
	(mA) = (milliamp)
	(mV) = (millivolt)
}

PARAMETER {
	gnabar	= .1 	(mho/cm2)
	gkbar	= .06 	(mho/cm2)

	ena		(mV)
	ek		(mV)
	celsius		(degC)
	dt              (ms)
	v               (mV)
	vtraub	= -55	(mV)	: adjusts threshold
}

STATE {
	m h n
}

ASSIGNED {
	ina	(mA/cm2)
	ik	(mA/cm2)
	il	(mA/cm2)
	m_inf
	h_inf
	n_inf
	tau_m
	tau_h
	tau_n
	m_exp
	h_exp
	n_exp
	tadj
}


BREAKPOINT {
	SOLVE states METHOD cnexp
	ina = gnabar * m*m*m*h * (v - ena)
	ik  = gkbar * n*n*n*n * (v - ek)
}


DERIVATIVE states {   : use this for exact Hodgkin-Huxley equations
	evaluate_fct(v)
	m' = (m_inf - m) / tau_m
	h' = (h_inf - h) / tau_h
	n' = (n_inf - n) / tau_n
}

:PROCEDURE states() {	: this discretized form is more stable
:	evaluate_fct(v)
:	m = m + m_exp * (m_inf - m)
:	h = h + h_exp * (h_inf - h)
:	n = n + n_exp * (n_inf - n)
:	VERBATIM
:	return 0;
:	ENDVERBATIM
:}

UNITSOFF
INITIAL {
:
:  Q10 was assumed to be 3 for both currents
:
	tadj = 3.0 ^ ((celsius-36)/ 10 )
	evaluate_fct(v)
	m= m_inf
	h= h_inf
	n= n_inf
}

PROCEDURE evaluate_fct(v(mV)) { LOCAL a,b,v2,vh

	v2 = v - vtraub : convert to traub convention
	vh = 5
	
	a = 0.32 * (13-v2) / ( exp((13-v2)/4) - 1)
	b = 0.28 * (v2-40) / ( exp((v2-40)/5) - 1)
	tau_m = 1 / (a + b) / tadj
	m_inf = a / (a + b)

	:a = 0.128 * exp((17-v2)/18)
	:b = 4 / ( 1 + exp((40-v2)/5) )
	:tau_h = 1 / (a + b) / tadj
	:h_inf = a / (a + b)
	
	a = 0.128 * exp((17-v2-vh)/18)
	b = 4 / ( 1 + exp((40-v2-vh)/5) )
	tau_h = 1 / (a + b) / tadj
	h_inf = a / (a + b)	
	
	
	a = 0.032 * (15-v2) / ( exp((15-v2)/5) - 1)
	b = 0.5 * exp((10-v2)/40)
	tau_n = 1 / (a + b) / tadj
	n_inf = a / (a + b)

	m_exp = 1 - exp(-dt/tau_m)
	h_exp = 1 - exp(-dt/tau_h)
	n_exp = 1 - exp(-dt/tau_n)
}

UNITSON
