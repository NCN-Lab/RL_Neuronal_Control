using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Mcs.Usb;

namespace MCS_Devices
{
    public class Stimulator
    {
        CStg200xDownloadNet Stg;
        CMcsUsbListNet UsbDeviceList;

        bool connected = false;
        bool isAutomatic = true;

        List<int> stimElecs_IDs_STG1;
        List<int> stimElecs_IDs_STG2;
        //  List<int> stimElecs_inds;

        /// <summary>
        /// triggerBitmap 01 - trigger 1 - STG 1 
        /// triggerBitmap 10 - trigger 2 -  STG 2 
        /// triggerBitmap 11 - trigger 3 -  STG 1 and 2
        /// </summary>
        uint STG_triggerBitmap;
        bool STG_1_downloaded = false;
        bool STG_2_downloaded = false;

        ElecIDsManager elecIDsManager;
        MeaLayoutEnum meaLayout;

        public Stimulator(MeaLayoutEnum MeaLayout) 
        {
            meaLayout = MeaLayout;
            Stg = new CStg200xDownloadNet();
            UsbDeviceList = new CMcsUsbListNet(DeviceEnumNet.MCS_DEVICE_USB);

            // Stimulation Electrodes:
            stimElecs_IDs_STG1 = new List<int>();
            stimElecs_IDs_STG2 = new List<int>();

            elecIDsManager = new ElecIDsManager(meaLayout);

            PrepareStimElectrodes();
        }

        public (uint, uint, uint, uint) GetNumberofChannels()
        {
            uint nChannels = Stg.GetNumberOfAnalogChannels();
            uint nSync = Stg.GetNumberOfSyncoutChannels();
            uint nStim = Stg.GetNumberOfStimulationSourcesPerElectrode();
            uint nTrigger = Stg.GetNumberOfTriggerInputs();

            return (nChannels, nSync, nStim, nTrigger);
        }


        public void ActivateStimulationElectrodes(int STG_ID)
        {          
            // Prepare all electrodes
            PrepareStimElectrodes();

            // Get stimElectrodes for the selected STG
            List<int> stimElecs_IDs = Get_STG_StimElecs_IDs(STG_ID);

            // Set Stimulation channel to stim electrode
            for (int i = 0; i < stimElecs_IDs.Count; i++)
            {
                Stg.SetElectrodeDacMux((uint)stimElecs_IDs[i], 0, (ElectrodeDacMuxEnumNet)STG_ID);
                Stg.SetElectrodeEnable((uint)stimElecs_IDs[i], 0, true);
            }
        }


        public void ActivateStimulationElectrodes(List<int>  stimElecs_IDs, int STG_ID)
        {
            // Prepare all electrodes
            PrepareStimElectrodes();

            if (STG_ID == 1)
                stimElecs_IDs_STG1 = stimElecs_IDs;
            else
                stimElecs_IDs_STG2 = stimElecs_IDs;

            Set_STG_StimElecs_IDs(stimElecs_IDs, STG_ID);

            // Set Stimulation channel to stim electrode
            for (int i = 0; i < stimElecs_IDs.Count; i++)
            {
                Stg.SetElectrodeDacMux((uint)stimElecs_IDs[i], 0, (ElectrodeDacMuxEnumNet)STG_ID);
                Stg.SetElectrodeEnable((uint)stimElecs_IDs[i], 0, true);
            }
        }

        public void PrepareWellElectrodes(IEnumerable<int> monitoredElecsIDs)
        {
            if (connected == false)
                Connect_USB_A();

            // Stimulation Electrode:
            foreach (var elec in monitoredElecsIDs)
            {

                // ElectrodeMode: emManual: electrode is permanently selected for stimulation
                Stg.SetElectrodeMode((uint)elec, isAutomatic ? ElectrodeModeEnumNet.emAutomatic : ElectrodeModeEnumNet.emManual); // REGISTERS: 0x9c70 -  0x9c77

                // ElectrodeEnable: disable previous stimulation electrode
                Stg.SetElectrodeEnable((uint)elec, 0, false);
                //Stg.SetElectrodeEnable((uint)elec, 0, true);

                // ElectrodeDacMux: DAC to use for stimulation
                Stg.SetElectrodeDacMux((uint)elec, 0, ElectrodeDacMuxEnumNet.Ground);
                //Stg.SetElectrodeDacMux((uint)elec, 0, ElectrodeDacMuxEnumNet.Stg1);

                // AmplifierProtectionSwitch: 
                // true: disconnect ADC to electrode while stimulation is running
                // false: Keep ADC connected to electrode even while stimulation is running
                Stg.SetEnableAmplifierProtectionSwitch((uint)elec, true);

                // BlankingEnable:
                // true: blank the ADC signal while stimulation is running
                // false: do not blank the ADC signal while stimulation is running
                Stg.SetBlankingEnable((uint)elec, false);
            }
        }

