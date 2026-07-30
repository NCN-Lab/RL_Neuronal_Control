//using General_Logic;
using Google.Protobuf.WellKnownTypes;
using MCS_Devices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;

namespace RL
{
    public static class LogBuffer
    {
        public static readonly ConcurrentQueue<string> Messages = new ConcurrentQueue<string>();
    }

    public class Worker
    {
        private string _id;
        public string chipID { get; set; }

        private MeaDacq dacq;
        private Stimulator stg;
        private ElecIDsManager elecsIDsManager;
        private SpikeDetector spikeDetector;
        private NetworkBurstDetector networkBurstDetector;
        private StimulationDetector stimulationDetector;

        private int STG_selected = 1;
        List<int> monitoredElecsIDs = new List<int> { };

        // Stimulation parameters
        private double stimAmplitude_mV;
        private double pulseDuration_ms;
        private List<double> stimTimes_s = new List<double> { };
        private List<double> recentStimTimes_s = new List<double> { };
        private double stimUsage;
        public String currentPolicy = "generalist";

        // RL parameters
        private List<int> actionSpaceElectrodes;
        private List<int> ignoreElectrodes;
        private List<int> ignoreElectrodesBase;
        private int bestElectrode;
        private long stepDuration_ms;
        private int maxNumSteps;

        // Environment-specific parameters
        private double minSpikeInterval_ms;
        private double maxNetworkBurstIsi_ms;
        private int minSpikesPerElectrode;
        private double minIbi_ms;
        private double minRatioActiveElectrodes;
        private int minActiveElectrodes;

        // Training variables
        private PPO agent;
        private String preTrainedAgentPath;
        public AgentParameters agentParams;

        public int nSteps;
        public int stepsPerUpdate;
        public int batchSize;
        public int nEpochs;

        public List<(double[], int, double, double[])> transitionBuffer = new List<(double[], int, double, double[])>();
        private List<float> logProbBuffer = new List<float>();
        public List<int> doneBuffer = new List<int>();
        public List<EpisodeData> episodeBatches = new List<EpisodeData>();
        public List<(double, double)> bufferBursts = new List<(double, double)>();
        public List<double> bufferNibi_ms = new List<double>();
        public List<double[]> bufferWeightedElectrodes = new List<double[]>();
        private double elapsedTimeSinceBurst_ms = 0;
        public int currentEpisode = 0;

        // State
        public List<double> stepTimes_s = new List<double> { };
        public double startTime_s;
        public double episodeStartTime_ms;
        public double currentTime_s;
        public int currentStep;
        public int currentUpdate;
        public double[] currentState;
        private bool offPreviousBurst;
        private bool done;
        private double duration_s = 9e9;

        private Dictionary<int, List<double>> electrodeSpikes_s;
        private Dictionary<string, List<double>> allElectrodeSpikes_s = new Dictionary<string, List<double>>();

        private Stopwatch timer;

        private readonly CancellationToken _ct;
        private readonly Action<string> _log;



        public Worker(MeaDacq dacq, Stimulator stg, SpikeDetector spikeDetector, StimulationDetector stimulationDetector, ElecIDsManager elecManager, TaskParameters taskParams,
            String id, CancellationToken ct, string preTrainedAgent = null, Action<string> logger = null)
        {
            _id = id;
            _ct = ct;
            _log = logger;
            this.dacq = dacq;
            this.stg = stg;
            this.spikeDetector = spikeDetector;
            this.stimulationDetector = stimulationDetector;
            this.elecsIDsManager = elecManager;
            this.networkBurstDetector = new NetworkBurstDetector(9);

            // Initialize stimulation parameters
            stimAmplitude_mV = taskParams.stimAmplitude_mV;
            pulseDuration_ms = taskParams.pulseDuration_ms;

            // Actor-Critic parameters
            agentParams = new AgentParameters
            {
                inputSize = taskParams.inputSize,
                hiddenSizes = taskParams.hiddenSizes,
                outputSize = taskParams.outputSize,

                initialLrActor = taskParams.initialLrActor,
                initialLrCritic = taskParams.initialLrCritic,

                gamma = taskParams.gamma,
                weightEntropy = taskParams.weightEntropy,
                clipEpsilon = taskParams.clipEpsilon,

                ignoreElectrodes = taskParams.ignoreElectrodes
            };

            // RL parameters
            actionSpaceElectrodes = taskParams.actionSpaceElectrodes;
            ignoreElectrodes = new List<int>(taskParams.ignoreElectrodes);
            ignoreElectrodesBase = new List<int>(taskParams.ignoreElectrodes);
            stepDuration_ms = taskParams.stepDuration_ms;
            maxNumSteps = taskParams.maxNumSteps;

            // Environment-specific parameters
            minSpikeInterval_ms = taskParams.minSpikeInterval_ms;
            maxNetworkBurstIsi_ms = taskParams.maxNetworkBurstIsi_ms;
            minSpikesPerElectrode = taskParams.minSpikesPerElectrode;
            minIbi_ms = taskParams.minIbi_ms;
            minRatioActiveElectrodes = taskParams.minRatioActiveElectrodes;
            minActiveElectrodes = taskParams.minActiveElectrodes;

            // Initialize agent
            agent = new PPO(agentParams);

            preTrainedAgentPath = preTrainedAgent;

            // State parameters
            offPreviousBurst = true;
            done = false;

            PrepareStimulation();

            // Events
            dacq.OnStim += (t) => { stimTimes_s.Add(t); recentStimTimes_s.Add(t); };
        }

