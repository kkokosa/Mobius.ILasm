using Mobius.ILasm.interfaces;
using Mono.ILASM;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mobius.ILasm.Core.Runner
{
    public class Logger : ILogger
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();        

        public Logger()
        {            
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
