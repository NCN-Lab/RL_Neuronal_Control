using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TorchSharp.torch.optim.lr_scheduler;
using static TorchSharp.torch.optim;
using static TorchSharp.torch;
using TorchSharp.Modules;
using TorchSharp;
using System.Text.Json;
using System.Diagnostics;
using Tensorboard;
using System.Security.Policy;
using static TorchSharp.torch.optim.lr_scheduler.impl;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using System.Reflection.Emit;

namespace RL
{
    public class Actor : nn.Module<Tensor, Tensor>
    {
        private readonly Sequential hidden;
        private readonly Linear output;
        private readonly Softmax softmax;

        public Tensor maskLogits;

        public Tensor logProbs { get; set; }
        public List<double> loss { get; set; }

        public Actor(int inputSize, IReadOnlyList<int> hiddenSizes, int outputSize) : base("Actor")
        {
            var layers = new List<nn.Module>();
            var inSize = inputSize;
            foreach (int h in hiddenSizes)
            {
                layers.Add(nn.Linear(inSize, h));
                layers.Add(nn.Mish());
                inSize = h;
            }

            /* --- give names *and* cast to Module<Tensor,Tensor> --- */
            var named = layers
                .Select((m, i) => (
                    $"{i}",
                    (nn.Module<Tensor, Tensor>)m))          
                .ToArray();


            hidden = nn.Sequential(named);
            output = nn.Linear(inSize, outputSize);
            softmax = nn.Softmax(-1);

            RegisterComponents();             
            foreach (var module in modules())
                InitWeights(module);

            loss = new List<double>();
        }

        /// <summary>Forward pass returning action probabilities.</summary>
        public override Tensor forward(Tensor input)
        {
            var x = hidden.forward(input);
            var logits = output.forward(x);
            logits = logits.masked_fill(maskLogits.eq(0), float.NegativeInfinity);
            return softmax.forward(logits);
        }

        private static void InitWeights(nn.Module m)
        {
            if (m is Linear linear)
            {
                nn.init.xavier_normal_(linear.weight);
                nn.init.constant_(linear.bias, 0.0);
            }
        }

        public void ResetWeights()
        {
            foreach (var m in modules())
            {
                InitWeights(m);
            }
        }

    }

    public class Critic : nn.Module<Tensor, Tensor>
    {
        private readonly Sequential hidden;
        private readonly Linear output;

        public List<double> loss { get; private set; }

        public Critic(int inputSize, IReadOnlyList<int> hiddenSizes, int outputSize) : base("Critic")
        {
            var layers = new List<nn.Module>();
            var inSize = inputSize;
            foreach (int h in hiddenSizes)
            {
                layers.Add(nn.Linear(inSize, h));
                layers.Add(nn.Mish());
                inSize = h;
            }

            var named = layers
                .Select((m, i) => (
                    $"{i}",
                    (nn.Module<Tensor, Tensor>)m))          
                .ToArray();


            hidden = nn.Sequential(named);
            output = nn.Linear(inSize, outputSize);

            RegisterComponents();             
            foreach (var module in modules())
                InitWeights(module);


            loss = new List<double>();

        }

        /// <summary>Forward pass returning action probabilities.</summary>
        public override Tensor forward(Tensor input)
        {
            var x = hidden.forward(input);
            return output.forward(x);
        }

        private static void InitWeights(nn.Module m)
        {
            if (m is Linear linear)
            {
                nn.init.xavier_normal_(linear.weight);
                nn.init.constant_(linear.bias, 0.0);
            }
        }

        public void ResetWeights()
        {
            foreach (var m in modules())
            {
                InitWeights(m);
            }
        }
    }
    
    public class AgentParameters
    {
        public int inputSize { get; set; }
        public List<int> hiddenSizes { get; set; }
        public int outputSize { get; set; }
    
        public double initialLrActor { get; set; }
        public double initialLrCritic { get; set; }
    
        public double gamma { get; set; }
        public double weightEntropy { get; set; }
        public double clipEpsilon { get; set; }

        public List<int> ignoreElectrodes { get; set; }
    }


    public class PPO
    {
        public readonly Actor actor;
        public readonly Critic critic;

        public int inputSize { get; }
        //public int hiddenLayerSize { get; }
        public List<int> hiddenSizes { get; }
        public int outputSize { get; }
        public double initialLrActor { get; set; }
        public double initialLrCritic { get; set; }
        public double gamma { get; set; }
        public double weightEntropy { get; set; }
        public double clipEpsilon { get; set; }

        private Tensor maskLogits { get; set; }
        
        private Optimizer actorOptimizer;
        private Optimizer criticOptimizer;
        
        

