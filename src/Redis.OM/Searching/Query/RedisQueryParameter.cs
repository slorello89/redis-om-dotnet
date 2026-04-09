using System;

namespace Redis.OM.Searching.Query
{
    /// <summary>
    /// Represents a single RediSearch query parameter.
    /// </summary>
    public sealed class RedisQueryParameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueryParameter"/> class.
        /// </summary>
        /// <param name="name">The query parameter name.</param>
        /// <param name="value">The query parameter value.</param>
        public RedisQueryParameter(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Query parameter names cannot be null or whitespace.", nameof(name));
            }

            Name = name;
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets the query parameter name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the query parameter value.
        /// </summary>
        public object Value { get; }
    }
}
