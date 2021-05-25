using System.IO;

namespace Mobius.ILasm.Core.Runner
{
    class Program
    {

        static void Main(string[] args)
        {
            var logger = new Logger();
            var driver = new Driver(logger);            

            using var memoryStream = new MemoryStream();
            driver.Assemble(new string[] { "./resources/helloworldconsole.il", "/exe" }, memoryStream);

            using FileStream fileStream = new FileStream("file.exe", FileMode.Create, FileAccess.Write);
            memoryStream.WriteTo(fileStream);
        }
    }
}
