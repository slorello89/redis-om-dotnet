using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using StackExchange.Redis;

namespace Redis.OM.Searching
{
    /// <summary>
    /// A lightweight ad hoc projection row for RediSearch field selections.
    /// </summary>
    /// <remarks>
    /// Use <see cref="SearchProjection"/> when a query only needs a few selected fields and creating
    /// a dedicated DTO would add unnecessary type surface area. Field names are matched using
    /// <see cref="StringComparer.OrdinalIgnoreCase"/>, and values are exposed as strings so callers can
    /// inspect arbitrary RediSearch projections and opt into typed conversion with <see cref="GetValue{T}(string)"/>.
    /// This type does not provide compile-time property binding like DTO projections.
    /// </remarks>
    public sealed class SearchProjection : IReadOnlyDictionary<string, string>
    {
        private readonly Dictionary<string, string> _fields;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchProjection"/> class from a RediSearch document reply.
        /// </summary>
        /// <param name="hash">The projected field/value pairs.</param>
        internal SearchProjection(IDictionary<string, RedisReply> hash)
        {
            _fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in hash)
            {
                _fields[kvp.Key] = kvp.Value.ToString();
            }
        }

        /// <inheritdoc/>
        public IEnumerable<string> Keys => _fields.Keys;

        /// <inheritdoc/>
        public IEnumerable<string> Values => _fields.Values;

        /// <inheritdoc/>
        public int Count => _fields.Count;

        /// <inheritdoc/>
        public string this[string key] => _fields[key];

        /// <summary>
        /// Gets a projected field as a converted value.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="fieldName">The returned field or alias name.</param>
        /// <returns>The converted value.</returns>
        public T GetValue<T>(string fieldName)
        {
            if (fieldName is null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            var value = _fields[fieldName];
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        /// <inheritdoc/>
        public bool ContainsKey(string key) => _fields.ContainsKey(key);

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _fields.GetEnumerator();

        /// <inheritdoc/>
        public bool TryGetValue(string key, out string value) => _fields.TryGetValue(key, out value!);

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