        public void PrepareStimElectrodes()
        {
            if (connected == false)
                Connect_USB_A();

            // Stimulation Electrode:
            for (uint elec = 0; elec < elecIDsManager.GetNumberOfElectrodes(); elec++)
            {

                // ElectrodeMode: emManual: electrode is permanently selected for stimulation
                Stg.SetElectrodeMode(elec, ElectrodeModeEnumNet.emAutomatic); // REGISTERS: 0x9c70 -  0x9c77

                // ElectrodeEnable: disable previous stimulation electrode
                Stg.SetElectrodeEnable(elec, 0, false);

                // ElectrodeDacMux: DAC to use for stimulation
                Stg.SetElectrodeDacMux(elec, 0, ElectrodeDacMuxEnumNet.Ground);
                //Stg.SetElectrodeDacMux(elec, 0, ElectrodeDacMuxEnumNet.Stg1);

                // AmplifierProtectionSwitch: 
                // true: disconnect ADC to electrode while stimulation is running
                // false: Keep ADC connected to electrode even while stimulation is running
                Stg.SetEnableAmplifierProtectionSwitch(elec, true);

                // BlankingEnable:
                // true: blank the ADC signal while stimulation is running
                // false: do not blank the ADC signal while stimulation is running
                Stg.SetBlankingEnable(elec, false);
            }
        }

        public void DownloadStimulus(int STG_ID, int[] amplitude_uV, ulong[] duration_us)
        {
            Connect_USB_A();
            Stg.SetVoltageMode();

            int[] SB_bits = new int[2] { 1 << 8, 0 }; // // user defined sideband (use bits > 8)
            int[] stimulusActive = new int[2] { 1, 0 };

            uint Bit0Time = 800; // bit0 (blanking switch) activation duration prolongation in µs
            uint Bit3Time = 800; // bit3 (stimulation switch) activation duration prolongation in µs
            uint Bit4Time = 40; // bit4 (stimulus selection switch) activation duration prolongation in µs

            Stg.PrepareAndSendData((uint)STG_ID - 1, amplitude_uV, duration_us, STG_DestinationEnumNet.channeldata_voltage);

            CStimulusFunctionNet.SidebandData SidebandData = Stg.Stimulus.CreateSideband(stimulusActive, SB_bits, duration_us, Bit0Time, Bit3Time, Bit4Time);
            Stg.PrepareAndSendData((uint)STG_ID - 1, SidebandData.Sideband, SidebandData.Duration, STG_DestinationEnumNet.syncoutdata);

            //// Ensure blanking lasts the full stimulation period + margin
            //ulong blankingMargin = 1000; // 1 ms post-stimulus blanking
            //int[] SB_bits = new int[2] { (1 << 0) | (1 << 3) | (1 << 4) | (1 << 8), 0 }; // in binary: 1 00011001, 00000       
            ////int[] SB_bits = new int[2] { 0x19, 0x00 }; // in binary:  11001, 00000 
            //ulong[] SBS_durs = new ulong[2] { duration_us[0] + duration_us[1] + blankingMargin, blankingMargin };
            //Stg.PrepareAndSendData((uint)STG_ID - 1, SB_bits, SBS_durs, STG_DestinationEnumNet.syncoutdata);


            // Atribute STG channel map to trigger
            uint channelmap = (uint)((STG_ID == 1) ? 0x1 : 0x2); // STG 1 → Trigger 1, STG 2 → Trigger 2
            uint repetitions = 1;

            // Setup the trigger
            Stg.SetupTriggerSingle((uint)STG_ID - 1, channelmap, channelmap, repetitions);

            if (STG_ID == 1)
                STG_1_downloaded = true;
            else if (STG_ID == 2)
                STG_2_downloaded = true;
        }


