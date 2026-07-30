using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RL
{
    public class TaskParameters
    {
        // Stimulation parameters
        public double stimAmplitude_mV { set; get; }
        public double pulseDuration_ms { set; get; }

        // Actor-Critic Networks parameters
        public int inputSize { set; get; }
        public List<int> hiddenSizes { set; get; }
        public int outputSize { set; get; }
        public double gamma { set; get; }
        public double weightEntropy { set; get; }
        public double clipEpsilon { set; get; }
        public double initialLrActor { set; get; }
        public double initialLrCritic { set; get; }

        // RL parameters
        public int nElectrodes { set; get; }
        public List<int> actionSpaceElectrodes { set; get; }
        public List<int> ignoreElectrodes { set; get; }
        public long stepDuration_ms { set; get; }
        public int maxNumSteps { set; get; }

        // Environment-specific parameters
        public double minSpikeInterval_ms { set; get; }
        public double maxNetworkBurstIsi_ms { set; get; }
        public int minSpikesPerElectrode { set; get; }
        public double minIbi_ms { set; get; }
        public double minRatioActiveElectrodes { set; get; }
        public int minActiveElectrodes { set; get; }

        public TaskParameters()
        {
            stimAmplitude_mV = -400.0;
            pulseDuration_ms = 0.2;

            inputSize = 10;
            hiddenSizes = new List<int> { 32 };
            outputSize = 10;
            gamma = 0;
            weightEntropy = 0.001;
            initialLrActor = 3e-3;
            initialLrCritic = 1e-4;

            nElectrodes = 9;
            actionSpaceElectrodes = new List<int> {0,1,2,3,4,5,6,7,8,9};
            ignoreElectrodes = new List<int> { };
            stepDuration_ms = 200;
            maxNumSteps = 600;

            minSpikeInterval_ms = 3;
            maxNetworkBurstIsi_ms = 20;
            minSpikesPerElectrode = 3;
            minIbi_ms = 200;
            minRatioActiveElectrodes = 0.3;
            minActiveElectrodes = (int)(nElectrodes * minRatioActiveElectrodes);
        }
    }
}
