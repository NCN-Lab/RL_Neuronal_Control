using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MCS_Devices;
//using General_Logic;
using Mcs.Usb;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml;
using System.Diagnostics;

namespace GUI
{
    public partial class ElecDataForm : Form
    {
        MeaDacq meaDaq;
        ElecIDsManager elecManager;
        int recElec_ind;
        int recElec_ID;
        List<double[]> data;
        int nFrames;
        MeaButtonMatrix buttonsMatrix;

        public ElecDataForm(MeaDacq meadaq)
        {
            InitializeComponent();

            meaDaq = meadaq;

            // Create Matrix of Buttons
            elecManager = new ElecIDsManager(meaDaq.Get_MeaLayout());

            int elecSide = 40;
            int left_x = 600;
            int bottom_y = 60;
            buttonsMatrix = new MeaButtonMatrix(meaDaq.Get_MeaLayout(), Controls, ButtonClick, elecSide, left_x, bottom_y);

            recElec_ind = 0;
            recElec_ID = elecManager.GetIDFromIndex(recElec_ind);
            buttonsMatrix.ChangeButtonColors(recElec_ind, Color.LightSeaGreen);
            Set_CanvasSize();
        }


        private void Set_CanvasSize()
        {
            int[] size = buttonsMatrix.Get_buttons_size();
            Size = new Size(size[0] + 150, size[1] + 175);
        }


        private void ElecDataForm_Load(object sender, EventArgs e){}

        private void ButtonClick(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            buttonsMatrix.UnselectButton(recElec_ind);
            recElec_ind = Convert.ToInt32(clickedButton.Name);
            buttonsMatrix.SelectButton(recElec_ind);
            recElec_ID = elecManager.GetIDFromIndex(recElec_ind);
        }

        private void Button_startDaq_Click(object sender, EventArgs e)
        {
            meaDaq.Connect();
            meaDaq.SetDataCallback(ElecDataForm_ChannelDataEvent); 
            // Set nFrames based on recording window selected:
            double recWindow_s = Convert.ToDouble(Text_recBlock.Text);
            nFrames = (int)Math.Round(meaDaq.Get_sampleRate() * recWindow_s);

            meaDaq.Set_nFrames(nFrames);
            meaDaq.ConfigureDacq(); 
            meaDaq.StartDacq();
        }

        private void Button_stopDaq_Click(object sender, EventArgs e)
        {
            meaDaq.StopDacq();            
            meaDaq.Disconnect();  
        }

           
        
        private void ElecDataForm_ChannelDataEvent(CMcsUsbDacqNet dacq, int CbHandle, int numFrames)
        {
            (data,_,_) = meaDaq.ReadData_uV_(numFrames);
            BeginInvoke(new Action(DisplayData));          
        }

        private void DisplayData()
        {
            double[] elecData = data[recElec_ID];
            Chart_ElecRec.Series[0].Points.Clear();
            
            for (int i = 0; i < elecData.Length; i++)
            {
                Chart_ElecRec.Series[0].Points.AddY(elecData[i]);
            }            
        }
    }
}
