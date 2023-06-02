using log4net;
using Newtonsoft.Json;
using OSIsoft.AF.PI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace ReplayHistoryData
{
    internal class Program
    {
        private static readonly ConcurrentDictionary<string, PIServer> piServerDictionary = new ConcurrentDictionary<string, PIServer>();

        static void Main(string[] args)
        {
            // Load tag configuration from JSON file
            string eventGeneratorConfigFilePath = ConfigurationManager.AppSettings["eventGeneratorConfigFilePath"];
            string sdkReadIntervalInHours = ConfigurationManager.AppSettings["sdkReadIntervalInHours"];
            //string logFolderPath = ConfigurationManager.AppSettings["logFolderPath"];

            // Set the value of the LogFolderPath property
            //log4net.GlobalContext.Properties["LogFolderPath"] = logFolderPath;

            // Configure log4net
            log4net.Config.XmlConfigurator.Configure();

            ILog logger = LogManager.GetLogger(typeof(Program));
            ILog dataReplayerLogger = LogManager.GetLogger(typeof(DataReplayer));
            ILog configValidatorLogger = LogManager.GetLogger(typeof(ConfigValidator));
            ILog errorCheckerLogger = LogManager.GetLogger(typeof(ErrorChecker));

            // Make an instance of class ConfigValidator
            ConfigValidator configValidator = new ConfigValidator(eventGeneratorConfigFilePath, sdkReadIntervalInHours, configValidatorLogger);

            // Call function ValidateConfig
            configValidator.ValidateConfig();

            // Make an instance of class LoggerUtility
            LoggerUtility loggerUtility = new LoggerUtility();

            List<TagConfig> tagConfigs = new List<TagConfig>();

            // Read new input file format
            string configJson = File.ReadAllText(eventGeneratorConfigFilePath);
            var tagConfigsFromFile = JsonConvert.DeserializeObject<List<TagConfig>>(configJson);

            // List to store error and warning messages
            List<string> errorMessages = new List<string>();
            List<string> warningMessages = new List<string>();

            // Make an instance of class ErrorChecker
            ErrorChecker errorChecker = new ErrorChecker(errorCheckerLogger, loggerUtility);

            // Convert TagMaskConfig to TagConfig
            foreach (var tagConfigFromFile in tagConfigsFromFile)
            {
                // Check if tag config is valid
                bool tagConfigValid = errorChecker.CheckTagConfig(tagConfigFromFile, piServerDictionary, errorMessages);
                if (!tagConfigValid) { continue; }

                // Connect to input and output PI servers
                PIServer inputPIServer = Utility.GetPIServer(tagConfigFromFile.Input_PIServer, piServerDictionary, errorMessages);
                PIServer outputPIServer = Utility.GetPIServer(tagConfigFromFile.Output_PIServer, piServerDictionary, errorMessages);

                if ((tagConfigFromFile.Input_Tag.Contains("*") && tagConfigFromFile.Output_Tag.Contains("*")) || (tagConfigFromFile.Input_Tag.Contains("?") && tagConfigFromFile.Output_Tag.Contains("?")))
                {
                    // Use tag masks to create TagConfig objects
                    List<string> inputTags = GetTagsFromTagMask(tagConfigFromFile.Input_Tag, tagConfigFromFile.Input_PIServer);
                    List<string> outputTags = GetTagsFromTagMask(tagConfigFromFile.Output_Tag, tagConfigFromFile.Output_PIServer);

                    bool tagsCountValid = errorChecker.CheckTagCount(tagConfigFromFile, inputTags, outputTags, errorMessages);
                    if (!tagsCountValid) { continue; }

                    // Match input and output tags based on their names
                    foreach (var inputTag in inputTags)
                    {
                        string outputTag = outputTags.Find(t => t.EndsWith(inputTag));
                        if (outputTag != null)
                        {
                            TagConfig tagConfig = new TagConfig
                            {
                                Start_Time = tagConfigFromFile.Start_Time,
                                End_Time = tagConfigFromFile.End_Time,
                                Input_Tag = inputTag,
                                Input_PIServer = tagConfigFromFile.Input_PIServer,
                                Output_Tag = outputTag,
                                Output_PIServer = tagConfigFromFile.Output_PIServer
                            };

                            // Check if input and output tags have the same data type
                            if (!Utility.CheckTagsHaveSameDataType(inputPIServer, tagConfig.Input_Tag, outputPIServer, tagConfig.Output_Tag))
                            {
                                // Handle tags with different data types
                                string message = $"Input tag '{tagConfig.Input_Tag}' and output tag '{tagConfig.Output_Tag}' have different data types.";
                                loggerUtility.AddError(errorMessages, logger, message);
                                continue;
                            }


                            // Add TagConfig object to list
                            tagConfigs.Add(tagConfig);
                        }
                        else
                        {
                            string message = $"Could not find a matching destination tag for source tag '{inputTag}'. This source tag will be ignored.";
                            loggerUtility.AddWarning(warningMessages, logger, message);
                        }
                    }

                    foreach (var outputTag in outputTags)
                    {
                        string inputTag = inputTags.Find(t => outputTag.EndsWith(t));
                        if (inputTag == null)
                        {
                            string message = $"Could not find a matching source tag for destination tag '{outputTag}'. This destination tag will be ignored.";
                            loggerUtility.AddWarning(warningMessages, logger, message);
                        }
                    }
                }
                else
                {
                    // Use tag names to create a single TagConfig object
                    TagConfig tagConfig = new TagConfig
                    {
                        Job_ID = tagConfigFromFile.Job_ID,
                        Start_Time = tagConfigFromFile.Start_Time,
                        End_Time = tagConfigFromFile.End_Time,
                        Input_Tag = tagConfigFromFile.Input_Tag,
                        Input_PIServer = tagConfigFromFile.Input_PIServer,
                        Output_Tag = tagConfigFromFile.Output_Tag,
                        Output_PIServer = tagConfigFromFile.Output_PIServer
                    };

                    // Check if input and output tags exist
                    bool inputTagExists = Utility.CheckTagExists(inputPIServer, tagConfig.Input_Tag);
                    if (!inputTagExists)
                    {
                        string message = $"Input Tag {tagConfig.Input_Tag} does not exist.";
                        loggerUtility.AddError(errorMessages, logger, message);
                    }

                    bool outputTagExists = Utility.CheckTagExists(outputPIServer, tagConfig.Output_Tag);
                    if (!outputTagExists)
                    {
                        string message = $"Output Tag {tagConfig.Output_Tag} does not exist.";
                        loggerUtility.AddError(errorMessages, logger, message);
                    }

                    bool tagsHaveSameDataType = Utility.CheckTagsHaveSameDataType(inputPIServer, tagConfigFromFile.Input_Tag, outputPIServer, tagConfigFromFile.Output_Tag);
                    if (!tagsHaveSameDataType)
                    {
                        string message = $"Input tag '{tagConfig.Input_Tag}' and output tag '{tagConfig.Output_Tag}' have different data types.";
                        loggerUtility.AddError(errorMessages, logger, message);
                    }

                    // Validate TagConfig object
                    if ((!inputTagExists) || (!outputTagExists) || (!tagsHaveSameDataType))
                        continue;

                    // Add TagConfig object to list
                    tagConfigs.Add(tagConfig);
                }
            }

            if (errorMessages.Count > 0)
            {
                // Set the console text color to red
                Console.ForegroundColor = ConsoleColor.Red;

                foreach (string errorMessage in errorMessages)
                {
                    Console.WriteLine($"Error: {errorMessage}");
                }

                // Reset the console color to its default value
                Console.ResetColor();

                Environment.Exit(1);
            }

            if (warningMessages.Count > 0)
            {
                // Set the console text color to red
                Console.ForegroundColor = ConsoleColor.Yellow;

                foreach (string warningMessage in warningMessages)
                {
                    Console.WriteLine($"Warning: {warningMessage}");
                }

                // Reset the console color to its default value
                Console.ResetColor();
            }

            if (tagConfigs.Count > 0)
            {
                // Create data replayer and start replaying data
                DataReplayer dataReplayer = new DataReplayer(tagConfigs, sdkReadIntervalInHours, piServerDictionary, dataReplayerLogger);
                dataReplayer.Start();
            }
        }

        private static List<string> GetTagsFromTagMask(string tagMask, string piServerName)
        {
            // Connect to PI Data Archive
            PIServer piServer = new PIServers()[piServerName];
            piServer.Connect();

            // Search for PI Points using tag mask
            PIPointList points = new PIPointList(PIPoint.FindPIPoints(piServer, tagMask));

            // Get tag names from PI Points
            List<string> tags = new List<string>();
            foreach (var point in points)
            {
                //Console.WriteLine($"Tag Name: {point.Name}");
                tags.Add(point.Name);
            }

            return tags;
        }
    }
}
