using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DialogGenerator.Core
{
    public class Logger : ILogger
    {
        private readonly ILog mcDecimalSerialLog = LogManager.GetLogger(ApplicationData.Instance.DecimaSeriallLoggerKey);
        private readonly ILog mcLogDialog = LogManager.GetLogger(ApplicationData.Instance.DialogLoggerKey);
        private readonly ILog mcDefaultLog = LogManager.GetLogger(ApplicationData.Instance.DefaultLoggerKey);


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
            else if (string.Equals(type, ApplicationData.Instance.DecimaSeriallLoggerKey, StringComparison.OrdinalIgnoreCase))
            {
                return mcDecimalSerialLog;
            }
            else if (string.Equals(type, ApplicationData.Instance.DialogLoggerKey, StringComparison.OrdinalIgnoreCase))
            {
                return mcLogDialog;
            }

            return null;
        }

        public void Error(string _loggerType = null, string _message = null, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0)
        {
            _getLogger(_loggerType)?.Error(_file + " " + _line + " Message : " + _message);
        }

        public void Info(string _loggerType = null, string _message = null, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0)
        {
            _getLogger(_loggerType)?.Error(_file + " " + _line + " Message : " + _message);
        }

        public void Warning(string _loggerType = null, string _message = null, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0)
        {
            _getLogger(_loggerType)?.Error(_file + " " + _line + " Message : " + _message);
        }

        public void Debug(string _loggerType = null, string _message = null, [CallerFilePath] string _file = "", [CallerLineNumber] int _line = 0)
        {
            _getLogger(_loggerType)?.Debug(_file + " " + _line + " Message : " + _message);
        }
    }
}
