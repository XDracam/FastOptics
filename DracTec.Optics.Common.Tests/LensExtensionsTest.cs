namespace DracTec.Optics.Common.Tests;

public partial class LensExtensionsTest
{
    public record Name(string First, string Last);

    [WithLenses]
    public partial record struct Person(Name Name, int Age);
    
    [Test]
    public void Update_AppliesUpdateToValue()
    {
        var lens = new BasicLens<Person, int>(p => p.Age, (p, v) => p with { Age = v });
        var person = new Person(new Name("John", "Doe"), 42);
        var updated = lens.Update(person, age => age + 1);
        Assert.That(updated.Age, Is.EqualTo(43));
    }
    
    [Test]
    public void ComposeWithLens_AppliesNestedLens()
    {
        var person = new Person(new Name("John", "Doe"), 42);
        var composed = Person.Lens.Name
            .Compose(new BasicLens<Name, string>(n => n.Last, (n, v) => n with { Last = v }));
        var updated = composed.Set(person, "Smith");
        Assert.That(updated.Name.Last, Is.EqualTo("Smith"));
        Assert.That(updated.Name.Last, Is.EqualTo(composed.Get(updated)));
    }
    
    [Test]
    public void ComposeWithClosures_AppliesNestedLens()
    {
        var person = new Person(new Name("John", "Doe"), 42);
        var composed = Person.Lens.Name
            .Compose(n => n.Last, (n, v) => n with { Last = v });
        var updated = composed.Set(person, "Smith");
        Assert.That(updated.Name.Last, Is.EqualTo("Smith"));
        Assert.That(updated.Name.Last, Is.EqualTo(composed.Get(updated)));
    }
}