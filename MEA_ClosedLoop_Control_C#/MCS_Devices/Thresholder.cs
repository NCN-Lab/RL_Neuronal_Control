using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCS_Devices
{
    public class Thresholder
    {
        readonly ElecIDsManager elecManager;
        int nElecs;
        int nFrames;    // number of frames to read
        double std_thresh;
        double[] thresholds_uv;
        int sampleRate = 10000;
        bool withAutoThresh = false;

        public Thresholder(ElecIDsManager elecManager, int samplerate, double nStds = 5.0, double timeWindow_s = 0.1)
        {
            this.elecManager = elecManager;
            nElecs = elecManager.GetNumberOfElectrodes();
            std_thresh = nStds;
            thresholds_uv = new double[elecManager.GetNumberOfElectrodes()];
            sampleRate = samplerate;
            nFrames = (int)Math.Round(sampleRate * timeWindow_s);
        }


        public double[] Calc_AutoThresholds(List<double[]> allData_uV)
        {
            foreach (var elecID in elecManager.GetAllIDs())
            {
                int elecInd = elecManager.GetIndexFromID(elecID);
                thresholds_uv[elecInd] = Calc_AutoThreshold(allData_uV[elecID]);
            }
            withAutoThresh = true;

            return thresholds_uv;
        }


        /// <summary>
        /// Calculate spike detection threhsold for a SINGLE electrode based on the MEA data in microvolts
        /// Returns the array of thresholds for all electrodes
        /// </summary>
        /// <param name="elec_data_uV">elec_data_uV</param>
        /// <returns>array of thresholds for all electrodes</returns>
        public double Calc_AutoThreshold(double[] elec_data_uV)
        {
            // Threshold:
            double avg = elec_data_uV.Average();
            double sumOfSquaresOfDifferences = elec_data_uV.Select(val => (val - avg) * (val - avg)).Sum();
            double std = Math.Sqrt(sumOfSquaresOfDifferences / elec_data_uV.Length);

            return std_thresh * std;
        }

        public void Set_sampleRate(int samplerate)
        {
            sampleRate = samplerate;
        }

        public void Set_nSTDs_thresh(double nStds)
        {
            std_thresh = nStds;
        }

        public void Set_time_window(double timeWindow_s)
        {
            nFrames = (int)Math.Round(sampleRate * timeWindow_s);
        }

        public void Set_nFrames(int nframes)
        {
            nFrames = nframes;
        }

        public void Set_Manual_elec_ID_threshold_uV(int elecID, double thresh_uV)
        {
            thresholds_uv[elecID] = thresh_uV;
            withAutoThresh = false;
        }


        public void Set_Manual_Thresholds(double thresh_uV)
        {
            for (int i = 0; i < nElecs; i++)
                thresholds_uv[i] = thresh_uV;

            withAutoThresh = false;
        }

        public void Set_Thresholds(double[] thresh_uV)
        {
            for (int i = 0; i < nElecs; i++)
                thresholds_uv[i] = thresh_uV[i];

            withAutoThresh = false;
        }

        public double[] Get_Thresholds_uV()
        {
            return thresholds_uv;
        }

        public double Get_nSTDs_thresh()
        {
            return std_thresh;
        }

        public bool WithAutoThresholds()
        {
            return withAutoThresh;
        }
    }
}