        private void PrepareStimulation()
        {
            bool isConnected = stg.Connect_USB_A();

            // Set up stimulation parameters
            int STG_ID = STG_selected;
            int[] amplitude_uV = { (int)(stimAmplitude_mV * 1000), 0 };
            ulong[] duration_us = { (ulong)(pulseDuration_ms * 1000), 1200 }; // Prolong 2nd phase to protect amplifier (avoid building up of charge/noise)

            // Download stimulation data
            stg.Deactivate_all_STG_StimElecs(STG_ID);
            stg.DownloadStimulus(STG_ID, amplitude_uV, duration_us);
            LogBuffer.Messages.Enqueue($"Stimulus downloaded to STG {STG_ID}.");

            // Prepare well electrodes
            foreach (var elec in actionSpaceElectrodes)
            {
                var label = $"{_id}{elec}";
                monitoredElecsIDs.Add(elecsIDsManager.GetIDFromLabel(label));
            }
            stg.Set_STG_StimElecs_IDs(monitoredElecsIDs, STG_ID);
            stg.PrepareWellElectrodes(monitoredElecsIDs);
        }

        private void ThrowIfCancelled()         // local method
        {
            if (_ct.IsCancellationRequested)
                _ct.ThrowIfCancellationRequested();
        }


        public void SetInitialTemporalDynamics()
        {
            bufferBursts.Clear();
            var currentTime_ms = dacq.GetCurrentTime() * 1000;
            bufferBursts.Add((currentTime_ms - 8600, currentTime_ms - 8400));
            bufferBursts.Add((currentTime_ms - 6400, currentTime_ms - 6200));
            bufferBursts.Add((currentTime_ms - 4400, currentTime_ms - 4200));
            bufferBursts.Add((currentTime_ms - 2600, currentTime_ms - 2400));
            bufferBursts.Add((currentTime_ms - 1000, currentTime_ms - 800));
            bufferNibi_ms = new List<double> { 2200, 2000, 1800, 1600, 1400 }; // To avoid ramping up in long IBI
        }

        public bool ChangePolicy(String policy)
        {
            currentPolicy = policy;
            switch (policy)
            {
                case "specialist":

                    this.duration_s = 9e9;

                    agentParams.initialLrActor = 3e-3;
                    agentParams.initialLrCritic = 1e-4;
                    agentParams.gamma = 0;
                    agentParams.weightEntropy = 0.001;
                    agentParams.clipEpsilon = 0.2;

                    ignoreElectrodes = new List<int>(ignoreElectrodesBase);
                    agentParams.ignoreElectrodes = ignoreElectrodes;

                    agent.Reset(agentParams);
                    SetInitialTemporalDynamics();

                    break;

                case "generalist":

                    agentParams.initialLrActor = 3e-3;
                    agentParams.initialLrCritic = 1e-4;
                    agentParams.gamma = 0;
                    agentParams.weightEntropy = 0.001;
                    agentParams.clipEpsilon = 0.2;

                    ignoreElectrodes = new List<int>(ignoreElectrodesBase);
                    agentParams.ignoreElectrodes = ignoreElectrodes;

                    agent.Reset(agentParams);
                    if (preTrainedAgentPath != null)
                    {
                        agent.LoadAgent(preTrainedAgentPath);

                    }
                    SetInitialTemporalDynamics();

                    break;

                case "withoutBest":

                    agentParams.initialLrActor = 3e-3;
                    agentParams.initialLrCritic = 1e-4;
                    agentParams.gamma = 0;
                    agentParams.weightEntropy = 0.001;
                    agentParams.clipEpsilon = 0.2;

                    ignoreElectrodes = new List<int>(ignoreElectrodesBase);
                    ignoreElectrodes.Add(bestElectrode);
                    agentParams.ignoreElectrodes = ignoreElectrodes;

                    agent.Reset(agentParams);
                    if (preTrainedAgentPath != null)
                    {
                        agent.LoadAgent(preTrainedAgentPath);

                    }
                    SetInitialTemporalDynamics();

                    break;

                case "random":

                    ignoreElectrodes = new List<int>(ignoreElectrodesBase);
                    //stimUsage = CalculateStimulationUsage(episodeBatches, 10);

                    break;


            }

            LogBuffer.Messages.Enqueue($"Switched to {currentPolicy} policy.");

            currentEpisode = 0;
            episodeBatches.Clear();
            agent.actor.loss.Clear();
            agent.critic.loss.Clear();

            return true;
        }



