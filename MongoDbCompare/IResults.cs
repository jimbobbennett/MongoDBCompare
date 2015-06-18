using System;
using System.Collections.Generic;

namespace MongoDbCompare
{
    /// <summary>
    /// An interface that defines the results of a comparison between two MongoDB collections
    /// </summary>
    /// <typeparam name="T">The type for the document stored in the collection</typeparam>
    public interface IResults<T> where T : class
    {
        /// <summary>
        /// Gets a list of documents that are only in the first collection
        /// </summary>
        IEnumerable<T> OnlyInCollection1 { get; }

        /// <summary>
        /// Gets a list of documents that are only in the second collection
        /// </summary>
        IEnumerable<T> OnlyInCollection2 { get; }

        /// <summary>
        /// Gets a list of documents that are in both collections but do not match
        /// </summary>
        IEnumerable<Tuple<T, T>> Different { get; }

        /// <summary>
        /// Gets if the collections match or not.
        /// </summary>
        bool Match { get; }
    }
}