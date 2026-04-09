using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Redis.OM.Aggregation;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using Redis.OM.Searching.Query;
using StackExchange.Redis;

[assembly: InternalsVisibleTo("Redis.OM.Unit.Tests")]

namespace Redis.OM
{
    /// <summary>
    /// Provides a connection to redis.
    /// </summary>
    public class RedisConnectionProvider : IRedisConnectionProvider
    {
        private readonly IRedisConnection? _connection;
        private readonly IConnectionMultiplexer? _mux;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConnectionProvider"/> class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to redis.</param>
        public RedisConnectionProvider(string connectionString)
        {
            var options = RedisUriParser.ParseConfigFromUri(connectionString);
            _mux = ConnectionMultiplexer.Connect(options);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConnectionProvider"/> class.
        /// </summary>
        /// <param name="connectionConfig">The configuration.</param>
        public RedisConnectionProvider(RedisConnectionConfiguration connectionConfig)
        {
            _mux = ConnectionMultiplexer.Connect(connectionConfig.ToStackExchangeConnectionString());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConnectionProvider"/> class.
        /// </summary>
        /// <param name="configurationOptions">The options relevant to a set of redis connections.</param>
        public RedisConnectionProvider(ConfigurationOptions configurationOptions)
        {
            _mux = ConnectionMultiplexer.Connect(configurationOptions);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConnectionProvider"/> class.
        /// </summary>
        /// <param name="connectionMultiplexer">The options relevant to a set of redis connections.</param>
        public RedisConnectionProvider(IConnectionMultiplexer connectionMultiplexer)
        {
            _mux = connectionMultiplexer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConnectionProvider"/> class for tests with a prebuilt Redis connection.
        /// </summary>
        /// <param name="connection">The Redis connection to reuse.</param>
        internal RedisConnectionProvider(IRedisConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Gets a command level interface to redis.
        /// </summary>
        public IRedisConnection Connection => _connection ?? new RedisConnection(_mux!.GetDatabase());

        /// <summary>
        /// Gets an aggregation set for redis.
        /// </summary>
        /// <typeparam name="T">The indexed type to run aggregations on.</typeparam>
        /// <param name="chunkSize">Size of chunks to use during pagination, larger chunks = larger payloads returned but fewer round trips.</param>
        /// <returns>the aggregation set.</returns>
        public RedisAggregationSet<T> AggregationSet<T>(int chunkSize = 100) => new (Connection, chunkSize: chunkSize);

        /// <summary>
        /// Executes a RediSearch query against the supplied index without constructing an <see cref="IRedisCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">The materialized result type.</typeparam>
        /// <param name="indexName">The RediSearch index name.</param>
        /// <param name="queryText">The RediSearch query string. Defaults to <c>*</c>.</param>
        /// <returns>A typed search response.</returns>
        /// <example>
        /// <code>
        /// var results = await provider.SearchAsync&lt;Person&gt;("person-idx", "@Name:{Steve}");
        /// </code>
        /// </example>
        public Task<SearchResponse<T>> SearchAsync<T>(string indexName, string queryText = "*")
            where T : notnull => SearchAsync<T>(CreateSearchQuery(indexName, queryText));

        /// <summary>
        /// Executes a parameterized RediSearch query against the supplied index without constructing an <see cref="IRedisCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">The materialized result type.</typeparam>
        /// <param name="indexName">The RediSearch index name.</param>
        /// <param name="queryText">The RediSearch query string.</param>
        /// <param name="queryParameters">An anonymous object or dictionary containing named query parameters.</param>
        /// <returns>A typed search response.</returns>
        /// <example>
        /// <code>
        /// var results = await provider.SearchAsync&lt;Person&gt;(
        ///     "person-idx",
        ///     "@Name:{$name}",
        ///     new { name = "Steve" });
        /// </code>
        /// </example>
        public Task<SearchResponse<T>> SearchAsync<T>(string indexName, string queryText, object queryParameters)
            where T : notnull => SearchAsync<T>(CreateSearchQuery(indexName, queryText, queryParameters));

        /// <summary>
        /// Executes a provider-level RediSearch query using the shared command implementation.
        /// </summary>
        /// <typeparam name="T">The materialized result type.</typeparam>
        /// <param name="query">The shared RediSearch query object.</param>
        /// <returns>A typed search response.</returns>
        public virtual Task<SearchResponse<T>> SearchAsync<T>(RedisQuery query)
            where T : notnull => Connection.SearchAsync<T>(query);

        /// <summary>
        /// Gets a redis collection.
        /// </summary>
        /// <typeparam name="T">The type the collection will be retrieving.</typeparam>
        /// <param name="chunkSize">Size of chunks to use during pagination, larger chunks = larger payloads returned but fewer round trips.</param>
        /// <returns>A RedisCollection.</returns>
        public IRedisCollection<T> RedisCollection<T>(int chunkSize = 100)
            where T : notnull => new RedisCollection<T>(Connection, chunkSize);

        /// <summary>
        /// Gets a redis collection.
        /// </summary>
        /// <typeparam name="T">The type the collection will be retrieving.</typeparam>
        /// <param name="saveState">Whether or not the RedisCollection should maintain the state of documents it enumerates.</param>
        /// <param name="chunkSize">Size of chunks to use during pagination, larger chunks = larger payloads returned but fewer round trips.</param>
        /// <returns>A RedisCollection.</returns>
        public IRedisCollection<T> RedisCollection<T>(bool saveState, int chunkSize = 100)
            where T : notnull => new RedisCollection<T>(Connection, saveState, chunkSize);

        /// <summary>
        /// Builds the shared <see cref="RedisQuery"/> instance used by provider-level search entry points.
        /// </summary>
        /// <param name="indexName">The RediSearch index name.</param>
        /// <param name="queryText">The RediSearch query string.</param>
        /// <returns>The shared query object.</returns>
        internal virtual RedisQuery CreateSearchQuery(string indexName, string queryText = "*") =>
            new (indexName) { QueryText = queryText ?? "*" };

        /// <summary>
        /// Builds the shared <see cref="RedisQuery"/> instance used by provider-level parameterized search entry points.
        /// </summary>
        /// <param name="indexName">The RediSearch index name.</param>
        /// <param name="queryText">The RediSearch query string.</param>
        /// <param name="queryParameters">The named query parameters.</param>
        /// <returns>The shared query object.</returns>
        internal virtual RedisQuery CreateSearchQuery(string indexName, string queryText, object queryParameters)
        {
            var normalizedQueryText = queryText ?? "*";
            var query = CreateSearchQuery(indexName, normalizedQueryText);
            query.NamedParameters = BuildNamedParameters(queryParameters, normalizedQueryText);
            return query;
        }

        private static List<RedisQueryParameter> BuildNamedParameters(object queryParameters, string queryText)
        {
            if (queryParameters is null)
            {
                throw new ArgumentNullException(nameof(queryParameters));
            }

            var parameters = ExtractNamedParameters(queryParameters);
            ValidateNamedParameters(parameters);
            ValidateRequiredParameters(queryText, parameters);
            return parameters;
        }

        private static List<RedisQueryParameter> ExtractNamedParameters(object queryParameters)
        {
            if (TryExtractDictionaryParameters(queryParameters, out var dictionaryParameters))
            {
                return dictionaryParameters;
            }

            var type = queryParameters.GetType();
            if (type == typeof(string) || type.IsPrimitive)
            {
                throw new ArgumentException("Query parameters must be provided as an anonymous object or dictionary.", nameof(queryParameters));
            }

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && x.GetIndexParameters().Length == 0)
                .ToArray();

            if (!properties.Any())
            {
                throw new ArgumentException("Query parameters must expose at least one readable public property.", nameof(queryParameters));
            }

            return properties
                .Select(property =>
                {
                    var value = property.GetValue(queryParameters);
                    if (value is null)
                    {
                        throw new ArgumentException($"Query parameter '{property.Name}' cannot be null.", nameof(queryParameters));
                    }

                    return new RedisQueryParameter(property.Name, value);
                })
                .ToList();
        }

        private static bool TryExtractDictionaryParameters(object queryParameters, out List<RedisQueryParameter> parameters)
        {
            if (queryParameters is IDictionary dictionary)
            {
                parameters = dictionary.Keys
                    .Cast<object>()
                    .Select(key => key?.ToString() ?? throw new ArgumentException("Query parameter names cannot be null.", nameof(queryParameters)))
                    .Select(key =>
                    {
                        var value = dictionary[key];
                        if (value is null)
                        {
                            throw new ArgumentException($"Query parameter '{key}' cannot be null.", nameof(queryParameters));
                        }

                        return new RedisQueryParameter(key, value);
                    })
                    .ToList();
                return true;
            }

            var dictionaryInterface = queryParameters.GetType().GetInterfaces()
                .FirstOrDefault(x => x.IsGenericType
                    && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    && x.GetGenericArguments()[0].IsGenericType
                    && x.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
                    && x.GetGenericArguments()[0].GetGenericArguments()[0] == typeof(string));

            if (dictionaryInterface is null)
            {
                parameters = null!;
                return false;
            }

            var keyValuePairType = dictionaryInterface.GetGenericArguments()[0];
            var keyProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<string, object>.Key))
                ?? throw new InvalidOperationException("Unable to read query parameter keys.");
            var valueProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<string, object>.Value))
                ?? throw new InvalidOperationException("Unable to read query parameter values.");

            parameters = ((IEnumerable)queryParameters).Cast<object>()
                .Select(item =>
                {
                    var key = (string?)keyProperty.GetValue(item);
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        throw new ArgumentException("Query parameter names cannot be null or whitespace.", nameof(queryParameters));
                    }

                    var parameterName = key ?? throw new ArgumentException("Query parameter names cannot be null or whitespace.", nameof(queryParameters));
                    var value = valueProperty.GetValue(item);
                    if (value is null)
                    {
                        throw new ArgumentException($"Query parameter '{parameterName}' cannot be null.", nameof(queryParameters));
                    }

                    return new RedisQueryParameter(parameterName, value);
                })
                .ToList();

            return true;
        }

        private static void ValidateRequiredParameters(string queryText, List<RedisQueryParameter> parameters)
        {
            var placeholders = Regex.Matches(queryText, @"\$(?<name>[A-Za-z_][A-Za-z0-9_]*)")
                .Cast<Match>()
                .Select(x => x.Groups["name"].Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (!placeholders.Any())
            {
                return;
            }

            var provided = parameters.ToDictionary(x => x.Name, StringComparer.Ordinal);
            foreach (var placeholder in placeholders)
            {
                if (!provided.ContainsKey(placeholder))
                {
                    throw new ArgumentException($"Query parameter '{placeholder}' was not provided.", nameof(queryText));
                }
            }
        }

        private static void ValidateNamedParameters(List<RedisQueryParameter> parameters)
        {
            var seenNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parameter in parameters)
            {
                if (int.TryParse(parameter.Name, out _))
                {
                    throw new ArgumentException("Named query parameter names cannot be numeric.", nameof(parameters));
                }

                if (!seenNames.Add(parameter.Name))
                {
                    throw new ArgumentException($"Query parameter '{parameter.Name}' was provided more than once.", nameof(parameters));
                }
            }
        }
    }
}
