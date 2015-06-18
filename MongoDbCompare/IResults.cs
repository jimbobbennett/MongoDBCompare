using System;
using System.Collections.Generic;

namespace MongoDbCompare
{
    public interface IResults<T> where T : class
    {
        IEnumerable<T> OnlyInCollection1 { get; }
        IEnumerable<T> OnlyInCollection2 { get; }
        IEnumerable<Tuple<T, T>> Different { get; }
        bool Match { get; }
    }
}