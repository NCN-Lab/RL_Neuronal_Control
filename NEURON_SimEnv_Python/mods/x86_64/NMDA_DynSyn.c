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
 
#define nrn_init _nrn_init__NMDA_DynSyn
#define _nrn_initial _nrn_initial__NMDA_DynSyn
#define nrn_cur _nrn_cur__NMDA_DynSyn
#define _nrn_current _nrn_current__NMDA_DynSyn
#define nrn_jacob _nrn_jacob__NMDA_DynSyn
#define nrn_state _nrn_state__NMDA_DynSyn
#define _net_receive _net_receive__NMDA_DynSyn 
#define state state__NMDA_DynSyn 
 
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
#define tau_rise _p[0]
#define tau_rise_columnindex 0
#define tau_decay _p[1]
#define tau_decay_columnindex 1
#define U1 _p[2]
#define U1_columnindex 2
#define tau_rec _p[3]
#define tau_rec_columnindex 3
#define tau_fac _p[4]
#define tau_fac_columnindex 4
#define e _p[5]
#define e_columnindex 5
#define ca_ratio _p[6]
#define ca_ratio_columnindex 6
#define i _p[7]
#define i_columnindex 7
#define g _p[8]
#define g_columnindex 8
#define ica _p[9]
#define ica_columnindex 9
#define inon _p[10]
#define inon_columnindex 10
#define A _p[11]
#define A_columnindex 11
#define B _p[12]
#define B_columnindex 12
#define mgo _p[13]
#define mgo_columnindex 13
#define factor _p[14]
#define factor_columnindex 14
#define DA _p[15]
#define DA_columnindex 15
#define DB _p[16]
#define DB_columnindex 16
#define v _p[17]
#define v_columnindex 17
#define _g _p[18]
#define _g_columnindex 18
#define _tsav _p[19]
#define _tsav_columnindex 19
#define _nd_area  *_ppvar[0]._pval
#define _ion_ica	*_ppvar[2]._pval
#define _ion_dicadv	*_ppvar[3]._pval
#define _ion_mgo	*_ppvar[4]._pval
 
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
 /* declaration of user functions */
 static double _hoc_mgblock(void*);
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

 extern Prop* nrn_point_prop_;
 static int _pointtype;
 static void* _hoc_create_pnt(Object* _ho) { void* create_point_process(int, Object*);
 return create_point_process(_pointtype, _ho);
}
 static void _hoc_destroy_pnt(void*);
 static double _hoc_loc_pnt(void* _vptr) {double loc_point_process(int, void*);
 return loc_point_process(_pointtype, _vptr);
}
 static double _hoc_has_loc(void* _vptr) {double has_loc_point(void*);
 return has_loc_point(_vptr);
}
 static double _hoc_get_loc_pnt(void* _vptr) {
 double get_loc_point_process(void*); return (get_loc_point_process(_vptr));
}
 extern void _nrn_setdata_reg(int, void(*)(Prop*));
 static void _setdata(Prop* _prop) {
 _extcall_prop = _prop;
 }
 static void _hoc_setdata(void* _vptr) { Prop* _prop;
 _prop = ((Point_process*)_vptr)->_prop;
   _setdata(_prop);
 }
 /* connect user functions to hoc names */
 static VoidFunc hoc_intfunc[] = {
 0,0
};
 static Member_func _member_func[] = {
 "loc", _hoc_loc_pnt,
 "has_loc", _hoc_has_loc,
 "get_loc", _hoc_get_loc_pnt,
 "mgblock", _hoc_mgblock,
 0, 0
};
#define mgblock mgblock_NMDA_DynSyn
 extern double mgblock( _threadargsprotocomma_ double );
 /* declare global and static user variables */
 /* some parameters have upper and lower limits */
 static HocParmLimits _hoc_parm_limits[] = {
 0,0,0
};
 static HocParmUnits _hoc_parm_units[] = {
 "tau_rise", "ms",
 "tau_decay", "ms",
 "U1", "1",
 "tau_rec", "ms",
 "tau_fac", "ms",
 "e", "mV",
 "ca_ratio", "1",
 "i", "nA",
 "g", "umho",
 "ica", "nA",
 "inon", "nA",
 0,0
};
 static double A0 = 0;
 static double B0 = 0;
 static double delta_t = 0.01;
 /* connect global user variables to hoc */
 static DoubScal hoc_scdoub[] = {
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
 static void _hoc_destroy_pnt(void* _vptr) {
   destroy_point_process(_vptr);
}
 
static int _ode_count(int);
static void _ode_map(int, double**, double**, double*, Datum*, double*, int);
static void _ode_spec(NrnThread*, _Memb_list*, int);
static void _ode_matsol(NrnThread*, _Memb_list*, int);
 
#define _cvode_ieq _ppvar[5]._i
 static void _ode_matsol_instance1(_threadargsproto_);
 /* connect range variables in _p that hoc is supposed to know about */
 static const char *_mechanism[] = {
 "7.7.0",
"NMDA_DynSyn",
 "tau_rise",
 "tau_decay",
 "U1",
 "tau_rec",
 "tau_fac",
 "e",
 "ca_ratio",
 0,
 "i",
 "g",
 "ica",
 "inon",
 0,
 "A",
 "B",
 0,
 0};
 static Symbol* _ca_sym;
 static Symbol* _mg_sym;
 
extern Prop* need_memb(Symbol*);

static void nrn_alloc(Prop* _prop) {
	Prop *prop_ion;
	double *_p; Datum *_ppvar;
  if (nrn_point_prop_) {
	_prop->_alloc_seq = nrn_point_prop_->_alloc_seq;
	_p = nrn_point_prop_->param;
	_ppvar = nrn_point_prop_->dparam;
 }else{
 	_p = nrn_prop_data_alloc(_mechtype, 20, _prop);
 	/*initialize range parameters*/
 	tau_rise = 5;
 	tau_decay = 70;
 	U1 = 1;
 	tau_rec = 0.1;
 	tau_fac = 0.1;
 	e = 0;
 	ca_ratio = 0.1;
  }
 	_prop->param = _p;
 	_prop->param_size = 20;
  if (!nrn_point_prop_) {
 	_ppvar = nrn_prop_datum_alloc(_mechtype, 6, _prop);
  }
 	_prop->dparam = _ppvar;
 	/*connect ionic variables to this model*/
 prop_ion = need_memb(_ca_sym);
 	_ppvar[2]._pval = &prop_ion->param[3]; /* ica */
 	_ppvar[3]._pval = &prop_ion->param[4]; /* _ion_dicadv */
 prop_ion = need_memb(_mg_sym);
 nrn_promote(prop_ion, 1, 0);
 	_ppvar[4]._pval = &prop_ion->param[2]; /* mgo */
 
}
 static void _initlists();
  /* some states have an absolute tolerance */
 static Symbol** _atollist;
 static HocStateTolerance _hoc_state_tol[] = {
 0,0
};
 static void _net_receive(Point_process*, double*, double);
 static void _net_init(Point_process*, double*, double);
 static void _update_ion_pointer(Datum*);
 extern Symbol* hoc_lookup(const char*);
extern void _nrn_thread_reg(int, int, void(*)(Datum*));
extern void _nrn_thread_table_reg(int, void(*)(double*, Datum*, Datum*, NrnThread*, int));
extern void hoc_register_tolerance(int, HocStateTolerance*, Symbol***);
extern void _cvode_abstol( Symbol**, double*, int);

 void _NMDA_DynSyn_reg() {
	int _vectorized = 1;
  _initlists();
 	ion_reg("ca", -10000.);
 	ion_reg("mg", 2.0);
 	_ca_sym = hoc_lookup("ca_ion");
 	_mg_sym = hoc_lookup("mg_ion");
 	_pointtype = point_register_mech(_mechanism,
	 nrn_alloc,nrn_cur, nrn_jacob, nrn_state, nrn_init,
	 hoc_nrnpointerindex, 1,
	 _hoc_create_pnt, _hoc_destroy_pnt, _member_func);
 _mechtype = nrn_get_mechtype(_mechanism[1]);
     _nrn_setdata_reg(_mechtype, _setdata);
     _nrn_thread_reg(_mechtype, 2, _update_ion_pointer);
 #if NMODL_TEXT
  hoc_reg_nmodl_text(_mechtype, nmodl_file_text);
  hoc_reg_nmodl_filename(_mechtype, nmodl_filename);
#endif
  hoc_register_prop_size(_mechtype, 20, 6);
  hoc_register_dparam_semantics(_mechtype, 0, "area");
  hoc_register_dparam_semantics(_mechtype, 1, "pntproc");
  hoc_register_dparam_semantics(_mechtype, 2, "ca_ion");
  hoc_register_dparam_semantics(_mechtype, 3, "ca_ion");
  hoc_register_dparam_semantics(_mechtype, 4, "mg_ion");
  hoc_register_dparam_semantics(_mechtype, 5, "cvodeieq");
 	hoc_register_cvode(_mechtype, _ode_count, _ode_map, _ode_spec, _ode_matsol);
 	hoc_register_tolerance(_mechtype, _hoc_state_tol, &_atollist);
 pnt_receive[_mechtype] = _net_receive;
 pnt_receive_init[_mechtype] = _net_init;
 pnt_receive_size[_mechtype] = 5;
 	hoc_register_var(hoc_scdoub, hoc_vdoub, hoc_intfunc);
 	ivoc_help("help ?1 NMDA_DynSyn /home/ncn-neuron/Documents/GitHub/neuromodulation-RL/mods/NMDA_DynSyn.mod\n");
 hoc_register_limits(_mechtype, _hoc_parm_limits);
 hoc_register_units(_mechtype, _hoc_parm_units);
 }
static int _reset;
static char *modelname = "NMDA receptor with Ca influx and pre-synaptic short-term plasticity";

static int error;
static int _ninits = 0;
static int _match_recurse=1;
static void _modl_cleanup(){ _match_recurse=1;}
 
static int _ode_spec1(_threadargsproto_);
/*static int _ode_matsol1(_threadargsproto_);*/
 static int _slist1[2], _dlist1[2];
 static int state(_threadargsproto_);
 
/*CVODE*/
 static int _ode_spec1 (double* _p, Datum* _ppvar, Datum* _thread, NrnThread* _nt) {int _reset = 0; {
   DA = - A / tau_rise ;
   DB = - B / tau_decay ;
   }
 return _reset;
}
 static int _ode_matsol1 (double* _p, Datum* _ppvar, Datum* _thread, NrnThread* _nt) {
 DA = DA  / (1. - dt*( ( - 1.0 ) / tau_rise )) ;
 DB = DB  / (1. - dt*( ( - 1.0 ) / tau_decay )) ;
  return 0;
}
 /*END CVODE*/
 static int state (double* _p, Datum* _ppvar, Datum* _thread, NrnThread* _nt) { {
    A = A + (1. - exp(dt*(( - 1.0 ) / tau_rise)))*(- ( 0.0 ) / ( ( - 1.0 ) / tau_rise ) - A) ;
    B = B + (1. - exp(dt*(( - 1.0 ) / tau_decay)))*(- ( 0.0 ) / ( ( - 1.0 ) / tau_decay ) - B) ;
   }
  return 0;
}
 
double mgblock ( _threadargsprotocomma_ double _lv ) {
   double _lmgblock;
 _lmgblock = 1.0 / ( 1.0 + exp ( 0.062 * - _lv ) * ( mgo / 3.57 ) ) ;
   
return _lmgblock;
 }
 
static double _hoc_mgblock(void* _vptr) {
 double _r;
   double* _p; Datum* _ppvar; Datum* _thread; NrnThread* _nt;
   _p = ((Point_process*)_vptr)->_prop->param;
  _ppvar = ((Point_process*)_vptr)->_prop->dparam;
  _thread = _extcall_thread;
  _nt = (NrnThread*)((Point_process*)_vptr)->_vnt;
 _r =  mgblock ( _p, _ppvar, _thread, _nt, *getarg(1) );
 return(_r);
}
 
static void _net_receive (Point_process* _pnt, double* _args, double _lflag) 
{  double* _p; Datum* _ppvar; Datum* _thread; NrnThread* _nt;
   _thread = (Datum*)0; _nt = (NrnThread*)_pnt->_vnt;   _p = _pnt->_prop->param; _ppvar = _pnt->_prop->dparam;
  if (_tsav > t){ extern char* hoc_object_name(); hoc_execerror(hoc_object_name(_pnt->ob), ":Event arrived out of order. Must call ParallelContext.set_maxstep AFTER assigning minimum NetCon.delay");}
 _tsav = t; {
   _args[3] = _args[3] * exp ( - ( t - _args[4] ) / tau_fac ) ;
   _args[3] = _args[3] + U1 * ( 1.0 - _args[3] ) ;
   _args[2] = 1.0 - ( 1.0 - _args[2] ) * exp ( - ( t - _args[4] ) / tau_rec ) ;
   _args[1] = _args[3] * _args[2] ;
   _args[2] = _args[2] - _args[3] * _args[2] ;
   _args[4] = t ;
     if (nrn_netrec_state_adjust && !cvode_active_){
    /* discon state adjustment for cnexp case (rate uses no local variable) */
    double __state = A;
    double __primary = (A + _args[0] * factor * _args[1]) - __state;
     __primary += ( 1. - exp( 0.5*dt*( ( - 1.0 ) / tau_rise ) ) )*( - ( 0.0 ) / ( ( - 1.0 ) / tau_rise ) - __primary );
    A += __primary;
  } else {
 A = A + _args[0] * factor * _args[1] ;
     }
   if (nrn_netrec_state_adjust && !cvode_active_){
    /* discon state adjustment for cnexp case (rate uses no local variable) */
    double __state = B;
    double __primary = (B + _args[0] * factor * _args[1]) - __state;
     __primary += ( 1. - exp( 0.5*dt*( ( - 1.0 ) / tau_decay ) ) )*( - ( 0.0 ) / ( ( - 1.0 ) / tau_decay ) - __primary );
    B += __primary;
  } else {
 B = B + _args[0] * factor * _args[1] ;
     }
 } }
 
static void _net_init(Point_process* _pnt, double* _args, double _lflag) {
       double* _p = _pnt->_prop->param;
    Datum* _ppvar = _pnt->_prop->dparam;
    Datum* _thread = (Datum*)0;
    NrnThread* _nt = (NrnThread*)_pnt->_vnt;
 _args[2] = 1.0 ;
   _args[3] = 0.0 ;
   _args[4] = t ;
   }
 
static int _ode_count(int _type){ return 2;}
 
static void _ode_spec(NrnThread* _nt, _Memb_list* _ml, int _type) {
   double* _p; Datum* _ppvar; Datum* _thread;
   Node* _nd; double _v; int _iml, _cntml;
  _cntml = _ml->_nodecount;
  _thread = _ml->_thread;
  for (_iml = 0; _iml < _cntml; ++_iml) {
    _p = _ml->_data[_iml]; _ppvar = _ml->_pdata[_iml];
    _nd = _ml->_nodelist[_iml];
    v = NODEV(_nd);
  mgo = _ion_mgo;
     _ode_spec1 (_p, _ppvar, _thread, _nt);
  }}
 
static void _ode_map(int _ieq, double** _pv, double** _pvdot, double* _pp, Datum* _ppd, double* _atol, int _type) { 
	double* _p; Datum* _ppvar;
 	int _i; _p = _pp; _ppvar = _ppd;
	_cvode_ieq = _ieq;
	for (_i=0; _i < 2; ++_i) {
		_pv[_i] = _pp + _slist1[_i];  _pvdot[_i] = _pp + _dlist1[_i];
		_cvode_abstol(_atollist, _atol, _i);
	}
 }
 
static void _ode_matsol_instance1(_threadargsproto_) {
 _ode_matsol1 (_p, _ppvar, _thread, _nt);
 }
 
static void _ode_matsol(NrnThread* _nt, _Memb_list* _ml, int _type) {
   double* _p; Datum* _ppvar; Datum* _thread;
   Node* _nd; double _v; int _iml, _cntml;
  _cntml = _ml->_nodecount;
  _thread = _ml->_thread;
  for (_iml = 0; _iml < _cntml; ++_iml) {
    _p = _ml->_data[_iml]; _ppvar = _ml->_pdata[_iml];
    _nd = _ml->_nodelist[_iml];
    v = NODEV(_nd);
  mgo = _ion_mgo;
 _ode_matsol_instance1(_threadargs_);
 }}
 extern void nrn_update_ion_pointer(Symbol*, Datum*, int, int);
 static void _update_ion_pointer(Datum* _ppvar) {
   nrn_update_ion_pointer(_ca_sym, _ppvar, 2, 3);
   nrn_update_ion_pointer(_ca_sym, _ppvar, 3, 4);
   nrn_update_ion_pointer(_mg_sym, _ppvar, 4, 2);
 }

static void initmodel(double* _p, Datum* _ppvar, Datum* _thread, NrnThread* _nt) {
  int _i; double _save;{
  A = A0;
  B = B0;
 {
   double _ltp ;
 A = 0.0 ;
   B = 0.0 ;
   _ltp = ( tau_rise * tau_decay ) / ( tau_decay - tau_rise ) * log ( tau_decay / tau_rise ) ;
   factor = - exp ( - _ltp / tau_rise ) + exp ( - _ltp / tau_decay ) ;
   factor = 1.0 / factor ;
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
 _tsav = -1e20;
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
  mgo = _ion_mgo;
 initmodel(_p, _ppvar, _thread, _nt);
 }
}

static double _nrn_current(double* _p, Datum* _ppvar, Datum* _thread, NrnThread* _nt, double _v){double _current=0.;v=_v;{ {
   g = B - A ;
   i = g * mgblock ( _threadargscomma_ v ) * ( v - e ) ;
   ica = ca_ratio * i ;
   inon = ( 1.0 - ca_ratio ) * i ;
   }
 _current += ica;
 _current += inon;

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
  mgo = _ion_mgo;
 _g = _nrn_current(_p, _ppvar, _thread, _nt, _v + .001);
 	{ double _dica;
  _dica = ica;
 _rhs = _nrn_current(_p, _ppvar, _thread, _nt, _v);
  _ion_dicadv += (_dica - ica)/.001 * 1.e2/ (_nd_area);
 	}
 _g = (_g - _rhs)/.001;
  _ion_ica += ica * 1.e2/ (_nd_area);
 _g *=  1.e2/(_nd_area);
 _rhs *= 1.e2/(_nd_area);
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
  mgo = _ion_mgo;
 {   state(_p, _ppvar, _thread, _nt);
  } }}

}

static void terminal(){}

static void _initlists(){
 double _x; double* _p = &_x;
 int _i; static int _first = 1;
  if (!_first) return;
 _slist1[0] = A_columnindex;  _dlist1[0] = DA_columnindex;
 _slist1[1] = B_columnindex;  _dlist1[1] = DB_columnindex;
_first = 0;
}

#if defined(__cplusplus)
} /* extern "C" */
#endif

