using Mobius.ILasm.interfaces;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mobius.ILasm.infrastructure
{
    public class LoggerFactory : ILoggerFactory
    {
        public ILog Create(Type type)
        {
            return new Logger(LogManager.GetLogger(type.FullName));
        }

        public ILog Create(string loggerName)
        {
            return new Logger(LogManager.GetLogger(loggerName));
        }

        public ILog Create()
        {
            return new Logger(LogManager.CreateNullLogger());
        }
    }
}
