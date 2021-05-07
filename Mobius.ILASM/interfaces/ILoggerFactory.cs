using System;
using System.Collections.Generic;
using System.Text;

namespace Mobius.ILasm.interfaces
{
    public interface ILoggerFactory
    {
        ILog Create(Type type);
        ILog Create(string loggerName);
        ILog Create();
    }
}
