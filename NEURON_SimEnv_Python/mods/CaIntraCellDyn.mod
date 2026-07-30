TITLE decay of internal calcium concentration
:
: Internal calcium concentration due to calcium currents and pump.
: Differential equations.
:
: Simple model of ATPase pump with 3 kinetic constants (Destexhe 92)
:     Cai + P <-> CaP -> Cao + P  (k1,k2,k3)
: A Michaelis-Menten approximation is assumed, which reduces the complexity
: of the system to 2 parameters: 
:       kt = <tot enzyme concentration> * k3  -> TIME CONSTANT OF THE PUMP
:	kd = k2/k1 (dissociation constant)    -> EQUILIBRIUM CALCIUM VALUE
: The values of these parameters are chosen assuming a high affinity of 
: the pump to calcium and a low transport capacity (cfr. Blaustein, 
: TINS, 11: 438, 1988, and references therein).  
:
: Units checked using "modlunit" -> factor 10000 needed in ca entry
:
: VERSION OF PUMP + DECAY (decay can be viewed as simplified buffering)
:
: All variables are range variables
:
:
: This mechanism was published in:  Destexhe, A. Babloyantz, A. and 
: Sejnowski, TJ.  Ionic mechanisms for intrinsic slow oscillations in
: thalamic relay neurons. Biophys. J. 65: 1538-1552, 1993)
:
: Written by Alain Destexhe, Salk Institute, Nov 12, 1992
:
: "The normal resting [Ca2+]i lies in the range of 30 to 200 nM 
: in living cells." (Hille 2001)
: Parameter changes by Paulo Aguiar and Mafalda Sousa, IBMC, May 2008
: pauloaguiar@fc.up.pt; mafsousa@ibmc.up.pt



INDEPENDENT {t FROM 0 TO 1 WITH 1 (ms)}

NEURON {
	SUFFIX CaIntraCellDyn
	USEION ca READ ica, cai WRITE cai	
        RANGE cai_new, depth, cai_inf, cai_tau
}

UNITS {
	(molar) = (1/liter)		: moles do not appear in units
	(mM)	= (millimolar)
	(um)	= (micron)
	(mA)	= (milliamp)
	(msM)	= (ms mM)
	FARADAY = (faraday) (coulomb)
}


PARAMETER {
	depth	= 0.1	  (um)		: depth of shell
	cai_tau	= 2.0     (ms)		: rate of calcium removal
	cai_inf	= 50.0e-6 (mM)		: equilibrium intracellular calcium concentration
	cai		  (mM)
}

STATE {
	cai_new		(mM) 
}

INITIAL {

	cai_new = cai_inf
}

ASSIGNED {
	ica		(mA/cm2)
	drive_channel	(mM/ms)
}
	
BREAKPOINT {
	SOLVE state METHOD derivimplicit
}

DERIVATIVE state { 

	drive_channel =  - (10000) * ica / (2 * FARADAY * depth)
	if (drive_channel <= 0.) { drive_channel = 0.  }   : cannot pump inward 
         
	cai_new' = drive_channel + (cai_inf-cai_new)/cai_tau
	cai = cai_new
}
