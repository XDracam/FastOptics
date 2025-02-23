using System.Drawing;
using BenchmarkDotNet.Attributes;
using DracTec.Optics;

namespace DracTecOptics.Benchmarks;

public partial class GeneratedLensBenchmarks
{
    public sealed record Vec2(int X, int Y);
    public sealed record Point(Vec2 Pos, Color color);
    public sealed record NamedPoint(Point Point, string Name);

    [WithLenses]
    public sealed partial record Base(NamedPoint NamedPoint, float Junk);
    
    private readonly Base _base = new(new(new(new(1337, 42), Color.Aqua), "TestPoint"), 0xdeadbeef);

    [Benchmark]
    public Base SetXWithLenses()
    {
        return Base.Lens.NamedPoint.Point.Pos.X.Set(_base, 69);
    }
    
    [Benchmark]
    public Base SetXRegularly()
    {
        return _base with
        {
            NamedPoint = _base.NamedPoint with
            {
                Point = _base.NamedPoint.Point with { Pos = _base.NamedPoint.Point.Pos with { X = 69 } }
            }
        };
    }
    
    [Benchmark]
    public int GetXWithLenses()
    {
        return Base.Lens.NamedPoint.Point.Pos.X.Get(_base);
    }
    
    [Benchmark]
    public int GetXRegularly()
    {
        return _base.NamedPoint.Point.Pos.X;
    }
}