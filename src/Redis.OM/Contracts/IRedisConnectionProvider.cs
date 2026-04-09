using System.Threading.Tasks;
using Redis.OM.Aggregation;
using Redis.OM.Searching;
using Redis.OM.Searching.Query;

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
        Task<SearchResponse<T>> SearchAsync<T>(string indexName, string queryText, object queryParameters)
            where T : notnull;

        /// <summary>
        /// Executes a RediSearch query against the supplied index and limits the returned fields for projection scenarios.
        /// </summary>
        /// <typeparam name="T">The materialized result type.</typeparam>
        /// <param name="indexName">The RediSearch index name.</param>
        /// <param name="queryText">The RediSearch query string.</param>
        /// <param name="returnFields">The fields to request from RediSearch.</param>
        /// <returns>A typed search response.</returns>
        /// <remarks>
        /// Projection DTOs bind returned field names to properties on <typeparamref name="T"/>.
        /// Use <see cref="ReturnField"/> aliases or <see cref="Redis.OM.Modeling.RedisFieldAttribute"/>
        /// when the returned field name should map to a different property name.
        /// </remarks>
        /// <example>
        /// <code>
        /// var results = await provider.SearchAsync&lt;PersonNameProjection&gt;(
        ///     "person-idx",
        ///     "@Name:{Steve}",
        ///     new ReturnFields(new[]
        ///     {
        ///         new ReturnField("Name", "DisplayName"),
        ///         new ReturnField("Age", "YearsOld"),
        ///     }));
        /// </code>
        /// </example>
        Task<SearchResponse<T>> SearchAsync<T>(string indexName, string queryText, ReturnFields returnFields)
            where T : notnull;

        /// <summary>
        /// Executes a parameterized RediSearch query against the supplied index and limits the returned fields for projection scenarios.
        /// </summary>
        /// <typeparam name="T">The materialized result type.</typeparam>
        /// <param name="indexName">The RediSearch index name.</param>
        /// <param name="queryText">The RediSearch query string.</param>
        /// <param name="queryParameters">An anonymous object or dictionary containing named query parameters.</param>
        /// <param name="returnFields">The fields to request from RediSearch.</param>
        /// <returns>A typed search response.</returns>
        /// <remarks>
        /// Projection DTOs bind returned field names to properties on <typeparamref name="T"/>.
        /// Use <see cref="ReturnField"/> aliases or <see cref="Redis.OM.Modeling.RedisFieldAttribute"/>
        /// when the returned field name should map to a different property name.
        /// </remarks>
        /// <example>
        /// <code>
        /// var results = await provider.SearchAsync&lt;PersonNameProjection&gt;(
        ///     "person-idx",
        ///     "@Name:{$name}",
        ///     new { name = "Steve" },
        ///     new ReturnFields(new[]
        ///     {
        ///         new ReturnField("Name", "DisplayName"),
        ///         new ReturnField("Age", "YearsOld"),
        ///     }));
        /// </code>
        /// </example>
        Task<SearchResponse<T>> SearchAsync<T>(string indexName, string queryText, object queryParameters, ReturnFields returnFields)
            where T : notnull;

        /// <summary>
        /// Executes a RediSearch query that has already been configured through <see cref="RedisQuery"/>.
        /// </summary>
        /// <typeparam name="T">The materialized result type.</typeparam>
        /// <param name="query">The RediSearch query to execute.</param>
        /// <returns>A typed search response.</returns>
        Task<SearchResponse<T>> SearchAsync<T>(RedisQuery query)
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
