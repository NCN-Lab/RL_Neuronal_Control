//using General_Logic;
using Mcs.Usb;
using MCS_Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Windows.Forms.DataVisualization.Charting;

namespace GUI
{
    public partial class SpikesForm : Form
    {
        MeaDacq mea;
        SpikeDetector spkDetector;

        ThresholderForm threshTab;
        MonitorElecsForm monitorElecsTab;
        MeaButtonMatrix monitorElecButtons;
        MeaChartMatrix SpkDetectCharts;

        int clock_i = 0;
        double clock_s = 0;
        double dt;
        List<int> monitorElec_IDs = new List<int>();
        double[] mea_Data;

        bool detectingSpikes = false;
        List<int> last_spiked_elec_IDs = new List<int>(); // IDs of electrodes that spiked in the previous frame
        List<int> plot_spike_elec_IDs = new List<int>();

        List<List<int>> spk_times_buffer;
        
        bool update_plot = true;
        double past_s = 5;
        double future_s = 2;



        // Timer:
        long lastSamp_ticks = 0; // time of the last sampling
        double samplingInterval_ticks;
        bool moveWindow = false;

        Random random = new Random();


        public SpikesForm(MeaDacq meaDaq)
        {
            mea = meaDaq;

            InitializeComponent();

            // Thresholder Form:
            threshTab = new ThresholderForm(thresholder_tab, mea);
            int[] tab_size = threshTab.Get_Canvas_Size();
            thresholder_tab.Size = new Size(tab_size[0] + 100, tab_size[1] + 100);
            tab_container.Size = new Size(tab_size[0] + 100, tab_size[1] + 105);
          
            threshTab.TopLevel = false;
            threshTab.Show();
            thresholder_tab.Controls.Add(threshTab);

            Size = new Size(tab_size[0] + 150, tab_size[1] + 175);

            // Monitoring Electrodes Form:
            monitorElecsTab = new MonitorElecsForm(MonitorElecs_tab, mea);
            monitorElecsTab.TopLevel = false;
            monitorElecsTab.Show();
            MonitorElecs_tab.Controls.Add(monitorElecsTab);

            ElecIDsManager elecIDsManager = new ElecIDsManager(mea.Get_MeaLayout());
            spkDetector = new SpikeDetector(mea.Get_nElecs(), elecIDsManager, threshTab.Get_Thresholder());

            // Spike Detection Tab:
            dt = 1 / (double)mea.Get_sampleRate();
            start_button.Enabled = true;
            stop_button.Enabled = false;
            past_txt.Text = past_s.ToString();
            future_txt.Text = future_s.ToString();
            deadtime_textbox.Text = (spkDetector.Get_deadtime()*1000).ToString();
            raster_chart.ChartAreas[0].AxisX.Maximum = past_s + future_s;
            raster_chart.ChartAreas[0].AxisX.Minimum = 0;
        }



        void mea_ChannelDataEvent_SpkDetect(CMcsUsbDacqNet dacq, int CbHandle, int numFrames)
        {
                (List<double[]> data, double[] timestamps_s, _) = mea.ReadData_uV_(numFrames);                
                spkDetector.DetectSpikes(data, timestamps_s);
            //    if (last_spiked_elec_IDs.Count > 0)
            //    {
            //        plot_spike_elec_IDs = new List<int>();
            //        for (int i = 0; i < last_spiked_elec_IDs.Count; i++)
            //        {
            //            plot_spike_elec_IDs.Add(last_spiked_elec_IDs[i]);
            //        }
            //        UpdatePlot();
            //    }
              
            //    // Update clock:
            //    clock_i++;
            //    clock_s = (double)clock_i / (double)mea.Get_sampleRate() * (double)mea.Get_nFrames();

            //double max_X = raster_chart.ChartAreas[0].AxisX.Maximum;    
            //if (clock_s + future_s > max_X)
            //        moveWindow = true;                
        }


        private void start_button_Click(object sender, EventArgs e)
        {
            detectingSpikes = true;
            start_button.Enabled = false;
            stop_button.Enabled = true;

            // Set callback for spike detection (so that it overwrites the Thresholder event callback)
            mea.SetDataCallback(mea_ChannelDataEvent_SpkDetect);

            spkDetector.Set_Thresholder(threshTab.Get_Thresholder());
            spkDetector.Set_MonitoringElecs(monitorElecsTab.Get_MonitoringElecs());
            spkDetector.Set_Deadtime_sec(Convert.ToDouble(deadtime_textbox.Text)/1000);
            clock_s = 0;
            spkDetector.Restart_Clock();

            // Start DAQ
            mea.Set_nFrames(1);
            mea.ConfigureDacq();
            mea.StartDacq();
        }
       

        private void UpdatePlot()
        {
            if (raster_chart.InvokeRequired)
            {
                raster_chart.Invoke(new Action(() =>
                {
                    UpdatePlot();
                }
                ));
            }
            else
            {
                DisplayData();
            }
        }


        private void DisplayData()
        {   
            for (int i = 0; i < plot_spike_elec_IDs.Count; i++)
                raster_chart.Series[0].Points.AddXY(clock_s, plot_spike_elec_IDs[i]);

            // Move time window forward
            if (moveWindow)
            {
                moveWindow = false;
                raster_chart.ChartAreas[0].AxisX.Maximum = clock_s + future_s;
                raster_chart.ChartAreas[0].AxisX.Minimum = clock_s - past_s;
            }
        }



            private void set_past_button_Click(object sender, EventArgs e)
        {
            past_s = Convert.ToDouble(past_txt.Text);
        }

        private void set_future_button_Click(object sender, EventArgs e)
        {
            future_s = Convert.ToDouble(future_txt.Text);
        }

        private void stop_button_Click(object sender, EventArgs e)
        {
            mea.StopDacq();
            detectingSpikes = false;

            stop_button.Enabled = false;
            start_button.Enabled=true;
        }

        private void SpkDetector_tab_Click(object sender, EventArgs e)
        {

        }

        private void raster_chart_Click(object sender, EventArgs e)
        {

        }
    }
}
