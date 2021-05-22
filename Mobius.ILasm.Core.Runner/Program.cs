using System.IO;

namespace Mobius.ILasm.Core.Runner
{
    class Program
    {

        static void Main(string[] args)
        {
            var logger = new Logger();
            var driver = new Driver(logger);

            //driver.Assemble(args);

            using (var memoryStream = new MemoryStream())
            {
                driver.Assemble(new string[] { "resources/helloworldconsole.il", "/dll" }, memoryStream);
                memoryStream.Position = 0;
                using(BinaryReader reader = new BinaryReader(memoryStream))
                {
                    for (int i =0; i < memoryStream.Length - 1; i++)
                        System.Console.WriteLine(reader.ReadByte());
                }
            }
        }
    }
}
