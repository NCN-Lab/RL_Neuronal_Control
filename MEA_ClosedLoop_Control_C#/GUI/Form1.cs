//using General_Logic;
using MCS_Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.WindowsAPICodePack.Dialogs;
using static MCS_Devices.MeaDacq;
using Mcs.Usb;
using System.Windows.Media.Animation;
using RL;
using System.Windows.Controls;
using ScottPlot;
using System.Windows.Documents;
using static OpenTK.Graphics.OpenGL.GL;
using ScottPlot.Plottables;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace GUI
{
    public partial class Form1 : Form
    {
        MeaDacq dacq;
        Thresholder thresholder;
        SpikeDetector spkDetector;
        StimulationDetector stimDetector;
        ElecIDsManager elecIDsManager;
        Stimulator stg;
        Worker worker;

        /* ------- constants ------- */
        const int nChannels = 9;          // electrodes
        const int sampleRate = 10000;     // sample-rate 10 kHz
        const int windowBuffer_s = 5;     // seconds shown

        /* ------- chart appearance ------- */
        const int UI_REFRESH_ms = 100;      // redraw ~10 FPS
        const double TRACE_OFFSET = 50;    // separation of channels in µV


        private bool isPaused = false;
        readonly object locker = new object(); // protects rb + spikes

        /* ------- timers & clock ------- */
        readonly System.Windows.Forms.Timer uiTimer = new System.Windows.Forms.Timer { Interval = UI_REFRESH_ms };
        private float[,] _uiScratch;
        private int _idx;

        /* ------- settings ------- */
        private System.Windows.Forms.GroupBox groupBox_settings;
        private System.Windows.Forms.TextBox textBox_chipID;
        private System.Windows.Forms.Label label_chipID;
        private System.Windows.Forms.ComboBox comboBox_well;
        private System.Windows.Forms.Label label_well;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.Button button_folder;
        private System.Windows.Forms.TextBox textBox_savePath;
        private System.Windows.Forms.ListBox listBox_outputs;
        private System.Windows.Forms.CheckBox checkBox_saveData;
        private System.Windows.Forms.CheckBox checkBox_triggerCamera;
        private System.Windows.Forms.Label label_path;

        /* ------- controls ------- */
        private System.Windows.Forms.GroupBox groupBox_runControls;
        private System.Windows.Forms.Button button_start;
        private System.Windows.Forms.Button button_stop;
        private System.Windows.Forms.Button button_pause;


        /* ------- charts & series ------- */
        int[] monitoredElecsIDs;
        private long _totalSamples = 0;      
        private long uiLastSampleAppended = 0;
        readonly Dictionary<int, int> idToRow = new Dictionary<int, int>();
        private ScottPlot.WinForms.FormsPlot formsPlot_traces;
        private ScottPlot.WinForms.FormsPlot formsPlot_raster;
        private ScottPlot.Plottables.SignalXY[] signal_traces = new ScottPlot.Plottables.SignalXY[nChannels];
        private ScottPlot.Plottables.Scatter[] scatter_rasters = new ScottPlot.Plottables.Scatter[nChannels];
        private ScottPlot.Plottables.VerticalLine[] vlines_stimulations_traces = new ScottPlot.Plottables.VerticalLine[0];
        private ScottPlot.Plottables.VerticalLine[] vlines_stimulations_raster = new ScottPlot.Plottables.VerticalLine[0];

        private readonly Dictionary<int, Queue<double>> windowSpikes_s = new Dictionary<int, Queue<double>>();
        private readonly Queue<double> windowStim_s = new Queue<double>();
        private readonly object _lock = new object();           // only if you have multiple threads

        ScottPlot.Colormaps.Dense colormap = new ScottPlot.Colormaps.Dense();
        private System.Windows.Forms.Button button_sendPulse;
        private System.Windows.Forms.CheckBox checkBox_triggerCamara;
        private CancellationTokenSource _cts;

        public Form1(MeaDacq meaDacq, Thresholder thresh)
        {
            dacq = meaDacq;
            thresholder = thresh;
            elecIDsManager = new ElecIDsManager(dacq.Get_MeaLayout());
            stg = new Stimulator(dacq.Get_MeaLayout());
            SetupStimulator();
            InitializeComponent();
            SetupCharts();

            //textBox_savePath.Text = Directory.GetCurrentDirectory();
            textBox_savePath.Text = @"D:\Eduardo\Paper1\experiments\raw";
            listBox_outputs.MeasureItem += listBox_MeasureItem;
            listBox_outputs.DrawItem += listBox_DrawItem;

            /* ---- events ---- */
            uiTimer.Tick += uiTimer_Tick;
            meaDacq.OnStim += (t) => { windowStim_s.Enqueue(t); };
        }

        private void uiTimer_Tick(object sender, EventArgs e)
        {
            if (_uiScratch == null)                 // allocate once
                _uiScratch = new float[
                    dacq.GetBufferRows(),
                    dacq.GetBufferCols()];

            int idx = dacq.GetRingBufferSnapshot(_uiScratch);
            long totalSamples = dacq.Get_totalSamples();

            _idx = idx;
            _totalSamples = totalSamples;

            // long rendering (10-20 ms) — no lock held
            RefreshPlots();

            while (LogBuffer.Messages.TryDequeue(out var msg))
            {
                listBox_outputs.Items.Add(msg);
                listBox_outputs.TopIndex = listBox_outputs.Items.Count - 1;

                const int maxLogLines = 500;
                while (listBox_outputs.Items.Count > maxLogLines)
                    listBox_outputs.Items.RemoveAt(0);
            }
        }

        // Re-write MeaDacq callback function to detect spikes and stimuli
        void ChannelDataEvent(CMcsUsbDacqNet Dacq, int CbHandle, int numFrames)
        {
            lock (_lock)
            {
                (List<double[]> meaData_uV, double[] timestamps_s, _) = dacq.ReadData_uV_(numFrames, true);
                //spkDetector.DetectSpikes(meaData_uV, timestamps_s);
                //stimDetector.DetectStimuli(syncout, timestamps_s);
            }
        }

        private void SetupStimulator()
        {
            stg.Connect_USB_A();
            stg.Deactivate_Full_MEA();
        }

        /* ===== plot refresh ===== */
        private void RefreshPlots()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(RefreshPlots));
                return;
            }

            //float[,] ringBuffer, int idx) = dacq.GetRingBufferSnapshot();
            float[,] ringBuffer = _uiScratch;
            int idx = _idx;
            int bufferLength = ringBuffer.GetLength(1);
            //int nNew = (int)Math.Min(_totalSamples - uiLastSampleAppended, bufferLength);
            //if (nNew <= 0) return;

            /* ----------- time range we want to display ----------- */
            double winStart = ringBuffer[ringBuffer.GetLength(0) - 1, idx];
            double winEnd = ringBuffer[ringBuffer.GetLength(0) - 1, (idx - 1 + bufferLength) % bufferLength];


            var spikeDict_s = spkDetector.GetSpikeDictionary();
            AddElectrodeSpikes(spikeDict_s, winStart);
            //spkDetector.ResetSpikeDictionary();

            // Remove old stimuli
            while (windowStim_s.Count > 0 && windowStim_s.Peek() < winStart)
                windowStim_s.Dequeue();

            if (isPaused) return;

            formsPlot_traces.Plot.Remove<ScottPlot.Plottables.SignalXY>();

            for (int ch = 0; ch < nChannels; ch++)
            {
                double[] displayData = new double[bufferLength];
                double[] xList = Generate.Consecutive(bufferLength, first: winStart, delta: 1.0 / sampleRate);

                var label = $"{comboBox_well.Text}{ch + 1}";
                //int chID = elecIDsManager.GetIDFromLabel(label);
                for (int p = 0; p < bufferLength; p++)
                {
                    displayData[p] = ringBuffer[ch, (idx + p) % bufferLength] + ch * TRACE_OFFSET;
                }

                var sig = formsPlot_traces.Plot.Add.SignalXY(xList, displayData);
                //sig.Color = palette.GetColor(ch);
                sig.Color = colormap.GetColor(ch, 9, startFraction: 0.2, endFraction: 0.8);
                sig.LineWidth = 1;
                signal_traces[ch] = sig;
            }
            formsPlot_traces.Plot.Axes.SetLimits(winStart, winEnd, -TRACE_OFFSET + 1, -1 + nChannels * TRACE_OFFSET);


            formsPlot_raster.Plot.Remove<ScottPlot.Plottables.Scatter>();

            // Raster Chart Optimization: Manual filtering and update
            foreach (int elecID in monitoredElecsIDs)
            {
                int ch = idToRow[elecID];

                List<double> currentSpikesX = new List<double>();
                List<double> currentSpikesY = new List<double>();

                // Filter spikes within the current window
                Queue<double> spikes = windowSpikes_s[elecID];
                foreach (double ts in spikes)
                {
                    if (ts >= winStart)
                    {
                        currentSpikesX.Add(ts);
                        currentSpikesY.Add(ch);
                    }
                }

                var sc = formsPlot_raster.Plot.Add.ScatterPoints(
                    xs: currentSpikesX.ToArray(),
                    ys: Generate.Repeating(currentSpikesX.Count, ch));

                sc.MarkerShape = MarkerShape.VerticalBar;
                sc.MarkerColor = Colors.Black;
                sc.MarkerSize = 10;
                sc.MarkerLineWidth = 2;
                scatter_rasters[ch] = sc;
            }
            formsPlot_raster.Plot.Axes.SetLimits(winStart, winEnd, -0.5, nChannels - 0.5);
            RefreshStimulationLines();

            formsPlot_traces.Refresh();
            formsPlot_raster.Refresh();
        }

        private void RefreshStimulationLines()
        {
            if (this.InvokeRequired)
            {
                // 2. If it's not the UI thread, create a delegate that points back to this method
                // and invoke it on the UI thread.
                // MethodInvoker is a simple delegate type that takes no arguments and returns void.
                this.Invoke(new MethodInvoker(RefreshStimulationLines));
                return; // Important: Exit the current method call on the background thread.
                        // The method will be re-executed on the UI thread.
            }

            if (isPaused) return;

            if (windowStim_s == null) return;

            // Remove existing vertical lines for stimulations
            foreach (var vline in vlines_stimulations_traces)
            {
                formsPlot_traces.Plot.Remove(vline);
            }
            foreach (var vline in vlines_stimulations_raster)
            {
                formsPlot_raster.Plot.Remove(vline);
            }

            // Create new vertical lines for stimulations
            vlines_stimulations_traces = new ScottPlot.Plottables.VerticalLine[windowStim_s.Count];
            vlines_stimulations_raster = new ScottPlot.Plottables.VerticalLine[windowStim_s.Count];

            int i = 0;
            foreach (double stimTime in windowStim_s)
            {
                // For trace plot
                var vlineTrace = formsPlot_traces.Plot.Add.VerticalLine(stimTime);
                vlineTrace.Color = ScottPlot.Colors.Red;
                vlineTrace.LineStyle.Width = 1;
                vlines_stimulations_traces[i] = vlineTrace;

                // For raster plot
                var vlineRaster = formsPlot_raster.Plot.Add.VerticalLine(stimTime);
                vlineRaster.Color = ScottPlot.Colors.Red;
                vlineRaster.LineStyle.Width = 1;
                vlines_stimulations_raster[i] = vlineRaster;

                i++;
            }
        }

        private void SetupCharts()
        {
            /* ---------- traces ------------------------------------------------ */
            for (int ch = 0; ch < nChannels; ch++)
            {
                // an empty ring-buffer (updated in RefreshPlots)
                double[] buf = new double[windowBuffer_s * sampleRate];
                var sig = formsPlot_traces.Plot.Add.SignalXY(Generate.Repeating(windowBuffer_s * sampleRate, 0), buf);

                sig.Data.YOffset = ch * TRACE_OFFSET;

                //sig.Color = palette.GetColor(ch);
                sig.Color = colormap.GetColor(ch, 9, startFraction: 0.2, endFraction: 0.8);
                sig.LineWidth = 1;
                signal_traces[ch] = sig;
            }

            formsPlot_traces.Plot.Axes.SetLimitsY(-TRACE_OFFSET + 1, -1 + nChannels * TRACE_OFFSET);
            formsPlot_traces.Plot.YLabel("Electrode");
            formsPlot_traces.Plot.Grid.YAxisStyle.IsVisible = false;

            /* ---------- raster ------------------------------------------------ */
            for (int ch = 0; ch < nChannels; ch++)
            {
                // start with an empty scatter – we overwrite X/Y arrays each frame
                var sc = formsPlot_raster.Plot.Add.ScatterPoints(
                    xs: Array.Empty<double>(),
                    ys: Array.Empty<double>());

                sc.MarkerShape = MarkerShape.VerticalBar;
                sc.MarkerColor = Colors.Black;
                sc.MarkerSize = 10;
                sc.MarkerLineWidth = 2;
                scatter_rasters[ch] = sc;
            }

            formsPlot_raster.Plot.Axes.SetLimitsY(-0.5, nChannels - 0.5);
            formsPlot_raster.Plot.YLabel("Electrode");
            formsPlot_raster.Plot.Grid.YAxisStyle.IsVisible = false;
            formsPlot_raster.Plot.Grid.XAxisStyle.IsVisible = false;

            /* ---------- first view window (0-WIN s) --------------------------- */
            ScottPlot.TickGenerators.NumericAutomatic tickGenX = new ScottPlot.TickGenerators.NumericAutomatic();
            tickGenX.MinimumTickSpacing = 100;
            formsPlot_traces.Plot.Axes.Bottom.TickGenerator = tickGenX;
            formsPlot_raster.Plot.Axes.Bottom.TickGenerator = tickGenX;
            formsPlot_traces.Plot.Axes.SetLimitsX(0, windowBuffer_s);
            formsPlot_raster.Plot.Axes.SetLimitsX(0, windowBuffer_s);

            formsPlot_traces.Plot.Axes.Left.MinorTickStyle.Length = 0;
            formsPlot_raster.Plot.Axes.Left.MinorTickStyle.Length = 0;

            PixelPadding padding = new PixelPadding(50, 20, 20, 20);
            formsPlot_traces.Plot.Layout.Fixed(padding);
            formsPlot_raster.Plot.Layout.Fixed(padding);

            // create a static function containing the string formatting logic
            string CustomFormatterTraces(double value)
            {
                return $"{value / TRACE_OFFSET + 1}";
            }
            ScottPlot.TickGenerators.NumericAutomatic myTickGenerator_traces = new ScottPlot.TickGenerators.NumericAutomatic()
            {
                LabelFormatter = CustomFormatterTraces
            };
            formsPlot_traces.Plot.Axes.Left.TickGenerator = myTickGenerator_traces;


            // create a static function containing the string formatting logic
            string CustomFormatterRaster(double channel)
            {
                return $"{(int)channel + 1}";
            }
            ScottPlot.TickGenerators.NumericAutomatic myTickGenerator_raster = new ScottPlot.TickGenerators.NumericAutomatic()
            {
                LabelFormatter = CustomFormatterRaster
            };
            formsPlot_raster.Plot.Axes.Left.TickGenerator = myTickGenerator_raster;

            // update linking options
            formsPlot_raster.Plot.Axes.Link(formsPlot_traces, true, false);
            formsPlot_traces.Plot.Axes.Link(formsPlot_raster, true, false);

            AxisLimits limits_traces = formsPlot_traces.Plot.Axes.GetLimits();
            formsPlot_traces.Plot.Axes.Rules.Add(
                new ScottPlot.AxisRules.LockedVertical(formsPlot_traces.Plot.Axes.Left, limits_traces.Bottom, limits_traces.Top));

            AxisLimits limits_raster = formsPlot_raster.Plot.Axes.GetLimits();
            formsPlot_raster.Plot.Axes.Rules.Add(
                new ScottPlot.AxisRules.LockedVertical(formsPlot_raster.Plot.Axes.Left, limits_raster.Bottom, limits_raster.Top));


            formsPlot_traces.Refresh();
            formsPlot_raster.Refresh();
        }

        private void button_pause_Click(object sender, EventArgs e)
        {
            if (button_pause.Text == "Pause")
            {
                button_pause.Text = "Resume";
                isPaused = true;
            }
            else if (button_pause.Text == "Resume")
            {
                button_pause.Text = "Pause";
                isPaused = false;

                RefreshPlots();
            }

        }

        private async void button_start_Click(object sender, EventArgs e)
        {
            formsPlot_traces.Visible = true;
            formsPlot_raster.Visible = true;

            button_start.Enabled = false;
            button_stop.Enabled = true;
            button_pause.Enabled = true;

            listBox_outputs.Items.Clear();

            monitoredElecsIDs = new int[9];
            double[] thresholdsForGate = new double[9];
            foreach (var elec in new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 })
            {
                var label = $"{comboBox_well.Text}{elec}";
                monitoredElecsIDs[elec - 1] = elecIDsManager.GetIDFromLabel(label);
                int elecInd = elecIDsManager.GetIndexFromID(monitoredElecsIDs[elec - 1]);
                thresholdsForGate[elec - 1] = 3.0 / 5 * thresholder.Get_Thresholds_uV()[elecInd];
            }

            dacq.ConfigureSavedChannels(monitoredElecsIDs);

            // ---- Construct cleaner (per-electrode LocalFitChannels) ----
            var cleaner = new SalpaCleaner(
                numChannels: 9,
                sampleRate_Hz: dacq.Get_sampleRate(),
                halfWindowSamples: dacq.Get_salpaLatency(),
                thresholdsForGate: thresholdsForGate,
                blankDepeg_ms: 0.6,
                chi2Window_ms: 0.6,
                zeroCrossResume: false,
                tooPoorCnt: 3,
                railMin: -500.0,
                railMax: 500.0,
                useSyncAsPeg: true
            );
            dacq.ConfigureSalpa(cleaner, true);

            spkDetector = new SpikeDetector(dacq.Get_nElecs(), elecIDsManager, thresholder, 0.003, monitoredElecsIDs);
            dacq.ConfigureSpikeDetector(spkDetector);
            //stimDetector = new StimulationDetector();

            windowSpikes_s.Clear();
            windowStim_s.Clear();

            for (int row = 0; row < monitoredElecsIDs.Length; row++)
                idToRow[monitoredElecsIDs[row]] = row;    //  e.g.  {60→0, 63→1 …}

            checkBox_saveData.Enabled = false;
            if (checkBox_saveData.Checked)
            {
                dacq.Set_channelsToSave(monitoredElecsIDs);

                // Prepare and write metadata at the beginning of the file
                ExperimentMetadata currentMetadata = new ExperimentMetadata
                {
                    StartTime = DateTime.Now,
                    ChipID = textBox_chipID.Text,
                    Well = comboBox_well.Text,
                    SampleRate = sampleRate,
                    Notes = "Generated by MEA Simulation Application",
                    CustomParameters = new Dictionary<string, string>
                    {
                        //{"SpikeTemplateLength", spikeTemplate.Length.ToString()},
                        //{"TraceOffset_uV", TRACE_OFFSET.ToString()},
                        //{"StimProbabilityPerSample", STIM_PROBABILITY_PER_SAMPLE.ToString()}
                    }

                };
                string filename = $"raw_{textBox_chipID.Text}_well{comboBox_well.Text}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.bin";
                dacq.SetupDataSaving(currentMetadata, textBox_savePath.Text, filename);

                // Add a note to the ListBox
                Invoke((MethodInvoker)delegate
                {
                    AppendOutput($"Saving data to: {textBox_savePath.Text}");
                });
            }

            List<int> ignoreElectrodes = new List<int>();

            var taskParams = new TaskParameters
            {
                stimAmplitude_mV = -400,
                //stimAmplitude_mV = -20,
                pulseDuration_ms = 0.2,
                inputSize = 10,
                hiddenSizes = new List<int> { 32 },
                outputSize = 10,
                initialLrActor = 3e-3,
                initialLrCritic = 1e-4,
                gamma = 0,
                weightEntropy = 0.01,
                clipEpsilon = 0.2,
                actionSpaceElectrodes = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 },
                ignoreElectrodes = ignoreElectrodes,
                stepDuration_ms = 200,
                maxNumSteps = 600,
                minSpikeInterval_ms = 3,
                maxNetworkBurstIsi_ms = 20,
                minSpikesPerElectrode = 3,
                minIbi_ms = 200,
                minRatioActiveElectrodes = 0.3,
                minActiveElectrodes = 3
            };

            //string preTrainedAgent = "D:\\Eduardo\\RL_Neuromodulation\\RL_Neuromodulation\\global_PPO_confident.json";
            string preTrainedAgent = "D:\\Eduardo\\RL_Neuromodulation\\RL_Neuromodulation\\global_PPO_hidden=1_size=32.json";

            if (!dacq.isConnected()) 
                dacq.Connect();

            dacq.SetDataCallback(ChannelDataEvent);
            dacq.Set_nFrames(10);
            dacq.ConfigureDacq();
            dacq.StartDacq();
            uiTimer.Start();
            checkBox_triggerCamera.Enabled = false;
            if (checkBox_triggerCamera.Checked) {
                dacq.TriggerCamera();
                AppendOutput("TTL sent to camera...");
            }

            bool finished = false;
            _cts = new CancellationTokenSource();       
            var token = _cts.Token;
            string well = comboBox_well.Text;
            try
            {
                await Task.Run(() =>
                {
                    worker = new Worker(dacq, stg, spkDetector, stimDetector,
                                        elecIDsManager, taskParams,
                                        well, token,
                                        preTrainedAgent, LogToListBox);    
                    worker.chipID = textBox_chipID.Text;
                    finished = worker.Run();
                    //finished = worker.RunTraining(nSteps: 128, stepsPerUpdate: 32, batchSize: 8, nEpochs: 10, exploration: true, saveRun: true);
                    //finished = worker.RunTraining(nSteps: 3072, stepsPerUpdate: 256, batchSize: 64, nEpochs: 10, exploration: true, saveRun: true);
                }, token);
            }
            catch (OperationCanceledException)
            {
                AppendOutput("Worker cancelled.");
            }

            if (finished) button_stop.PerformClick();
        }

        private void LogToListBox(string message)
        {
            if (listBox_outputs.InvokeRequired)
            {
                listBox_outputs.BeginInvoke(new Action(() => LogToListBox(message)));
            }
            else
            {
                listBox_outputs.Items.Add(message);
                listBox_outputs.TopIndex = listBox_outputs.Items.Count - 1;
            }
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            button_start.Enabled = true;
            button_stop.Enabled = false;
            button_pause.Enabled = false;
            uiTimer.Stop();
            _cts?.Cancel();
            dacq.StopDacq();

            if (button_pause.Text == "Resume")
            {
                button_pause.Text = "Pause";
                isPaused = false;
            }

            checkBox_triggerCamera.Enabled = true;
            checkBox_saveData.Enabled = true;
            if (checkBox_saveData.Checked)
            {
                dacq.CloseDataSaving();
                Invoke((MethodInvoker)delegate
                {
                    AppendOutput($"Data saved to: {textBox_savePath.Text}");
                });
            }

            checkBox_triggerCamera.Enabled = true;

            //stimDetector.ResetStimTimes();
            spkDetector.ResetSpikeDictionary();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.button_start = new System.Windows.Forms.Button();
            this.button_stop = new System.Windows.Forms.Button();
            this.button_pause = new System.Windows.Forms.Button();
            this.groupBox_runControls = new System.Windows.Forms.GroupBox();
            this.checkBox_triggerCamara = new System.Windows.Forms.CheckBox();
            this.textBox_chipID = new System.Windows.Forms.TextBox();
            this.label_chipID = new System.Windows.Forms.Label();
            this.comboBox_well = new System.Windows.Forms.ComboBox();
            this.label_well = new System.Windows.Forms.Label();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.button_folder = new System.Windows.Forms.Button();
            this.textBox_savePath = new System.Windows.Forms.TextBox();
            this.groupBox_settings = new System.Windows.Forms.GroupBox();
            this.label_path = new System.Windows.Forms.Label();
            this.checkBox_saveData = new System.Windows.Forms.CheckBox();
            this.listBox_outputs = new System.Windows.Forms.ListBox();
            this.formsPlot_traces = new ScottPlot.WinForms.FormsPlot();
            this.formsPlot_raster = new ScottPlot.WinForms.FormsPlot();
            this.button_sendPulse = new System.Windows.Forms.Button();
            this.checkBox_triggerCamera = new System.Windows.Forms.CheckBox();
            this.groupBox_runControls.SuspendLayout();
            this.groupBox_settings.SuspendLayout();
            this.SuspendLayout();
            // 
            // button_start
            // 
            this.button_start.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_start.ForeColor = System.Drawing.SystemColors.WindowText;
            this.button_start.Location = new System.Drawing.Point(15, 65);
            this.button_start.Name = "button_start";
            this.button_start.Size = new System.Drawing.Size(143, 36);
            this.button_start.TabIndex = 0;
            this.button_start.Text = "Start Experiment";
            this.button_start.UseVisualStyleBackColor = true;
            this.button_start.Click += new System.EventHandler(this.button_start_Click);
            // 
            // button_stop
            // 
            this.button_stop.Enabled = false;
            this.button_stop.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_stop.ForeColor = System.Drawing.SystemColors.WindowText;
            this.button_stop.Location = new System.Drawing.Point(256, 65);
            this.button_stop.Name = "button_stop";
            this.button_stop.Size = new System.Drawing.Size(144, 36);
            this.button_stop.TabIndex = 1;
            this.button_stop.Text = "Stop Experiment";
            this.button_stop.UseVisualStyleBackColor = true;
            this.button_stop.Click += new System.EventHandler(this.button_stop_Click);
            // 
            // button_pause
            // 
            this.button_pause.Enabled = false;
            this.button_pause.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_pause.ForeColor = System.Drawing.SystemColors.WindowText;
            this.button_pause.Location = new System.Drawing.Point(162, 65);
            this.button_pause.Name = "button_pause";
            this.button_pause.Size = new System.Drawing.Size(89, 36);
            this.button_pause.TabIndex = 4;
            this.button_pause.Text = "Pause";
            this.button_pause.UseVisualStyleBackColor = true;
            this.button_pause.Click += new System.EventHandler(this.button_pause_Click);
            // 
            // groupBox_runControls
            // 
            this.groupBox_runControls.Controls.Add(this.checkBox_triggerCamara);
            this.groupBox_runControls.Controls.Add(this.button_pause);
            this.groupBox_runControls.Controls.Add(this.button_stop);
            this.groupBox_runControls.Controls.Add(this.button_start);
            this.groupBox_runControls.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox_runControls.ForeColor = System.Drawing.Color.Teal;
            this.groupBox_runControls.Location = new System.Drawing.Point(31, 182);
            this.groupBox_runControls.Name = "groupBox_runControls";
            this.groupBox_runControls.Size = new System.Drawing.Size(420, 126);
            this.groupBox_runControls.TabIndex = 7;
            this.groupBox_runControls.TabStop = false;
            this.groupBox_runControls.Text = "Run Controls";
            // 
            // checkBox_triggerCamara
            // 
            this.checkBox_triggerCamara.AutoSize = true;
            this.checkBox_triggerCamara.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.checkBox_triggerCamara.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBox_triggerCamara.ForeColor = System.Drawing.SystemColors.WindowText;
            this.checkBox_triggerCamara.Location = new System.Drawing.Point(15, 98);
            this.checkBox_triggerCamara.Name = "checkBox_triggerCamara";
            this.checkBox_triggerCamara.Size = new System.Drawing.Size(133, 22);
            this.checkBox_triggerCamara.TabIndex = 5;
            this.checkBox_triggerCamara.Text = "Trigger Camara";
            this.checkBox_triggerCamara.UseVisualStyleBackColor = true;
            // 
            // textBox_chipID
            // 
            this.textBox_chipID.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_chipID.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_chipID.Location = new System.Drawing.Point(15, 57);
            this.textBox_chipID.Name = "textBox_chipID";
            this.textBox_chipID.Size = new System.Drawing.Size(100, 24);
            this.textBox_chipID.TabIndex = 8;
            // 
            // label_chipID
            // 
            this.label_chipID.AutoSize = true;
            this.label_chipID.BackColor = System.Drawing.SystemColors.Control;
            this.label_chipID.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_chipID.ForeColor = System.Drawing.SystemColors.WindowText;
            this.label_chipID.Location = new System.Drawing.Point(11, 35);
            this.label_chipID.Name = "label_chipID";
            this.label_chipID.Size = new System.Drawing.Size(70, 20);
            this.label_chipID.TabIndex = 9;
            this.label_chipID.Text = "Chip ID:";
            // 
            // comboBox_well
            // 
            this.comboBox_well.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBox_well.FormattingEnabled = true;
            this.comboBox_well.Items.AddRange(new object[] {
            "A",
            "B",
            "C",
            "D",
            "E",
            "F"});
            this.comboBox_well.Location = new System.Drawing.Point(141, 57);
            this.comboBox_well.Name = "comboBox_well";
            this.comboBox_well.Size = new System.Drawing.Size(50, 26);
            this.comboBox_well.TabIndex = 10;
            // 
            // label_well
            // 
            this.label_well.AutoSize = true;
            this.label_well.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_well.ForeColor = System.Drawing.SystemColors.WindowText;
            this.label_well.Location = new System.Drawing.Point(137, 35);
            this.label_well.Name = "label_well";
            this.label_well.Size = new System.Drawing.Size(47, 20);
            this.label_well.TabIndex = 11;
            this.label_well.Text = "Well:";
            // 
            // button_folder
            // 
            this.button_folder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.button_folder.Image = ((System.Drawing.Image)(resources.GetObject("button_folder.Image")));
            this.button_folder.Location = new System.Drawing.Point(369, 114);
            this.button_folder.Name = "button_folder";
            this.button_folder.Size = new System.Drawing.Size(35, 30);
            this.button_folder.TabIndex = 12;
            this.button_folder.UseVisualStyleBackColor = false;
            this.button_folder.Click += new System.EventHandler(this.button_folder_Click);
            // 
            // textBox_savePath
            // 
            this.textBox_savePath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_savePath.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_savePath.Location = new System.Drawing.Point(58, 118);
            this.textBox_savePath.Name = "textBox_savePath";
            this.textBox_savePath.Size = new System.Drawing.Size(305, 24);
            this.textBox_savePath.TabIndex = 13;
            // 
            // groupBox_settings
            // 
            this.groupBox_settings.Controls.Add(this.label_path);
            this.groupBox_settings.Controls.Add(this.checkBox_saveData);
            this.groupBox_settings.Controls.Add(this.textBox_savePath);
            this.groupBox_settings.Controls.Add(this.button_folder);
            this.groupBox_settings.Controls.Add(this.label_well);
            this.groupBox_settings.Controls.Add(this.comboBox_well);
            this.groupBox_settings.Controls.Add(this.label_chipID);
            this.groupBox_settings.Controls.Add(this.textBox_chipID);
            this.groupBox_settings.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox_settings.ForeColor = System.Drawing.Color.Teal;
            this.groupBox_settings.Location = new System.Drawing.Point(31, 12);
            this.groupBox_settings.Name = "groupBox_settings";
            this.groupBox_settings.Size = new System.Drawing.Size(420, 155);
            this.groupBox_settings.TabIndex = 14;
            this.groupBox_settings.TabStop = false;
            this.groupBox_settings.Text = "Settings";
            // 
            // label_path
            // 
            this.label_path.AutoSize = true;
            this.label_path.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_path.ForeColor = System.Drawing.SystemColors.WindowText;
            this.label_path.Location = new System.Drawing.Point(10, 120);
            this.label_path.Name = "label_path";
            this.label_path.Size = new System.Drawing.Size(48, 20);
            this.label_path.TabIndex = 16;
            this.label_path.Text = "Path:";
            // 
            // checkBox_saveData
            // 
            this.checkBox_saveData.AutoSize = true;
            this.checkBox_saveData.Checked = true;
            this.checkBox_saveData.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox_saveData.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBox_saveData.ForeColor = System.Drawing.SystemColors.WindowText;
            this.checkBox_saveData.Location = new System.Drawing.Point(15, 96);
            this.checkBox_saveData.Name = "checkBox_saveData";
            this.checkBox_saveData.Size = new System.Drawing.Size(93, 20);
            this.checkBox_saveData.TabIndex = 15;
            this.checkBox_saveData.Text = "Save Data";
            this.checkBox_saveData.UseVisualStyleBackColor = true;
            this.checkBox_saveData.CheckedChanged += new System.EventHandler(this.checkBox_saveData_CheckedChanged);
            // 
            // listBox_outputs
            // 
            this.listBox_outputs.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.listBox_outputs.FormattingEnabled = true;
            this.listBox_outputs.ItemHeight = 16;
            this.listBox_outputs.Location = new System.Drawing.Point(469, 23);
            this.listBox_outputs.Name = "listBox_outputs";
            this.listBox_outputs.Size = new System.Drawing.Size(425, 255);
            this.listBox_outputs.TabIndex = 6;
            // 
            // formsPlot_traces
            // 
            this.formsPlot_traces.DisplayScale = 0F;
            this.formsPlot_traces.Location = new System.Drawing.Point(31, 331);
            this.formsPlot_traces.Name = "formsPlot_traces";
            this.formsPlot_traces.Size = new System.Drawing.Size(1233, 256);
            this.formsPlot_traces.TabIndex = 15;
            // 
            // formsPlot_raster
            // 
            this.formsPlot_raster.DisplayScale = 0F;
            this.formsPlot_raster.Location = new System.Drawing.Point(31, 599);
            this.formsPlot_raster.Name = "formsPlot_raster";
            this.formsPlot_raster.Size = new System.Drawing.Size(1233, 175);
            this.formsPlot_raster.TabIndex = 16;
            // 
            // button_sendPulse
            // 
            this.button_sendPulse.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_sendPulse.Location = new System.Drawing.Point(916, 23);
            this.button_sendPulse.Name = "button_sendPulse";
            this.button_sendPulse.Size = new System.Drawing.Size(144, 36);
            this.button_sendPulse.TabIndex = 17;
            this.button_sendPulse.Text = "Send Pulse";
            this.button_sendPulse.UseVisualStyleBackColor = true;
            this.button_sendPulse.Click += new System.EventHandler(this.button_sendPulse_Click);
            // 
            // checkBox_triggerCamera
            // 
            this.checkBox_triggerCamera.Location = new System.Drawing.Point(0, 0);
            this.checkBox_triggerCamera.Name = "checkBox_triggerCamera";
            this.checkBox_triggerCamera.Size = new System.Drawing.Size(104, 24);
            this.checkBox_triggerCamera.TabIndex = 0;
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(1297, 806);
            this.Controls.Add(this.button_sendPulse);
            this.Controls.Add(this.formsPlot_raster);
            this.Controls.Add(this.formsPlot_traces);
            this.Controls.Add(this.groupBox_settings);
            this.Controls.Add(this.groupBox_runControls);
            this.Controls.Add(this.listBox_outputs);
            this.Name = "Form1";
            this.groupBox_runControls.ResumeLayout(false);
            this.groupBox_runControls.PerformLayout();
            this.groupBox_settings.ResumeLayout(false);
            this.groupBox_settings.PerformLayout();
            this.ResumeLayout(false);

        }

        private void button_folder_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = textBox_savePath.Text;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textBox_savePath.Text = dialog.FileName;
            }
        }

        private void AppendOutput(string text)
        {
            listBox_outputs.BeginUpdate();                   // optional: avoids flicker
            listBox_outputs.Items.Add(text);

            // 2. scroll so the newly-added item is visible
            listBox_outputs.TopIndex = listBox_outputs.Items.Count - 1;
            //    ^^^^^^^^^^^^^^^^^^^  0-based, so last item is Count-1

            listBox_outputs.EndUpdate();
        }

        private void listBox_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            if (e.Index > -1)
            {
                e.ItemHeight = (int)e.Graphics.MeasureString(listBox_outputs.Items[e.Index].ToString(), listBox_outputs.Font, listBox_outputs.Width).Height;

            }
        }
        private void listBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index > -1)
            {
                e.DrawBackground();
                e.DrawFocusRectangle();
                e.Graphics.DrawString(listBox_outputs.Items[e.Index].ToString(), e.Font, new SolidBrush(e.ForeColor), e.Bounds);
            }
        }

        private void checkBox_saveData_CheckedChanged(object sender, EventArgs e)
        {
            label_path.Enabled = textBox_savePath.Enabled = button_folder.Enabled = (checkBox_saveData.Checked) ? true : false;
            dacq.Set_isSaving(checkBox_saveData.Checked);
        }

        private void AddElectrodeSpikes(Dictionary<int, List<double>> electrodeSpikes_s, double winStart)
        {
            lock (_lock)
            {
                foreach (var q in windowSpikes_s.Values)
                    while (q.Count > 0 && q.Peek() < winStart - 5)
                        q.Dequeue();

                foreach (var kvp in electrodeSpikes_s)
                {
                    int ch = kvp.Key;
                    IReadOnlyList<double> newSpikes = kvp.Value;

                    if (!windowSpikes_s.TryGetValue(ch, out var q))
                    {
                        q = new Queue<double>(capacity: electrodeSpikes_s.Count + 4);
                        windowSpikes_s[ch] = q;
                    }

                    foreach (double ts in newSpikes)
                        q.Enqueue(ts);
                }
            }
        }

        private void button_sendPulse_Click(object sender, EventArgs e)
        {
            // Set up stimulation parameters
            int STG_ID = 1;
            int[] amplitude_uV = { 0 * 1000, 0 }; // Example amplitude
            ulong[] duration_us = { 200, 200 }; // Example duration

            // Download stimulation data
            stg.DownloadStimulus(STG_ID, amplitude_uV, duration_us);
            AppendOutput($"Stimulus downloaded to STG {STG_ID}.");
            stg.Set_STG_StimElecs_IDs(monitoredElecsIDs, STG_ID);
            stg.PrepareWellElectrodes(monitoredElecsIDs);


            Random random = new Random();
            int stimElectrode = random.Next(1, monitoredElecsIDs.Length + 1);

            // Activate electrode
            int elec_ID = elecIDsManager.GetIDFromLabel(comboBox_well.Text + stimElectrode.ToString());
            stg.Activate_StimElecID(elec_ID, STG_ID);
            //stg.Enable_StimElec_ID((uint)elec_ID);

            // Trigger stimulation
            stg.Stimulate(0x1);
            //stg.Stimulate();

            // Deactivate electrode
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed.TotalMilliseconds < 3)
            {
                Thread.SpinWait(100);
            }
            //stg.Disable_StimElec_ID((uint)elec_ID);
            stg.Deactivate_StimElecID(elec_ID);

            AppendOutput($"Pulse sent to electrode {comboBox_well.Text + stimElectrode.ToString()}");

        }

    }
}

