namespace DracTec.Optics;

/// <summary>
/// Add this attribute to any partial method to generate a lens for the specified path.
/// The partial method must return an <see cref="ILens{A,B}"/> and the path string must
///  start with <c>.</c> and form a valid expression of the return type's source type.
/// </summary>
/// <example><c>
/// record Name(string First, string Last);
/// record Person(Name Name, int Age);
///
/// [Lens(".Name.First")]
/// public partial ILens&lt;Person, string&gt; FirstNameLens();
/// </c></example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public class LensAttribute(string path) : Attribute
{
    public readonly string Path = path;
}