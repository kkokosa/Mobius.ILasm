using Mobius.ILasm.interfaces;
using Mono.ILASM;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mobius.ILasm.infrastructure
{
    public class Logger : ILog
    {
        private readonly ILogger logger;

        public Logger(ILogger logger)
        {
            this.logger = logger;
        }

        public void Error(string message)
        {
            logger.Error(message);
        }

        public void Error(Location location, string message)
        {
            string location_str = " : ";
            if (location != null)
                location_str = " (" + location.line + ", " + location.column + ") : ";

            //TODO - Include File Path like in the report class.
            logger.Error(string.Format("{0}Error : {1}", location_str, message));
        }

        public void Info(string message)
        {
            logger.Info(message);
        }

        public void Warning(string message)
        {
            logger.Warn(message);
        }

        public void Warning(Location location, string message)
        {
            string location_str = " : ";
            if (location != null)
                location_str = " (" + location.line + ", " + location.column + ") : ";


            //TODO - Include File Path like in the report class.
            logger.Warn(string.Format("{0}Error : {1}", location_str, message));
        }
    }
}
