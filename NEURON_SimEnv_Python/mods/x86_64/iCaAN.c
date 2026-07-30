/* Created by Language version: 7.7.0 */
/* VECTORIZED */
#define NRN_VECTORIZED 1
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include "mech_api.h"
#undef PI
#define nil 0
#include "md1redef.h"
#include "section.h"
#include "nrniv_mf.h"
#include "md2redef.h"
 
#if METHOD3
extern int _method3;
#endif

#if !NRNGPU
#undef exp
#define exp hoc_Exp
extern double hoc_Exp(double);
#endif
 
#define nrn_init _nrn_init__iCaAN
#define _nrn_initial _nrn_initial__iCaAN
#define nrn_cur _nrn_cur__iCaAN
#define _nrn_current _nrn_current__iCaAN
#define nrn_jacob _nrn_jacob__iCaAN
#define nrn_state _nrn_state__iCaAN
#define _net_receive _net_receive__iCaAN 
#define evaluate_fct evaluate_fct__iCaAN 
#define states states__iCaAN 
 
#define _threadargscomma_ _p, _ppvar, _thread, _nt,
#define _threadargsprotocomma_ double* _p, Datum* _ppvar, Datum* _thread, NrnThread* _nt,
#define _threadargs_ _p, _ppvar, _thread, _nt
#define _threadargsproto_ double* _p, Datum* _ppvar, Datum* _thread, NrnThread* _nt
 	/*SUPPRESS 761*/
	/*SUPPRESS 762*/
	/*SUPPRESS 763*/
	/*SUPPRESS 765*/
	 extern double *getarg();
 /* Thread safe. No static _p or _ppvar. */
 
#define t _nt->_t
#define dt _nt->_dt
#define gbar _p[0]
#define gbar_columnindex 0
#define ican _p[1]
#define ican_columnindex 1
#define m_inf _p[2]
#define m_inf_columnindex 2
#define tau_m _p[3]
#define tau_m_columnindex 3
#define m _p[4]
#define m_columnindex 4
#define ecan _p[5]
#define ecan_columnindex 5
#define cai _p[6]
#define cai_columnindex 6
#define Dm _p[7]
#define Dm_columnindex 7
#define tadj _p[8]
#define tadj_columnindex 8
#define v _p[9]
#define v_columnindex 9
#define _g _p[10]
#define _g_columnindex 10
#define _ion_ecan	*_ppvar[0]._pval
#define _ion_ican	*_ppvar[1]._pval
#define _ion_dicandv	*_ppvar[2]._pval
#define _ion_cai	*_ppvar[3]._pval
 
#if MAC
#if !defined(v)
#define v _mlhv
#endif
#if !defined(h)
#define h _mlhh
#endif
#endif
 
