using System.Runtime.CompilerServices;
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
            where T : notnull => Connection.SearchAsync<T>(new RedisQuery(indexName) { QueryText = queryText ?? "*" });

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
    }
}
