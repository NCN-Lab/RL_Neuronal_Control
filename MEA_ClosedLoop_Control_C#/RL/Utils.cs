//using General_Logic;
using MCS_Devices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TorchSharp.Modules;

namespace RL
{
    public class SimulationData
    {
        public List<List<List<float>>> s { get; set; }
        public List<List<int>> a { get; set; }
        public List<List<float>> r { get; set; }
        public List<List<List<float>>> s_prime { get; set; }

        public List<float> critic_losses { get; set; }
        public List<float> actor_losses { get; set; }
    }

    public class NetworkBurstDetector
    {

        int nElectrodes;

        public NetworkBurstDetector(int nElectrodes)
        {
            this.nElectrodes = nElectrodes;
        }

        public Dictionary<int, List<double>> ConvertToSpikeDictionary(string filePath)
        {
            // Read the JSON file
            var jsonData = File.ReadAllText(filePath);
            string decodedJson = JsonDocument.Parse(jsonData).RootElement.GetString();

            // Attempt to deserialize as a root object with "allElectrodeSpikes"
            var root = JsonSerializer.Deserialize<Dictionary<string, List<List<double>>>>(decodedJson);

            // Debug: Check if "allElectrodeSpikes" exists
            if (!root.ContainsKey("allElectrodeSpikes"))
            {
                throw new Exception("The JSON does not contain the key 'allElectrodeSpikes'.");
            }

            // Extract the "allElectrodeSpikes" property
            var allElectrodeSpikes = root["allElectrodeSpikes"];

            // Convert the list of lists to a dictionary
            var spikeDictionary = new Dictionary<int, List<double>>();
            for (int i = 0; i < allElectrodeSpikes.Count; i++)
            {
                spikeDictionary[i] = allElectrodeSpikes[i];
            }

            return spikeDictionary;
        }

        public (List<(double, double)>, List<List<int>>) DetectBursts(
            Dictionary<int, List<double>> electrodeSpikes_s,
            int minActiveElectrodes,
            int minSpikesPerElectrode,
            double maxISI,
            double minIBI,
            double blanking_ms = 6,
            List<double> stimTimes_s = null)
        {

            // Remove spikes that fall within the blanking window after stimTimes_ms
            if (stimTimes_s != null && stimTimes_s.Count > 0)
            {
                stimTimes_s.Sort(); // Pre-sort stimTimes for binary search
                foreach (var key in electrodeSpikes_s.Keys.ToList())
                {
                    electrodeSpikes_s[key] = electrodeSpikes_s[key]
                    .Where(spikeTime =>
                    {
                        // Find last stim ≤ spikeTime
                        int index = stimTimes_s.BinarySearch(spikeTime);
                        if (index < 0) index = ~index - 1;  // Now index points to last stim < spikeTime

                        if (index < 0) return true;  // No stim before spike → keep it

                        double stimTime = stimTimes_s[index];
                        return spikeTime >= stimTime + blanking_ms / 1000.0;
                    })
                    .ToList();
                }
            }

            int nElectrodes = electrodeSpikes_s.Count;
            var activeElectrodes = new Dictionary<int, bool>();
            var spikeCounts = new Dictionary<int, int>();
            foreach (var electrode in electrodeSpikes_s.Keys)
            {
                activeElectrodes[electrode] = false;
                spikeCounts[electrode] = 0;
            }

            List<(double, double)> burstIntervals = new List<(double, double)>();
            List<List<int>> burstActiveElectrodes = new List<List<int>>();

            var allSpikes = electrodeSpikes_s
                .SelectMany(kvp => kvp.Value.Select(spikeTime => (electrode: kvp.Key, spikeTime: spikeTime * 1000)))
                .OrderBy(s => s.spikeTime)
                .ToList();

            double? burstStartTime = null;
            double lastTime = double.NegativeInfinity;
            int lastElectrode = -1;

            foreach (var (electrode, spikeTime) in allSpikes)
            {
                double isi = spikeTime - lastTime;

                if (isi < maxISI)
                {
                    if (burstStartTime == null)
                    {
                        burstStartTime = lastTime;
                        activeElectrodes[lastElectrode] = true;
                        spikeCounts[lastElectrode]++;
                    }

                    // Mark electrode as active and increment spike count
                    activeElectrodes[electrode] = true;
                    spikeCounts[electrode]++;

                }
                else
                {
                    // Validate burst
                    int activeElectrodeCount = spikeCounts.Values.Count(c => c >= minSpikesPerElectrode);

                    if (activeElectrodeCount >= minActiveElectrodes)
                    {
                        if (burstIntervals.Count > 0 && burstStartTime - burstIntervals[burstIntervals.Count - 1].Item2 < minIBI)
                        {
                            // Merge bursts
                            foreach (var kvp in spikeCounts)
                            {
                                if (kvp.Value > 0) activeElectrodes[kvp.Key] = true;
                            }
                            burstIntervals[burstIntervals.Count - 1] = (burstIntervals[burstIntervals.Count - 1].Item1, lastTime);
                        }
                        else
                        {
                            // Record burst
                            burstIntervals.Add((burstStartTime ?? lastTime, lastTime));
                            burstActiveElectrodes.Add(activeElectrodes.Keys.Where(e => activeElectrodes[e]).ToList());
                        }
                    }

                    // Reset burst state
                    foreach (var key in spikeCounts.Keys.ToList())
                    {
                        activeElectrodes[key] = false;
                        spikeCounts[key] = 0;
                    }
                    burstStartTime = null;
                }

                lastTime = spikeTime;
                lastElectrode = electrode;
            }

            // Add the last burst
            if (burstStartTime != null)
            {
                int activeElectrodeCount = spikeCounts.Values.Count(c => c >= minSpikesPerElectrode);

                if (activeElectrodeCount >= minActiveElectrodes)
                {
                    if (burstIntervals.Count > 0 && burstStartTime - burstIntervals[burstIntervals.Count - 1].Item2 < minIBI)
                    {
                        // Merge bursts
                        foreach (var kvp in spikeCounts)
                        {
                            if (kvp.Value > 0) activeElectrodes[kvp.Key] = true;
                        }
                        burstIntervals[burstIntervals.Count - 1] = (burstIntervals[burstIntervals.Count - 1].Item1, lastTime);
                    }
                    else
                    {
                        // Record burst
                        burstIntervals.Add((burstStartTime ?? lastTime, lastTime));
                        burstActiveElectrodes.Add(activeElectrodes.Keys.Where(e => activeElectrodes[e]).ToList());
                    }
                }
            }

            return (burstIntervals, burstActiveElectrodes);
        }