#if defined(__cplusplus)
extern "C" {
#endif
 static int hoc_nrnpointerindex =  -1;
 static Datum* _extcall_thread;
 static Prop* _extcall_prop;
 /* external NEURON variables */
 extern double celsius;
 /* declaration of user functions */
 static void _hoc_evaluate_fct(void);
 static void _hoc_states(void);
 static int _mechtype;
extern void _nrn_cacheloop_reg(int, int);
extern void hoc_register_prop_size(int, int, int);
extern void hoc_register_limits(int, HocParmLimits*);
extern void hoc_register_units(int, HocParmUnits*);
extern void nrn_promote(Prop*, int, int);
extern Memb_func* memb_func;
 
#define NMODL_TEXT 1
#if NMODL_TEXT
static const char* nmodl_file_text;
static const char* nmodl_filename;
extern void hoc_reg_nmodl_text(int, const char*);
extern void hoc_reg_nmodl_filename(int, const char*);
#endif

 extern void _nrn_setdata_reg(int, void(*)(Prop*));
 static void _setdata(Prop* _prop) {
 _extcall_prop = _prop;
 }
 static void _hoc_setdata() {
 Prop *_prop, *hoc_getdata_range(int);
 _prop = hoc_getdata_range(_mechtype);
   _setdata(_prop);
 hoc_retpushx(1.);
}
 /* connect user functions to hoc names */
 static VoidFunc hoc_intfunc[] = {
 "setdata_iCaAN", _hoc_setdata,
 "evaluate_fct_iCaAN", _hoc_evaluate_fct,
 "states_iCaAN", _hoc_states,
 0, 0
};
 /* declare global and static user variables */
#define beta beta_iCaAN
 double beta = 0.002;
#define cac cac_iCaAN
 double cac = 0.0005;
#define tau_factor tau_factor_iCaAN
 double tau_factor = 40;
#define taumin taumin_iCaAN
 double taumin = 0.1;
 /* some parameters have upper and lower limits */
 static HocParmLimits _hoc_parm_limits[] = {
 0,0,0
};
 static HocParmUnits _hoc_parm_units[] = {
 "beta_iCaAN", "1/ms",
 "cac_iCaAN", "mM",
 "taumin_iCaAN", "ms",
 "gbar_iCaAN", "mho/cm2",
 "ican_iCaAN", "mA/cm2",
 "tau_m_iCaAN", "ms",
 0,0
};
 static double delta_t = 1;
 static double m0 = 0;
 /* connect global user variables to hoc */
 static DoubScal hoc_scdoub[] = {
 "beta_iCaAN", &beta_iCaAN,
 "tau_factor_iCaAN", &tau_factor_iCaAN,
 "cac_iCaAN", &cac_iCaAN,
 "taumin_iCaAN", &taumin_iCaAN,
 0,0
};
 static DoubVec hoc_vdoub[] = {
 0,0,0
};
 static double _sav_indep;
 static void nrn_alloc(Prop*);
static void  nrn_init(NrnThread*, _Memb_list*, int);
static void nrn_state(NrnThread*, _Memb_list*, int);
 static void nrn_cur(NrnThread*, _Memb_list*, int);
static void  nrn_jacob(NrnThread*, _Memb_list*, int);
 
static int _ode_count(int);
 /* connect range variables in _p that hoc is supposed to know about */
 static const char *_mechanism[] = {
 "7.7.0",
"iCaAN",
 "gbar_iCaAN",
 0,
 "ican_iCaAN",
 "m_inf_iCaAN",
 "tau_m_iCaAN",
 0,
 "m_iCaAN",
 0,
 0};
 static Symbol* _can_sym;
 static Symbol* _ca_sym;
 
extern Prop* need_memb(Symbol*);

static void nrn_alloc(Prop* _prop) {
	Prop *prop_ion;
	double *_p; Datum *_ppvar;
 	_p = nrn_prop_data_alloc(_mechtype, 11, _prop);
 	/*initialize range parameters*/
 	gbar = 0.00025;
 	_prop->param = _p;
 	_prop->param_size = 11;
 	_ppvar = nrn_prop_datum_alloc(_mechtype, 4, _prop);
 	_prop->dparam = _ppvar;
 	/*connect ionic variables to this model*/
 prop_ion = need_memb(_can_sym);
 nrn_promote(prop_ion, 0, 1);
 	_ppvar[0]._pval = &prop_ion->param[0]; /* ecan */
 	_ppvar[1]._pval = &prop_ion->param[3]; /* ican */
 	_ppvar[2]._pval = &prop_ion->param[4]; /* _ion_dicandv */
 prop_ion = need_memb(_ca_sym);
 nrn_promote(prop_ion, 1, 0);
 	_ppvar[3]._pval = &prop_ion->param[1]; /* cai */
 
}
 static void _initlists();
 static void _update_ion_pointer(Datum*);
 extern Symbol* hoc_lookup(const char*);
extern void _nrn_thread_reg(int, int, void(*)(Datum*));
extern void _nrn_thread_table_reg(int, void(*)(double*, Datum*, Datum*, NrnThread*, int));
extern void hoc_register_tolerance(int, HocStateTolerance*, Symbol***);
extern void _cvode_abstol( Symbol**, double*, int);

 void _iCaAN_reg() {
	int _vectorized = 1;
  _initlists();
 	ion_reg("can", 1.0);
 	ion_reg("ca", -10000.);
 	_can_sym = hoc_lookup("can_ion");
 	_ca_sym = hoc_lookup("ca_ion");
 	register_mech(_mechanism, nrn_alloc,nrn_cur, nrn_jacob, nrn_state, nrn_init, hoc_nrnpointerindex, 1);
 _mechtype = nrn_get_mechtype(_mechanism[1]);
     _nrn_setdata_reg(_mechtype, _setdata);
     _nrn_thread_reg(_mechtype, 2, _update_ion_pointer);
 #if NMODL_TEXT
  hoc_reg_nmodl_text(_mechtype, nmodl_file_text);
  hoc_reg_nmodl_filename(_mechtype, nmodl_filename);
#endif
  hoc_register_prop_size(_mechtype, 11, 4);
  hoc_register_dparam_semantics(_mechtype, 0, "can_ion");
  hoc_register_dparam_semantics(_mechtype, 1, "can_ion");
  hoc_register_dparam_semantics(_mechtype, 2, "can_ion");
  hoc_register_dparam_semantics(_mechtype, 3, "ca_ion");
 	hoc_register_cvode(_mechtype, _ode_count, 0, 0, 0);
 	hoc_register_var(hoc_scdoub, hoc_vdoub, hoc_intfunc);
 	ivoc_help("help ?1 iCaAN /home/ncn-neuron/Documents/Eduardo/in_silico/mods/iCaAN.mod\n");
 hoc_register_limits(_mechtype, _hoc_parm_limits);
 hoc_register_units(_mechtype, _hoc_parm_units);
 }
static int _reset;
static char *modelname = "Slow Ca-dependent cation current";

static int error;
static int _ninits = 0;
static int _match_recurse=1;
static void _modl_cleanup(){ _match_recurse=1;}
static int evaluate_fct(_threadargsprotocomma_ double, double);
static int states(_threadargsproto_);
 
static int  states ( _threadargsproto_ ) {
   evaluate_fct ( _threadargscomma_ v , cai ) ;
   m = m + ( 1.0 - exp ( - dt / tau_m ) ) * ( m_inf - m ) ;
    return 0; }
 
static void _hoc_states(void) {
  double _r;
   double* _p; Datum* _ppvar; Datum* _thread; NrnThread* _nt;
   if (_extcall_prop) {_p = _extcall_prop->param; _ppvar = _extcall_prop->dparam;}else{ _p = (double*)0; _ppvar = (Datum*)0; }
  _thread = _extcall_thread;
  _nt = nrn_threads;
 _r = 1.;
 states ( _p, _ppvar, _thread, _nt );
 hoc_retpushx(_r);
}
 
static int  evaluate_fct ( _threadargsprotocomma_ double _lv , double _lcai ) {
   double _lalpha2 ;
 _lalpha2 = beta * pow( ( _lcai / cac ) , 2.0 ) ;
   tau_m = tau_factor / ( _lalpha2 + beta ) / tadj ;
   m_inf = _lalpha2 / ( _lalpha2 + beta ) ;
   if ( tau_m < taumin ) {
     tau_m = taumin ;
     }
    return 0; }
 
static void _hoc_evaluate_fct(void) {
  double _r;
   double* _p; Datum* _ppvar; Datum* _thread; NrnThread* _nt;
   if (_extcall_prop) {_p = _extcall_prop->param; _ppvar = _extcall_prop->dparam;}else{ _p = (double*)0; _ppvar = (Datum*)0; }
  _thread = _extcall_thread;
  _nt = nrn_threads;
 _r = 1.;
 evaluate_fct ( _p, _ppvar, _thread, _nt, *getarg(1) , *getarg(2) );
 hoc_retpushx(_r);
}
 
static int _ode_count(int _type){ hoc_execerror("iCaAN", "cannot be used with CVODE"); return 0;}
 extern void nrn_update_ion_pointer(Symbol*, Datum*, int, int);
 static void _update_ion_pointer(Datum* _ppvar) {
   nrn_update_ion_pointer(_can_sym, _ppvar, 0, 0);
   nrn_update_ion_pointer(_can_sym, _ppvar, 1, 3);
   nrn_update_ion_pointer(_can_sym, _ppvar, 2, 4);
   nrn_update_ion_pointer(_ca_sym, _ppvar, 3, 1);
 }

static void initmodel(double* _p, Datum* _ppvar, Datum* _thread, NrnThread* _nt) {
  int _i; double _save;{
  m = m0;
 {
   tadj = pow( 3.0 , ( ( celsius - 22.0 ) / 10.0 ) ) ;
   evaluate_fct ( _threadargscomma_ v , cai ) ;
   m = m_inf ;
   }
 
}
}

static void nrn_init(NrnThread* _nt, _Memb_list* _ml, int _type){
double* _p; Datum* _ppvar; Datum* _thread;
Node *_nd; double _v; int* _ni; int _iml, _cntml;
#if CACHEVEC
    _ni = _ml->_nodeindices;
#endif
_cntml = _ml->_nodecount;
_thread = _ml->_thread;
for (_iml = 0; _iml < _cntml; ++_iml) {
 _p = _ml->_data[_iml]; _ppvar = _ml->_pdata[_iml];
#if CACHEVEC
  if (use_cachevec) {
    _v = VEC_V(_ni[_iml]);
  }else
#endif
  {
    _nd = _ml->_nodelist[_iml];
    _v = NODEV(_nd);
  }
 v = _v;
  ecan = _ion_ecan;
  cai = _ion_cai;
 initmodel(_p, _ppvar, _thread, _nt);
 }
}

static double _nrn_current(double* _p, Datum* _ppvar, Datum* _thread, NrnThread* _nt, double _v){double _current=0.;v=_v;{ {
   ican = gbar * m * m * ( v - ecan ) ;
   }
 _current += ican;

} return _current;
}

static void nrn_cur(NrnThread* _nt, _Memb_list* _ml, int _type) {
double* _p; Datum* _ppvar; Datum* _thread;
Node *_nd; int* _ni; double _rhs, _v; int _iml, _cntml;
#if CACHEVEC
    _ni = _ml->_nodeindices;
#endif
_cntml = _ml->_nodecount;
_thread = _ml->_thread;
for (_iml = 0; _iml < _cntml; ++_iml) {
 _p = _ml->_data[_iml]; _ppvar = _ml->_pdata[_iml];
#if CACHEVEC
  if (use_cachevec) {
    _v = VEC_V(_ni[_iml]);
  }else
#endif
  {
    _nd = _ml->_nodelist[_iml];
    _v = NODEV(_nd);
  }
  ecan = _ion_ecan;
  cai = _ion_cai;
 _g = _nrn_current(_p, _ppvar, _thread, _nt, _v + .001);
 	{ double _dican;
  _dican = ican;
 _rhs = _nrn_current(_p, _ppvar, _thread, _nt, _v);
  _ion_dicandv += (_dican - ican)/.001 ;
 	}
 _g = (_g - _rhs)/.001;
  _ion_ican += ican ;
#if CACHEVEC
  if (use_cachevec) {
	VEC_RHS(_ni[_iml]) -= _rhs;
  }else
#endif
  {
	NODERHS(_nd) -= _rhs;
  }
 
}
 
}

static void nrn_jacob(NrnThread* _nt, _Memb_list* _ml, int _type) {
double* _p; Datum* _ppvar; Datum* _thread;
Node *_nd; int* _ni; int _iml, _cntml;
#if CACHEVEC
    _ni = _ml->_nodeindices;
#endif
_cntml = _ml->_nodecount;
_thread = _ml->_thread;
for (_iml = 0; _iml < _cntml; ++_iml) {
 _p = _ml->_data[_iml];
#if CACHEVEC
  if (use_cachevec) {
	VEC_D(_ni[_iml]) += _g;
  }else
#endif
  {
     _nd = _ml->_nodelist[_iml];
	NODED(_nd) += _g;
  }
 
}
 
}

static void nrn_state(NrnThread* _nt, _Memb_list* _ml, int _type) {
double* _p; Datum* _ppvar; Datum* _thread;
Node *_nd; double _v = 0.0; int* _ni; int _iml, _cntml;
#if CACHEVEC
    _ni = _ml->_nodeindices;
#endif
_cntml = _ml->_nodecount;
_thread = _ml->_thread;
for (_iml = 0; _iml < _cntml; ++_iml) {
 _p = _ml->_data[_iml]; _ppvar = _ml->_pdata[_iml];
 _nd = _ml->_nodelist[_iml];
#if CACHEVEC
  if (use_cachevec) {
    _v = VEC_V(_ni[_iml]);
  }else
#endif
  {
    _nd = _ml->_nodelist[_iml];
    _v = NODEV(_nd);
  }
 v=_v;
{
  ecan = _ion_ecan;
  cai = _ion_cai;
 {  { states(_p, _ppvar, _thread, _nt); }
  } }}

}

static void terminal(){}

static void _initlists(){
 double _x; double* _p = &_x;
 int _i; static int _first = 1;
  if (!_first) return;
_first = 0;
}

#if defined(__cplusplus)
} /* extern "C" */
#endif