        public void Set_Stimulation_Triggers(bool trigger_STG_1, bool trigger_STG_2) 
        {        
            // triggerBitmap 01 - trigger 1 - STG 1 
            // triggerBitmap 10 - trigger 2 - STG 2 
            // triggerBitmap 11 - trigger 3 - STG 1 and 2        
            uint channelmap = 0;

            if (trigger_STG_1)
            {
                channelmap = channelmap + 01; // First bit to select STG 1
            }

            if (trigger_STG_2)
            {
                channelmap = channelmap + 10;  // Second bit to select STG 2
            }

            STG_triggerBitmap = channelmap;
        }


        public void Stimulate(uint triggerbitmap)
        {
            Stg.SendStart(triggerbitmap);
        }
        
        public void Stimulate()
        {
            Stg.SendStart(STG_triggerBitmap);
        }
       

        // Stimulator needs to connect via USB-A
        public bool Connect_USB_A()
        {
            // Connect to Stimulator via USB-A:
            CMcsUsbListEntryNet[] entries = UsbDeviceList.GetUsbListEntries();

            uint USB_A = 0;
            if (entries[0].SerialNumber.Last() == 'B')
                USB_A = 1;

            uint status = Stg.Connect(UsbDeviceList.GetUsbListEntry(USB_A), 0);

            if (status == 0)
                connected = true;

            return connected;
        }


        public void Disconnect()
        {
            Stg.Disconnect();
            connected = false;
        }


        // Connect Stim Electrodes to one STG:
        // Can be done once for all available stim electrode. Then you can just enable/disable individual stim electrodes.
        public void Set_DacMux_StimElecs(int STG_ID)
        {
            // Get stimElectrodes for the selected STG
            List<int> stimElecs_IDs = Get_STG_StimElecs_IDs(STG_ID);

            for (int i = 0; i < stimElecs_IDs.Count(); i++)
            {
                Stg.SetElectrodeDacMux((uint)stimElecs_IDs[i], 0, (ElectrodeDacMuxEnumNet)STG_ID); 
            }
        }


        // Connect Stim Electrodes to STG DAC:
        // Disconnect the STG Mux for all stim electrodes. Useful when you change the full set of stim electrodes
        public void unSet_DacMux_StimElecs(int STG_ID)
        {
            // Get stimElectrodes for the selected STG
            List<int> stimElecs_IDs = Get_STG_StimElecs_IDs(STG_ID);

            for (int i = 0; i < stimElecs_IDs.Count(); i++)
            {
                Stg.SetElectrodeDacMux((uint)stimElecs_IDs[i], 0, ElectrodeDacMuxEnumNet.Ground); 
            }
        }


        // Enable Stim Electrodes:
        public void Enable_all_StimElecs(int STG_ID)
        {
            // Get stimElectrodes for the selected STG
            List<int> stimElecs_IDs = Get_STG_StimElecs_IDs(STG_ID);

            for (int i = 0; i < stimElecs_IDs.Count(); i++)
            {
                Stg.SetElectrodeEnable((uint)stimElecs_IDs[i], 0, true);
            }
        }


        // Activate = Set DacMux and Enable
        public void Activate_StimElecID(int elec_ID, int STG_ID)
        {
            Stg.SetElectrodeDacMux((uint)elec_ID, 0, (ElectrodeDacMuxEnumNet)STG_ID);
            Stg.SetElectrodeEnable((uint)elec_ID, 0, true);
        }

        // Deactivate = Unet DacMux and Disable
        public void Deactivate_StimElecID(int elec_ID)
        {
            Stg.SetElectrodeEnable((uint)elec_ID, 0, false);
            Stg.SetElectrodeDacMux((uint)elec_ID, 0, ElectrodeDacMuxEnumNet.Ground);
        }

        // Activate = Set DacMux and Enable
        // Connect Stim Electrodes to STG DAC and Enable:
        // Use this if you want to use all the stimulation electrodes for every stimulus
        public void Activate_all_StimElecs(int STG_ID)
        {
            // Get stimElectrodes for the selected STG
            List<int> stimElecs_IDs = Get_STG_StimElecs_IDs(STG_ID);

            for (int i = 0; i < stimElecs_IDs.Count(); i++)
            {
                Stg.SetElectrodeDacMux((uint)stimElecs_IDs[i], 0, (ElectrodeDacMuxEnumNet)STG_ID); 
                Stg.SetElectrodeEnable((uint)stimElecs_IDs[i], 0, true);   
            }
        }

        // Disable all Stim Electrodes:
        public void Disable_all_STG_StimElecs(int STG_ID)
        {
            // Get stimElectrodes for the selected STG
            List<int> stimElecs_IDs = Get_STG_StimElecs_IDs(STG_ID);

            for (int i = 0; i < stimElecs_IDs.Count(); i++)
            {
                Stg.SetElectrodeEnable((uint)stimElecs_IDs[i], 0, false); 
            }
        }

