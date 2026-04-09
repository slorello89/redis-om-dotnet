using System.Threading.Tasks;
using Redis.OM.Aggregation;
using Redis.OM.Searching;

namespace Redis.OM.Contracts
{
    /// <summary>
    /// Provides a connection to redis.
    /// </summary>
    public interface IRedisConnectionProvider
    {
        /// <summary>
        /// Gets a command level interface to redis.
        /// </summary>
        IRedisConnection Connection { get; }

        /// <summary>
        /// Gets an aggregation set for redis.
        /// </summary>
        /// <typeparam name="T">The indexed type to run aggregations on.</typeparam>
        /// <param name="chunkSize">Size of chunks to use during pagination, larger chunks = larger payloads returned but fewer round trips.</param>
        /// <returns>the aggregation set.</returns>
        RedisAggregationSet<T> AggregationSet<T>(int chunkSize = 100);

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
        Task<SearchResponse<T>> SearchAsync<T>(string indexName, string queryText = "*")
            where T : notnull;

        /// <summary>
        /// Gets a redis collection.
        /// </summary>
        /// <typeparam name="T">The type the collection will be retrieving.</typeparam>
        /// <param name="chunkSize">Size of chunks to use during pagination, larger chunks = larger payloads returned but fewer round trips.</param>
        /// <returns>A RedisCollection.</returns>
        IRedisCollection<T> RedisCollection<T>(int chunkSize = 100)
          where T : notnull;

        /// <summary>
        /// Gets a redis collection.
        /// </summary>
        /// <typeparam name="T">The type the collection will be retrieving.</typeparam>
        /// <param name="saveState">Whether or not the RedisCollection should maintain the state of documents it enumerates.</param>
        /// <param name="chunkSize">Size of chunks to use during pagination, larger chunks = larger payloads returned but fewer round trips.</param>
        /// <returns>A RedisCollection.</returns>
        IRedisCollection<T> RedisCollection<T>(bool saveState, int chunkSize = 100)
          where T : notnull;
    }
}
