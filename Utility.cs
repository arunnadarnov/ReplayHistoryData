using OSIsoft.AF.PI;
using OSIsoft.AF;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace ReplayHistoryData
{
    public static class Utility
    {

        public static bool CheckTagsHaveSameDataType(PIServer inputPIServer, string inputTagName, PIServer outputPIServer, string outputTagName)
        {
            PIPoint inputPIPoint = PIPoint.FindPIPoint(inputPIServer, inputTagName);
            PIPointType inputTagType = inputPIPoint.PointType;

            PIPoint outputPIPoint = PIPoint.FindPIPoint(outputPIServer, outputTagName);
            PIPointType outputTagType = outputPIPoint.PointType;

            return inputTagType == outputTagType;
        }

        public static PIServer FindPIServer(string serverName)
        {
            try
            {
                PISystem piSystem = new PISystems().DefaultPISystem;

                return PIServer.FindPIServer(piSystem, serverName);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to find PI server '{serverName}': {ex.Message}");

                return null;
            }
        }

        public static bool CheckPIServerName(string serverName)
        {
            if (string.IsNullOrEmpty(serverName)) return false;

            PIServer piServer = FindPIServer(serverName);

            if (piServer == null)
            {
                return false;
            }
            return true;
        }

        public static bool CheckTagExists(PIServer piServer, string tagName)
        {
            bool tagExists = PIPoint.TryFindPIPoint(piServer, tagName, out PIPoint _);

            return tagExists;
        }

        public static bool ConnectToPIServer(string serverName, ConcurrentDictionary<string, PIServer> piServerDictionary)
        {

            if (!piServerDictionary.TryGetValue(serverName, out PIServer piServer))
            {
                piServer = new PIServers()[serverName];

                try
                {
                    piServer.Connect();
                    piServerDictionary.TryAdd(serverName, piServer);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

        public static PIServer GetPIServer(string serverName, ConcurrentDictionary<string, PIServer> piServerDictionary, List<string> messages)
        {

            if (!piServerDictionary.TryGetValue(serverName, out PIServer piServer))
            {
                piServer = new PIServers()[serverName];

                try
                {
                    piServer.Connect();
                    piServerDictionary.TryAdd(serverName, piServer);
                }
                catch (Exception ex)
                {
                    messages.Add($"Could not connect to PI server '{serverName}'. {ex.Message}");

                    return null;
                }
            }

            return piServer;
        }

        // Check start time
        public static bool CheckIfTimestampIsValid(string timestampString)
        {
            if (string.IsNullOrEmpty(timestampString))
            {
                return false;
            }
            else if (!DateTime.TryParse(timestampString, out DateTime timestamp))
            {
                return false;
            }
            else if (timestamp < DateTime.MinValue || timestamp > DateTime.MaxValue)
            {
                return false;
            }

            return true;
        }

        public static bool IsEndTimeLater(string startTime, string endTime)
        {
            DateTime start = DateTime.Parse(startTime);
            DateTime end = DateTime.Parse(endTime);
            return end > start;
        }
    }
}
