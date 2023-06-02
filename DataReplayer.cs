using log4net;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReplayHistoryData
{
    public class DataReplayer
    {
        private readonly List<TagConfig> _tagConfigs;
        private readonly string _sdkReadIntervalInHoursString;
        private readonly ConcurrentDictionary<string, PIServer> _piServers;
        private readonly ILog _log;

        public DataReplayer(List<TagConfig> tagConfigs, string sdkReadIntervalInHoursString, ConcurrentDictionary<string, PIServer> piServerDictionary, ILog log)
        {
            _tagConfigs = tagConfigs;
            //_piServers = new Dictionary<string, PIServer>();
            _piServers = piServerDictionary;
            _sdkReadIntervalInHoursString = sdkReadIntervalInHoursString;
            _log = log;
        }

        public void Start()
        {
            float sdkReadInterval = float.Parse(_sdkReadIntervalInHoursString);

            // List to store tasks
            List<Task> tasks = new List<Task>();

            // Continuously write data from multiple source tags to multiple destination tags with new timestamps based on the current time and the time differences between the original timestamps
            while (true)
            {
                DateTime currentTime = DateTime.Now;

                // Clear the List
                tasks.Clear();

                foreach (var tagConfig in _tagConfigs)
                {
                    var task = Task.Run(() =>
                    {
                        try
                        {
                            // Current time for thread
                            DateTime currentTimeForThread = currentTime;

                            // Connect to source and destination PI Data Archives
                            PIServer sourcePIServer = GetPIServer(tagConfig.Input_PIServer);
                            PIServer destinationPIServer = GetPIServer(tagConfig.Output_PIServer);

                            // Get source and destination PI Points
                            PIPoint sourcePIPoint = PIPoint.FindPIPoint(sourcePIServer, tagConfig.Input_Tag);
                            PIPoint destinationPIPoint = PIPoint.FindPIPoint(destinationPIServer, tagConfig.Output_Tag);

                            // Divide time range into hourly intervals
                            DateTime startTime = DateTime.Parse(tagConfig.Start_Time);
                            DateTime endTime = DateTime.Parse(tagConfig.End_Time);
                            TimeSpan interval = TimeSpan.FromHours(sdkReadInterval);
                            AFTime? lastTimestamp = null;
                            while (startTime < endTime)
                            {
                                DateTime intervalEndTime = startTime.Add(interval);
                                if (intervalEndTime > endTime)
                                {
                                    intervalEndTime = endTime;
                                }

                                // Get data for current interval
                                AFTimeRange timeRange = new AFTimeRange(startTime, intervalEndTime);
                                var recordedValues = sourcePIPoint.RecordedValues(timeRange, AFBoundaryType.Inside, null, false);

                                // Handle first value separately
                                if (recordedValues.Count > 0)
                                {
                                    // Check if first value in current iteration has same timestamp as last value in previous iteration
                                    if (lastTimestamp.HasValue && recordedValues[0].Timestamp == lastTimestamp.Value)
                                    {
                                        // Ignore first value in current iteration
                                    }
                                    else
                                    {
                                        if (lastTimestamp.HasValue)
                                        {
                                            TimeSpan timeDiff = recordedValues[0].Timestamp.LocalTime - lastTimestamp.Value.LocalTime;
                                            currentTimeForThread = currentTimeForThread.Add(timeDiff);
                                        }
                                        while (currentTimeForThread > DateTime.Now)
                                        {
                                            Thread.Sleep(100);
                                        }
                                        AFValue newValue = new AFValue(recordedValues[0].Value, new AFTime(currentTimeForThread));
                                        destinationPIPoint.UpdateValue(newValue, AFUpdateOption.Replace, AFBufferOption.BufferIfPossible);
                                    }

                                    // Write data to destination tag with new timestamps based on the current time and the time differences between the original timestamps
                                    for (int i = 1; i < recordedValues.Count; i++)
                                    {
                                        var val = recordedValues[i];
                                        TimeSpan timeDiff = recordedValues[i].Timestamp.LocalTime - recordedValues[i - 1].Timestamp.LocalTime;
                                        currentTimeForThread = currentTimeForThread.Add(timeDiff);
                                        while (currentTimeForThread > DateTime.Now)
                                        {
                                            Thread.Sleep(100);
                                            //Thread.Sleep((int)Math.Round(timeDiff.TotalMilliseconds) - 10);
                                        }
                                        AFValue newValue = new AFValue(val.Value, new AFTime(currentTimeForThread));
                                        destinationPIPoint.UpdateValue(newValue, AFUpdateOption.Replace, AFBufferOption.BufferIfPossible);
                                    }

                                    // Store timestamp of last value in this iteration
                                    lastTimestamp = recordedValues[recordedValues.Count - 1].Timestamp;
                                }
                                startTime = intervalEndTime;
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error("An error occurred: ", ex);
                        }
                    });
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
                Thread.Sleep(1000);
            }
        }

        private PIServer GetPIServer(string serverName)
        {
            if (!_piServers.ContainsKey(serverName))
            {
                PIServer piServer = new PIServers()[serverName];
                piServer.Connect();
                _piServers[serverName] = piServer;
            }
            return _piServers[serverName];
        }
    }
}
