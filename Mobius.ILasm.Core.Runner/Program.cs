using System;

namespace Mobius.ILasm.Core.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var driver = new Driver();
            driver.Assemble(args);
        }
    }
}
