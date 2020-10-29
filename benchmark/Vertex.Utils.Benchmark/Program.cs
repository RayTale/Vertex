using System;
using BenchmarkDotNet.Running;
using Vertex.Utils.Benchmark.Channels;
using Vertex.Utils.Benchmark.TaskSource;

namespace Vertex.Utils.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<TaskSourceBenchmark>();
        }
    }
}