        /// <summary>
        /// Calculate electrode weights based on spike times within a given burst interval.
        /// </summary>
        /// <param name="electrodeSpikes">Dictionary containing the spike times per electrode.</param>
        /// <param name="burstInterval">Tuple containing the start and end of the burst interval (b[0], b[1]).</param>
        public double[] CalculateElectrodeWeights(Dictionary<int, List<double>> electrodeSpikes, (double, double) burstInterval)
        {
            // Create an array to store the weighted electrodes
            double[] weightedElectrodes = new double[nElectrodes];

            // Iterate through each electrode
            int i = 0;
            foreach (var key in electrodeSpikes.Keys.ToList())
            {
                // Compute the weighted sum of spikes within the burst interval
                weightedElectrodes[i] = electrodeSpikes[key]
                    .Where(t => t * 1000 >= burstInterval.Item1 && t * 1000 <= burstInterval.Item2)
                    .Sum(t  => Math.Exp(-Math.Log(2) / 10 * (t * 1000 - burstInterval.Item1)));
                i++;
            }

            return weightedElectrodes;
        }
    }


    public class StimulationDetector
    {
        /// <summary>
        /// Last value of suncoyt channel
        /// </summary>
        int last_value = 0; // stores the last value of the syncout channel

        /// <summary>
        /// Spike dictionary storing spike times for each electrode
        /// </summary>
        private Queue<double> stimTimes_s = new Queue<double>();

        private readonly object _lock = new object();

        public StimulationDetector()
        {
        }

        /// <summary>
        /// Detect triggers of stimulation in syncout channel
        /// </summary>
        /// <param name="meaData_uV">Data recorded from the MEA. meaData_uV is a List with an array of doubles for each channel, but each array only has 1 element (the spike detector is expecting nFrames to be 1)</param>
        /// <param name="clock_s"></param>
        /// <returns></returns>
        public void DetectStimuli(int[] syncout, double[] timestamps_s)
        {
            int frames = timestamps_s.Length;
            for (int f = 0; f < frames; f++)
            {
                double t = timestamps_s[f];

                if (syncout[f] != 0 && last_value == 0)
                {
                    stimTimes_s.Enqueue(t);
                    //LogBuffer.Messages.Enqueue($"[STIM-DETECT] t = {t:F4} s via syncout");
                }

                last_value = syncout[f];
            }
        }

        /// <summary>
        /// Get the stimulation times
        /// </summary>
        /// <returns>List of stimulation times</returns>
        public double[] GetStimTimes()
        {
            lock (_lock) return stimTimes_s.ToArray();
        }

        /// <summary>
        /// Resets stimTimes_s.
        /// </summary>
        public void ResetStimTimes()
        {
            stimTimes_s.Clear();
        }
    }
}
