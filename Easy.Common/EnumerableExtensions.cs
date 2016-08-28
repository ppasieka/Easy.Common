﻿// ReSharper disable PossibleMultipleEnumeration
namespace Easy.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Extension methods for <see cref="System.Collections.Generic.IEnumerable{T}"/>
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Convenience method for retrieving a specific page of items within a collection.
        /// </summary>
        /// <typeparam name="T">The type of element in the sequence</typeparam>
        /// <param name="sequence">The sequence of elements</param>
        /// <param name="pageIndex">The index for the page</param>
        /// <param name="pageSize">The size of the elements in the page</param>
        /// <returns>The returned paged sequence</returns>
        public static IEnumerable<T> GetPage<T>(this IEnumerable<T> sequence, int pageIndex, int pageSize)
        {
            Ensure.That<ArgumentException>(pageIndex >= 0, "The page index cannot be negative.");
            Ensure.That<ArgumentException>(pageSize > 0, "The page size must be greater than zero.");

            return sequence.Skip(pageIndex * pageSize).Take(pageSize);
        }

        /// <summary>
        /// Converts an Enumerable into a read-only collection
        /// </summary>
        public static IEnumerable<T> ToReadOnlyCollection<T>(this IEnumerable<T> sequence)
        {
            Ensure.NotNull(sequence, nameof(sequence));
            return sequence.Skip(0);
        }

        /// <summary>
        /// Validates that the <paramref name="sequence"/> is not null and contains items.
        /// </summary>
        public static bool IsNotNullOrEmpty<T>(this IEnumerable<T> sequence)
        {
            return sequence != null && sequence.Any();
        }

        /// <summary>
        /// Concatenates the members of a collection, using the specified separator between each member.
        /// </summary>
        /// <typeparam name="T">The type of data in <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">The sequence of data to separate by the given <paramref name="separator"/></param>
        /// <param name="separator">The string to use for separating each items in the <paramref name="sequence"/>.</param>
        /// <returns>The string containing the data in the <paramref name="sequence"/> separated by the <paramref name="separator"/>.</returns>
        public static string ToStringSeparated<T>(this IEnumerable<T> sequence, string separator)
        {
            Ensure.NotNull(sequence, nameof(sequence));

            if (!sequence.Any()) { return string.Empty; }

            var characterSeparated = new StringBuilder();
            foreach (var item in sequence)
            {
                characterSeparated.AppendFormat("{0}{1}", item, separator);
            }

            return characterSeparated.ToString(0, characterSeparated.Length - separator.Length);
        }

        /// <summary>
        /// Converts <paramref name="sequence"/> to a <paramref name="delimiter"/> separated string
        /// </summary>
        /// <typeparam name="T">The type of data in <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">The sequence of data to separate by the given <paramref name="delimiter"/></param>
        /// <param name="delimiter">The character to use for separating each items in the <paramref name="sequence"/>.</param>
        /// <returns>The string containing the data in the <paramref name="sequence"/> separated by the <paramref name="delimiter"/>.</returns>
        public static string ToCharSeparated<T>(this IEnumerable<T> sequence, char delimiter)
        {
            return ToStringSeparated(sequence, delimiter.ToString());
        }

        /// <summary>
        /// Converts <paramref name="sequence"/> to a comma separated string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public static string ToCommaSeparated<T>(this IEnumerable<T> sequence)
        {
            return ToCharSeparated(sequence, ',');
        }

        /// <summary>
        /// Executes an <paramref name="action"/> for each of the items in the sequence
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sequence"></param>
        /// <param name="action"></param>
        [DebuggerStepThrough]
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            Ensure.NotNull(action, nameof(action));

            foreach (var item in sequence) { action(item); }
        }

        /// <summary>
        /// Selects a random element from an Enumerable with only one pass (O(N) complexity); 
        /// It contains optimizations for arguments that implement ICollection{T} by using the 
        /// Count property and the ElementAt LINQ method. The ElementAt LINQ method itself contains 
        /// optimizations for <see cref="IList{T}"/>.
        /// </summary>
        public static T SelectRandom<T>(this IEnumerable<T> sequence)
        {
            var random = new Random();

            // Optimization for ICollection<T>
            var collection = sequence as ICollection<T>;
            if (collection != null)
            {
                return collection.ElementAt(random.Next(collection.Count));
            }

            var count = 1;
            var selected = default(T);

            foreach (var element in sequence)
            {
                if (random.Next(count++) == 0)
                {
                    // Select the current element with 1/count probability
                    selected = element;
                }
            }
            return selected;
        }

        /// <summary>
        /// Randomizes a <paramref name="sequence"/>.
        /// </summary>
        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> sequence)
        {
            return sequence.OrderBy(s => Guid.NewGuid());
        }

        /// <summary>
        /// Allows exception handling when yield returning an IEnumerable
        /// <example>
        /// myList.HandleExceptionWhenYieldReturning{int}(e => 
        /// {
        ///     Logger.Error(e);
        ///     throw new SybaseException("Exception occurred", e);
        /// }, e => e is AseException || e is DbException);
        /// </example>
        /// </summary>
        /// <typeparam name="T">Type of data to enumerate.</typeparam>
        /// <param name="sequence">The sequence of <typeparamref name="T"/> which will be enumerated.</param>
        /// <param name="exceptionPredicate">The predicate specifying which exception(s) to handle.</param>
        /// <param name="actionToExecuteOnException">The action to which the handled exception will be passed to.</param>
        /// <returns></returns>
        public static IEnumerable<T> HandleExceptionWhenYieldReturning<T>(this IEnumerable<T> sequence, Func<Exception, bool> exceptionPredicate, Action<Exception> actionToExecuteOnException)
        {
            Ensure.NotNull(exceptionPredicate, nameof(exceptionPredicate));
            Ensure.NotNull(actionToExecuteOnException, nameof(actionToExecuteOnException));

            var enumerator = sequence.GetEnumerator();

            while (true)
            {
                T result;
                try
                {
                    if (!enumerator.MoveNext()) { break; }
                    result = enumerator.Current;
                }
                catch (Exception e)
                {
                    if (exceptionPredicate(e))
                    {
                        actionToExecuteOnException(e);
                        yield break;
                    }
                    throw;
                }
                yield return result;
            }

            enumerator.Dispose();
        }
    }
}