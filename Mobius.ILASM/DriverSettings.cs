using Mobius.ILasm.infrastructure;
using Mobius.ILasm.interfaces;

namespace Mobius.ILasm.Core
{
    public class DriverSettings
    {
        public static DriverSettings Default { get; } = new DriverSettings();

        public IManifestResourceResolver ResourceResolver { get; set; } = new FileResourceResolver();
        public bool ShowParser { get; set; }
        public bool DebuggingInfo { get; set; }
        public bool ShowTokens { get; set; }
    }
}
