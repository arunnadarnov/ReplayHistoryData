using System;
using System.Collections.Generic;
using System.IO;
using log4net;

namespace ReplayHistoryData
{
    internal class ConfigValidator
    {
        private readonly List<string> _errorMessages;
        private readonly string _eventGeneratorConfigFilePath;
        private readonly string _sdkReadIntervalInHours;
        //private readonly string _logFolderPath;
        private readonly ILog _logger;

        public ConfigValidator(string eventGeneratorConfigFilePath, string sdkReadIntervalInHours, ILog logger)
        {
            _eventGeneratorConfigFilePath = eventGeneratorConfigFilePath;
            _sdkReadIntervalInHours = sdkReadIntervalInHours;
            //_logFolderPath = logFolderPath;
            _errorMessages = new List<string>();
            _logger = logger;
        }

        public void ValidateConfig()
        {
            // Validate config
            CheckEventGeneratorConfigFile();
            CheckSdkReadInterval();
            //CheckLogFolder();

            // Check if there were any errors
            if (_errorMessages.Count > 0)
            {
                // Set the console text color to red
                Console.ForegroundColor = ConsoleColor.Red;

                foreach (string errorMessage in _errorMessages)
                {
                    Console.WriteLine($"Error: {errorMessage}");
                }

                // Reset the console color to its default value
                Console.ResetColor();

                Environment.Exit(1);
            }
        }
        private void CheckEventGeneratorConfigFile()
        {
            if (string.IsNullOrEmpty(_eventGeneratorConfigFilePath))
            {
                AddError("Event Generator Config file location not specified in config file.");
            }
            else if (!File.Exists(_eventGeneratorConfigFilePath))
            {
                AddError($"Event Generator Config file '{_eventGeneratorConfigFilePath}' does not exist.");
            }
            else if (new FileInfo(_eventGeneratorConfigFilePath).Length == 0)
            {
                AddError($"Event Generator Config file '{_eventGeneratorConfigFilePath}' is empty.");
            }
        }

        private void CheckSdkReadInterval()
        {
            if (!float.TryParse(_sdkReadIntervalInHours, out float sdkReadInterval))
            {
                AddError("sdkReadInterval value is not a valid floating-point number.");

            }

            // Check if SdkReadInterval is valid
            if (sdkReadInterval <= 0)
            {
                AddError("Data retrieval hours must be a positive integer.");
            }
        }

        /*private void CheckLogFolder()
        {
            if (string.IsNullOrEmpty(_logFolderPath))
            {
                AddError("Log folder path not specified in config file.");
            }
            else if (!Path.IsPathRooted(_logFolderPath))
            {
                AddError("Log Folder must be an absolute path.");
            }
            else
            {
                // Check if logFolderPath exists or not
                if (!Directory.Exists(_logFolderPath))
                {
                    try
                    {
                        // Get the complete path for the logFolderPath
                        string outputPath = Path.GetFullPath(_logFolderPath);

                        // Create the directory using the complete path
                        Directory.CreateDirectory(_logFolderPath);
                    }
                    catch (Exception ex)
                    {
                        AddError("Unable to create Log Folder. Exception: " + ex.Message);
                    }
                }
            }
        }*/

        private void AddError(string message)
        {
            _errorMessages.Add(message);
            _logger.Error(message);
        }
    }
}
