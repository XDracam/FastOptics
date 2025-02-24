using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using DracTec.Optics.Benchmarks;

var config = DefaultConfig.Instance
    .AddExporter(MarkdownExporter.GitHub);

BenchmarkRunner.Run<GeneratedLensBenchmarks>(config);