#if NMODL_TEXT
static const char* nmodl_filename = "/home/ncn-neuron/Documents/GitHub/neuromodulation-RL/mods/NMDA_DynSyn.mod";
static const char* nmodl_file_text = 
  "TITLE  NMDA receptor with Ca influx and pre-synaptic short-term plasticity\n"
  "\n"
  "\n"
  "COMMENT\n"
  "Dynamic presynaptic activity based on Fuhrmann et al, 2002: \"Coding of temporal information by activity-dependent synapses\" \n"
  "\n"
  "Written by Paulo Aguiar and Mafalda Sousa, IBMC, May 2008\n"
  "pauloaguiar@fc.up.pt ; mafsousa@ibmc.up.pt\n"
  "ENDCOMMENT\n"
  "\n"
  "\n"
  "NEURON {\n"
  "	POINT_PROCESS NMDA_DynSyn\n"
  "	USEION ca WRITE ica	\n"
  "	USEION mg READ mgo VALENCE 2\n"
  "	RANGE tau_rise, tau_decay\n"
  "	RANGE U1, tau_rec, tau_fac\n"
  "	RANGE i, g, e, mg, inon, ica, ca_ratio\n"
  "	NONSPECIFIC_CURRENT inon\n"
  "    }\n"
  "    \n"
  "UNITS {\n"
  "	(nA) = (nanoamp)\n"
  "	(mV) = (millivolt)\n"
  "	(molar) = (1/liter)\n"
  "	(mM) = (millimolar)\n"
  "    }    \n"
  "    \n"
  "    PARAMETER {\n"
  "  	tau_rise  = 5.0   (ms)  : dual-exponential conductance profile\n"
  "	tau_decay = 70.0  (ms)  : IMPORTANT: tau_rise < tau_decay\n"
  "	U1        = 1.0   (1)   : The parameter U1, tau_rec and tau_fac define\n"
  "	tau_rec   = 0.1   (ms)  : the pre-synaptic SP short-term plasticity\n"
  "	tau_fac   = 0.1   (ms)  : mechanism (see Fuhrmann et al, 2002)\n"
  "	e         = 0.0   (mV)  : synapse reversal potential\n"
  "	mgo		  = 1.0   (mM)  : external magnesium concentration\n"
  "	ca_ratio  = 0.1   (1)   : ratio of calcium current to total current( Burnashev/Sakmann J Phys 1995 485 403-418)\n"
  "    }\n"
  "    \n"
  "    \n"
  "ASSIGNED {\n"
  "	v		(mV)\n"
  "	i		(nA)\n"
  "	g		(umho)\n"
  "	factor	(1)\n"
  "	ica		(nA)\n"
  "	inon	(nA)\n"
  "}\n"
  "\n"
  "STATE {\n"
  "	A\n"
  "	B\n"
  "}\n"
  "\n"
  "INITIAL{\n"
  "	LOCAL tp\n"
  "	A = 0\n"
  "	B = 0\n"
  "	tp = (tau_rise*tau_decay)/(tau_decay-tau_rise)*log(tau_decay/tau_rise)\n"
  "	factor = -exp(-tp/tau_rise)+exp(-tp/tau_decay)\n"
  "	factor = 1/factor\n"
  "}\n"
  "\n"
  "BREAKPOINT {\n"
  "	SOLVE state METHOD cnexp\n"
  "	g = B-A\n"
  "	i = g*mgblock(v)*(v-e)\n"
  "	ica = ca_ratio*i\n"
  "	inon = (1-ca_ratio)*i\n"
  "	:printf(\"\\nt=%f\\tinon=%f\\tica=%f\\ti=%f\\tmgb=%f\",t, inon, ica, i, mgblock(v))\n"
  "}\n"
  "\n"
  "DERIVATIVE state{\n"
  "	A' = -A/tau_rise\n"
  "	B' = -B/tau_decay\n"
  "}\n"
  "\n"
  "FUNCTION mgblock(v(mV)) {\n"
  "	: from Jahr & Stevens 1990\n"
  "	mgblock = 1 / (1 + exp(0.062 (/mV) * -v) * (mgo / 3.57 (mM)))\n"
  "}\n"
  "\n"
  "NET_RECEIVE (weight, Pv, P, Use, t0 (ms)){\n"
  "	INITIAL{\n"
  "		P=1\n"
  "		Use=0\n"
  "		t0=t\n"
  "	}	\n"
  "\n"
  "	Use = Use * exp(-(t-t0)/tau_fac)\n"
  "	Use = Use + U1*(1-Use) \n"
  "	P = 1-(1- P) * exp(-(t-t0)/tau_rec)\n"
  "	Pv= Use * P\n"
  "	P = P - Use * P\n"
  "	\n"
  "	t0=t\n"
  "	\n"
  "	A=A + weight*factor*Pv\n"
  "	B=B + weight*factor*Pv\n"
  "}\n"
  "\n"
  ;
#endif
