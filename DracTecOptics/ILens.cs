namespace DracTec.Optics;

/// <summary>
/// A functional lens for some type <typeparamref name="TRecord"/> which encapsulates
///  access to a (potentially deeply nested) property of type <typeparamref name="TValue"/>.
/// </summary>
public interface ILens<TRecord, TValue>
{
    TValue Get(TRecord theRecord);
    TRecord Set(TRecord theRecord, TValue value);

    public TValue this[TRecord theRecord] => Get(theRecord);
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

public static class LensExtensions
{
    /// <summary>
    /// Composes two lenses into a new lens which accesses the nested property.
    /// Note that this might incur a performance penalty as compared to using the generated lenses directly.
    /// </summary>
    public static ILens<TRecord, TNewValue> Compose<TRecord, TValue, TNewValue>(
        this ILens<TRecord, TValue> self,
        ILens<TValue, TNewValue> next
    ) => new BasicLens<TRecord, TNewValue>(
        r => next.Get(self.Get(r)), 
        (r, v) => self.Set(r, next.Set(self.Get(r), v))
    );
    
    /// <summary>
    /// Composes a lens with additional getter and setter closures.
    /// Note that this might incur a performance penalty as compared to using the generated lenses directly.
    /// </summary>
    public static ILens<TRecord, TNewValue> Compose<TRecord, TValue, TNewValue>(
        this ILens<TRecord, TValue> self,
        Func<TValue, TNewValue> getter,
        Func<TValue, TNewValue, TValue> setter
    ) => new BasicLens<TRecord, TNewValue>(
        r => getter(self.Get(r)), 
        (r, v) => self.Set(r, setter(self.Get(r), v))
    );
}

