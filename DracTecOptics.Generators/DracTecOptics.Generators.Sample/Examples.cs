using DracTec.Optics;

namespace DracTecOptics.Generators.Sample;

public static class Examples
{
    public static Person ChangeLastNameAfterMarriage(Person person, string newLastName) =>
        Person.Lens.Name.Last.Set(person, newLastName);
    
    public static Person ChangeLastNameAfterMarriageComposed(Person person, string newLastName) =>
        Person.Lens.Name
            .Compose(new BasicLens<Name, string>(n => n.Last, (n, v) => n with { Last = v }))
            .Set(person, newLastName);
}