        public PPO(AgentParameters agentParams)
        {
            
            inputSize = Convert.ToInt32(agentParams.inputSize);
            hiddenSizes = agentParams.hiddenSizes;
            outputSize = Convert.ToInt32(agentParams.outputSize);

            actor = new Actor(inputSize, hiddenSizes, outputSize);
            critic = new Critic(inputSize, hiddenSizes, outputSize);

            var ignoreElectrodes = agentParams.ignoreElectrodes;
            var mask = Enumerable.Range(0, outputSize).Select(e => !ignoreElectrodes.Contains(e)).ToArray();
            maskLogits = tensor(mask, dtype: ScalarType.Bool);
            actor.maskLogits = maskLogits;

            gamma = Convert.ToDouble(agentParams.gamma);
            weightEntropy = Convert.ToDouble(agentParams.weightEntropy);
            clipEpsilon = 0.2; // fixed clip for PPO

            initialLrActor = Convert.ToDouble(agentParams.initialLrActor);
            initialLrCritic = Convert.ToDouble(agentParams.initialLrCritic);
            
            actorOptimizer = optim.Adam(actor.parameters(), initialLrActor, eps: 1e-8);
            criticOptimizer = optim.Adam(critic.parameters(), initialLrCritic, eps: 1e-8);
        }

        public (int, double) GetElectrodeToStimulate(Tensor state, bool exploration = false)
        {
            var probs = actor.forward(state);
            var logProbs = (probs + 1e-10).log();

            var probsArray = probs.data<float>().ToArray();
            //int numCols = 10;

            //for (int i = 0; i < probsArray.Length; i += numCols)
            //{
            //    var row = probsArray.Skip(i).Take(numCols);
            //    Console.WriteLine(string.Join(", ", row.Select(x => x.ToString("F4"))));
            //}

            int stimElectrode;
            stimElectrode = exploration ? probs.multinomial(1).ToInt32() : probs.argmax().ToInt32();
            var chosenLogProb = logProbs.squeeze()[stimElectrode].ToDouble();
            return (stimElectrode, chosenLogProb);
        }
        
        public void Update(Tensor states, Tensor actions, Tensor oldLogProbs, Tensor rewards, Tensor nextStates, Tensor dones, bool optimizerStep = true, bool printInfo = false)
        {

            //Console.WriteLine("Done:");
            //Console.WriteLine(string.Join(", ", done.data<Int64>().ToArray().Select(x => x.ToString("F4"))));
            Tensor targets;
            Tensor Vcurr;

            using (var noGrad = torch.no_grad())
            {
                var allQ = critic.forward(states);
                var probsCurr = actor.forward(states); 
                Vcurr = (allQ * probsCurr).sum(1);

                if (gamma > 0)
                {
                    var allQNext = critic.forward(nextStates);
                    var probsNext = actor.forward(nextStates);
                    var Vnext = (allQNext * probsNext).sum(1);

                    targets = rewards + gamma * (1 - dones) * Vnext;
                }
                else
                {
                    targets = rewards;
                }
            }

            var advantages = targets - Vcurr;
            advantages = (advantages - advantages.mean()) / (advantages.std(unbiased: false) + 1e-8);


            // ACTOR
            // ==================================================================
            var probs = actor.forward(states);
            var dist = torch.distributions.Categorical(probs);
            var logProbs = dist.log_prob(actions);

            var ratio = (logProbs - oldLogProbs).exp();
            var surr1 = ratio * advantages;
            var surr2 = torch.clamp(ratio, 1.0 - clipEpsilon, 1.0 + clipEpsilon) * advantages;
            var policyLoss = -torch.min(surr1, surr2).mean();
            var entropy = dist.entropy().mean();

            if (optimizerStep)
            {
                actorOptimizer.zero_grad();
                (policyLoss - weightEntropy * entropy).backward();
                actorOptimizer.step();
            }

            // CRITIC
            // ==================================================================
            var allQPred = critic.forward(states);
            var QPred = allQPred.index(torch.arange(allQPred.shape[0], dtype: torch.int64), actions);

            Tensor valueLoss = (targets - QPred).pow(2).mean();

            if (optimizerStep)
            {
                criticOptimizer.zero_grad();
                valueLoss.backward();
                criticOptimizer.step();
            }

            actor.loss.Add(policyLoss.item<float>());
            critic.loss.Add(valueLoss.item<float>());

            if (printInfo)
            {
                Console.WriteLine($"Actor Loss: {policyLoss.item<float>():F6}, Critic Loss: {valueLoss.item<float>():F6}, Entropy: {entropy.item<float>():F6}");
            }
        }

        private int RandomChoice(float[] probabilities)
        {
            var cumulative = probabilities.Select((p, i) => probabilities.Take(i + 1).Sum()).ToArray();
            var rand = new Random().NextDouble();
            for (int i = 0; i < cumulative.Length; i++)
            {
                if (rand < cumulative[i]) return i;
            }
            return cumulative.Length - 1;
        }

        //public void ResetSchedulers()
        //{
        //    foreach (var paramGroup in actorOptimizer.ParamGroups)
        //    {
        //        paramGroup.LearningRate = initialLrActor;
        //    }
        //    actorOptimizer.state_dict().Clear();

