TITLE  NMDA receptor with Ca influx and pre-synaptic short-term plasticity


COMMENT
Dynamic presynaptic activity based on Fuhrmann et al, 2002: "Coding of temporal information by activity-dependent synapses" 

Written by Paulo Aguiar and Mafalda Sousa, IBMC, May 2008
pauloaguiar@fc.up.pt ; mafsousa@ibmc.up.pt
ENDCOMMENT


NEURON {
	POINT_PROCESS NMDA_DynSyn
	USEION ca WRITE ica	
	USEION mg READ mgo VALENCE 2
	RANGE tau_rise, tau_decay
	RANGE U1, tau_rec, tau_fac
	RANGE i, g, e, mg, inon, ica, ca_ratio
	NONSPECIFIC_CURRENT inon
    }
    
UNITS {
	(nA) = (nanoamp)
	(mV) = (millivolt)
	(molar) = (1/liter)
	(mM) = (millimolar)
    }    
    
    PARAMETER {
  	tau_rise  = 5.0   (ms)  : dual-exponential conductance profile
	tau_decay = 70.0  (ms)  : IMPORTANT: tau_rise < tau_decay
	U1        = 1.0   (1)   : The parameter U1, tau_rec and tau_fac define
	tau_rec   = 0.1   (ms)  : the pre-synaptic SP short-term plasticity
	tau_fac   = 0.1   (ms)  : mechanism (see Fuhrmann et al, 2002)
	e         = 0.0   (mV)  : synapse reversal potential
	mgo		  = 1.0   (mM)  : external magnesium concentration
	ca_ratio  = 0.1   (1)   : ratio of calcium current to total current( Burnashev/Sakmann J Phys 1995 485 403-418)
    }
    
    
ASSIGNED {
	v		(mV)
	i		(nA)
	g		(umho)
	factor	(1)
	ica		(nA)
	inon	(nA)
}

STATE {
	A
	B
}

INITIAL{
	LOCAL tp
	A = 0
	B = 0
	tp = (tau_rise*tau_decay)/(tau_decay-tau_rise)*log(tau_decay/tau_rise)
	factor = -exp(-tp/tau_rise)+exp(-tp/tau_decay)
	factor = 1/factor
}

BREAKPOINT {
	SOLVE state METHOD cnexp
	g = B-A
	i = g*mgblock(v)*(v-e)
	ica = ca_ratio*i
	inon = (1-ca_ratio)*i
	:printf("\nt=%f\tinon=%f\tica=%f\ti=%f\tmgb=%f",t, inon, ica, i, mgblock(v))
}

DERIVATIVE state{
	A' = -A/tau_rise
	B' = -B/tau_decay
}

FUNCTION mgblock(v(mV)) {
	: from Jahr & Stevens 1990
	mgblock = 1 / (1 + exp(0.062 (/mV) * -v) * (mgo / 3.57 (mM)))
}

NET_RECEIVE (weight, Pv, P, Use, t0 (ms)){
	INITIAL{
		P=1
		Use=0
		t0=t
	}	

	Use = Use * exp(-(t-t0)/tau_fac)
	Use = Use + U1*(1-Use) 
	P = 1-(1- P) * exp(-(t-t0)/tau_rec)
	Pv= Use * P
	P = P - Use * P
	
	t0=t
	
	A=A + weight*factor*Pv
	B=B + weight*factor*Pv
}

