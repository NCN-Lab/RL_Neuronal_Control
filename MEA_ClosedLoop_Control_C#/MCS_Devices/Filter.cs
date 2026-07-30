using System;
using System.Collections.Generic;
using System.Text;
using Mcs.Usb;

namespace MCS_Devices
{
    public class Filter
    {
        // Filters Data using the DSP [I GUESS], only works for USB-B, USB-A remains ulfiltered (for MEA60 it also works with USB-A)
        double HP_cut_Hz;
        MeaDacq mea;

        public Filter(MeaDacq meaDaq, double HP_Hz = 200)
        {
            mea = meaDaq;
            HP_cut_Hz = HP_Hz;
        }

 
        public void HP_Filter( )
        {
            if (!mea.isConnected())
                mea.Connect();
            WriteToReg();
        }

        public void HP_Filter(double HP_Hz)
        {
            HP_cut_Hz = HP_Hz;
            if (!mea.isConnected())
                mea.Connect();
            WriteToReg();
        }

        // SetupHardwareFilters
        private void WriteToReg()
        {
                CMeaUSBDeviceNet mea_obj = mea.Get_dacq();
                double[] xcoeffs;
                double[] ycoeffs;
                mkfilterNet.mkfilter("Bu", 0, "Hp", 2, HP_cut_Hz / 50000.0, 0, out xcoeffs, out ycoeffs);
                mea_obj.WriteRegister(0xc00, DoubleToFixedInt(1, 16, 30, xcoeffs[0]));
                mea_obj.WriteRegister(0xc02, DoubleToFixedInt(1, 15, 30, xcoeffs[1]));
                mea_obj.WriteRegister(0xc03, DoubleToFixedInt(1, 30, 30, ycoeffs[1]));
                mea_obj.WriteRegister(0xc04, DoubleToFixedInt(1, 16, 30, xcoeffs[2]));
                mea_obj.WriteRegister(0xc05, DoubleToFixedInt(1, 30, 30, ycoeffs[2]));
                mea_obj.WriteRegister(0xc07, 0x00000001); // enable
                mea_obj.WriteRegister(0x880, 2); // Send data Filtered By DSP
            }

        public void Set_Freq_Cut_Hz(double HP_freq_cut, CMeaUSBDeviceNet mea)
        {
            HP_cut_Hz = HP_freq_cut;
        }


        public void Set_Freq_Cut_Hz(double HP_freq_cut)
        {
            HP_cut_Hz = HP_freq_cut;
        }


        public double Get_FreqCut_Hz()
        {
            return HP_cut_Hz;
        }

        uint DoubleToFixedInt(int vk, int nk, int commaPos, double valF)
        {
            valF *= 1 << nk;
            if (valF > 0)
            {
                valF += 0.5;
            }
            else
            {
                valF -= 0.5;
            }
            ulong mask = ((ulong)1 << (vk + nk + 1)) - 1;
            ulong val = (ulong)valF;
            uint value = (uint)(val & mask);
            if (commaPos > nk)
            {
                value = value << (commaPos - nk);
            }

            return value;
        }
    }
}
