using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobius.ILasm.Runner
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
