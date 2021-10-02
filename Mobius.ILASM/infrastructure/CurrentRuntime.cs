using System;
using System.Runtime.InteropServices;

namespace Mobius.ILasm.infrastructure
{
    internal static class CurrentRuntime
    {
        public static bool IsNetFramework { get; } = RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
    }
}
