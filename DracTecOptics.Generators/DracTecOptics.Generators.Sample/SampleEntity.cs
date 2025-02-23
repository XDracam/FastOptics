using DracTec.Optics;

namespace DracTecOptics.Generators.Sample;

public record Name(string First, string Last);

[WithLenses]
public partial record Person(Name Name, int Age);