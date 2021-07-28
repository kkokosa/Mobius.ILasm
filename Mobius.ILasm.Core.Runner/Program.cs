using System;
using System.IO;
using System.Reflection;
using PowerArgs;

namespace Mobius.ILasm.Core.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new Logger();
            var driver = new Driver(logger);

            try
            {
                var parsedArgs = Args.Parse<Arguments>(args);
                using var memoryStream = new MemoryStream();
                driver.Assemble(new[] 
                    { 
                        parsedArgs.InputFile, 
                        $"/{parsedArgs.OutputType}"
                    }, 
                    memoryStream);

                var outputFilename = parsedArgs.OutputFile ??
                                     $"{Path.GetFileNameWithoutExtension(parsedArgs.InputFile)}.{parsedArgs.OutputType}";
                using FileStream fileStream = new FileStream(outputFilename, FileMode.Create, FileAccess.Write);
                memoryStream.WriteTo(fileStream);
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<Arguments>());
            }
        }
    }

    public class Arguments
    {
        // This argument is required and if not specified the user will 
        // be prompted.
        [ArgRequired, ArgDescription("IL file to be parsed"), ArgExistingFile]
        public string InputFile { get; set; }

        public string OutputFile { get; set; }

        // This argument is not required, but if specified must be >= 0 and <= 60

        [ArgRegex("exe|dll"), ArgDefaultValue("exe")]
        public string OutputType { get; set; }
    }
}
