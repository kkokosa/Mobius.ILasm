using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using PowerArgs;

namespace Mobius.ILasm.Core.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new Logger();

            try
            {
                var parsedArgs = Args.Parse<Arguments>(args);
                using var memoryStream = new MemoryStream();
                var driver = new Driver(logger, parsedArgs.OutputType == "exe" ? Driver.Target.Exe : Driver.Target.Dll,
                    parsedArgs.ShowParser, parsedArgs.Debug, parsedArgs.ShowTokens);
                driver.Assemble(new [] { File.ReadAllText(parsedArgs.InputFile) }, memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var outputFilename = parsedArgs.OutputFile ??
                                     $"{Path.GetFileNameWithoutExtension(parsedArgs.InputFile)}.{parsedArgs.OutputType}";
                using (FileStream fileStream = new FileStream(outputFilename, FileMode.Create, FileAccess.Write))
                {
                    memoryStream.WriteTo(fileStream);
                    memoryStream.Flush();
                }

                //var assemblyContext = new AssemblyLoadContext(null);
                //var assembly = assemblyContext.LoadFromStream(memoryStream);
                //var assembly = assemblyContext.LoadFromAssemblyPath(@"d:\github\vms\Mobius.ILasm\Mobius.ILasm.Core.Runner\bin\x64\Debug\net5.0\" + outputFilename);
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<Arguments>());
            }
        }

        private void Version()
        {
            var version1 = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version1 is not null)
            {
                string version = version1.ToString();
                Console.WriteLine("Mono IL assembler compiler version {0}", version);
            }
        }
    }

    public class Arguments
    {
        // This argument is required and if not specified the user will be prompted.
        [ArgRequired, ArgDescription("IL file to be parsed"), ArgExistingFile]
        public string InputFile { get; set; }

        public string OutputFile { get; set; }

        [ArgRegex("exe|dll"), ArgDefaultValue("exe")]
        public string OutputType { get; set; }

        public bool NoAutoInherit { get; set; }

        public bool Debug { get; set; }

        public bool ShowParser { get; set; }

        public bool ShowTokens { get; set; }

        [ArgDescription("Strongname using the specified key file")]
        public string StrongKeyFile { get; set; }

        [ArgDescription("Strongname using the specified key container")]
        public string StrongKeyContainer { get; set; }
    }
}
