using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MCS_Devices
{
    public enum State { OK, PEGGING, PEGGED, FORCEPEG, TOOPOOR, BLANKDEPEG, DEPEGGING }

    /// <summary>
    /// Multi-channel SALPA cleaner that wraps one LocalFitChannel per electrode.
    /// - Constant latency = 2N (N is half-window).
    /// - Optional per-channel rail detection is handled inside LocalFitChannel, 
    ///   with N-sample look-ahead mechanism.
    /// - Optional external sync: on a rising edge we start PEGGED exactly at the
    ///   current emission index (center timestamp) for every channel.
    /// Call ProcessSample() once per frame.
    /// </summary>
    public sealed class SalpaCleaner
    {
        public int NumChannels { get; }
        public int SampleRateHz { get; }
        public int N { get; }
        public int LatencySamples => 2 * N;

        private readonly LocalFitChannel[] _ch;
        private readonly bool _useSyncAsPeg;
        private bool _prevSync;

        // SALPA can distort spikes within NBs. We could minimize this by introducing a
        // maximum duration for the cleaning, emitting the raw sample when not necessary.
        private int _cooldownTimer = 0;
        private const double maxCleaning_ms = 100.0;

        /// <param name="numChannels">Number of analog channels/electrodes</param>
        /// <param name="sampleRate_Hz">Sampling rate</param>
        /// <param name="halfWindowSamples">N (window = 2N+1)</param>
        /// <param name="thresholdsForGate">Threshold for Asym gate (µV)</param>
        /// <param name="blankDepeg_ms">Extra blank after gate pass (Wagenaar -b), ms</param>
        /// <param name="chi2Window_ms">Asym gate window length, ms (e.g., 0.6 ms)</param>
        /// <param name="zeroCrossResume">Use zero-cross criterion in BLANKDEPEG</param>
        /// <param name="tooPoorCnt">Hysteresis for Asym gate (e.g., 3)</param>
        /// <param name="railMin">Optional saturation min (µV)</param>
        /// <param name="railMax">Optional saturation max (µV)</param>
        /// <param name="useSyncAsPeg">If true, a sync rising edge starts PEGGED at the emission index</param>
        public SalpaCleaner(
            int numChannels,
            int sampleRate_Hz,
            int halfWindowSamples,
            IReadOnlyList<double> thresholdsForGate,
            double blankDepeg_ms = 0.6,
            double chi2Window_ms = 0.6,
            bool zeroCrossResume = false,
            int tooPoorCnt = 3,
            double railMin = double.NegativeInfinity,
            double railMax = double.PositiveInfinity,
            bool useSyncAsPeg = true)
        {
            if (numChannels <= 0) throw new ArgumentOutOfRangeException(nameof(numChannels));
            if (sampleRate_Hz <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate_Hz));
            if (halfWindowSamples < 1) throw new ArgumentOutOfRangeException(nameof(halfWindowSamples));
            if (thresholdsForGate is null) throw new ArgumentNullException(nameof(thresholdsForGate));
            if (thresholdsForGate.Count != numChannels)
                throw new ArgumentException($"thresholdsForGate.Count ({thresholdsForGate.Count}) must equal numChannels ({numChannels}).",
                                            nameof(thresholdsForGate));
            if (thresholdsForGate.Any(x => !(x > 0.0)))
                throw new ArgumentException("All thresholdsForGate values must be > 0.", nameof(thresholdsForGate));


            NumChannels = numChannels;
            SampleRateHz = sampleRate_Hz;
            N = halfWindowSamples;
            _useSyncAsPeg = useSyncAsPeg;

            int blankDepeg_s = (int)Math.Round(blankDepeg_ms * sampleRate_Hz / 1000.0);
            int chi2Win_s = Math.Max(1, (int)Math.Round(chi2Window_ms * sampleRate_Hz / 1000.0));

            _ch = new LocalFitChannel[NumChannels];
            for (int c = 0; c < NumChannels; c++)
            {

                //double[] Noises = { 5.97535944, 5.62497747, 5.37975597, 8.45691705, 5.45228004,
                //                    6.04486442, 6.56723928, 6.40464234, 5.74169755 }; // 3 / 5 * thresholds


                _ch[c] = new LocalFitChannel(
                    halfWindowSamples: N,
                    thresholdForGate: thresholdsForGate[c],
                    //thresholdForGate: Noises[c],
                    blankDepegSamples: blankDepeg_s,
                    chi2WindowSamples: chi2Win_s,
                    zeroCrossResume: zeroCrossResume,
                    tooPoorCnt: tooPoorCnt,
                    railMin: railMin,
                    railMax: railMax
                );
                _ch[c]._id = c + 1;

            }
        }

        /// <summary>
        /// Process one interleaved frame.
        /// Returns true once the first full window is available (constant latency = 2N).
        /// When true, 'cleaned_uV' contains one cleaned sample per channel aligned to 'outEmitIndex'.
        /// </summary>
        public bool ProcessSample(double[] rawFrame_uV,
                                  long sampleIndex,
                                  bool syncHigh,
                                  double[] cleaned_uV,
                                  out long outEmitIndex)
        {
            if (rawFrame_uV.Length != NumChannels)
                throw new ArgumentException($"Expected {NumChannels} channels", nameof(rawFrame_uV));

            // Rising edge => start PEGGED at the *current emission index* on all channels
            bool rising = syncHigh && !_prevSync;
            _prevSync = syncHigh;

            if (_useSyncAsPeg && rising)
            {
                _cooldownTimer = (int)(maxCleaning_ms * SampleRateHz / 1000.0);
                for (int c = 0; c < NumChannels; c++)
                    _ch[c].TriggerPegAtCurrentEmission();
            }
            else if (_cooldownTimer > 0)
            {
                _cooldownTimer--;
            }

            //cleaned_uV = new double[NumChannels];

            // Drive each LocalFitChannel with its sample
            bool ready = _ch[0].Step(rawFrame_uV[0], out cleaned_uV[0]);
            for (int c = 1; c < NumChannels; c++)
            {
                // We keep calling Step even if ready==false to fill all rings uniformly
                _ch[c].Step(rawFrame_uV[c], out cleaned_uV[c]);
            }

            if (!ready)
            {
                outEmitIndex = 0;
                return false; // warm-up period (fewer than 3N+1 samples seen)
            }

            // LocalFitChannel is constant-latency: the emitted sample corresponds to (sampleIndex - 2N)
            outEmitIndex = sampleIndex - 2 * N;

            // We only use the SALPA-subtracted value if we are NOT in State.OK
            // OR if the hardware sync is still high.
            for (int c = 0; c < NumChannels; c++)
            {
                if (_ch[c].GetCurrentState() == State.OK && !syncHigh && _cooldownTimer <= 0)
                {
                    // The state machine says this channel is stable and no stim is active.
                    // Return the RAW sample from N samples ago to avoid cubic distortion.
                    cleaned_uV[c] = _ch[c].GetRawAtLatency();
                }
            }

            return true;
        }
    }



    /// <summary>
    /// Faithful mirror of Wagenaar's LocalFit state machine (single channel).
    /// Window half-size = N (tau). Emits 1 sample per Step() once the first window is filled.
    /// </summary>
    public sealed class LocalFitChannel
    {
        public int _id;

        // ---------------- Configuration ----------------
        public readonly int N;                    // tau
        public readonly int W;                    // 3N+1
        public readonly double RailMin;           // saturation rails (µV)
        public readonly double RailMax;
        public readonly int t_blankdepeg;         // extra blank after gate pass (samples)
        public readonly int t_chi2;               // gate window length (samples)
        public readonly bool useZeroCross;        // like usenegv in C code
        public readonly int TOOPOORCNT;           // small hysteresis counter for gate

        // noise scale for gate
        private readonly double y_threshold;      // ≈ noise RMS per channel
        private readonly double my_thresh;        // 3.92 * t_chi2 * y_threshold^2

        // geometric sums (only depend on N)
        private readonly int T0, T2, T4, T6;
        private readonly int tau_plus_1, minus_tau;
        private readonly int tau_plus_1_sq, minus_tau_sq;
        private readonly int tau_plus_1_cu, minus_tau_cu;

        // ---------------- Ring buffer ----------------
        private readonly double[] ring;
        private int rStart;                       // index of oldest
        private long oldestIdx;                   // absolute index of oldest
        private int rCount;
        private long latestIdx;                   // absolute index of newest pushed sample

        // ---------------- State machine ----------------
        private State state = State.PEGGED;
        public State GetCurrentState() => state;

        private long t_stream;                    // next emission index
        private long t0;                          // fit center for cubic phases

        // running sums around current center for OK-state update (even block)
        private double X0, X1, X2, X3;

        // full-cubic coefficients around t0
        private double a0, a1, a2, a3;

        // gate hysteresis and zero-crossing polarity
        private int toopoorcnt;
        private bool negv;

        private bool _pegAtNextEmission;
        private bool okSumsValid = true;

        public LocalFitChannel(
            int halfWindowSamples,
            double thresholdForGate,
            int blankDepegSamples,
            int chi2WindowSamples,
            bool zeroCrossResume,
            int tooPoorCnt = 3,
            double railMin = double.NegativeInfinity,
            double railMax = double.PositiveInfinity)
        {
            if (halfWindowSamples < 1) throw new ArgumentOutOfRangeException(nameof(halfWindowSamples));
            if (chi2WindowSamples < 1) throw new ArgumentOutOfRangeException(nameof(chi2WindowSamples));
            if (tooPoorCnt < 1) throw new ArgumentOutOfRangeException(nameof(tooPoorCnt));

            N = halfWindowSamples;
            W = 3 * N + 1; // 2N+1 for the window + N for look-ahead

            RailMin = railMin;
            RailMax = railMax;

            t_blankdepeg = Math.Max(0, blankDepegSamples);
            t_chi2 = chi2WindowSamples;
            useZeroCross = zeroCrossResume;
            TOOPOORCNT = tooPoorCnt;

            y_threshold = Math.Max(1e-12, thresholdForGate);
            my_thresh = 3.92 * t_chi2 * y_threshold * y_threshold;

            // geometric sums & constants
            T0 = 2 * N + 1;
            int t2 = 0, t4 = 0, t6 = 0;
            for (int t = -N; t <= N; t++)
            {
                int t_2 = t * t;
                t2 += t_2;
                int t_4 = t_2 * t_2;
                t4 += t_4;
                t6 += t_4 * t_2;
            }
            T2 = t2; T4 = t4; T6 = t6;

            tau_plus_1 = N + 1;
            minus_tau = -N;
            tau_plus_1_sq = tau_plus_1 * tau_plus_1;
            minus_tau_sq = minus_tau * minus_tau;
            tau_plus_1_cu = tau_plus_1_sq * tau_plus_1;
            minus_tau_cu = minus_tau_sq * minus_tau;

            // ring
            ring = new double[W];
            rStart = 0;
            oldestIdx = 0;
            rCount = 0;
            latestIdx = -1;

            // initial state
            state = State.PEGGED;
            t_stream = -1;
        }


        // -------------- Public streaming API --------------
        /// <summary>
        /// Push one new sample (absolute index increases by 1 each call).
        /// Emits one output when the first full window is available.
        /// </summary>
        public bool Step(double sample, out double yOut)
        {
            // Warm-up: Fill the ring buffer until we have a full window (3N+1)
            if (rCount < W)
            {
                ring[(rStart + rCount) % W] = sample;
                latestIdx++;
                rCount++;

                // Once the buffer is full, align t_stream to middle of window and start in PEGGED state until we have enough data to fit the model.
                if (rCount == W)
                {
                    t_stream = oldestIdx + N;
                    t0 = t_stream + N;
                }

                yOut = 0.0;
                return false;
            }

            // Synchronization: Handle external peg requests (e.g., hardware sync)
            if (_pegAtNextEmission)
            {
                t0 = latestIdx - N;
                RecalcX3AtCenter(t0, out X3);
                RecalcX012AtCenter(t0, out X0, out X1, out X2);
                CalcAlpha0123();
                state = State.PEGGING;
                //if (_id == 1) { Logger.LogInfo($"[Ch {_id}] PEGGING @ t_stream={t_stream}, t0={t0}"); }
                _pegAtNextEmission = false;
                okSumsValid = false;
            }

            long t_limit = latestIdx - N;
            if (t_stream > t_limit) { yOut = 0.0; return false; }

            // Dispatch to state logic
            switch (state)
            {
                case State.OK: goto Label_OK;
                case State.PEGGING: goto Label_PEGGING;
                case State.PEGGED: goto Label_PEGGED;
                case State.FORCEPEG: goto Label_FORCEPEG;
                case State.TOOPOOR: goto Label_TOOPOOR;
                case State.BLANKDEPEG: goto Label_BLANKDEPEG;
                case State.DEPEGGING: goto Label_DEPEGGING;
                default: goto Error_State;
            }

        // --- State Logic ---

        Label_OK:
            if (!okSumsValid) {
                t0 = t_stream; // Ensure the center is correctly aligned to the current emission index
                RecalcX012AtCenter(t_stream, out X0, out X1, out X2); 
                okSumsValid = true;
            }


            yOut = Get(t_stream) - CalcAlpha0FromX0X2();

            // Look-ahead: If the incoming sample is pegged, transition immediately
            if (IsPegged(sample))
            {
                t0 = latestIdx - N; // Set fit center to the peg onset
            RecalcX3AtCenter(t0, out X3);
                RecalcX012AtCenter(t0, out X0, out X1, out X2);
                CalcAlpha0123();
                state = State.PEGGING;
                okSumsValid = false;
                goto Label_PEGGING;
            }

            t_stream++;
            goto Post_Process;

        Label_PEGGED:
            yOut = 0.0;

            // If we are still pegged at the next sample, stay pegged (handles long pegs without look-ahead)
            if (IsPegged(Get(t_stream)) || t_stream < t0)
            {
                t_stream++; goto Post_Process;
            }

            // Transition to TOOPOOR: Start fitting the recovery curve
            t0 = t_stream + N;
            //if (_id == 1) { Logger.LogInfo($"[Ch {_id}] PEGGED -> TOOPOOR. First unpegged at t_stream={t_stream}. new t0={t0}, latestIdx={latestIdx}"); }
            RecalcX012AtCenter(t0, out X0, out X1, out X2);
            RecalcX3AtCenter(t0, out X3);
            CalcAlpha0123();
            toopoorcnt = TOOPOORCNT;
            state = State.TOOPOOR;
            t_stream++; t0++;
            goto Post_Process;

        Label_PEGGING:
            // If t_stream has reached the fit center plus latency, we are fully inside the peg.
            // This is the only instance where fit center t0 is allowed to be behind the stream index t_stream.
            if (t_stream > t0 + N)
            {
                //if (_id == 1) { Logger.LogInfo($"[Ch {_id}] PEGGING -> PEGGED. t_stream={t_stream} reached first pegged sample at t0+N+1={t0 + N + 1}"); }
                state = State.PEGGED;
                goto Label_PEGGED; // Transition immediately to start blanking
            }

            // Calculate the cubic model value at the current offset from the center
            int dt_pegging = (int)(t_stream - t0);
            double model_val = EvalCubic(dt_pegging);

            // Subtract model from raw data
            yOut = Get(t_stream) - model_val;

            t_stream++;
            goto Post_Process;

        Label_FORCEPEG:
            // If we haven't reached the new artifact center t0, keep blanking
            if (t_stream < t0)
            {
                yOut = 0.0;
                t_stream++;
                goto Post_Process;
            }

            // Once we reach t0, we transition to standard PEGGED behavior
            state = State.PEGGED;
            goto Label_PEGGED;

        Label_TOOPOOR:

            RecalcX012AtCenter(t0, out X0, out X1, out X2); // for numerical stability problem!
            RecalcX3AtCenter(t0, out X3);
            CalcAlpha0123();

            // asymmetry gate over t_chi2 samples (model - data)
            double asym = 0.0, sig = 0.0;
            int gateCount = Math.Min(t_chi2, (int)(latestIdx - t_stream + 1));
            for (int i = 0; i < gateCount; i++)
            {
                long ti = t_stream + i;
                int dt = (int)(ti - t0);
                double f = EvalCubic(dt);
                double dy = f - GetSafe(ti);
                asym += dy;
                sig += dy * dy;
            }
            double asym2 = asym * asym;

            if (asym2 < my_thresh) toopoorcnt--;
            else toopoorcnt = TOOPOORCNT;

            if (toopoorcnt <= 0 && asym2 < my_thresh / 3.92)
            {
                if (useZeroCross)
                {
                    int dt = (int)(t_stream - t0);
                    double f = EvalCubic(dt);
                    negv = GetSafe(t_stream) < f;
                }

                RecalcX012AtCenter(t0, out X0, out X1, out X2); // for numerical stability problem!
                RecalcX3AtCenter(t0, out X3);

                //if (_id == 1) { Logger.LogInfo($"[Ch {_id}] TOOPOOR -> BLANKDEPEG. t_stream={t_stream}, t0={t0}"); }
                state = State.BLANKDEPEG;
                goto Label_BLANKDEPEG;
            }

            yOut = 0.0;
            t_stream++; t0++;
            Update_X0123(ring[(rStart + 2 * N + 1) % W], ring[rStart]); // Shift the cubic fit window

            goto Post_Process;

        Label_DEPEGGING:
            if (t_stream == t0 + N) 
            {
               // if (_id == 1) { Logger.LogInfo($"[Ch {_id}] DEPEGGING -> OK. t_stream={t_stream} reached t0={t0}. Changing t0 to {latestIdx - N}."); }
                t0 = t_stream;
                RecalcX012AtCenter(t0, out X0, out X1, out X2);
                RecalcX3AtCenter(t0, out X3);
                CalcAlpha0123();
                state = State.OK;
                goto Label_OK; 
            }
            yOut = Get(t_stream) - EvalCubic((int)(t_stream - t0));
            t_stream++;
            goto Post_Process;

        Label_BLANKDEPEG:
            // Condition A: Have we waited long enough?
            bool readyByTime = t_stream >= (t0 - N + t_blankdepeg);
            bool readyByZeroCross = false;

            // Condition B: Has the signal crossed the model line? (Polarity flip)
            if (useZeroCross && !readyByTime)
            {
                int dt_blank = (int)(t_stream - t0);
                double diff = Get(t_stream) - EvalCubic(dt_blank);

                // If the sign of the difference is different from the 'negv' captured in TOOPOOR
                if ((diff < 0) != negv)
                {
                    readyByZeroCross = true;
                    yOut = diff; // Output the value at the zero-crossing for a smoother transition
                    t_stream++;
                }
            }

            if (readyByTime || readyByZeroCross)
            {
                //if (_id == 1) { Logger.LogInfo($"[Ch {_id}] BLANKDEPEG -> DEPEGGING. t_stream={t_stream}, t0={t0}. readyByTime={readyByTime}, readyByZeroCross={readyByZeroCross}"); }
                state = State.DEPEGGING;
                goto Label_DEPEGGING; // Transition to start emitting cleaned data
            }

            // While waiting, output is still blanked
            yOut = 0.0;
            t_stream++;
            goto Post_Process;

        // --- Finalization ---

        Post_Process:
            // Only update OK moments if we are currently IN the OK state.
            // If we just transitioned to PEGGING, we must NOT slide the OK moments 
            // because t_stream is no longer the valid center for alpha0.
            if (state == State.OK)
            {

                Update_X012(ring[(rStart + 2 * N + 1) % W], ring[rStart]);
            }
            else
            {
                // Force a recompute the next time we enter OK to prevent drift
                okSumsValid = false;
            }

            // Push sample into ring and advance pointers
            ring[rStart] = sample;
            rStart = (rStart + 1) % W;
            oldestIdx++;
            latestIdx++;
            return true;

        Error_State:
            yOut = 0.0;
            return false;
        }

        // -------------- Math helpers: moments & models --------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double EvalCubic(int dt) => ((a3 * dt + a2) * dt + a1) * dt + a0;

        private void CalcAlpha0123()
        {
            // even block
            double den02 = (double)T0 * T4 - (double)T2 * T2;
            a0 = (T4 * X0 - T2 * X2) / den02;
            a2 = ((double)T0 * X2 - (double)T2 * X0) / den02;
            // odd block
            double den13 = (double)T2 * T6 - (double)T4 * T4;
            a1 = (T6 * X1 - T4 * X3) / den13;
            a3 = (T2 * X3 - T4 * X1) / den13;
        }

        private double CalcAlpha0FromX0X2()
        {
            double den02 = (double)T0 * T4 - (double)T2 * T2;
            return (T4 * X0 - T2 * X2) / den02;
        }

        // full recomputation of X0..X2 around center c
        private void RecalcX012AtCenter(long center, out double x0, out double x1, out double x2)
        {

            double s0 = 0, s1 = 0, s2 = 0;
            for (int t = -N; t <= N; t++)
            {
                double v = GetSafe(center + t);
                s0 += v;
                s1 += t * v;
                s2 += (double)t * t * v;
            }
            X0 = x0 = s0; X1 = x1 = s1; X2 = x2 = s2;
        }

        private void RecalcX3AtCenter(long center, out double x3)
        {
            double s3 = 0;
            for (int t = -N; t <= N; t++)
            {
                double v = GetSafe(center + t);
                s3 += (double)t * t * t * v;
            }
            X3 = x3 = s3;
        }

        // O(1) update for OK (center shifts by +1); uses the incoming 'y_new' and the leaving 'y_old'
        private void Update_X012(double y_new, double y_old)
        {
            X0 += y_new - y_old;                                  // X0'
            X1 += tau_plus_1 * y_new - minus_tau * y_old - X0;    // uses X0'
            X2 += tau_plus_1_sq * y_new - minus_tau_sq * y_old - X0 - 2.0 * X1; // uses X0', X1'
        }

        // O(1) update for cubic phases when t0 shifts by +1
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update_X0123(double y_new, double y_old)
        {
            X0 += y_new - y_old;
            X1 += tau_plus_1 * y_new - minus_tau * y_old - X0;
            X2 += tau_plus_1_sq * y_new - minus_tau_sq * y_old - X0 - 2.0 * X1;
            X3 += tau_plus_1_cu * y_new - minus_tau_cu * y_old - X0 - 3.0 * X1 - 3.0 * X2;
        }

        // -------------- Peg detection helpers --------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsPegged(double v) =>
            (!double.IsInfinity(RailMin) && v <= RailMin) ||
            (!double.IsInfinity(RailMax) && v >= RailMax);

        private int FindNextPegWithin(long from, int span)
        {
            long last = Math.Min(from + span, latestIdx);
            for (long i = from + 1; i <= last; i++)
                if (IsPegged(GetSafe(i)))
                    return (int)(i - from);
            return 0;
        }

        /// <summary>
        /// Request the state machine to enter PEGGED at the *next* emission index
        /// (i.e., at the constant-latency center timestamp).
        /// Call this on a sync rising edge.
        /// </summary>
        public void TriggerPegAtCurrentEmission() => _pegAtNextEmission = true;

        // -------------- Ring access --------------

        private double Get(long absIdx)
        {
            // must be within the ring
            if (absIdx < oldestIdx || absIdx > latestIdx)
                throw new InvalidOperationException("Access outside ring.");
            int offset = (int)(absIdx - oldestIdx);
            return ring[(rStart + offset) % W];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double GetSafe(long absIdx)
        {
            // clamp to available range (mirrors C code assumptions during early steps)
            if (absIdx < oldestIdx) absIdx = oldestIdx;
            if (absIdx > latestIdx) absIdx = latestIdx;
            int offset = (int)(absIdx - oldestIdx);
            return ring[(rStart + offset) % W];
        }

        /// <summary>
        /// Returns the raw (unfiltered) sample currently at the emission point.
        /// This corresponds to the sample from exactly N samples ago.
        /// </summary>
        public double GetRawAtLatency()
        {
            // t_stream is the absolute index of the sample we are currently emitting.
            // Because SALPA is a centered-window filter, t_stream is always 
            // exactly N samples behind the latest incoming data point.
            return GetSafe(t_stream);
        }
    }
}
