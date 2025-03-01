namespace DracTec.Optics.Generators.Sample;

public record Name(string First, string Last);

[WithLenses]
public partial record struct Person(Name Name, int Age);

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

public static partial class Examples
{
    // generated lenses
    
    public static Person HappyBirthday(Person person) => 
        Person.Lens.Age.Update(person, age => age + 1);
    
    public static Person ChangeLastNameAfterMarriage(Person person, string newLastName) =>
        Person.Lens.Name.Last.Set(person, newLastName);
    
    public static Person ChangeLastNameAfterMarriageComposed(Person person, string newLastName) =>
        Person.Lens.Name
            .Compose(new BasicLens<Name, string>(n => n.Last, (n, v) => n with { Last = v }))
            .Set(person, newLastName);
    
    // manual lenses
    
    [Lens(".Name.First")]
    public static partial ILens<Person, string> FirstNameLens { get; }
    
    public static Person ChangeFirstName(Person person, string newName) => 
        FirstNameLens.Set(person, newName);

    [Lens(".Name.Last")]
    public static partial ILens<Person, string> LastNameLens();
    
    public static Person ChangeLastName(Person person, string newName) => 
        LastNameLens().Set(person, newName);
}