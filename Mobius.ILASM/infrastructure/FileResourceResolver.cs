using System;
using System.IO;
using Mobius.ILasm.interfaces;

namespace Mobius.ILasm.infrastructure
{
    public class FileResourceResolver : IManifestResourceResolver
    {
        private readonly string basePath;

        public FileResourceResolver(string basePath = null)
        {
            this.basePath = basePath;
        }

        public bool TryGetResourceBytes(string path, out byte[] bytes, out string error)
        {
            if (basePath != null && !Path.IsPathRooted(path))
                path = Path.Combine(basePath, path);

            bytes = Array.Empty<byte>();
            if (!File.Exists(path))
            {
                error = $"Resource file '{path}' was not found";
                return false;
            }

            try
            {
                bytes = File.ReadAllBytes(path);
                error = null;
                return true;
            }
            // Until https://github.com/dotnet/runtime/issues/27217
            catch (FileNotFoundException ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
