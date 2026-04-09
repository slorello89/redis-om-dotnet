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
        /// Executes a RediSearch aggregation against the supplied index without constructing a <see cref="RedisAggregationSet{T}"/>.
        /// </summary>
        /// <typeparam name="T">The indexed document type used as the aggregation record shell.</typeparam>
        /// <param name="indexName">The RediSearch index name.</param>
        /// <param name="queryText">The RediSearch query string. Defaults to <c>*</c>.</param>
        /// <returns>The materialized aggregation rows.</returns>
        Task<AggregationResult<T>[]> AggregateAsync<T>(string indexName, string queryText = "*")
            where T : notnull;

        /// <summary>
        /// Executes a configured RediSearch aggregation pipeline.
        /// </summary>
        /// <typeparam name="T">The indexed document type used as the aggregation record shell.</typeparam>
        /// <param name="aggregation">The aggregation pipeline to execute.</param>
        /// <returns>The materialized aggregation rows.</returns>
        /// <remarks>
        /// Aggregations return row-oriented <see cref="AggregationResult{T}"/> values rather than <see cref="SearchResponse{T}"/>.
        /// Read computed values from <see cref="AggregationResult{T}.Aggregations"/> or call <see cref="AggregationResult{T}.Hydrate"/>
        /// only when the pipeline loaded the fields needed for the document type.
        /// </remarks>
        /// <example>
        /// <code>
        /// var aggregation = new RedisAggregation("person-idx")
        /// {
        ///     RawQuery = "@Department:{Engineering}",
        /// };
        ///
        /// aggregation.Predicates.Push(new ZeroArgumentReduction(ReduceFunction.COUNT));
        /// aggregation.Predicates.Push(new GroupBy(new[] { "Department" }));
        ///
        /// var rows = await provider.AggregateAsync&lt;Person&gt;(aggregation);
        /// var count = rows[0]["COUNT"];
        /// </code>
        /// </example>
        Task<AggregationResult<T>[]> AggregateAsync<T>(RedisAggregation aggregation)
            where T : notnull;

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
        /// Use <see cref="SearchProjection"/> for lightweight ad hoc projections when you want dictionary-style
        /// access to returned fields without defining a dedicated DTO.
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
        ///
        /// var rows = await provider.SearchAsync&lt;SearchProjection&gt;(
        ///     "person-idx",
        ///     "@Name:{Steve}",
        ///     new ReturnFields(new[]
        ///     {
        ///         new ReturnField("Name", "DisplayName"),
        ///         new ReturnField("Age", "YearsOld"),
        ///     }));
        ///
        /// var first = rows.Documents.Values.First();
        /// var displayName = first["DisplayName"];
        /// var age = first.GetValue&lt;int&gt;("YearsOld");
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
        /// Use <see cref="SearchProjection"/> for lightweight ad hoc projections when you want dictionary-style
        /// access to returned fields without defining a dedicated DTO.
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
        ///
        /// var rows = await provider.SearchAsync&lt;SearchProjection&gt;(
        ///     "person-idx",
        ///     "@Name:{$name}",
        ///     new { name = "Steve" },
        ///     new ReturnFields(new[]
        ///     {
        ///         new ReturnField("Name", "DisplayName"),
        ///         new ReturnField("Age", "YearsOld"),
        ///     }));
        ///
        /// var first = rows.Documents.Values.First();
        /// var displayName = first["DisplayName"];
        /// var age = first.GetValue&lt;int&gt;("YearsOld");
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