        public bool RunTraining(int nSteps = 3072, int stepsPerUpdate = 256, int batchSize = 64, int nEpochs = 10,
            bool exploration = true, bool saveRun = true)
        {
            this.nSteps = nSteps;
            this.stepsPerUpdate = stepsPerUpdate;
            this.batchSize = batchSize;
            this.nEpochs = nEpochs;

            SetInitialTemporalDynamics();

            // Set artificial electrode weights
            bufferWeightedElectrodes.Add(new double[] { 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11 });
            bufferWeightedElectrodes.Add(new double[] { 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11 });
            bufferWeightedElectrodes.Add(new double[] { 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11 });
            bufferWeightedElectrodes.Add(new double[] { 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11 });
            bufferWeightedElectrodes.Add(new double[] { 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11 });

            RecordBaseline(3 * 60.0);


            ChangePolicy("generalist");
            Train(60.0);
            RecordBaseline(0.5 * 60.0, saveRun: true);


            ChangePolicy("specialist");

            currentEpisode = 0;
            currentUpdate = 0;
            int stepCounter = 0;
            currentState = GetState();
            startTime_s = dacq.GetCurrentTime();
            while (stepCounter < nSteps)
            {
                ThrowIfCancelled();
                episodeStartTime_ms = dacq.GetCurrentTime() * 1000;
                RunEpisode(exploration, update: true);
                currentEpisode++;
                stepCounter += currentStep;
            }

            if (saveRun)
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"D:\\Eduardo\\Paper1\\experiments\\json\\{chipID}_well{_id}_{currentPolicy}_{timestamp}.json";
                SavePolicy(filename);
            }

            allElectrodeSpikes_s.Clear();
            stimTimes_s.Clear();
            stepTimes_s.Clear();
            recentStimTimes_s = new List<double> { };

            //RecordBaseline(0.5 * 60.0, saveRun: true);

            //ChangePolicy("generalist");
            //Train(60.0);

            RecordBaseline(3 * 60.0);

            dacq.StopDacq();

            LogBuffer.Messages.Enqueue($"Worker finished at {DateTime.Now:HH:mm:ss.fff}");

            return true;
        }

        public static int GenerateSecureSeed()
        {
            byte[] bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            // Convert to int (little endian)
            return BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF; // Make sure it's non-negative
        }

