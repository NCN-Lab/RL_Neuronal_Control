using Mcs.Usb;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace MCS_Devices
{
    /// <summary>
    /// Class for managing data acquisition from Multi Channel Systems (MCS) MEA headstages (e.g., MEA256, MEA60).
    /// Handles USB device connection, hardware configuration, data acquisition, timestamping,
    /// ring buffer management, artefact cleaning, stimulation detection, and file saving.
    /// </summary>
    public class MeaDacq
    {
        // --- Connection state ---
        private bool connected = false;

        // --- Channel configuration ---
        private int nElecs;                 // Number of MEA electrodes
        private int nChannels;              // Number of acquisition channels
        private int nIFBChannels;           // Interface board channels (e.g., 8 for MEA60)
        private int channelsInBlock = 0;    // Channels per data block (acquired, includes sideband)

        // --- Sampling configuration ---
        private int sampleRate;             // Sampling rate in Hz
        private int nFrames;                // Number of frames per callback

        // --- Tick/timestamp conversion ---
        private double to_uV;               // ADC conversion to microvolts
        private static ulong wrapCount = 0; // 64-bit tick counter overflow tracking
        private static uint prevTick = 0;   // Previous 32-bit tick
        private bool haveOriginTick = false;
        private uint originTick = 0;

        // --- Channel selection ---
        private int[] savedPhys = Array.Empty<int>();   // Physical indices to keep in buffer
        private int savedCount = 0;                     // Total number of saved channels
        private int syncPhysIndex = 69;                 // Physical index of sync/sideband channel
        private const int EXTRA = 2;                    // +Sync +Timestamp
        private int BUF_SYNC => savedCount;             // last-but-one row
        private int BUF_TIME => savedCount + 1;         // last row

        // --- Buffer configuration ---
        private float[,] ringRaw = null;
        private float[,] ringClean = null;
        private bool[] cleanValid = null;
        private int writeIdx = 0;
        private const int bufferDuration_s = 5;         // Total buffer duration (in seconds)
        private int bufferLength;                       // Total buffer size (in samples)
        private long totalSamples = 0;                  // Running sample count
        private readonly object locker = new object();  // Thread safety

        // --- SALPA integration ---
        private bool salpaActive = true;
        private SalpaCleaner salpa;
        private float salpaLatency_ms = 3.0f;  // Half-window duration in milliseconds
        private int salpaLatency;              // Half-window duration in samples
        private uint prevSyncWord = 0;         // Last raw 32-bit sideband word for edge detection

        // ---- Embedded stimulation detector ----
        private uint lastSyncWord = 0;                                 // for rising-edge detection
        private readonly object stimLock = new object();
        private Queue<double> stimTimes_s = new Queue<double>(1024);   // timestamps (s), grows as needed
        public event Action<double> OnStim;                            // optional: notify with timestamp_s

        // --- Spike detection integration ---
        private SpikeDetector spikeDetector;
        private bool spikeDetectionActive = false;

        // --- File saving ---
        private bool isSaving = true;
        private string outputFilePath;
        private int[] channelsToSave; // Subset of channels to write
        private ExperimentMetadata fileMetadata;
        private FileStream dataFileStream;
        private BinaryWriter binaryWriter;

        /// <summary>
        /// Serializable metadata block written at the top of the binary file.
        /// Includes chip ID, layout, acquisition parameters, and arbitrary notes.
        /// </summary>
        [Serializable]
        public class ExperimentMetadata
        {
            public DateTime StartTime { get; set; }
            public string ChipID { get; set; }
            public string Well { get; set; }
            public int SampleRate { get; set; }
            public string Notes { get; set; }
            public Dictionary<string, string> CustomParameters { get; set; } // Arbitrary metadata
        }

        // --- MCS SDK device interaction ---
        private CMeaUSBDeviceNet dacq = new CMeaUSBDeviceNet(); // MCS object for low-level MEA system interaction
        private CMcsUsbListEntryNet[] available_MeaUsbEntries;  // List of all detected MEA USB devices (MEA256_B, MEA256_A, MEA60_A, MEA60_B)
        private CMcsUsbListEntryNet selected_MeaUsbEntry;       // Currently selected MEA device for acquisition
        private MeaLayoutEnum meaLayout;                        // Layout type of the connected MEA chip (e.g., MEA256-1well, MEA256-9well, MEA60-1well, MEA60-6well)

        // --- Event handling ---
        private List<dataCallbackFunction> eventHandlers = new List<dataCallbackFunction>();
        public delegate void dataCallbackFunction(CMcsUsbDacqNet dacq, int CbHandle, int numFrames);

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MeaDacq class for MEA data acquisition.
        /// </summary>
        /// <param name="deviceKey">Index of the MEA USB device (e.g., 0 for USB_A, 1 for USB_B).</param>
        /// <param name="meaLayout">Layout of the MEA chip (e.g., MEA256_1well, MEA60_6well).</param>
        /// <param name="samplerate">Sampling rate in Hz (e.g., 10000 Hz).</param>
        /// <param name="nframes">Number of frames to collect before triggering a data callback.</param>
        /// <param name="channelToSave">Indices of channels to keep in buffer.</param>
        public MeaDacq(int deviceKey = 0, MeaLayoutEnum meaLayout = MeaLayoutEnum.Undefined, int samplerate = 10000, int nframes = 1, int[] chsToSave = null)
        {
            sampleRate  = samplerate;
            nFrames     = nframes;
            Set_AvailableMeaDevices();
            Set_MeaUsbEntry(deviceKey);
            Set_MeaLayout(meaLayout);

            channelsToSave = chsToSave;
            salpaLatency = (int)(salpaLatency_ms * sampleRate / 1000); // Convert ms to samples
            bufferLength = bufferDuration_s * sampleRate;
        }

        #endregion Constructor

        #region Device Detection and Configuration

        /// <summary>
        /// Queries the system for connected MCS USB devices and stores them in <c>available_MeaUsbEntries</c>.
        /// </summary>
        /// <remarks>
        /// This method must be called before selecting a USB entry via <see cref="Set_MeaUsbEntry"/>.
        /// Detected devices include MEA256_A/B and MEA60_A/B.
        /// </remarks>
        public void Set_AvailableMeaDevices()
        {
            CMcsUsbListNet UsbDeviceList = new CMcsUsbListNet(DeviceEnumNet.MCS_DEVICE_USB);
            available_MeaUsbEntries = UsbDeviceList.GetUsbListEntries();
        }

        /// <summary>
        /// Selects a MEA USB device for acquisition based on its index in the list of available devices.
        /// </summary>
        /// <param name="device_ind">Zero-based index of the target device in <c>available_MeaUsbEntries</c>.</param>
        /// <returns>
        /// <c>true</c> if the device index is valid and the selection was successful; <c>false</c> otherwise.
        /// </returns>
        public bool Set_MeaUsbEntry(int device_ind)
        {
            if (device_ind < available_MeaUsbEntries.Length)
            {
                selected_MeaUsbEntry = available_MeaUsbEntries[device_ind];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the MEA layout and configures the number of electrodes and acquisition channels accordingly.
        /// </summary>
        /// <param name="meaLayout">
        /// Layout of the connected MEA chip. Valid values are:
        /// <list type="bullet">
        /// <item><description><see cref="MeaLayoutEnum.MEA256_1well"/> — 252 electrodes, 256 channels</description></item>
        /// <item><description><see cref="MeaLayoutEnum.MEA256_6well"/> — 252 electrodes, 256 channels</description></item>
        /// <item><description><see cref="MeaLayoutEnum.MEA256_9well"/> — 234 electrodes, 256 channels</description></item>
        /// <item><description><see cref="MeaLayoutEnum.MEA60_1well"/> — 60 electrodes, 60 channels</description></item>
        /// <item><description><see cref="MeaLayoutEnum.MEA60_6well"/> — 54 electrodes, 60 channels</description></item>
        /// </list>
        /// </param> 
        public void Set_MeaLayout(MeaLayoutEnum meaLayout)
        {
            this.meaLayout = meaLayout;

            nIFBChannels = 8; // This is the case for the MEA60. Need to check MEA256!

            switch (meaLayout)
            {
                case MeaLayoutEnum.MEA256_1well:
                    nElecs = 252;
                    nChannels = 256;
                    break;

                case MeaLayoutEnum.MEA256_6well:
                    nElecs = 252;
                    nChannels = 256;
                    break;

                case MeaLayoutEnum.MEA256_9well:
                    nElecs = 234;
                    nChannels = 256;
                    break;

                case MeaLayoutEnum.MEA60_1well:
                    nElecs = 60;
                    nChannels = 60;
                    break;

                case MeaLayoutEnum.MEA60_6well:
                    nElecs = 54;
                    nChannels = 60;
                    break;

                default:
                    nElecs = 0;
                    nChannels = 0;
                    break;
            }

            if (channelsToSave == null && nChannels > 0)
            {
                // Default to all channels if none specified
                savedPhys = new int[nChannels];
                for (int i = 0; i < nChannels; i++)
                    savedPhys[i] = i;
                savedCount = nChannels;
                ConfigureSavedChannels(savedPhys);
            }
        }

        /// <summary>
        /// Calculates the microvolt conversion factor based on hardware range, ADC resolution, and gain.
        /// Attempts to reconnect temporarily to query hardware values. Fallback values are used if conversion fails.
        /// </summary>
        /// <returns><c>true</c> if conversion factor was calculated successfully; <c>false</c> if fallback was used.</returns>
        private bool microVoltsConverter()
        {
            bool converted = true;

            if (connected)
            {

                int range = dacq.GetVoltageRangeInMilliVolt(); // ±range in mV
                uint adc = dacq.GetAdcDataFormat(0); // ADC resolution (e.g., 16-bit)

                int foo = dacq.GetGain();   // For some mysterious reason, the first time you call GetGain() it may return 0, but the second time always returns the right value :/
                int gain = dacq.GetGain();  // Gain in millivolts

                // Conversion: raw ADC → µV
                to_uV = 1000.0 * (range * 2) / Math.Pow(2, adc) / (0.001 * gain);
                //val_uV = data_int * (range*2) / (2^adc) / (0.001*gain) [* 1000]
                //                           |                 |
                //                           |                 L> milli to gain
                //                           L> because of ± in the range
                //
                //     
            }

            if (Double.IsNaN(to_uV) || Double.IsInfinity(to_uV))
            {
                // Fallback values in case hardware reports gain = 0
                to_uV = nChannels == 256 ? 0.03 : 0.008;
                converted = false;
            }

            return converted;
        }

        #endregion

        #region Connection Management
        /// <summary>
        /// Connects to a MEA headstage using the specified USB device index.
        /// </summary>
        /// <param name="deviceKey">Index of the USB connection (e.g., 0 for USB_A, 1 for USB_B).</param>
        /// <returns><c>true</c> if connection is successful; <c>false</c> otherwise.</returns>
        public bool Connect(int deviceKey)
        {
            bool valid = Set_MeaUsbEntry(deviceKey);
            connected = false;

            if (valid)
                connected = Connect();

            return connected;
        }

        /// <summary>
        /// Establishes a connection with the currently selected MEA USB device.
        /// </summary>
        /// <returns><c>true</c> if the device is successfully connected; <c>false</c> otherwise.</returns>
        public bool Connect()
        {
            uint status = dacq.Connect(selected_MeaUsbEntry);
            connected = (status == 0) ? true : false;
            return connected;
        }

        /// <summary>
        /// Disconnects from the currently connected MEA USB device.
        /// </summary>
        public void Disconnect()
        {
            dacq.Disconnect();
            connected = false;
        }

        /// <summary>
        /// Returns the current connection status of the MEA device.
        /// </summary>
        /// <returns><c>true</c> if connected; <c>false</c> otherwise.</returns>
        public bool isConnected()
        {
            connected = dacq.IsConnected();
            return connected;
        }

        #endregion

        #region Acquisition Setup

        /// <summary>
        /// Configures the MEA acquisition system with current sampling parameters.
        /// Initializes data layout, digital channels, timestamps, and FIFO streaming buffer.
        /// </summary>
        public void ConfigureDacq()
        {
            microVoltsConverter();

            if (!isConnected())
                connected = Connect();

            dacq.StopDacq(0); // Ensure acquisition is stopped before reconfiguring

            CSCUFunctionNet scu = new CSCUFunctionNet(dacq);
            scu.SetDacqLegacyMode(false);

            dacq.SetSamplerate(sampleRate, 0, 0);
            dacq.SetDataMode(DataModeEnumNet.Signed_32bit, 0);

            // Specify number of analog channels (MEA + IFB).
            dacq.SetNumberOfAnalogChannels((uint)nChannels, 0, 0, (uint)nIFBChannels, 0);

            /*  For the MEA60:
             * 0 - 59: Electrode Channels
             * 60 - 67 IFB Analog IFB channels
             * 68 Digital In/Out
             * 69 - 74 Sideband channels
             * 75 - 76 Checksum channels
            */

            // Enable sideband and digital lines (used for stim/sync/blanking detection)
            dacq.EnableDigitalIn(
                DigitalDatastreamEnableEnumNet.DigitalIn |
                DigitalDatastreamEnableEnumNet.DigitalOut |
                DigitalDatastreamEnableEnumNet.Hs1SidebandLow |
                DigitalDatastreamEnableEnumNet.Hs1SidebandHigh,
                0);

            dacq.EnableTimestamp(
                true,   // enable flag
                0);     // device index – always 0 for single IFB

            dacq.EnableChecksum(true, 0);

            // Query data layout and prepare buffer dimensions
            dacq.GetChannelLayout(out int analogChannels, out int digitalChannels,
                                  out int checksumChannels, out int timestampChannels,
                                  out channelsInBlock, 0);

            Console.WriteLine($"Analog channels      : {analogChannels}");
            Console.WriteLine($"Digital channels     : {digitalChannels}");
            Console.WriteLine($"Checksum channels    : {checksumChannels}");
            Console.WriteLine($"Timestamp channels   : {timestampChannels}");
            Console.WriteLine($"Total 16-bit words   : {channelsInBlock}");
            Console.WriteLine($"Expected 32-bit channels: {channelsInBlock / 2}");


            /*
            // Alternative method (commented out):
            // Creates one FIFO queue per channel. This is more flexible but less efficient than using a shared FIFO.
            // Kept here for reference or debugging scenarios that require per-channel access.
            //dacq.ChannelBlock.SetSelectedChannels(
            //    Enumerable.Repeat(true, channelsInBlock / 2).ToArray(),
            //    sampleRate, nFrames,
            //    SampleSizeNet.SampleSize32Signed,
            //    SampleDstSizeNet.SampleDstSize32,
            //    channelsInBlock);
            */

            // Preferred method: one FIFO buffer for all channels (more efficient for high-throughput acquisition)
            dacq.ChannelBlock.SetSelectedData(
                channelsInBlock / 2, sampleRate, nFrames,
                SampleSizeNet.SampleSize32Signed,
                SampleDstSizeNet.SampleDstSize32,
                channelsInBlock);

            /*
            // Legacy syntax (commented): uses Init/AddBlocksAndChannels explicitly.
            // Useful for low-level control or when using multiple handles.
            //dacq.ChannelBlock.Init(channelsInBlock / 2);
            //dacq.ChannelBlock.AddBlocksAndChannels(
            //    ChannelBlockTypeNet.OneHandleOneQueue,
            //    Enumerable.Repeat(true, channelsInBlock / 2).ToArray(),
            //    sampleRate,
            //    nFrames,
            //    SampleSizeNet.SampleSize32Signed,
            //    SampleDstSizeNet.SampleDstSize32,
            //    0, 0);
            */

            dacq.ChannelBlock.SetCommonThreshold(nFrames);
            dacq.ChannelBlock.SetCheckChecksum((uint)checksumChannels, (uint)timestampChannels);
        }

        /// <summary>
        /// Sets the sampling rate and reconfigures the acquisition system.
        /// </summary>
        /// <param name="samplerate">Sampling rate in Hz (e.g., 10000).</param>
        public void Set_SamplingRate(int samplerate)
        {
            sampleRate = samplerate;
            if (available_MeaUsbEntries.Length > 0)
                ConfigureDacq();
        }

        /// <summary>
        /// Sets the number of frames per acquisition callback.
        /// </summary>
        /// <param name="nframes">Frame count per block (e.g., 1–10).</param>
        public void Set_nFrames(int nframes) => nFrames = nframes;

        /// <summary>
        public void ConfigureSavedChannels(int[] physIndices)
        {
            savedPhys = physIndices.ToArray();
            savedCount = savedPhys.Length;
            ReallocRings();
        }

        /// <summary>
        public void SetSyncPhysicalIndex(int idx) => syncPhysIndex = idx;

        /// <summary>
        private void ReallocRings()
        {
            ringRaw = new float[savedCount + EXTRA, bufferLength];
            ringClean = new float[savedCount + EXTRA, bufferLength];
            cleanValid = new bool[bufferLength];
            Array.Clear(cleanValid, 0, cleanValid.Length);
        }

        /// <summary>
        public void ConfigureSalpa(SalpaCleaner cleaner, bool active)
        {
            salpa = cleaner;
            salpaActive = active;
            salpaLatency = cleaner?.LatencySamples ?? 0;
            prevSyncWord = 0;
        }

        /// <summary>
        public void SetSalpaActive(bool active) => salpaActive = active;

        /// <summary>
        public bool GetSalpaActive() => salpaActive;

        /// <summary>
        /// Configures and enables embedded spike detection.
        /// </summary>
        /// <param name="detector">The configured SpikeDetector instance.</param>
        public void ConfigureSpikeDetector(SpikeDetector detector)
        {
            spikeDetector = detector;
            spikeDetectionActive = (detector != null);
        }

        #endregion

        #region Acquisition Control

        /// <summary>
        /// Starts real-time data acquisition and resets internal buffers and timestamp.
        /// </summary>
        /// <returns><c>true</c> if acquisition started successfully; <c>false</c> otherwise.</returns>
        public bool StartDacq()
        {
            bool started = false;
            if (connected)
            {
                ResetRingBuffers();
                haveOriginTick = false;

                dacq.StartDacq();

                started = true;
            }
            return started;
        }

        public bool TriggerCamera()
        {
            bool triggered = false;
            dacq.SetDigitalSource(DigitalTargetEnumNet.Digout, 0, SCUDigitalSourceEnumNet.DigitalData, 0);
            CMeaDigitalDataFunctionNet dig = new CMeaDigitalDataFunctionNet(dacq);
            dig.SetDigitalData(0, true);
            dig.SetDigitalData(0, false);
            triggered = true;
            
            return triggered;
        }

        /// <summary>
        /// Stops real-time data acquisition if a device is currently connected.
        /// </summary>
        public void StopDacq()
        {
            if (connected)
                dacq.StopDacq(0);
        }                  

        /// <summary>
        /// Sets the callback function for incoming data during acquisition.
        /// Recommended to be called right before starting acquisition to ensure previous callbacks are cleared.
        /// </summary>
        /// <param name="dataCallback">Function to handle channel data events.</param>
        public void SetDataCallback(dataCallbackFunction dataCallback)
        {
            if (eventHandlers.Count > 0)
            {
                // Remove the existing callback (only one is expected)
                dacq.ChannelDataEvent -= new OnChannelData(eventHandlers[0]);
                eventHandlers.Clear();
            }

            dacq.ChannelDataEvent += new OnChannelData(dataCallback);
            eventHandlers.Add(dataCallback);
        }

        #endregion

        #region Data Handling

        /// <summary>
        /// Reads the most recent data block from the MEA FIFO buffer, pushes it to the ring buffer,
        /// and returns a snapshot of electrode data (in µV), timestamps (in seconds), and sync channel.
        /// </summary>
        /// <param name="callbackFrames">Number of frames requested from the FIFO (from callback).</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item><description>List of electrode data arrays [channel][time], in µV</description></item>
        /// <item><description>Timestamp array [time], in seconds</description></item>
        /// <item><description>Sync output array [time], raw 32-bit words from sideband channel</description></item>
        /// </list>
        /// </returns>
        public (List<double[]>, double[], int[]) ReadData_uV_(int callbackFrames, bool cleaned = false)
        {
            int[] data = dacq.ChannelBlock.ReadFramesI32(
                0, // single IFB handle
                0, // unified FIFO (via SetSelectedData)
                callbackFrames,
                out int framesRead);

            // Push raw int32 data into the ring buffer
            lock (ringRaw)
                PushToRingBuffer(data, framesRead);

            var meaData_uV = new List<double[]>(savedCount);
            var timestamps_s = new double[framesRead];
            var syncout = new int[framesRead];

            // Extract µV data, timestamps, and sync from the ring buffer (thread-safe snapshot)
            lock ((salpaActive && cleaned) ? ringClean : ringRaw)
            {
                for (int i = 0; i < savedCount; i++)
                {
                    double[] row = new double[framesRead];

                    for (int k = 0; k < framesRead; k++)
                    {
                        int idx = (writeIdx - framesRead + k + bufferLength) % bufferLength;
                        float v = (salpaActive && cleaned) ? ringClean[i, idx] : ringRaw[i, idx];
                        row[k] = v;
                    }
                    meaData_uV.Add(row);
                }

                for (int k = 0; k < framesRead; k++)
                {
                    int idx = (writeIdx - framesRead + k + bufferLength) % bufferLength;
                    timestamps_s[k] = (salpaActive && cleaned) ? ringClean[BUF_TIME, idx] : ringRaw[BUF_TIME, idx];
                    syncout[k] = (salpaActive && cleaned) ? (int)ringClean[BUF_SYNC, idx] : (int)ringRaw[BUF_SYNC, idx];
                }

                return (meaData_uV, timestamps_s, syncout);
            }
        }


        /// <summary>
        /// Copies acquired raw data into the circular ring buffer with timestamp decoding and wraparound handling.
        /// Also updates the 64-bit global time counter and optionally writes to file.
        /// </summary>
        /// <param name="data">Raw 32-bit signed integer values interleaved by channel and time.</param>
        /// <param name="frames">Number of frames represented in <paramref name="data"/> (must satisfy: data.Length = frames × channels).</param>
        void PushToRingBuffer(int[] data, int frames)
        {
            int chans = data.Length / frames;   // Total number of 32-bit channels
            int stride = chans;

            lock (ringRaw)
            {
                for (int f = 0; f < frames; f++)
                {
                    int dst = (writeIdx + f) % bufferLength;
                    int baseIdx = f * stride;

                    // Copy only selected analog channels (to µV)
                    for (int i = 0; i < savedCount; i++)
                    {
                        int phys = savedPhys[i];
                        ringRaw[i, dst] = (float)(data[baseIdx + phys] * to_uV);
                    }

                    // Decode and store timestamp
                    uint tick = unchecked((uint)data[baseIdx + stride - 2]);
                    if (!haveOriginTick)
                    {
                        originTick = tick;
                        prevTick = tick;
                        haveOriginTick = true;
                    }

                    // Detect tick counter overflow (wraparound)
                    const uint WRAP_THRESHOLD = 0x00FF_FFFF; // ~16M ticks ( ≈ 5 min @20 µs )
                    if (tick < prevTick && prevTick > WRAP_THRESHOLD && tick < WRAP_THRESHOLD) // accept wrap only if tick dropped *near zero*
                        wrapCount++;
                    prevTick = tick;

                    ulong fullTick = (wrapCount << 32) | tick;
                    ulong deltaTick = fullTick - originTick; // remove the origin offset
                    float timestamp_s = (float)(deltaTick * 20.0 * 1e-6); // 20 µs per tick → s
                    ringRaw[BUF_TIME, dst] = timestamp_s;

                    // Copy sync output channel and detect rising edge
                    uint syncWord = unchecked((uint)data[baseIdx + syncPhysIndex]);
                    ringRaw[BUF_SYNC, dst] = syncWord;
                    bool syncHigh = (lastSyncWord == 0) && (syncWord != 0);
                    lastSyncWord = syncWord;
                    if (syncHigh)
                    {
                        lock (stimLock) stimTimes_s.Enqueue(timestamp_s);
                        OnStim?.Invoke(timestamp_s);
                    }

                    //// Print time every 1000 samples (100 ms at 10kHz) just for debugging
                    //if (dst % 10000 == 0) Console.WriteLine($"Time: {ringBuffer[chans - 2, dst]} s");

                    //// Print trigger just for debugging
                    //if (ringBuffer[69, (dst - 1 + bufferLength) % bufferLength] == 0 & ringBuffer[69, dst] != 0)
                    //    Console.WriteLine("TRIGGER!!!");

                    // =========================
                    // SALPA (optional, per-frame)
                    // =========================
                    if (salpaActive && salpa != null)
                    {
                        // Build analog frame for SALPA from what we just wrote (µV)
                        double[] analog = new double[savedCount];
                        for (int i = 0; i < savedCount; i++)
                            analog[i] = ringRaw[i, dst];

                        // One-sample rising edge on sideband (peg trigger)
                        syncWord = (uint)ringRaw[BUF_SYNC, dst];
                        syncHigh = (prevSyncWord == 0) && (syncWord != 0);
                        prevSyncWord = syncWord;

                        long sampleIndex = totalSamples + f;

                        // Process; if SALPA emits, write CLEANED at latency-aligned slot
                        double[] cleaned = new double[savedCount];
                        if (salpa.ProcessSample(analog, sampleIndex, syncHigh, cleaned, out _))
                        {
                            var emitDst = (dst - salpaLatency - 1 + bufferLength) % bufferLength;
                            for (int i = 0; i < savedCount; i++)
                                ringClean[i, emitDst] = (float)cleaned[i];
                                //ringClean[i, emitDst] = ringRaw[i, emitDst];

                            // propagate sync & time rows for alignment (optional but handy)
                            ringClean[BUF_SYNC, emitDst] = ringRaw[BUF_SYNC, emitDst];
                            ringClean[BUF_TIME, emitDst] = ringRaw[BUF_TIME, emitDst];
                        }
                    }


                    // =========================
                    // SPIKE DETECTION (optional)
                    // =========================
                    if (spikeDetectionActive && spikeDetector != null)
                    {
                        List<double[]> currentFrameData_uV = new List<double[]>(savedCount);
                        double[] currentFrameTimestamp_s;
                        if (salpaActive)
                        {
                            var emitDst = (dst - salpaLatency - 1 + bufferLength) % bufferLength;
                            currentFrameTimestamp_s = new double[] { ringClean[BUF_TIME, emitDst] };
                            for (int i = 0; i < savedCount; i++)
                                currentFrameData_uV.Add(new double[] { ringClean[i, emitDst] });
                        }
                        else
                        {
                            currentFrameTimestamp_s = new double[] { ringRaw[BUF_TIME, dst] };
                            for (int i = 0; i < savedCount; i++)
                                currentFrameData_uV.Add(new double[] { ringRaw[i, dst] });
                        }
                        spikeDetector.DetectSpikes(currentFrameData_uV, currentFrameTimestamp_s);
                    }

                }

                if (isSaving)
                    WriteNewestSamplesToFile(frames);

                writeIdx = (writeIdx + frames) % bufferLength;
                totalSamples += frames;
            }
        }

        #endregion

        #region Ring Buffer Access

        /// <summary>
        /// Returns the latest timestamp (in seconds) from the ring buffer.
        /// </summary>
        public double GetCurrentTime()
        {
            return ringRaw[BUF_TIME, (writeIdx - 1 + bufferLength) % bufferLength];
        }

        /// <summary>
        /// Returns a snapshot (deep copy) of the ring buffer and current write index.
        /// Thread-safe via internal locking.
        /// </summary>
        public (float[,], int) GetRingBufferSnapshot()
        {
            lock (locker)
            {
                return ((salpaActive) ? (float[,])ringClean.Clone() : (float[,])ringRaw.Clone(), 
                    (salpaActive) ? (writeIdx - salpaLatency - 1 + bufferLength) % bufferLength : writeIdx);
            }
        }

        /// <summary>
        /// Returns the number of rows (channels) in the ring buffer.
        /// </summary>
        public int GetBufferRows()
        {
            return savedCount + 2;
        }

        /// <summary>
        /// Returns the number of columns (time samples) in the ring buffer.
        /// </summary>
        public int GetBufferCols()
        {
            return bufferLength;
        }

        /// <summary>
        /// Copies the full contents of the ring buffer into a user-supplied buffer.
        /// Buffer dimensions must match.
        /// </summary>
        /// <param name="target">Destination array with identical shape to internal buffer.</param>
        /// <returns>Current write index.</returns>
        /// <exception cref="ArgumentException">Thrown if the target array shape does not match the ring buffer.</exception>
        public int GetRingBufferSnapshot(float[,] target)
        {
            // Validate target dimensions
            if (target == null ||
                target.GetLength(0) != savedCount + 2 ||
                target.GetLength(1) != bufferLength)
                throw new ArgumentException("Snapshot array has wrong shape", nameof(target));

            // Perform a full buffer copy under lock for thread safety
            lock (locker)
            {
                int bytes = Buffer.ByteLength((salpaActive) ? ringClean : ringRaw);                    
                Buffer.BlockCopy((salpaActive) ? ringClean : ringRaw, 0, target, 0, bytes);

                return (salpaActive) ? (writeIdx - salpaLatency - 1 + bufferLength) % bufferLength : writeIdx; // Return current head of the circular buffer            
            }
        }

        /// <summary>
        /// Resets the internal ring buffers to zero.
        /// </summary>
        public void ResetRingBuffers()
        {
            writeIdx = 0;
            if (ringRaw != null) Array.Clear(ringRaw, 0, bufferLength);
            if (ringClean != null) Array.Clear(ringClean, 0, bufferLength);
        }

        #endregion

        #region Stimulation Detector Access

        public double[] GetStimTimesSnapshot()
        {
            lock (stimLock) return stimTimes_s.ToArray();
        }
        public void ClearStimTimes()
        {
            lock (stimLock) stimTimes_s.Clear();
        }

        #endregion Stimulation Detector Access

        #region Data Saving

        /// <summary>
        /// Prepares the system to save MEA data to disk by opening a file stream and writing metadata.
        /// Generates a unique filename if none is provided.
        /// </summary>
        /// <param name="metadata">Experiment metadata to embed in the file header.</param>
        /// <param name="selectedDirectory">Target directory for saving the binary file.</param>
        /// <param name="filename">Optional filename. If null, a timestamped filename will be used.</param>
        public void SetupDataSaving(ExperimentMetadata metadata, string selectedDirectory, string filename = null)
        {
            if (string.IsNullOrWhiteSpace(selectedDirectory) || !Directory.Exists(selectedDirectory))
            {
                outputFilePath = null; // Indicate that no file saving will occur
                return;
            }

            string fileName = filename ?? $"MEA_RawData_{DateTime.Now:yyyyMMdd_HHmmss}.bin";
            outputFilePath = Path.Combine(selectedDirectory, fileName);

            try
            {
                dataFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                binaryWriter = new BinaryWriter(dataFileStream, Encoding.UTF8, leaveOpen: true);

                fileMetadata = metadata;
                WriteMetadata(binaryWriter, fileMetadata);
            }
            catch (Exception ex)
            {
                outputFilePath = null;      // Reset if error
                dataFileStream?.Dispose();  // Ensure resources are released
                binaryWriter?.Dispose();
            }
        }

        /// <summary>
        /// Writes the most recent <paramref name="frames"/> of data for the specified <paramref name="channels"/>
        /// to the binary file. Includes timestamp, selected electrode channels, and raw sync word.
        /// </summary>
        /// <param name="frames">Number of frames to write.</param>
        /// <param name="channels">Indices of electrode channels to save.</param>
        void WriteNewestSamplesToFile(int frames)
        {
            if (binaryWriter != null && dataFileStream.CanWrite)
            {
                try
                {
                    int start = (writeIdx - frames + bufferLength) % bufferLength;

                    for (int f = 0; f < frames; f++)
                    {
                        int idx = (start + f) % bufferLength;

                        // Write timestamp (already in seconds)
                        binaryWriter.Write(ringRaw[BUF_TIME, idx]);

                        // Write selected electrode channels (in µV)
                        for (int ch = 0; ch < savedCount; ch++)
                            binaryWriter.Write(ringRaw[ch, idx]);

                        // Write raw sync/stim word as 32-bit int for post-hoc bit masking
                        binaryWriter.Write(BitConverter.GetBytes((int)ringRaw[BUF_SYNC, idx]));
                    }
                }
                catch (Exception ex)
                {
                    CloseDataSaving(); // Attempt to close the file gracefully on error
                }
            }
        }

        /// <summary>
        /// Writes file header and serialized experiment metadata to the binary stream.
        /// Includes a magic number and version ID before the actual data.
        /// </summary>
        /// <param name="writer">BinaryWriter to the open data stream.</param>
        /// <param name="metadata">Serializable metadata to embed at file start.</param>
        public void WriteMetadata(BinaryWriter writer, ExperimentMetadata metadata)
        {
            writer.Write(0xFEEDF00D);   // Magic number for this file type
            writer.Write(1);            // Version of the file format

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, metadata);
                byte[] metadataBytes = ms.ToArray();
                writer.Write(metadataBytes.Length);
                writer.Write(metadataBytes);
            }
        }

        /// <summary>
        /// Finalizes and closes all open file streams associated with data saving.
        /// Ensures all buffers are flushed and system resources are released.
        /// </summary>
        public void CloseDataSaving()
        {
            if (binaryWriter != null)
            {
                try
                {
                    binaryWriter.Flush();
                    dataFileStream?.Flush(); // Ensure OS file cache is flushed
                    binaryWriter.Close();
                }
                finally
                {
                    binaryWriter?.Dispose();
                    dataFileStream?.Dispose();
                    binaryWriter = null;
                    dataFileStream = null;
                    outputFilePath = null;
                }
            }
        }

        #endregion

        #region Getters

        public CMcsUsbListEntryNet[] Get_Available_MeaUsbEntries()
        {
            return available_MeaUsbEntries;
        }

        public CMcsUsbListEntryNet Get_Selected_MeaUsbEntry()
        {
            return selected_MeaUsbEntry;
        }

        public string Get_Selected_MeaUsbEntry_Name()
        {
            return selected_MeaUsbEntry.ToString();
        }

        public MeaLayoutEnum Get_MeaLayout()
        {
            return meaLayout;
        }
     
        public int Get_nElecs()
        {
            return nElecs;
        }

        public int Get_nChannels()
        {
            return nChannels;
        }

        public int Get_sampleRate()
        {
            return sampleRate;
        }

        public int Get_nFrames()
        {
            return nFrames;
        }

        public long Get_totalSamples()
        {
            return totalSamples;
        }

        public CMeaUSBDeviceNet Get_dacq()
        {
            return dacq;
        }

        public int Get_salpaLatency()
        {
            return salpaLatency;
        }

        #endregion

        #region Setters

        public void Set_isSaving(bool saveData)
        {
            isSaving = saveData;
        }

        public void Set_channelsToSave(int[] channels)
        {
            channelsToSave = channels;
        }

        #endregion

    }
}
