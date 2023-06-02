using log4net;
using OSIsoft.AF.PI;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ReplayHistoryData
{
    public class ErrorChecker
    {
        private readonly ILog logger;
        private readonly LoggerUtility loggerUtility;

        public ErrorChecker(ILog logger, LoggerUtility loggerUtility)
        {
            this.logger = logger;
            this.loggerUtility = loggerUtility;
        }

        public bool CheckTagConfig(TagConfig tagConfig, ConcurrentDictionary<string, PIServer> piServerDictionary, List<string> errorMessages)
        {
            // Check if start time and end time are valid
            bool startTimeValid = Utility.CheckIfTimestampIsValid(tagConfig.Start_Time);
            if (!startTimeValid)
            {
                string message = $"Start Time '{tagConfig.Start_Time}' is not a valid start time in Job {tagConfig.Job_ID}.";
                loggerUtility.AddError(errorMessages, logger, message);
            }

            bool endTimeValid = Utility.CheckIfTimestampIsValid(tagConfig.End_Time);
            if (!endTimeValid)
            {
                string message = $"End Time '{tagConfig.End_Time}' is not a valid End time in Job {tagConfig.Job_ID}.";
                loggerUtility.AddError(errorMessages, logger, message);
            }

            if (!startTimeValid || !endTimeValid) { return false; }

            bool endTimeGreaterThanStartTime = Utility.IsEndTimeLater(tagConfig.Start_Time, tagConfig.End_Time);
            if (!endTimeGreaterThanStartTime)
            {
                string message = $"The specified end time {tagConfig.End_Time} is earlier than the start time {tagConfig.Start_Time}. Please enter a valid end time that is later than the start time.";
                loggerUtility.AddError(errorMessages, logger, message);
                return false;
            }

            // Check if Input Tag and Output Tag is valid
            bool inputTagEmpty = string.IsNullOrEmpty(tagConfig.Input_Tag);
            if (inputTagEmpty)
            {
                string message = $"Input_Tag is empty in Job ID {tagConfig.Job_ID}. Please check.";
                loggerUtility.AddError(errorMessages, logger, message);
            }

            bool outputTagEmpty = string.IsNullOrEmpty(tagConfig.Output_Tag);
            if (outputTagEmpty)
            {
                string message = $"Output_Tag is empty in Job ID {tagConfig.Job_ID}. Please check.";
                loggerUtility.AddError(errorMessages, logger, message);
            }

            if (inputTagEmpty || outputTagEmpty) { return false; }

            // Check if Input PI Server Name and Output PI Server name is empty
            bool inputPIServerNameEmpty = string.IsNullOrEmpty(tagConfig.Input_PIServer);
            if (inputPIServerNameEmpty)
            {
                string message = ($"Input PI server is not specified in Job ID {tagConfig.Job_ID}.");
                loggerUtility.AddError(errorMessages, logger, message);
            }

            bool outputPIServerNameEmpty = string.IsNullOrEmpty(tagConfig.Output_PIServer);
            if (outputPIServerNameEmpty)
            {
                string message = ($"Output PI server name is not specified in Job ID {tagConfig.Job_ID}.");
                loggerUtility.AddError(errorMessages, logger, message);
            }

            if (inputPIServerNameEmpty || outputPIServerNameEmpty) { return false; }

            // Check if input and output PI server names are valid
            if (!Utility.CheckPIServerName(tagConfig.Input_PIServer))
            {
                string message = ($"PI server '{tagConfig.Input_PIServer}' does not exist.");
                loggerUtility.AddError(errorMessages, logger, message);

            }

            if (!Utility.CheckPIServerName(tagConfig.Output_PIServer))
            {
                string message = ($"PI server '{tagConfig.Output_PIServer}' does not exist.");
                loggerUtility.AddError(errorMessages, logger, message);
            }

            if ((!Utility.CheckPIServerName(tagConfig.Input_PIServer)) || (!Utility.CheckPIServerName(tagConfig.Output_PIServer)))
            {
                return false;
            }

            // Check if able to connect to input and output PI servers
            bool connectToInputPIServerSuccessful = Utility.ConnectToPIServer(tagConfig.Input_PIServer, piServerDictionary);
            if (!connectToInputPIServerSuccessful)
            {
                string message = $"Could not connect to PI server '{tagConfig.Input_PIServer}'.";
                loggerUtility.AddError(errorMessages, logger, message);
            }
            else
            {
                logger.Info($"Successfully connected to Input PI Server {tagConfig.Input_PIServer}");
            }

            bool connectToOutputPIServerSuccessful = Utility.ConnectToPIServer(tagConfig.Output_PIServer, piServerDictionary);
            if (!connectToOutputPIServerSuccessful)
            {
                string message = $"Could not connect to PI server '{tagConfig.Output_PIServer}'.";
                loggerUtility.AddError(errorMessages, logger, message);
            }
            else
            {
                logger.Info($"Successfully connected to Output PI Server {tagConfig.Output_PIServer}");
            }

            if ((!connectToInputPIServerSuccessful) || (!connectToOutputPIServerSuccessful))
                return false;

            return true;
        }

        public bool CheckTagCount(TagConfig tagConfig, List<string> inputTags, List<string> outputTags, List<string> errorMessages)
        {
            if (inputTags.Count == 0)
            {
                string message = ($"Could not find any input tags in PI server '{tagConfig.Input_PIServer}' matching the input tag mask '{tagConfig.Input_Tag}'.");
                loggerUtility.AddError(errorMessages, logger, message);
            }

            if (outputTags.Count == 0)
            {
                string message = ($"Could not find any output tags in PI server '{tagConfig.Output_PIServer}' matching the output tag mask '{tagConfig.Output_Tag}'.");
                loggerUtility.AddError(errorMessages, logger, message);
            }

            if ((inputTags.Count == 0) || (outputTags.Count == 0)) { return false; }

            return true;
        }
    }
}
