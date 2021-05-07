using Mono.ILASM;

namespace Mobius.ILasm.interfaces
{
    public interface ILog
    {
        void Info(string message);
        void Error(string message);
        void Error(Location location, string message);
        void Warning(string message);
        void Warning(Location location, string message);
    }
}