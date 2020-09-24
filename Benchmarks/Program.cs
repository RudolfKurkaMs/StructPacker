using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            IConfig config;

#if DEBUG
            config = new DebugInProcessConfig();
#else
            config = null;
#endif

            BenchmarkRunner.Run<SmallMessageBenchmarks>(config);
            BenchmarkRunner.Run<LargeMessageBenchmarks>(config);

            Console.ReadKey();
        }
    }
}