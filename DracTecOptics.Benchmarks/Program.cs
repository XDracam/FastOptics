using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using DracTecOptics.Benchmarks;

var config = DefaultConfig.Instance
    .AddExporter(MarkdownExporter.GitHub);

BenchmarkRunner.Run<GeneratedLensBenchmarks>(config);