        public bool Run()
        {
            SetInitialTemporalDynamics();

            // Set artificial electrode weights
            bufferWeightedElectrodes.Add(new double[] { 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11 });
            bufferWeightedElectrodes.Add(new double[] { 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11 });
            bufferWeightedElectrodes.Add(new double[] { 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11 });
            bufferWeightedElectrodes.Add(new double[] { 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11 });
            bufferWeightedElectrodes.Add(new double[] { 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11, 0.11 });



            //RecordBaseline(3 * 60.0);

            RecordBaseline(10.0);


            ChangePolicy("generalist");
            Train(60.0);
            stimUsage = CalculateStimulationUsage(episodeBatches, 26);
            bestElectrode = GetBestElectrode(episodeBatches, 26);

            RecordBaseline(0.5 * 60.0, saveRun: true);



            //ChangePolicy("generalist");
            //Train(60.0);

            //RecordBaseline(0.5 * 60.0, saveRun: true);

            //ChangePolicy("generalist");
            //Train(60.0);



            //ChangePolicy("withoutBest");
            //Train(60.0);

            //RecordBaseline(0.5 * 60.0, saveRun: true);

            //ChangePolicy("random");
            //Train(60.0);


            //ChangePolicy("random");
            //Train(60.0);

            //RecordBaseline(0.5 * 60.0, saveRun: true);

            //ChangePolicy("withoutBest");
            //Train(60.0);




            int secureSeed = GenerateSecureSeed();
            Random random = new Random(secureSeed);
            if (random.NextDouble() < 0.5)
            {
                ChangePolicy("withoutBest");
                Train(60.0);

                RecordBaseline(0.5 * 60.0, saveRun: true);

                ChangePolicy("random");
                Train(60.0);
            }
            else
            {
                ChangePolicy("random");
                Train(60.0);

                RecordBaseline(0.5 * 60.0, saveRun: true);

                ChangePolicy("withoutBest");
                Train(60.0);
            }




            RecordBaseline(3 * 60.0);


            dacq.StopDacq();

            LogBuffer.Messages.Enqueue($"Worker finished at {DateTime.Now:HH:mm:ss.fff}");

            return true;
        }

        private void RecordBaseline(double duration_s, bool saveRun = true)
        {
            startTime_s = dacq.GetCurrentTime();
            timer = Stopwatch.StartNew();
            currentStep = 0;
            while (dacq.GetCurrentTime() - startTime_s < duration_s)
            {
                ThrowIfCancelled();
                var targetTime_ms = startTime_s + (currentStep + 1) * stepDuration_ms;
                WaitForNextTimepoint(startTime_s, targetTime_ms);
                electrodeSpikes_s = spikeDetector.GetSpikeDictionary();
                AddElectrodeSpikes(electrodeSpikes_s);

                currentTime_s = dacq.GetCurrentTime();
                currentState = GetState(); // Try to get a valid state after a step.
                currentStep++;
            }

            if (saveRun)
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"D:\\Eduardo\\Paper1\\experiments\\json\\{chipID}_well{_id}_baseline_{timestamp}.json";
                SaveBaseline(filename);
            }

