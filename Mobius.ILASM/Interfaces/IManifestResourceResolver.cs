using System.IO;

namespace Mobius.ILasm.interfaces
{
    public interface IManifestResourceResolver
    {
        bool TryGetResourceBytes(string path, out byte[] bytes, out string error);
    }
}
