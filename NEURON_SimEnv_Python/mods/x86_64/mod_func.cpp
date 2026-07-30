#include <stdio.h>
#include "hocdec.h"
extern int nrnmpi_myid;
extern int nrn_nobanner_;
#if defined(__cplusplus)
extern "C" {
#endif

extern void _AMPA_DynSyn_reg(void);
extern void _CaIntraCellDyn_reg(void);
extern void _exp2nmdar_reg(void);
extern void _GABAa_DynSyn_reg(void);
extern void _GABAb_DynSyn_reg(void);
extern void _hh2_reg(void);
extern void _iCaAN_reg(void);
extern void _iCaL_reg(void);
extern void _iKCa_reg(void);
extern void _NMDA_DynSyn_reg(void);

void modl_reg() {
  if (!nrn_nobanner_) if (nrnmpi_myid < 1) {
    fprintf(stderr, "Additional mechanisms from files\n");
    fprintf(stderr, " \"AMPA_DynSyn.mod\"");
    fprintf(stderr, " \"CaIntraCellDyn.mod\"");
    fprintf(stderr, " \"exp2nmdar.mod\"");
    fprintf(stderr, " \"GABAa_DynSyn.mod\"");
    fprintf(stderr, " \"GABAb_DynSyn.mod\"");
    fprintf(stderr, " \"hh2.mod\"");
    fprintf(stderr, " \"iCaAN.mod\"");
    fprintf(stderr, " \"iCaL.mod\"");
    fprintf(stderr, " \"iKCa.mod\"");
    fprintf(stderr, " \"NMDA_DynSyn.mod\"");
    fprintf(stderr, "\n");
  }
  _AMPA_DynSyn_reg();
  _CaIntraCellDyn_reg();
  _exp2nmdar_reg();
  _GABAa_DynSyn_reg();
  _GABAb_DynSyn_reg();
  _hh2_reg();
  _iCaAN_reg();
  _iCaL_reg();
  _iKCa_reg();
  _NMDA_DynSyn_reg();
}

#if defined(__cplusplus)
}
#endif
