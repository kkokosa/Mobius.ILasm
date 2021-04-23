using BenchmarkDotNet.Running;
using System;
using System.IO;

namespace Mobius.ILasm.Core.BenchmarkDotNet
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<BasicBenchmark>();
        }
    }
}
