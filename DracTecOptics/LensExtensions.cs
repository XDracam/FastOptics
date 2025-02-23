namespace DracTec.Optics;

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
    
    public static Func<TRecord, TValue> AsFunc<TRecord, TValue>(this IGetter<TRecord, TValue> getter) => getter.Get;
    
    public static TRecord Update<TRecord, TValue>(
        this ILens<TRecord, TValue> lens, 
        TRecord record, 
        Func<TValue, TValue> update
    ) => lens.Set(record, update(lens.Get(record)));
}