using MCS_Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mcs.Usb;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace GUI
{
    public partial class Main : Form
    {
        MeaDacq dacq;
        int[] availableSampleRates = new int[] { 1000, 2000, 5000, 10000, 20000, 25000, 50000 };

        ThresholderForm thresholderForm;

        public Main()
        {
            InitializeComponent();
            dacq = new MeaDacq();
            FillDeviceList();
            FillMeaLayoutList();
            FillSamplingRatesList();
        }


        public void FillDeviceList()
        {
            List_devices.Items.Clear();
            List_devices.Items.AddRange(dacq.Get_Available_MeaUsbEntries());
            if (dacq.Get_Available_MeaUsbEntries().Length > 0)
            {// Connect to first available USB port
                List_devices.SelectedIndex = 0;
                dacq.Set_MeaUsbEntry(0);
            }
            else 
            {
                // !!! Just for debugging !!!
                dacq.Set_MeaLayout(MeaLayoutEnum.MEA60_1well);
                dacq.Set_MeaUsbEntry(0);
                // !!! Just for debugging !!!
            }
        }

        public void FillMeaLayoutList()
        {
            if (dacq.Get_Available_MeaUsbEntries().Length > 0) 
            {
                List<MeaLayoutEnum> layouts = new List<MeaLayoutEnum>();

                string deviceName = dacq.Get_Selected_MeaUsbEntry_Name();

                if (deviceName.Contains("256"))
                {
                    layouts.Clear();
                    layouts.Add(MeaLayoutEnum.MEA256_1well);
                    layouts.Add(MeaLayoutEnum.MEA256_6well);
                    layouts.Add(MeaLayoutEnum.MEA256_9well);
                }
                else if (deviceName.Contains("Mini"))
                {
                    layouts.Clear();
                    layouts.Add(MeaLayoutEnum.MEA60_1well);
                    layouts.Add(MeaLayoutEnum.MEA60_6well);
                }
                List_MeaLayout.Items.Clear();
                List_MeaLayout.Items.AddRange(layouts.Cast<object>().ToArray());
                List_MeaLayout.SelectedIndex = 1;
            }
             
           
        }


        public void FillSamplingRatesList() 
        {
            SamplingRatesList.Items.Clear();

            SamplingRatesList.Items.AddRange(availableSampleRates.Cast<object>().ToArray());
            if (dacq.Get_Available_MeaUsbEntries().Length > 0)
            {
                SamplingRatesList.SelectedIndex = 3; // 10000 Hz
                dacq.Set_SamplingRate(availableSampleRates[SamplingRatesList.SelectedIndex]);
            }
        }

        private void Button_ElecData_Click(object sender, EventArgs e)
        {
            ElecDataForm elecDataForm = new ElecDataForm(dacq);
            elecDataForm.Show();
        }

        private void List_devices_SelectedIndexChanged(object sender, EventArgs e)
        {
            dacq.Set_MeaUsbEntry(List_devices.SelectedIndex);
            FillMeaLayoutList();
        }

        private void List_MeaLayout_SelectedIndexChanged(object sender, EventArgs e)
        {
            MeaLayoutEnum layout = (MeaLayoutEnum)Enum.Parse(typeof(MeaLayoutEnum), List_MeaLayout.SelectedItem.ToString());
            dacq.Set_MeaLayout(layout);
        }

        //private void button6_Click(object sender, EventArgs e)
        //{
        //    StimulatorForm stimulatorForm = new StimulatorForm(meaDaq);
        //    stimulatorForm.Show();
        //}

        private void SamplingRatesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            dacq.Set_SamplingRate(availableSampleRates[SamplingRatesList.SelectedIndex]);
        }

        private void Button_SpikesForm_Click(object sender, EventArgs e)
        {
            SpikesForm spikeDetectorForm = new SpikesForm(dacq);
            spikeDetectorForm.Show();
        }

        private void button_newRL_Click(object sender, EventArgs e)
        {
            Form1 form = new Form1(dacq, thresholderForm.Get_Thresholder());
            form.Show();
        }

        private void button_thresholding_Click(object sender, EventArgs e)
        {
            thresholderForm = new ThresholderForm(dacq);
            thresholderForm.Show();
        }
    }
}
