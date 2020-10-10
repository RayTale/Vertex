using BenchmarkDotNet.Running;
using System;
using Vertex.Utils.Benchmark.Channels;
using Vertex.Utils.Benchmark.TaskSource;

namespace Vertex.Utils.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<TaskSourceBenchmark>();
        }
    }
}
