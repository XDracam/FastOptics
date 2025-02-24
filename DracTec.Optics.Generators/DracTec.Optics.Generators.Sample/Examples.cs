namespace DracTec.Optics.Generators.Sample;

public record Name(string First, string Last);

[WithLenses]
public partial record struct Person(Name Name, int Age);

public static class Examples
{ 
    public static Person HappyBirthday(Person person) => 
        Person.Lens.Age.Update(person, age => age + 1);
    
    public static Person ChangeLastNameAfterMarriage(Person person, string newLastName) =>
        Person.Lens.Name.Last.Set(person, newLastName);
    
    public static Person ChangeLastNameAfterMarriageComposed(Person person, string newLastName) =>
        Person.Lens.Name
            .Compose(new BasicLens<Name, string>(n => n.Last, (n, v) => n with { Last = v }))
            .Set(person, newLastName);
}