# Fast Optics

[This blog post provides a great introduction about why optics are useful.](https://medium.com/@heytherewill/functional-programming-optics-in-net-7e1998bfb47e)

There are some implementations for Lenses in C# out there ([dadhi/Lens.cs](https://gist.github.com/dadhi/3db1ed45a60bceaa16d051ee9a4ab1b7), [Tinkoff/Visor](https://github.com/Tinkoff/Visor), ...) but all of these implementations incur a significant runtime overhead.
And I like to have my cake and eat it too. Additionally, I wanted useful and simple and easy-to-understand lenses without all the fancy FP terms that can be used by junior developers as well as seasoned developers.

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

## Usage

TODO: upload to NuGet

Add the `[WithLenses]` attribute to your top level record that you want lenses for:

```cs
[WithLenses]
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

Lenses can also be reused and composed (with some overhead)

```cs
[WithLenses]
public record struct Name(string First, string Last);

[WithLenses(isRecursive: false)]
public partial record Person(Name Name, int Age);

ILens<Person, string> firstName = Person.Lens.Name.Combine(Name.Lens.First);
Person richard = new Person(new("Richard", "Smith"), 42);
Person rick = firstName.Set(richard, "Rick");

List<Person> people = queryAllPeople();
var firstNames = people.Select(firstName.AsFunc).ToList();
```

## Benchmark Results

