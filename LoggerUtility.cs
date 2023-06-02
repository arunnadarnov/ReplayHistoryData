using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayHistoryData
{
    public class LoggerUtility
    {
        public void AddError(List<string> errorMessages, ILog logger, string message)
        {
            errorMessages.Add(message);
            logger.Error(message);
        }

        public void AddWarning(List<string> warningMessages, ILog logger, string message)
        {
            warningMessages.Add(message);
            logger.Warn(message);
        }
    }
}
