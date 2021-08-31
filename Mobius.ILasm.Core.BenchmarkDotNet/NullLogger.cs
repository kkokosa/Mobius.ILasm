using Mobius.ILasm.interfaces;
using Mono.ILASM;

namespace Mobius.ILasm.Core.BenchmarkDotNet
{
    class NullLogger : ILogger
    {
        public void Error(string message)
        {            
        }

        public void Error(Location location, string message)
        {            
        }

        public void Info(string message)
        {            
        }

        public void Warning(string message)
        {            
        }

        public void Warning(Location location, string message)
        {         
        }
    }
}