        //    foreach (var paramGroup in criticOptimizer.ParamGroups)
        //    {
        //        paramGroup.LearningRate = initialLrCritic;
        //    }
        //    criticOptimizer.state_dict().Clear();
        //}

        public void Reset(AgentParameters agentParams)
        {
            // Reinitialize weights
            actor.ResetWeights();
            critic.ResetWeights();

            // Clear stored values
            actor.loss.Clear();
            critic.loss.Clear();

            var ignoreElectrodes = agentParams.ignoreElectrodes;
            var mask = Enumerable.Range(0, outputSize).Select(e => !ignoreElectrodes.Contains(e)).ToArray();
            maskLogits = tensor(mask, dtype: ScalarType.Bool);
            actor.maskLogits = maskLogits;

            gamma = Convert.ToDouble(agentParams.gamma);
            weightEntropy = Convert.ToDouble(agentParams.weightEntropy);
            initialLrActor = Convert.ToDouble(agentParams.initialLrActor);
            initialLrCritic = Convert.ToDouble(agentParams.initialLrCritic);

            actorOptimizer = optim.Adam(actor.parameters(), initialLrActor, eps: 1e-8);
            criticOptimizer = optim.Adam(critic.parameters(), initialLrCritic, eps: 1e-8);

            Console.WriteLine("PPO agent has been reset.");
        }

        private static object ConvertTensorToObject(Tensor t)
        {
            if (t.dim() == 1)
            {
                return t.data<float>().ToArray();
            }
            else if (t.dim() == 2)
            {
                int rows = (int)t.shape[0];
                int cols = (int)t.shape[1];
                float[] flatArray = t.data<float>().ToArray();
                var nested = new List<List<float>>(rows);
                for (int r = 0; r < rows; r++)
                {
                    var row = new List<float>(cols);
                    for (int c = 0; c < cols; c++)
                    {
                        row.Add(flatArray[r * cols + c]);
                    }
                    nested.Add(row);
                }
                return nested;
            }
            return t.data<float>().ToArray();
        }

        public void SaveAgent(string filename = null)
        {
            // Generate filename if not provided
            if (string.IsNullOrEmpty(filename))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                filename = $"D:/Eduardo/RL_burst_generator/trained_PPO/{timestamp}.json";
            }

            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var saveData = new
            {
                actor_state_dict = actor.state_dict().ToDictionary(kv => kv.Key, kv => ConvertTensorToObject(kv.Value)),
                critic_state_dict = critic.state_dict().ToDictionary(kv => kv.Key, kv => ConvertTensorToObject(kv.Value))
            };

            var json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions
            {
                WriteIndented = true // For readability
            });

            File.WriteAllText(filename, json);
            Console.WriteLine($"Agent saved to: {filename}");
        }

        public void LoadAgent(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException($"Checkpoint file not found: {filename}");
            }

            // Read JSON from file
            var json = File.ReadAllText(filename);

            // Handle string-encoded JSON objects if applicable
            if (json.TrimStart().StartsWith("\""))
            {
                json = JsonDocument.Parse(json).RootElement.GetString();
            }

            // Deserialize the JSON
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var checkpoint = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(json, options);

            // Load the actor state dictionary
            var actorStateDict = checkpoint["actor_state_dict"];
            var actorStateDictTensor = new Dictionary<string, Tensor>();

            foreach (var kvp in actorStateDict)
            {
                string key = kvp.Key.Replace("out", "output");
                actorStateDictTensor[key] = ConvertJsonElementToTensor(kvp.Value);
            }

            actor.load_state_dict(actorStateDictTensor, strict: true);

            // Load the critic state dictionary
            var criticStateDict = checkpoint["critic_state_dict"];
            var criticStateDictTensor = new Dictionary<string, Tensor>();

            foreach (var kvp in criticStateDict)
            {
                string key = kvp.Key.Replace("q_head", "output");
                criticStateDictTensor[key] = ConvertJsonElementToTensor(kvp.Value);
            }

            critic.load_state_dict(criticStateDictTensor, strict: true);

            Console.WriteLine($"Agent loaded from: {filename}");
        }

        private Tensor ConvertJsonElementToTensor(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                // Check if the element is a 2D array
                if (element[0].ValueKind == JsonValueKind.Array)
                {
                    // Deserialize as List<List<float>>
                    var nestedList = JsonSerializer.Deserialize<List<List<float>>>(element.GetRawText());
                    return ConvertToTensor(nestedList);
                }
                else
                {
                    // Deserialize as List<float>
                    var flatList = JsonSerializer.Deserialize<List<float>>(element.GetRawText());
                    return torch.tensor(flatList.ToArray(), dtype: torch.float32);
                }
            }

            throw new InvalidOperationException($"Unexpected JSON element type: {element.ValueKind}");
        }

        static Tensor ConvertToTensor(List<List<float>> nestedList)
        {
            var rows = nestedList.Count;
            var cols = nestedList[0].Count;

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


    }

}
