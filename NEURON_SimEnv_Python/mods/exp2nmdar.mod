
TITLE dual-exponential model of NMDA receptors

COMMENT

This provides a simple dual-exponential model of an NMDA
receptor synapse with a Jahr&Stevens Mg++ voltage dependency.

Changes were made by John Baker to the standard exp2syn.mod
file so that the voltage dependency is addressed. The mgblock
code was borrowed from a model by A. Destexhe.

The NMDA receptor is temperature sensitive. Any necesary
adjustment to the time constants should be done by
setting tau1 and tau2 via hoc.

Default values are more or less taken from Dalby and Mody,
J Neurophysiol 90: 786-797, 2003. No strong claims for 
physiological accuracy are made here.

--- (and now back to the original exp2syn comments) ---

Two state kinetic scheme synapse described by rise time tau1,
and decay time constant tau2. The normalized peak condunductance is 1.
Decay time MUST be greater than rise time.

The solution of A->G->bath with rate constants 1/tau1 and 1/tau2 is
 A = a*exp(-t/tau1) and
 G = a*tau2/(tau2-tau1)*(-exp(-t/tau1) + exp(-t/tau2))
	where tau1 < tau2

If tau2-tau1 -> 0 then we have a alphasynapse.
and if tau1 -> 0 then we have just single exponential decay.

The factor is evaluated in the
initial block such that an event of weight 1 generates a
peak conductance of 1.

Because the solution is a sum of exponentials, the
coupled equations can be solved as a pair of independent equations
by the more efficient cnexp method. 

ENDCOMMENT

NEURON {
	POINT_PROCESS Exp2NMDAR
	RANGE tau1, tau2, e, i
	NONSPECIFIC_CURRENT i

	RANGE g
	GLOBAL total

	RANGE mg
}

UNITS {
	(nA) = (nanoamp)
	(mV) = (millivolt)
	(uS) = (microsiemens)
}

PARAMETER {
	tau1=4 (ms) <1e-9,1e9>
	tau2=42 (ms) <1e-9,1e9>
	e=0	(mV)
	mg=1 (mM) : external magnesium concentration
}

ASSIGNED {
	v (mV)
	i (nA)
	g (uS)
	factor
	total (uS)
}

STATE {
	A (uS)
	B (uS)
}

INITIAL {
	LOCAL tp
	total = 0
	if (tau1/tau2 > .9999) {
		tau1 = .9999*tau2
	}
	A = 0
	B = 0
	tp = (tau1*tau2)/(tau2 - tau1) * log(tau2/tau1)
	factor = -exp(-tp/tau1) + exp(-tp/tau2)
	factor = 1/factor

	mgblock(v)
}

BREAKPOINT {
	SOLVE state METHOD cnexp

	g = B - A
	i = g*mgblock(v)*(v - e)
}

DERIVATIVE state {
	A' = -A/tau1
	B' = -B/tau2
}

NET_RECEIVE(weight (uS)) {
	state_discontinuity(A, A + weight*factor)
	state_discontinuity(B, B + weight*factor)
	total = total+weight
}

: The following is borrowed from Destexhe NMDAR model.
FUNCTION mgblock(v(mV)) {
	TABLE 
	DEPEND mg
	FROM -140 TO 80 WITH 1000

	: from Jahr & Stevens

	mgblock = 1 / (1 + exp(0.062 (/mV) * -v) * (mg / 3.57 (mM)))
}







