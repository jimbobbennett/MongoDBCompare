using System;
using System.Collections.Generic;
using System.Linq;

namespace MongoDbCompare
{
    internal class Results<T> : IResults<T> where T : class
    {
        internal Results(IEnumerable<T> onlyInCollection1, IEnumerable<T> onlyInCollection2, IEnumerable<Tuple<T, T>> different)
        {
            OnlyInCollection1 = onlyInCollection1;
            OnlyInCollection2 = onlyInCollection2;
            Different = different;
        }

        public IEnumerable<T> OnlyInCollection1 { get; private set; }
        public IEnumerable<T> OnlyInCollection2 { get; private set; }
        public IEnumerable<Tuple<T, T>> Different { get; private set; }

        public bool Match { get { return !OnlyInCollection1.Any() && !OnlyInCollection2.Any() && !Different.Any(); } }
    }
}