#if NMODL_TEXT
static const char* nmodl_filename = "/home/ncn-neuron/Documents/Eduardo/in_silico/mods/iCaAN.mod";
static const char* nmodl_file_text = 
  "TITLE Slow Ca-dependent cation current\n"
  ":\n"
  ":   Ca++ dependent nonspecific cation current ICAN\n"
  ":   Differential equations\n"
  ":\n"
  ":   Model based on a first order kinetic scheme\n"
  ":\n"
  ":      <closed> + n cai <-> <open>	(alpha,beta)\n"
  ":\n"
  ":   Following this model, the activation fct will be half-activated at \n"
  ":   a concentration of Cai = (beta/alpha)^(1/n) = cac (parameter)\n"
  ":\n"
  ":   The mod file is here written for the case n=2 (2 binding sites)\n"
  ":   ---------------------------------------------\n"
  ":\n"
  ":   Kinetics based on: Partridge & Swandulla, TINS 11: 69-72, 1988.\n"
  ":\n"
  ":   This current has the following properties:\n"
  ":      - inward current (non specific for cations Na, K, Ca, ...)\n"
  ":      - activated by intracellular calcium\n"
  ":      - NOT voltage dependent\n"
  ":\n"
  ":   A minimal value for the time constant has been added\n"
  ":\n"
  ":   Ref: Destexhe et al., J. Neurophysiology 72: 803-818, 1994.\n"
  ":\n"
  ":   Modifications by Arthur Houweling for use in MyFirstNEURON\n"
  ":\n"
  ":   Some parameter changes by Paulo Aguiar (pauloaguiar@fc.up.pt):\n"
  ":   tau_factor = 40 => parameter beta	changes from 2.0e-3 to 5.0e-5\n"
  ":		cac = 0.5e-3 (before was 1.0e-3)\n"
  "\n"
  "INDEPENDENT {t FROM 0 TO 1 WITH 1 (ms)}\n"
  "\n"
  "NEURON {\n"
  "	SUFFIX iCaAN\n"
  "	USEION can READ ecan WRITE ican VALENCE 1\n"
  "	USEION ca READ cai\n"
  "        RANGE gbar, m_inf, tau_m\n"
  "	RANGE ican\n"
  "	GLOBAL beta, cac, taumin\n"
  "}\n"
  "\n"
  "\n"
  "UNITS {\n"
  "	(mA) = (milliamp)\n"
  "	(mV) = (millivolt)\n"
  "	(molar) = (1/liter)\n"
  "	(mM) = (millimolar)\n"
  "}\n"
  "\n"
  "\n"
  "PARAMETER {\n"
  "	v		  (mV)\n"
  "	celsius		  (degC)\n"
  "        dt                (ms)\n"
  "	ecan	= -20	  (mV)		: reversal potential\n"
  "	cai		  (mM)\n"
  "	gbar	= 0.00025 (mho/cm2)\n"
  "	beta	= 2.0e-3  (1/ms)	: backward rate constant (original value)\n"
  "\n"
  "	tau_factor = 40         : scaling factor allowing tuning\n"
  "\n"
  "	:cac	= 1.0e-3  (mM)		: middle point of activation fct  (original value)\n"
  "	cac		= 5e-4	  (mM)		: middle point of activation fct\n"
  "\n"
  "	taumin	= 0.1	  (ms)		: minimal value of time constant\n"
  "	\n"
  "	:parameter tau_factor and cac were set to produce tau_m ~ 2000(ms) at cai=cac and celsius=36;\n"
  "	:implications of parameters change when cai=cac:\n"
  "	: -> BEFORE (beta=2.0e-3;tadj=4.66) => tau_m ~ 50   ms\n"
  "	: -> AFTER  (beta=5.0e-5;tadj=4.66) => tau_m ~ 2000 ms\n"
  "	:\n"
  "	:also cac was reduced to half, from 1.0 uM to 0.5 uM\n"
  "}\n"
  "\n"
  "\n"
  "STATE {\n"
  "	m\n"
  "}\n"
  "\n"
  "ASSIGNED {\n"
  "	ican	(mA/cm2)\n"
  "	m_inf\n"
  "	tau_m	(ms)\n"
  "	tadj\n"
  "}\n"
  "\n"
  "BREAKPOINT { \n"
  "	SOLVE states :METHOD euler\n"
  "	ican = gbar * m*m * (v - ecan)\n"
  "}\n"
  "\n"
  ":DERIVATIVE states {\n"
  ":       evaluate_fct(v,cai)\n"
  ":\n"
  ":       m'= (m_inf-m) / tau_m \n"
  ":}\n"
  "  \n"
  "PROCEDURE states() {\n"
  "        evaluate_fct(v,cai)\n"
  "	\n"
  "        m = m + ( 1-exp(-dt/tau_m) )*(m_inf-m)\n"
  "	:printf(\"\\n iCAN tau_m=%g\", tau_m)\n"
  "\n"
  "}\n"
  "\n"
  "UNITSOFF\n"
  "INITIAL {\n"
  ":\n"
  ":  activation kinetics are assumed to be at 22 deg. C\n"
  ":  Q10 is assumed to be 3\n"
  ":\n"
  "	tadj = 3.0 ^ ((celsius-22.0)/10)\n"
  "\n"
  "	evaluate_fct(v,cai)\n"
  "	m = m_inf\n"
  "}\n"
  "\n"
  "\n"
  "PROCEDURE evaluate_fct(v(mV),cai(mM)) {  LOCAL alpha2\n"
  "\n"
  "	alpha2 = beta * (cai/cac)^2\n"
  "	\n"
  "	tau_m = tau_factor / (alpha2 + beta) / tadj		: tau_m = tau_factor / ( beta * (1 + (cai/cac)^2) ) / tadj\n"
  "	\n"
  "	m_inf = alpha2 / (alpha2 + beta)							: m_inf = (cai/cac)^2 / ( 1 + (cai/cac)^2 )\n"
  "\n"
  "	if(tau_m < taumin) { tau_m = taumin }					: min value of time cst\n"
  "\n"
  "}\n"
  "UNITSON\n"
  ;
#endif
