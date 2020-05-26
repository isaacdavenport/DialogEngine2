using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DialogGenerator.Core
{
    public class Logger : ILogger
    {
        private readonly ILog mcDefaultLog = LogManager.GetLogger(ApplicationData.Instance.DialogLoggerKey);
        private readonly ILog mcBLEVectorsLog = LogManager.GetLogger(ApplicationData.Instance.BLEVectorsLoggerKey);

        public Logger()
        {
            XmlConfigurator.Configure(new FileInfo("log4net.config"));
        }

        // returns logger depends on type
        private ILog _getLogger(string type)
        {
            if (!string.IsNullOrEmpty(type) && string.Equals(type, ApplicationData.Instance.BLEVectorsLoggerKey, StringComparison.OrdinalIgnoreCase))
            {
                return mcBLEVectorsLog;
            } else
            {
                return mcDefaultLog;
            }         
        }

        public void Error(string message, string _loggerType = null, 
            [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0)
        {
            _getLogger(_loggerType)?.Error(message);
        }

        public void Info(string message, string _loggerType = null, 
            [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0)
        {
            _getLogger(_loggerType)?.Info(message);
        }

        public void Warning(string message, string _loggerType = null, 
            [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0)
        {
            _getLogger(_loggerType)?.Warn(message);
        }

        public void Debug(string message, string _loggerType = null, 
            [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0)
        {
            _getLogger(_loggerType)?.Debug(message);
        }
    }
}