        // Disable all Stim Electrodes:
        public void Disable_Full_MEA()
        {
            for (uint i = 0; i < elecIDsManager.GetNumberOfElectrodes(); i++)
            {
                Stg.SetElectrodeEnable(i, 0, false); 
            }
        }

        // Deactivate = Unset DacMux and Disable
        public void Deactivate_all_STG_StimElecs(int SGT_ID)
        {
            // Get stimElectrodes for the selected STG
            List<int> stimElecs_IDs = Get_STG_StimElecs_IDs(SGT_ID);

            for (int i = 0; i < stimElecs_IDs.Count(); i++)
            {
                Stg.SetElectrodeDacMux((uint)stimElecs_IDs[i], 0, ElectrodeDacMuxEnumNet.Ground); 
                Stg.SetElectrodeEnable((uint)stimElecs_IDs[i], 0, false); 
            }
        }


        // Deactivate = Unset DacMux and Disable
        public void Deactivate_Full_MEA()
        {
            for (uint i = 0; i < elecIDsManager.GetNumberOfElectrodes(); i++)
            {
                Stg.SetElectrodeDacMux(i, 0, ElectrodeDacMuxEnumNet.Ground); 
                Stg.SetElectrodeEnable(i, 0, false); 
            }
        }


        // Enable individual Stim Electrode:
        public void Enable_StimElec_ID(uint elec_ID)
        {
            Stg.SetElectrodeEnable(elec_ID, 0, true);
        }

        // Enable Stim Electrodes:
        public void Enable_StimElec_IDs(int[] elec_IDs)
        {
            foreach (var elec_ID in elec_IDs)
            {
                Stg.SetElectrodeEnable((uint)elec_ID, 0, true);
            }
        }


        // Disable individual Stim Electrode:
        public void Disable_StimElec_ID(uint elec_ID)
        {
            Stg.SetElectrodeEnable(elec_ID, 0, false);
        }

        // Disable Stim Electrodes:
        public void Disable_StimElec_IDs(int[] elec_IDs)
        {
            foreach (var elec_ID in elec_IDs)
            {
                Stg.SetElectrodeEnable((uint)elec_ID, 0, false);
            }
        }



        public List<int> Get_STG_StimElecs_IDs(int STG_ID)
        {
            // Get stimElectrodes for the selected STG
            List<int> stimElecs_IDs;
            if (STG_ID == 1)
                stimElecs_IDs = stimElecs_IDs_STG1;
            else
                stimElecs_IDs = stimElecs_IDs_STG2;
            
            return stimElecs_IDs;
        }


        //------------------------------------------
        //        Add Stimulation Electrodes:
        public void Set_STG_StimElecs_IDs(IEnumerable<int> IDs, int STG_ID)
        {
            if (STG_ID == 1)
            {
                stimElecs_IDs_STG1 = IDs.ToList();
            }
            else if (STG_ID == 2)
            {
                stimElecs_IDs_STG2 = IDs.ToList();
            }
        }

        public void Add_STG_StimElec_ID(int ID, int STG_ID)
        {
            List<int> stimElecs_IDs = Get_STG_StimElecs_IDs(STG_ID);
            stimElecs_IDs.Add(ID);
            Set_STG_StimElecs_IDs(stimElecs_IDs, STG_ID);
        }


        public void Add_STG_StimElecs_IDs(int[] ID, int STG_ID)
        {
            List<int> stimElecs_IDs = Get_STG_StimElecs_IDs(STG_ID);
            stimElecs_IDs.AddRange(ID);
            Set_STG_StimElecs_IDs(stimElecs_IDs, STG_ID);
        }
        //         Add Stimulation Electrodes
        //------------------------------------------

        public void Remove_STG_StimElec_ID(int ID, int STG_ID)
        {
            List<int> stimElecs_IDs = Get_STG_StimElecs_IDs(STG_ID);
            stimElecs_IDs.Remove(ID);
            Set_STG_StimElecs_IDs(stimElecs_IDs, STG_ID);
        }

        public bool Get_STG_1_downloaded()
        {
            return STG_1_downloaded;
        }

        public bool Get_STG_2_downloaded()
        {
            return STG_2_downloaded;
        }

        public CStg200xDownloadNet Get_Stimulator()
        {
            return Stg;
        }
    }
}
