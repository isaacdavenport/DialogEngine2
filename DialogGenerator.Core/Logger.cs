using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DialogGenerator.Core
{
    public class Logger : ILogger
    {
        private readonly ILog mcLogDialog = LogManager.GetLogger(ApplicationData.Instance.DialogLoggerKey);
        private readonly ILog mcDefaultLog = LogManager.GetLogger(ApplicationData.Instance.DefaultLoggerKey);
        private readonly ILog mcDecimalSerialLogDirectBLE = LogManager.GetLogger(ApplicationData.Instance.DecimalSerialDirectBLELoggerKey);

        public Logger()
        {
            XmlConfigurator.Configure(new FileInfo("log4net.config"));
        }

        // returns logger depends on type
        private ILog _getLogger(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return mcDefaultLog;
            }
            else if (string.Equals(type, ApplicationData.Instance.DialogLoggerKey, StringComparison.OrdinalIgnoreCase))
            {
                return mcDefaultLog;
                //return mcLogDialog;  IKE got rid of second log file
            }
            else if (string.Equals(type, ApplicationData.Instance.DecimalSerialDirectBLELoggerKey, StringComparison.OrdinalIgnoreCase))
            {
                return mcDecimalSerialLogDirectBLE;
            }

            return null;
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
        public void Dialog(string message, string _loggerType = null,
            [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0)
        {
            _getLogger(_loggerType)?.Info(message);
        }
    }
}
