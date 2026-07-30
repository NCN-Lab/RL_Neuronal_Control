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
using System.Windows.Forms.DataVisualization.Charting;
//using General_Logic;
using MCS_Devices;
using System.Runtime.InteropServices.ComTypes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

// Just for development!! Delete this later:
using System.Timers;
using System.Reflection;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;


namespace GUI
{
    public partial class ThresholderForm : Form
    {
        // Thresholder object for Logic:
        Thresholder thresholder;

        double nSTDs;

        // double[] data; // acquired mea data block
        List<double[]> data;

        double window_s = 0.1; // time window for calculating the threshold
        int nFrames;

        // Mea Charts: elec recording / positive thresh / negative thresh  
        int nSeries = 3;
        int yLims = 200;
        Color[] colors = new Color[3] { Color.Red, Color.Blue, Color.Blue };

        int[] canvas_size;

        // MeaElecCoords elecCoordsManager;

        // Initiatize Devices:
        MeaDacq mea;

        // Auxiliar functions
        MeaChartMatrix meaCharts;
        ElecIDsManager elecIDmanager;

        Filter filter;

        public ThresholderForm(Control Parent, MeaDacq mea_dev, int std_thresh = 5)
        {
            InitializeComponent();
         
            // MEA device:
            mea = mea_dev;
            
            elecIDmanager = new ElecIDsManager(mea.Get_MeaLayout());

            // Threshold Calculator:
            nSTDs = std_thresh;
            thresholder = new Thresholder(elecIDmanager, mea.Get_sampleRate(), nStds: std_thresh);

            // Charts Creator:
            meaCharts = new MeaChartMatrix(mea.Get_MeaLayout(), Controls, nSeries, yLims, colors);
            
            // Filter:
            double HP_cut_Hz = 200;
            filter = new Filter(mea, HP_cut_Hz);
            filter.HP_Filter();            

            // GUI elements:
            btn_set_Filter.Enabled = false;
            txt_Stds.Text = nSTDs.ToString();
            txt_Filter.Text = filter.Get_FreqCut_Hz().ToString();
            Set_CanvasSize();
            FormBorderStyle = FormBorderStyle.None;
            //Size = Parent.Size;
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
        }

        public ThresholderForm(MeaDacq mea_dev, int std_thresh = 5)
        {
            InitializeComponent();

            // MEA device:
            mea = mea_dev;

            elecIDmanager = new ElecIDsManager(mea.Get_MeaLayout());

            // Threshold Calculator:
            nSTDs = std_thresh;
            thresholder = new Thresholder(elecIDmanager, mea.Get_sampleRate(), nStds: std_thresh);

            // Charts Creator:
            meaCharts = new MeaChartMatrix(mea.Get_MeaLayout(), Controls, nSeries, yLims, colors);

            // Filter:
            double HP_cut_Hz = 200;
            filter = new Filter(mea, HP_cut_Hz);
            filter.HP_Filter();

            // GUI elements:
            btn_set_Filter.Enabled = false;
            txt_Stds.Text = nSTDs.ToString();
            txt_Filter.Text = filter.Get_FreqCut_Hz().ToString();
            Set_CanvasSize();
            //FormBorderStyle = FormBorderStyle.None;
            //Size = Parent.Size;
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
        }


        private void Set_CanvasSize()
        {
            canvas_size = meaCharts.Get_Charts_Size();
            Size = new Size(canvas_size[0] + 150, canvas_size[1] + 175);
        }


        public int[] Get_Canvas_Size()
        {
            return canvas_size;
        }


        private void ThreshForm_ChannelDataEvent(CMcsUsbDacqNet dacq, int CbHandle, int numFrames)
        {
            (data,_,_) = mea.ReadData_uV_(numFrames);
            mea.StopDacq();

            thresholder.Calc_AutoThresholds(data);
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(DisplayData));
            }
        }


        private void DisplayData()
        {
            // Plot Time-series:
            //int nFrames = (int)Math.Round(window_s * mea.Get_sampleRate());
            double[] time = new double[data[0].Length];
            for (int i = 0; i < data[0].Length; i++)
            {
                time[i] = i;
            }

            meaCharts.PlotMeaData(data, time, 0);

            double[] elecThreshesByInd = new double[elecIDmanager.GetNumberOfElectrodes()];
            double[] negElecThreshesByInd = new double[elecThreshesByInd.Length];

            int ind = 0;
            foreach (var thresh in thresholder.Get_Thresholds_uV())
            {
                elecThreshesByInd[ind] = thresh;
                negElecThreshesByInd[ind] = -thresh;
                ind++;
            }

            //foreach (var label in elecIDmanager.GetAllElectrodeLabels())
            //{
            //    int id = label.id;
            //    int index = elecIDmanager.GetIndexFromID(id);

            //    if (index >= 0 && index < elecThreshesByID.Length)
            //    {
            //        elecThreshesByInd[index] = elecThreshesByID[id];
            //        negElecThreshesByInd[index] = -elecThreshesByID[id];
            //    }
            //}

            double[] plotLims = new double[] { 0, nFrames };

            meaCharts.PlotHorizontalLines(elecThreshesByInd, plotLims, 1);
            meaCharts.PlotHorizontalLines(negElecThreshesByInd, plotLims, 2);
        }


        private void btn_startDacq_Click(object sender, EventArgs e)
        {
            if (!mea.isConnected())
                mea.Connect();
            nSTDs = double.Parse(txt_Stds.Text);
            thresholder.Set_nSTDs_thresh(nSTDs);

            nFrames = (int)Math.Round(window_s * mea.Get_sampleRate());
            mea.SetDataCallback(ThreshForm_ChannelDataEvent);
            mea.Set_nFrames(nFrames);
            mea.ConfigureDacq();
            mea.StartDacq();
        }


        private void btn_set_filter_Click(object sender, EventArgs e)
        {
            double HP_Hz = Convert.ToDouble(txt_Filter.Text);
            filter.HP_Filter(HP_Hz);
            btn_set_Filter.Enabled = false;
        }


        private void txt_filter_TextChanged(object sender, EventArgs e)
        {
            btn_set_Filter.Enabled = true;
        }

        private void text_stds_TextChanged(object sender, EventArgs e)
        {

        }

        public Thresholder Get_Thresholder()
        {
            return thresholder;
        }
    }
}
