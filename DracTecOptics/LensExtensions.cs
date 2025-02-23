namespace DracTec.Optics;

public static class LensExtensions
{
    public static Func<TRecord, TValue> AsFunc<TRecord, TValue>(this IGetter<TRecord, TValue> getter) => getter.Get;
    
    public static TRecord Update<TRecord, TValue>(
        this ILens<TRecord, TValue> lens, 
        TRecord record, 
        Func<TValue, TValue> update
    ) => lens.Set(record, update(lens.Get(record)));
    
    /// <summary>
    /// Composes two lenses into a new lens which accesses the nested property.
    /// Note that this might incur a performance penalty as compared to using the generated lenses directly.
    /// </summary>
    public static ILens<TRecord, TNewValue> Compose<TRecord, TValue, TNewValue>(
        this ILens<TRecord, TValue> self,
        ILens<TValue, TNewValue> next
    ) => new ComposedLens<TRecord, TValue, TNewValue>(self, next);
    
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
    
    private sealed record ComposedLens<TRecord, TValue, TNewValue>(
        ILens<TRecord, TValue> First, 
        ILens<TValue, TNewValue> Second
    ) : ILens<TRecord, TNewValue>
    {
        public TNewValue Get(TRecord theRecord) => Second.Get(First.Get(theRecord));
        public TRecord Set(TRecord theRecord, TNewValue value) => 
            First.Set(theRecord, Second.Set(First.Get(theRecord), value));
    }
}