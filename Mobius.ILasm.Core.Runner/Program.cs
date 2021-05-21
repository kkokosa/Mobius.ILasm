namespace Mobius.ILasm.Core.Runner
{
    class Program
    {

        static void Main(string[] args)
        {
            var logger = new Logger();
            var driver = new Driver(logger);

            driver.Assemble(args);
        }
    }
}
