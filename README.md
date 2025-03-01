# Fast Optics

[This blog post provides a great introduction about why optics are useful.](https://medium.com/@heytherewill/functional-programming-optics-in-net-7e1998bfb47e)

There are some implementations for Lenses in C# out there ([dadhi/Lens.cs](https://gist.github.com/dadhi/3db1ed45a60bceaa16d051ee9a4ab1b7), [Tinkoff/Visor](https://github.com/Tinkoff/Visor), ...) but all of these implementations incur a significant runtime overhead.
And I like to have my cake and eat it too. Additionally, I wanted useful and simple and easy-to-understand lenses without all the fancy FP terms that can be used by junior developers as well as seasoned developers.

**The goal of this project is to provide the convenience of functional optics without any significant runtime overhead.**

Still not sure why you'd want this? Look at this (deprived) benchmark example:

```cs
[Benchmark]
public Base SetXWithLenses()
{
    return Base.Lens.NamedPoint.Point.Pos.X.Set(_base, 13);
}

[Benchmark]
public Base SetXRegularly()
{
    return _base with
    {
        NamedPoint = _base.NamedPoint with
        {
            Point = _base.NamedPoint.Point with { Pos = _base.NamedPoint.Point.Pos with { X = 13 } }
        }
    };
}
```

## Getting Started

You can install [`DracTec.Optics` with NuGet](https://www.nuget.org/packages/DracTec.Optics):

    Install-Package DracTec.Optics

Or via the .NET Core command line interface:

    dotnet add package DracTec.Optics

Either commands, from Package Manager Console or .NET Core CLI, will download and install `DracTec.Optics`.

## Usage

Add the `[WithLenses]` attribute to your top level record that you want lenses for:

```cs
public record struct Name(string First, string Last);

[WithLenses]
public partial record Person(Name Name, int Age);
```

Now you can use the generated lenses with effectively zero overhead!

```cs
Person alice = new Person(new Name("Alice", "Smith"), 23);
Person marriedAlice = Person.Lens.Name.Last.Set(alice, "Thorsson");
Debug.Assert(marriedAlice.Name.Last == Person.Lens.Name.Last.Get(alice));
```

Alternatively, you can generate lenses for records that you do not own.
In both cases, the generated lens is a singleton that is only allocated once.

```cs 
[Lens(".Name.First")]
public static partial ILens<Person, string> FirstNameLens { get; }

[Lens(".Name.Last")]
public static partial ILens<Person, string> LastNameLens();
```

Lenses can also be reused and composed (with some overhead)

```cs
[WithLenses]
public record struct Name(string First, string Last);

[WithLenses(isRecursive: false)]
public partial record Person(Name Name, int Age);

ILens<Person, string> firstName = Person.Lens.Name.Combine(Name.Lens.First);
Person richard = new Person(new("Richard", "Smith"), 42);
Person rick = firstName.Set(richard, "Rick");

// happy birthday!
Person olderRick = Person.Lens.Age.Update(rick, age => age + 1);

List<Person> people = queryAllPeople();
var firstNames = people.Select(firstName.AsFunc).ToList();
```

## Benchmark Results

```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4890/23H2/2023Update/SunValley3)
12th Gen Intel Core i5-12600K, 1 CPU, 16 logical and 10 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


```
| Method                         | Mean       | Error     | StdDev    | Median     |
|------------------------------- |-----------:|----------:|----------:|-----------:|
| SetXWithLenses                 | 18.4337 ns | 0.4108 ns | 0.9684 ns | 18.2913 ns |
| SetXWithLensesThroughInterface | 17.5305 ns | 0.3938 ns | 0.8727 ns | 17.3956 ns |
| SetXRegularly                  | 16.9095 ns | 0.3647 ns | 0.9735 ns | 16.5907 ns |
| GetXWithLenses                 |  0.0129 ns | 0.0057 ns | 0.0050 ns |  0.0146 ns |
| GetXWithLensesThroughInterface |  0.1277 ns | 0.0038 ns | 0.0032 ns |  0.1269 ns |
| GetXRegularly                  |  0.1283 ns | 0.0037 ns | 0.0033 ns |  0.1288 ns |

To note: `GetXWithLenses` uses the generated `Get` method directly, which has a `[MethodImpl(MethodImplOptions.AggressiveInlining)]`.
This seems to generate better results than just accessing the properties directly in this benchmark case.