            allElectrodeSpikes_s.Clear();
            timer.Stop();
        }

        private void Train(double duration_s, bool exploration = true, bool saveRun = true)
        {
            this.duration_s = duration_s;
            startTime_s = dacq.GetCurrentTime();
            currentState = GetState();
            while (dacq.GetCurrentTime() - startTime_s < this.duration_s)
            {
                ThrowIfCancelled();
                episodeStartTime_ms = dacq.GetCurrentTime() * 1000.0;
                RunEpisode(exploration);
                currentEpisode++;
            }

            if (saveRun)
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"D:\\Eduardo\\Paper1\\experiments\\json\\{chipID}_well{_id}_{currentPolicy}_{timestamp}.json";
                SavePolicy(filename);
            }

            allElectrodeSpikes_s.Clear();
            stimTimes_s.Clear();
            stepTimes_s.Clear();
            recentStimTimes_s = new List<double> { };
        }

        private void RunEpisode(bool exploration, bool update = false)
        {
            currentStep = 0;
            done = false;
            timer = Stopwatch.StartNew();

            // Initialize episode data
            var episodeData = new EpisodeData
            {
                States = new List<double[]> { },
                Actions = new List<int> { },
                Rewards = new List<double> { },
                NextStates = new List<double[]> { }
            };

            while (!done && currentStep < maxNumSteps)
            {
                ThrowIfCancelled(); 
                currentTime_s = episodeStartTime_ms / 1000.0 + timer.Elapsed.TotalSeconds;
                if (currentTime_s - startTime_s > this.duration_s) break;
                stepTimes_s.Add(currentTime_s);

                var sars = Step(exploration);

                //if (sars == null) Console.WriteLine("State is NULL!");

                if (sars != null)
                {
                    var (s, a, r, sPrime) = sars.Value;
                    transitionBuffer.Add((s, a, r, sPrime));
                    doneBuffer.Add(done ? 1 : 0);

                    // Append to episode data
                    episodeData.States.Add(s);
                    episodeData.Actions.Add(a);
                    episodeData.Rewards.Add(r);
                    episodeData.NextStates.Add(sPrime);


                    // Update
                    if (update && transitionBuffer.Count >= stepsPerUpdate && (currentPolicy == "specialist" || currentPolicy == "generalist"))
                    {
                        var transitions = new List<(double[], int, float, double, double[], int)>();

                        for (int i = 0; i < transitionBuffer.Count; i++)
                        {
                            (s, a, r, sPrime) = transitionBuffer[i];
                            float lp = logProbBuffer[i];
                            int d = doneBuffer[i];
                            transitions.Add((s, a, lp, r, sPrime, d));
                        }

                        for (int epoch = 0; epoch < nEpochs; epoch++)
                        {
                            for (int i = 0; i < transitions.Count; i += batchSize)
                            {
                                var batch = transitions.Skip(i).Take(batchSize).ToList();

                                var states = ConvertToTensor(batch.Select(x => x.Item1).ToList());
                                var actions = torch.tensor(batch.Select(x => x.Item2).ToList(), dtype: torch.int64);
                                var logProbs = torch.tensor(batch.Select(x => x.Item3).ToList(), dtype: torch.float32);
                                var rewards = torch.tensor(batch.Select(x => x.Item4).ToList(), dtype: torch.float32);
                                var nextStates = ConvertToTensor(batch.Select(x => x.Item5).ToList());
                                var dones = torch.tensor(batch.Select(x => x.Item6 == 1).ToList(), dtype: torch.int64);

                                agent.Update(states, actions, logProbs, rewards, nextStates, dones);
                            }
                        }

                        currentUpdate++;

                        transitionBuffer.Clear();
                        logProbBuffer.Clear();
                        doneBuffer.Clear();
                    }
                }
            }

            episodeBatches.Add(episodeData);
            timer.Stop();

        }

        public (double[], int, double, double[])? Step(bool exploration)
        {
            var state = currentState;
            int stimElectrode = 0;
            double logProb = 0;

            // Stimulate only if current state is not null
            if (state != null)
            {
                if (currentPolicy == "random")
                {

                    stimElectrode = GenerateRandomAction(actionSpaceElectrodes.Except(ignoreElectrodes).ToList(), stimUsage);
                }
                else
                {
                    var stateTensor = torch.tensor(state, dtype: torch.float32);
                    (stimElectrode, logProb) = agent.GetElectrodeToStimulate(stateTensor, exploration);
                    logProbBuffer.Add(((float)logProb));
                }

                if (stimElectrode > 0)
                {
                    Stimulate(stimElectrode);
                    double stimTime = dacq.GetCurrentTime();
                    //LogBuffer.Messages.Enqueue($"[STIM-TRIGGER] Electrode {stimElectrode}, t = {stimTime:F4} s");
                }

            }

            // Wait until next timepoint
            var targetTime = episodeStartTime_ms + (currentStep + 1) * stepDuration_ms;
            //var targetTime = currentTime_s * 1000.0 + stepDuration_ms;

            currentStep++;
            WaitForNextTimepoint(episodeStartTime_ms, targetTime);

            // Add small buffer to ensure stim detection had time to occur
            Thread.Sleep(5);

            //if (stimElectrode > 0)
            //{
            //    var stimTimes = stimulationDetector.GetStimTimes();
            //    foreach (double st in stimTimes)
            //    {
            //        stimTimes_s.Add(st);
            //        recentStimTimes_s.Add(st);
            //        //Console.WriteLine($"Stimulus at {st}s");
            //    }
            //    stimulationDetector.ResetStimTimes();

            //    LogBuffer.Messages.Enqueue($"Stimulated electrode {_id}{stimElectrode} at {stimTimes_s.Last():F3}s");
            //}

            if (stimElectrode > 0)
                LogBuffer.Messages.Enqueue($"Stimulated electrode {_id}{stimElectrode} at {stimTimes_s.Last():F3}s");

            electrodeSpikes_s = spikeDetector.GetSpikeDictionary();
            AddElectrodeSpikes(electrodeSpikes_s);

            currentTime_s = episodeStartTime_ms / 1000.0 + timer.Elapsed.TotalSeconds;

            // Calculate new state
            var state_prime = GetState();

            currentState = done ? null : state_prime;

            if (state is null || state_prime is null)
            {
                return null;
            }
            else
            {
                var reward = GetReward(state, stimElectrode);
                return (state, stimElectrode, reward, state_prime);
            }
        }

        private void Stimulate(int elec)
        {
            // Activate electrode
            int elec_ID = elecsIDsManager.GetIDFromLabel(_id + elec.ToString());
            stg.Activate_StimElecID(elec_ID, STG_selected);

            if (STG_selected == 1)
            {
                stg.Stimulate(0x1);
            }
            else
            {
                stg.Stimulate(0x2);
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(30);                    // exact delay not timing-critical
                stg.Deactivate_StimElecID(elec_ID);
            });

        }



        private double[] GetState()
        {
            double startTime;

            if (!offPreviousBurst)
            {
                var lastBurst = bufferBursts[bufferBursts.Count - 1];
                startTime = lastBurst.Item1 - maxNetworkBurstIsi_ms;
            }
            else
            {
                startTime = currentTime_s * 1000.0 - stepDuration_ms;
            }

            //if (recentStimTimes_s.Count > 0)
            //    LogBuffer.Messages.Enqueue($"[CHECK] recentStimTimes_s contains {recentStimTimes_s.Count} entries. Most recent: {recentStimTimes_s.Last():F4}s");

            var (burstIntervals, _) = networkBurstDetector.DetectBursts(
                electrodeSpikes_s,
                minActiveElectrodes,
                minSpikesPerElectrode,
                maxNetworkBurstIsi_ms,
                minIbi_ms,
                blanking_ms: 2,
                stimTimes_s: recentStimTimes_s);

            if (burstIntervals.Count > 0)
            {
                foreach (var burstInterval in burstIntervals)
                {
                    if (!offPreviousBurst)
                    {
                        bufferBursts.RemoveAt(bufferBursts.Count - 1);
                        bufferWeightedElectrodes.RemoveAt(bufferWeightedElectrodes.Count - 1);
                    }
                    else
                    {
                        done = true;
                        bufferNibi_ms.Add(burstInterval.Item1 - (bufferNibi_ms.Count == 0 ? 0 : bufferBursts[bufferBursts.Count - 1].Item2));
                    }

                    //Console.WriteLine($"Burst detected at {(burstInterval.Item1 / 1000.0):F3}-{(burstInterval.Item2 / 1000.0):F3}s");


                    bufferBursts.Add(burstInterval);
                    bufferWeightedElectrodes.Add(
                        networkBurstDetector.CalculateElectrodeWeights(electrodeSpikes_s, burstInterval)
                        );

                    //LogBuffer.Messages.Enqueue($"[BURST] {(burstInterval.Item1 / 1000.0):F3}–{(burstInterval.Item2 / 1000.0):F3} s");
                }

                if (bufferBursts[bufferBursts.Count - 1].Item2 < currentTime_s * 1000.0 - minIbi_ms)
                {
                    offPreviousBurst = true;
                    recentStimTimes_s = new List<double> { };
                    spikeDetector.ResetSpikeDictionary();

                    LogBuffer.Messages.Enqueue($"[BURST] {(bufferBursts[bufferBursts.Count - 1].Item1 / 1000.0):F3}–{(bufferBursts[bufferBursts.Count - 1].Item2 / 1000.0):F3} s");
                }
                else
                {
                    offPreviousBurst = false;
                    //return null;

                    if (currentState is null)
                    {
                        return null;
                    }
                    else
                    {
                        //done = true;
                        elapsedTimeSinceBurst_ms = currentTime_s * 1000.0 - bufferBursts[bufferBursts.Count - 2].Item2;
                        double[] s = currentState;
                        s[0] = elapsedTimeSinceBurst_ms / CalculateMedian(bufferNibi_ms);
                        return s;
                    }
                }
            }

            if (bufferBursts.Count > 4 && offPreviousBurst)
            {
                if (bufferBursts.Count > 5)
                {
                    bufferBursts.RemoveAt(0);
                    bufferWeightedElectrodes.RemoveAt(0);
                    bufferNibi_ms.RemoveAt(0);
                }

                elapsedTimeSinceBurst_ms = currentTime_s * 1000.0 - bufferBursts[bufferBursts.Count - 1].Item2;

                //Console.WriteLine($"Elapsed time since last NB: {elapsedTimeSinceBurst_ms} ms");

                double[] relElapsedTime = { elapsedTimeSinceBurst_ms / CalculateMedian(bufferNibi_ms) };
                double[] sumWeightedElectrodes = bufferWeightedElectrodes
                .Aggregate(new double[bufferWeightedElectrodes.First().Length], (acc, item) =>
                {
                    for (int i = 0; i < item.Length; i++)
                    {
                        acc[i] += item[i];
                    }
                    return acc;
                });

                double generalistSum = sumWeightedElectrodes.Sum();
                double[] relWeightedElectrodes = sumWeightedElectrodes
                .Select(x => x / generalistSum)
                .ToArray();

                if (offPreviousBurst)
                {
                    recentStimTimes_s = new List<double> { };
                    spikeDetector.ResetSpikeDictionary();
                }

                return relElapsedTime.Concat(relWeightedElectrodes).ToArray();
            }
            else
            {
                if (offPreviousBurst)
                {
                    recentStimTimes_s = new List<double> { };
                    spikeDetector.ResetSpikeDictionary();
                }
                return null;
            }
        }

        private double GetReward(double[] state, int action)
        {
            double reward = 0.0;

            if (action > 0)
            {
                double delta = bufferBursts[bufferBursts.Count - 1].Item1 - (stimTimes_s.Last() * 1000.0);
                reward = (delta >= 0 && delta < 100) ? 1.0 : -0.25; // Check if the action led to a burst within the desired causal window
                //reward = done ? 1.0 : -0.25;
            }
            else
            {
                reward = done ? -1.0 : 0.25;
            }

            return reward;
        }

        private void SaveBaseline(string filePath)
        {

            // Prepare the data to be serialized
            var data = new Dictionary<string, object>
            {
                ["startDuration_s"] = startTime_s,
                ["endDuration_s"] = currentTime_s,
                ["allElectrodeSpikes_s"] = allElectrodeSpikes_s
            };

            // Serialize and save to file
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(filePath, JsonSerializer.Serialize(data, options));
        }


        private void SavePolicy(string filePath)
        {

            // Prepare the data to be serialized
            var data = new Dictionary<string, object>
            {
                ["startDuration_s"] = startTime_s,
                ["endDuration_s"] = currentTime_s,
                ["nEpisodes"] = episodeBatches.Count,
                ["episodeBatches"] = episodeBatches,
                ["allElectrodeSpikes_s"] = allElectrodeSpikes_s,
                ["actionSpaceElectrodes"] = actionSpaceElectrodes,
                ["ignoreElectrodes"] = ignoreElectrodes,
                ["stimAmplitude_mV"] = stimAmplitude_mV,
                ["pulseDuration_ms"] = pulseDuration_ms,
                ["allElectrodeSpikes_s"] = allElectrodeSpikes_s,
                ["stimTimes_s"] = stimTimes_s,
                ["minSpikeInterval_ms"] = minSpikeInterval_ms,
                ["maxNetworkBurstIsi_ms"] = maxNetworkBurstIsi_ms,
                ["minSpikesPerElectrode"] = minSpikesPerElectrode,
                ["minIbi_ms"] = minIbi_ms,
                ["minRatioActiveElectrodes"] = minRatioActiveElectrodes,
                ["minActiveElectrodes"] = minActiveElectrodes,
                ["stepDuration_ms"] = stepDuration_ms,
                ["maxNumSteps"] = maxNumSteps,
                ["stepTimes_s"] = stepTimes_s,
            };

            if (currentPolicy == "random") data["stimUsage"] = stimUsage;

            if (currentPolicy == "specialist")
            {
                data["inputSize"] = agent.inputSize;
                data["hiddenSizes"] = agent.hiddenSizes;
                data["outputSize"] = agent.outputSize;
                data["initialLrActor"] = agent.initialLrActor;
                data["initialLrCritic"] = agent.initialLrCritic;
                data["gamma"] = agent.gamma;
                data["weightEntropy"] = agent.weightEntropy;
                data["clipEpsilon"] = agent.clipEpsilon;
                data["actorLosses"] = agent.actor.loss;
                data["criticLosses"] = agent.critic.loss;
            }

            // Serialize and save to file
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(filePath, JsonSerializer.Serialize(data, options));
        }

        private static Tensor ConvertToTensor(List<double[]> nestedList)
        {
            var rows = nestedList.Count;
            var cols = nestedList[0].Length;

            var flatArray = new float[rows * cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    flatArray[i * cols + j] = (float)nestedList[i][j];
                }
            }

            return torch.tensor(flatArray, new long[] { rows, cols }, dtype: torch.float32);
        }

        // Calculate remaining time to wait after subtracting computation time from the step duration.
        private void WaitForNextTimepoint(double anchorTime_s, double targetTime_ms)
        {
            double now_ms;
            while ((now_ms = anchorTime_s + timer.Elapsed.TotalMilliseconds) < targetTime_ms)
            {
                double remainingTime = targetTime_ms - now_ms;

                if (remainingTime > 5.0)
                {
                    // Sleep for most of the remaining time to reduce CPU usage
                    Thread.Sleep((int)remainingTime - 2);
                }
                else if (remainingTime > 0.1)
                {
                    // Busy-wait for the last few milliseconds for high precision
                    Thread.SpinWait(100);
                }
                else
                {
                    break;
                }
            }
        }

        private void AddElectrodeSpikes(Dictionary<int, List<double>> electrodeSpikes_s)
        {
            foreach (var key in electrodeSpikes_s.Keys)
            {
                var elec_label = elecsIDsManager.GetLabelFromID(key);
                if (!allElectrodeSpikes_s.ContainsKey(elec_label))
                {
                    // Initialize if the key does not exist
                    allElectrodeSpikes_s[elec_label] = new List<double>();
                }

                // Add new spikes to the list
                allElectrodeSpikes_s[elec_label].AddRange(
                    electrodeSpikes_s[key].Where(spike => !allElectrodeSpikes_s[elec_label].Contains(spike))
                    );
            }

        }

        public static double CalculateMedian(List<double> arr)
        {
            if (arr.Count == 1)
            {
                return arr[0];
            }
            arr.Sort();
            int mid = arr.Count / 2;
            double median = 0;
            if (mid % 2 != 0)
            {
                median = arr[mid];
            }
            else
            {
                median = (arr[mid - 1] + arr[mid]) / 2;
            }

            return median;
        }

        public static double CalculateStimulationUsage(List<EpisodeData> episodeDataList, int count)
        {
            // Select the last 'count' elements from the episode data
            var lastEpisodes = episodeDataList.Skip(Math.Max(0, episodeDataList.Count - count));

            // Compute stimulation usage for each episode
            var stimUsages = lastEpisodes
                .Where(e => e.Actions.Count > 0)
                .Select(e => e.Actions.Count(a => a > 0) / (double)e.Actions.Count);

            return stimUsages.Any() ? stimUsages.Average() : 0;
        }

        public static int GetBestElectrode(List<EpisodeData> episodeDataList, int count)
        {
            // Select the last 'count' elements from the episode data
            var lastEpisodes = episodeDataList.Skip(Math.Max(0, episodeDataList.Count - count));

            // Compute reliability of electrodes
            Dictionary<int, int> actionCounts = new Dictionary<int, int>();
            Dictionary<int, int> successCounts = new Dictionary<int, int>();
            foreach (var episode in lastEpisodes)
            {
                for (int i = 0; i < episode.Actions.Count; i++)
                {
                    int action = episode.Actions[i];
                    double reward = episode.Rewards[i];

                    if (action > 0)
                    {
                        if (!actionCounts.ContainsKey(action))
                        {
                            actionCounts[action] = 0;
                            successCounts[action] = 0;
                        }

                        actionCounts[action]++;
                        if (reward == 1.0) successCounts[action]++;
                    }
                }
            }

            // Select best performing electrode
            int bestElectrode = successCounts.OrderByDescending(kvp => kvp.Value).First().Key;

            //int bestElectrode = -1;
            //double bestSuccessRate = -1.0;

            //foreach (var action in actionCounts.Keys)
            //{
            //    double rate = (double)successCounts[action] / actionCounts[action];
            //    if (rate > bestSuccessRate)
            //    {
            //        bestSuccessRate = rate;
            //        bestElectrode = action;
            //    }
            //}

            return bestElectrode;
        }

        public int GenerateRandomAction(List<int> electrodes, double stimulationUsage)
        {
            int secureSeed = GenerateSecureSeed();
            Random random = new Random(secureSeed);
            if (random.NextDouble() < stimulationUsage)
            {
                // Generate a random electrode number
                int index = random.Next(electrodes.Count);
                return electrodes[index];
            }

            // No stimulation
            return 0;
        }

        /// <summary>
        /// Struct with the episode data.
        /// </summary>
        [Serializable]
        public class EpisodeData
        {
            public List<double[]> States { get; set; }
            public List<int> Actions { get; set; }
            public List<double> Rewards { get; set; }
            public List<double[]> NextStates { get; set; }
        }
    }
}