using Mono.ILASM;
using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NLog.Conditions;
using NLog.Targets;
using ILogger = Mobius.ILasm.interfaces.ILogger;

namespace Mobius.ILasm
{
    public class Logger : ILogger
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();        

        public Logger()
        {
            var config = new NLog.Config.LoggingConfiguration();
            var logconsole = new NLog.Targets.ColoredConsoleTarget();
            logconsole.Layout = "${time} [${pad:padding=5:inner=${level:uppercase=true}}] ${message}";

            var highlightRule = new ConsoleRowHighlightingRule();
            highlightRule.Condition = ConditionParser.ParseExpression("level == LogLevel.Error");
            highlightRule.ForegroundColor = ConsoleOutputColor.Red;
            logconsole.RowHighlightingRules.Add(highlightRule);

            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            NLog.LogManager.Configuration = config;
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Error(Location location, string message)
        {
            string location_str = " : ";
            if (location != null)
                location_str = " (" + location.line + ", " + location.column + ") : ";

            //TODO - Include File Path like in the report class.
            _logger.Error(string.Format("{0}Error : {1}", location_str, message));
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Warning(string message)
        {
            _logger.Warn(message);
        }

        public void Warning(Location location, string message)
        {
            string location_str = " : ";
            if (location != null)
                location_str = " (" + location.line + ", " + location.column + ") : ";


            //TODO - Include File Path like in the report class.
            _logger.Warn(string.Format("{0}Error : {1}", location_str, message));
        }
    }
}
