namespace DracTec.Optics;

/// <summary>
/// Add this attribute to any record or record struct to generate lenses for all properties.
/// Lenses are generated as static properties, e.g. for records
/// <c>
/// record Name(string First, string Last);
/// record Person(Name Name, int Age);
/// </c>
/// this attribute generates static properties <c>Name</c> and <c>Age</c>.
/// The lens returned by <c>Name</c> also has properties <c>First</c> and <c>Last</c>.
/// <c>
/// Person alice = new Person(new Name("Alice", "Smith"), 42);
/// int age = Person.Age.Get(alice);
/// Person bob = Person.Name.First.Set(alice, "Bob");
/// Assert.Equal("Bob", bob.Name.First);
/// </c>
/// </summary>
/// <remarks>
/// The real power of lenses comes from modifying deeply nested properties.
/// What is nicer, <c>Person.Name.First.Set(alice, "Bob")</c>
///  or <c>alice with { Name = alice.Name with { First = "Bob" } }</c>?
/// Now consider three or more levels of nesting!
/// </remarks>
/// <param name="isRecursive"></param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class WithLensesAttribute(bool isRecursive = true) : Attribute
{
    public readonly bool IsRecursive = isRecursive;
}