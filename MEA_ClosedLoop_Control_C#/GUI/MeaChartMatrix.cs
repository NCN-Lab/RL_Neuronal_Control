using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using MCS_Devices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace GUI
{
    public class MeaChartMatrix
    {
        int chartWidth;
        int chartHeight;
        int yLims;
        int nSeries;
        Color[] colors;

        MeaElecCoords elecCoordsManager;
        ElecIDsManager elecIDsManager;
        List<int> elec_IDs;
        int[,] chartCoords;
        private Chart[] allCharts;
  //      Control.ControlCollection controls;

        public MeaChartMatrix(MeaLayoutEnum meaLayout, Control.ControlCollection controls, int nSeries = 1, int yLims = 200, Color[] colors = null, int chartWidth = 60, int chartHeight = 50,
            int left_x = 35, int bottom_y = 80, int wellSpacing = 50)
        {
            this.chartWidth = chartWidth;
            this.chartHeight = chartHeight;
     //       this.controls = controls;
            this.nSeries = nSeries;
            this.yLims = yLims;
            
            if (colors == null || colors.Length == 0)
                colors = new Color[] { Color.Blue };
            this.colors = colors;

            // Electrodes coordinates:
            elecCoordsManager = new MeaElecCoords(meaLayout, left_x, bottom_y, chartWidth, chartHeight, wellSpacing);
            chartCoords = elecCoordsManager.GetElecCoords();

            // Electrodes IDs:
            elecIDsManager = new ElecIDsManager(meaLayout);
            elec_IDs = elecIDsManager.GetAllIDs();

            // Create Chart objects
            allCharts = CreateMeaCharts(controls);

        }

        public void PlotMeaData(List<double[]> data, double[] time = null, int serie_i = 0) 
        {           
    
            for (int chart = 0; chart < allCharts.Length; chart++) 
            {
                if (time == null)
                    allCharts[chart].Series[serie_i].Points.DataBindY(data[elec_IDs[chart]]);
                else
                    allCharts[chart].Series[serie_i].Points.DataBindXY(time, data[elec_IDs[chart]]);
            }
        }

        public void PlotMeaData(double[] data)
        {
            for (int chart = 0; chart < allCharts.Length; chart++)
            {
                    allCharts[chart].Series[0].Points.AddY(data[elec_IDs[chart]]);
            }
        }

        public void PlotMeaData(double[] data, double time, int serie_i = 0)
        {
            for (int chart = 0; chart < allCharts.Length; chart++)
            {
                allCharts[chart].Series[serie_i].Points.AddXY(time, data[elec_IDs[chart]]);
            }

        }


        public void PlotHorizontalLines(double[] data, double[] time, int serie_i = 0)
        {
            for (int chart = 0; chart < allCharts.Length; chart++) 
            {
                double[] horzData = new double[2] { data[chart], data[chart] };
                //double[] horzData = new double[2] { data[elec_IDs[chart]], data[elec_IDs[chart]] };
                allCharts[chart].Series[serie_i].Points.DataBindXY(time, horzData);
            }
        }


        private Chart[] CreateMeaCharts(Control.ControlCollection controls)
        {
            Chart[] allCharts = new Chart[elecCoordsManager.Get_nElecs()];

            for (int elec = 0; elec < elecCoordsManager.Get_nElecs(); elec++)
            {
                Chart chart = new Chart();

                chart.Left = chartCoords[0, elec];
                chart.Top = chartCoords[1, elec];
                chart.Width = chartWidth;
                chart.Height = chartHeight;

                chart.ChartAreas.Add("area1");
                chart.ChartAreas[0].Position.X = 0;
                chart.ChartAreas[0].Position.Y = 0;
                chart.ChartAreas[0].Position.Height = 100;
                chart.ChartAreas[0].Position.Width = 100;
                chart.ChartAreas[0].AxisX.LabelStyle.Enabled = false;
                chart.ChartAreas[0].AxisY.LabelStyle.Enabled = false;
                chart.ChartAreas[0].AxisY.Maximum = yLims;
                chart.ChartAreas[0].AxisY.Minimum = -yLims;

                chart.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
                chart.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
                chart.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
                chart.ChartAreas[0].AxisY.MajorGrid.Enabled = false;


                for (int serie = 0; serie < nSeries; serie++)
                {
                    chart.Series.Add("series" + serie.ToString());
                    chart.Series["series" + serie.ToString()].ChartType = SeriesChartType.FastLine;
                    chart.Series["series" + serie.ToString()].Color = colors[serie];
                }

                allCharts[elec] = chart;
                controls.Add(allCharts[elec]);
            }
            return allCharts;
        }

        public int[] Get_Charts_Size()
        {            
            int max_x = 0;
            int max_y = 0;
            for (int i = 0; i < allCharts.Length; i++)
            {
                if (allCharts[i].Location.X > max_x)
                    max_x = allCharts[i].Location.X;

                if (allCharts[i].Location.Y > max_y)
                    max_y = allCharts[i].Location.Y;
            }
            int[] size = new int[2] {max_x, max_y};
            return size;
        }

        public void Set_X_lims(double x_min, double x_max)
        {
            for (int i = 0; i < allCharts.Length; i++)
            {
                allCharts[i].ChartAreas[0].AxisX.Maximum = x_max;
                allCharts[i].ChartAreas[0].AxisX.Minimum = x_min;
            }
        }

        public Chart[] Get_MeaCharts()
        {
            return allCharts;
        }
    }
}
