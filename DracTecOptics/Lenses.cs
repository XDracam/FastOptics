namespace DracTec.Optics;

public interface IGetter<in TRecord, out TValue>
{
    TValue Get(TRecord theRecord);
    public TValue this[TRecord theRecord] => Get(theRecord);
}

/// <summary>
/// A functional lens for some type <typeparamref name="TRecord"/> which encapsulates
///  access to a (potentially deeply nested) property of type <typeparamref name="TValue"/>.
/// </summary>
public interface ILens<TRecord, TValue> : IGetter<TRecord, TValue>
{
    TRecord Set(TRecord theRecord, TValue value);
}

/// <summary>
/// Basic implementation of <see cref="ILens{TRecord,TValue}"/> using closures.
/// Prefer the lenses generated through <see cref="WithLensesAttribute"/> for less overhead.
/// </summary>
public sealed record BasicLens<TRecord, TValue>(
    Func<TRecord, TValue> Getter, 
    Func<TRecord, TValue, TRecord> Setter
) : ILens<TRecord, TValue>
{
    public TValue Get(TRecord theRecord) => Getter(theRecord);
    public TRecord Set(TRecord theRecord, TValue value) => Setter(theRecord, value);
}

