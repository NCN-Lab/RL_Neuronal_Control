using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCS_Devices
{
    public class SpikeDetector
    {
        /// <summary>
        /// Object that manages the electrode thresholds
        /// </summary>
        private Thresholder thresholder;

        /// <summary>
        /// Object that manages the electrodes ID
        /// </summary>
        private ElecIDsManager elecIDsManager;

        /// <summary>
        /// number of electrodes given by the MEA Layout
        /// </summary>
        private int nElecs;

        /// <summary>
        /// Monitoring electrodes IDs for the spike detection
        /// </summary>
        private List<int> monitorElecs_ID;

        /// <summary>
        /// Monitoring electrodes inds for the spike detection
        /// </summary>
        private List<int> monitorElecs_ind;


        /// <summary>
        /// Deadtime for spike detection in seconds
        /// </summary>
        private double deadtime_s;

        /*
        /// <summary>
        /// Electrode data of the current time stamp in uV for all electrodes - length of nElecs
        /// </summary>
        double[] data_uV;
        */

        /// <summary>
        /// Absolute value of the electrode data of the past time stamp in uV for all electrodes (to compare wih current time stamp) - length of nElecs
        /// </summary>
        private double[] abs_prevData;

        /// <summary>
        /// Timestamp of previous spike for each electrode
        /// </summary>
        private double[] last_spk_time_s; // stores the time of the last spike of each detection electrode to compare with deadtime - length of nElecs

        /// <summary>
        /// Spike dictionary storing spike times for each electrode
        /// </summary>
        private readonly Dictionary<int, List<double>> spikeDictionary_s;

        private readonly object _lock = new object();

        public SpikeDetector(int nElecs, ElecIDsManager elecIDsManager, Thresholder thresholder_obj, double deadTime_s = 0.003, IEnumerable<int> monitorElecsId = null)
        {
            this.nElecs = nElecs;
            this.elecIDsManager = elecIDsManager;
            this.thresholder = thresholder_obj;
            this.deadtime_s = deadTime_s;

            /* ------------- allocate state ------------- */
            last_spk_time_s = new double[nElecs];
            abs_prevData = new double[nElecs];
            spikeDictionary_s = new Dictionary<int, List<double>>();

            monitorElecs_ID = new List<int>();
            monitorElecs_ind = new List<int>();

            // If caller gave no list, monitor everything
            IEnumerable<int> src = monitorElecsId ?? Enumerable.Range(0, nElecs);

            foreach (int id in src)
            {
                monitorElecs_ID.Add(id);
                monitorElecs_ind.Add(elecIDsManager.GetIndexFromID(id));
                spikeDictionary_s[id] = new List<double>();
            }

            for (int i = 0; i < nElecs; i++)
            {
                last_spk_time_s[i] = -1;
                abs_prevData[i] = 0;
            }
        }

        /// <summary>
        /// Detect spikes in meaData using positive and negative thresholds
        /// </summary>
        /// <param name="meaData_uV">Data recorded from the MEA. meaData_uV is a List with an array of doubles for each channel, but each array only has 1 element (the spike detector is expecting nFrames to be 1)</param>
        /// <param name="timestamps_s"></param>
        /// <returns></returns>
        public void DetectSpikes(List<double[]> meaData_uV, double[] timestamps_s)
        {
            int frames = timestamps_s.Length;
            for (int f = 0; f < frames; f++)
            {
                double t = timestamps_s[f];

                /* loop only over the electrodes we monitor */
                for (int k = 0; k < meaData_uV.Count; k++)
                {
                    int elecId = monitorElecs_ID[k];    // physical/channel ID
                    int elecInd = monitorElecs_ind[k];  // index into state arrays

                    /* current absolute amplitude of this sample */
                    double vAbs = Math.Abs(meaData_uV[k][f]);
                    double thr = thresholder.Get_Thresholds_uV()[elecInd];


                    /* dead-time and crossing test */
                    bool crossed = vAbs > thr &&      // over threshold now
                                   abs_prevData[elecInd] < thr &&    // was below before
                                   t > last_spk_time_s[elecInd] + deadtime_s;

                    if (crossed)
                    {
                        /* register spike */
                        last_spk_time_s[elecInd] = t;
                        spikeDictionary_s[elecId].Add(t);
                    }


                    //// Sorted list of previous spikes for this electrode
                    //var spikes = spikeDictionary_s[elecId];


                    //// Search for closest spike before and after time t
                    //int insertIdx = spikes.BinarySearch(t);
                    //if (insertIdx < 0) insertIdx = ~insertIdx;

                    //double? lastBefore = (insertIdx > 0) ? spikes[insertIdx - 1] : (double?)null;
                    //double? nextAfter = (insertIdx < spikes.Count) ? spikes[insertIdx] : (double?)null;

                    //bool safeBefore = !lastBefore.HasValue || (t - lastBefore.Value >= deadtime_s);
                    //bool safeAfter = !nextAfter.HasValue || (nextAfter.Value - t >= deadtime_s);

                    //bool crossed = vAbs > thr && abs_prevData[elecInd] < thr;

                    //if (crossed && safeBefore && safeAfter)
                    //{
                    //    // Insert while keeping order
                    //    spikeDictionary_s[elecId].Insert(insertIdx, t);
                    //}
                    //else if (crossed && safeBefore && nextAfter.HasValue && (nextAfter.Value - t < deadtime_s))
                    //{
                    //    // Replace later spike with earlier one
                    //    spikeDictionary_s[elecId][insertIdx] = t;
                    //}

                    //if (crossed && safeBefore && nextAfter.HasValue && (nextAfter.Value - t < deadtime_s))
                    //{
                    //    Console.WriteLine($"🔁 Replacing later spike at {nextAfter.Value:F4}s with earlier one at {t:F4}s (Δ = {nextAfter.Value - t:F4}s)");
                    //}

                    /* keep for next frame/call */
                    abs_prevData[elecInd] = vAbs;
                }
            }
        }

        /// <summary>
        /// Get the spike dictionary containing spike times for each electrode
        /// </summary>
        /// <returns>Dictionary of spike times per electrode</returns>
        public Dictionary<int, List<double>> GetSpikeDictionary()
        {
            lock (_lock)
            {
                return spikeDictionary_s.ToDictionary(
                    pair => pair.Key,                  // copy the key
                    pair => new List<double>(pair.Value)); // copy each List<double>
            }
        }

        /// <summary>
        /// Resets the spikeDictionary by clearing all spike times for each electrode.
        /// </summary>
        public void ResetSpikeDictionary()
        {
            lock (_lock)
            {
                foreach (var key in spikeDictionary_s.Keys.ToList())
                {
                    spikeDictionary_s[key].Clear();
                }
            }
        }

        public void Restart_Clock()
        {
            last_spk_time_s = new double[nElecs];  // [2] --> G13; [85] --> A2
        }


        public void Set_Thresholder(Thresholder newThresholder)
        {
            thresholder = newThresholder;
        }


        public void Set_MonitoringElecs(List<int> monitoringElecs_ID)
        {
            monitorElecs_ID = monitoringElecs_ID;
        }


        public void Set_Deadtime_sec(double deadtime_sec)
        {
            deadtime_s = deadtime_sec;
        }


        public List<int> Get_elecs_IDs()
        {
            return monitorElecs_ID; // 2 --> G13; 85 --> A2
        }


        public double Get_deadtime()
        {
            return deadtime_s;
        }


    }
}
