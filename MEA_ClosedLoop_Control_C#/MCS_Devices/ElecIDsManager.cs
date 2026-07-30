using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MCS_Devices
{
    /// <summary>
    /// Class with functions to deal with electrode IDs, inds and labels
    /// </summary>
    /// 
    public class ElecIDsManager
    {
        private int nElecs;
        private int nChannels;
        private Dictionary<int, int> indsOfIDs; // Map from ID to index
        private Dictionary<int, int> IDsOfInds; // Map from index to ID
        private Dictionary<string, labelStruct> labelMap; // Map from label to labelStruct
        public List<labelStruct> elecLabels { get; private set; } // Public access to electrode labels


        /// <summary>
        /// Constructor. 
        /// IMPORTANT: this is where the paths for the files with electrode labels are defined!
        /// </summary>
        /// <param name="deviceType">MeaLayoutEnum specifying the type of MEA chip used</param>
        public ElecIDsManager(MeaLayoutEnum deviceType)
        {
            string pathWellLabels = ConfigureDevice(deviceType);

            if (nElecs > 0)
            {
                elecLabels = OrganizeLabels(pathWellLabels);
                InitializeMappings();
            }
        }

        /// <summary>
        /// Configures device-specific parameters.
        /// </summary>
        private string ConfigureDevice(MeaLayoutEnum deviceType)
        {
            string pathWellLabels;
            switch (deviceType)
            {
                case MeaLayoutEnum.MEA256_1well:
                case MeaLayoutEnum.MEA256_6well:
                    nElecs = 252;
                    nChannels = 256;
                    pathWellLabels = GetSolutionDir() + @"\ElectrodeFiles\252_1well_electrode_labels.txt";
                    break;

                case MeaLayoutEnum.MEA256_9well:
                    nElecs = 234;
                    nChannels = 256;
                    pathWellLabels = GetSolutionDir() + @"\ElectrodeFiles\252_9well_electrode_labels.txt";
                    break;

                case MeaLayoutEnum.MEA60_1well:
                    nElecs = 60;
                    nChannels = 60;
                    pathWellLabels = GetSolutionDir() + @"\ElectrodeFiles\60_1_well_electrode_labels.txt";
                    break;

                case MeaLayoutEnum.MEA60_6well:
                    nElecs = 54;
                    nChannels = 60;
                    pathWellLabels = GetSolutionDir() + @"\ElectrodeFiles\60_6_well_electrode_labels.txt";
                    break;

                default:
                    nElecs = 0;
                    nChannels = 0;
                    pathWellLabels = null;
                    break;
            }
            return pathWellLabels;
        }

        /// <summary>
        /// Reads the electrode label file and organizes label structures.
        /// </summary>
        private List<labelStruct> OrganizeLabels(string path)
        {
            var labelList = new List<labelStruct>();
            string[] lines = File.ReadAllLines(path);

            int id = 0; // Skip grounds
            foreach (string rawLine in lines)
            {
                if (rawLine.Length < 6) // valid elec labels have up to 5 chars, ex: 'A13'
                {
                    string line = rawLine.Substring(1, rawLine.Length - 2);
                    var labelStruct = new labelStruct
                    {
                        label = line,
                        letter = line[0],
                        number = int.Parse(line.Substring(1)),
                        id = id
                    };
                    labelList.Add(labelStruct);
                }
                id++;
            }

            // Sort by label
            return labelList.OrderBy(l => l.letter).ThenBy(l => l.number).ToList();
        }

        /// <summary>
        /// Initializes mappings for efficient lookups.
        /// </summary>
        private void InitializeMappings()
        {
            indsOfIDs = new Dictionary<int, int>();
            IDsOfInds = new Dictionary<int, int>();
            labelMap = new Dictionary<string, labelStruct>();

            for (int i = 0; i < elecLabels.Count; i++)
            {
                var label = elecLabels[i];
                label.label_ind = i; // Assign index
                indsOfIDs[label.id] = i;
                IDsOfInds[i] = label.id;
                labelMap[label.label] = label;
            }
        }

        /// <summary>
        /// Retrieves the number of electrodes.
        /// </summary>
        /// <returns>The total number of electrodes.</returns>
        public int GetNumberOfElectrodes()
        {
            return nElecs;
        }

        /// <summary>
        /// Retrieves the number of channels.
        /// </summary>
        /// <returns>The total number of channels.</returns>
        public int GetNumberOfChannels()
        {
            return nChannels;
        }

        /// <summary>
        /// Retrieves the list of all electrode label structures.
        /// </summary>
        /// <returns>A list of labelStruct containing all electrode label details.</returns>
        public List<labelStruct> GetAllElectrodeLabels()
        {
            return elecLabels;
        }

        /// <summary>
        /// Retrieves the list of all electrode labels.
        /// </summary>
        /// <returns>A list of strings containing all electrode labels.</returns>
        public List<string> GetAllLabels()
        {
            // Return all the keys (labels) in the labelMap dictionary
            return labelMap.Keys.ToList();
        }

        /// <summary>
        /// Retrieves the list of all electrode indices.
        /// </summary>
        /// <returns>A list containing all electrode indices.</returns>
        public List<int> GetAllIndices()
        {
            return IDsOfInds.Keys.ToList();
        }

        /// <summary>
        /// Retrieves the list of all electrode IDs.
        /// </summary>
        /// <returns>A list containing all electrode IDs.</returns>
        public List<int> GetAllIDs()
        {
            return indsOfIDs.Keys.ToList();
        }

        /// <summary>
        /// Gets the index based on the label.
        /// </summary>
        public int GetIndexFromLabel(string label) =>
            labelMap.TryGetValue(label, out var labelStruct) ? labelStruct.label_ind : -1;

        /// <summary>
        /// Gets the ID based on the label.
        /// </summary>
        public int GetIDFromLabel(string label) =>
            labelMap.TryGetValue(label, out var labelStruct) ? labelStruct.id : -1;

        /// <summary>
        /// Gets the label based on the index.
        /// </summary>
        public string GetLabelFromIndex(int index) =>
            index >= 0 && index < elecLabels.Count ? elecLabels[index].label : "";

        /// <summary>
        /// Gets the label based on the ID.
        /// </summary>
        public string GetLabelFromID(int id) =>
            indsOfIDs.TryGetValue(id, out var index) ? GetLabelFromIndex(index) : "";

        /// <summary>
        /// Gets the ID based on the index.
        /// </summary>
        public int GetIDFromIndex(int index) =>
            index >= 0 && index < IDsOfInds.Count ? IDsOfInds[index] : -1;

        /// <summary>
        /// Gets the index based on the ID.
        /// </summary>
        public int GetIndexFromID(int id) =>
            indsOfIDs.TryGetValue(id, out var index) ? index : -1;

        /// <summary>
        /// Struct with the electrode labels and IDs.
        /// </summary>
        public struct labelStruct
        {
            public string label;
            public char letter;
            public int number;
            public int id;
            public int label_ind;
        }

        private static string GetSolutionDir(string currentPath = null)
        {
            var directory = new DirectoryInfo(
                currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory?.FullName ?? "";
        }


    }
}
