using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Searching;
using Redis.OM.Searching.Query;
using Xunit;

namespace Redis.OM.Unit.Tests.RediSearchTests
{
    public class RedisConnectionProviderSearchTests
    {
        private readonly IRedisConnection _connection = Substitute.For<IRedisConnection>();

        private readonly RedisReply _mockReply = new RedisReply[]
        {
            new(1),
            new("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
            new(new RedisReply[]
            {
                "$",
                "{\"Name\":\"Steve\",\"Age\":32,\"Height\":71.0, \"Id\":\"01FVN836BNQGYMT80V7RCVY73N\"}"
            })
        };

        [Fact]
        public async Task SearchAsyncExecutesRawQueryAgainstProvidedIndex()
        {
            _connection.ClearSubstitute();
            _connection.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var provider = new RedisConnectionProvider(_connection);

            var result = await provider.SearchAsync<Person>("person-idx", "@Name:{Steve}");

            await _connection.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "@Name:{Steve}");

            Assert.Equal(1, result.DocumentCount);
            Assert.Single(result.Documents);
            Assert.Equal("Steve", result.Documents.Values.First().Name);
        }

        [Fact]
        public async Task SearchAsyncRoutesThroughRedisQueryOverload()
        {
            var provider = new SpyRedisConnectionProvider(_connection, new SearchResponse<Person>(_mockReply));

            var result = await provider.SearchAsync<Person>("person-idx", "@Name:{Steve}");

            Assert.NotNull(provider.CapturedQuery);
            Assert.Equal("person-idx", provider.CapturedQuery!.Index);
            Assert.Equal("@Name:{Steve}", provider.CapturedQuery.QueryText);
            Assert.Equal(1, result.DocumentCount);
        }

        [Fact]
        public async Task SearchAsyncExecutesParameterizedQueryWithDictionary()
        {
            _connection.ClearSubstitute();
            _connection.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var provider = new RedisConnectionProvider(_connection);

            var result = await provider.SearchAsync<Person>(
                "person-idx",
                "@Name:{$name}",
                new Dictionary<string, string> { ["name"] = "Steve" });

            await _connection.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "@Name:{$name}",
                "PARAMS",
                2,
                "name",
                "Steve",
                "DIALECT",
                2);

            Assert.Single(result.Documents);
        }

        [Fact]
        public async Task SearchAsyncRoutesParameterizedQueriesThroughRedisQueryOverload()
        {
            var provider = new SpyRedisConnectionProvider(_connection, new SearchResponse<Person>(_mockReply));

            var result = await provider.SearchAsync<Person>(
                "person-idx",
                "@Name:{$name}",
                new { name = "Steve" });

            Assert.NotNull(provider.CapturedQuery);
            Assert.Single(provider.CapturedQuery!.NamedParameters);
            Assert.Equal("name", provider.CapturedQuery.NamedParameters[0].Name);
            Assert.Equal("Steve", provider.CapturedQuery.NamedParameters[0].Value);
            Assert.Equal(1, result.DocumentCount);
        }

        [Fact]
        public async Task SearchAsyncThrowsWhenRequiredParameterIsMissing()
        {
            var provider = new RedisConnectionProvider(_connection);

            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                provider.SearchAsync<Person>("person-idx", "@Name:{$name}", new { age = 32 }));

            Assert.Equal("Query parameter 'name' was not provided. (Parameter 'queryText')", exception.Message);
        }

        [Fact]
        public async Task SearchAsyncExecutesRedisQueryBuiltFromRegisteredJsonDocumentType()
        {
            _connection.ClearSubstitute();
            _connection.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(_mockReply);

            var provider = new RedisConnectionProvider(_connection);
            var query = new RedisQuery(typeof(Person)) { QueryText = "@Name:{Steve}" };

            var result = await provider.SearchAsync<Person>(query);

            await _connection.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "@Name:{Steve}");

            Assert.Single(result.Documents);
            Assert.Equal("01FVN836BNQGYMT80V7RCVY73N", result.Documents.Values.First().Id);
            Assert.Equal("Steve", result.Documents.Values.First().Name);
            Assert.Equal(32, result.Documents.Values.First().Age);
        }

        [Fact]
        public async Task SearchAsyncMaterializesRegisteredHashDocumentTypeWithMissingFields()
        {
            var hashReply = new RedisReply[]
            {
                new(1),
                new("Redis.OM.Unit.Tests.RediSearchTests.HashPerson:01FVN836BNQGYMT80V7RCVY73N"),
                new(new RedisReply[]
                {
                    "Id",
                    "01FVN836BNQGYMT80V7RCVY73N",
                    "Name",
                    "Steve",
                }),
            };

            _connection.ClearSubstitute();
            _connection.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(hashReply);

            var provider = new RedisConnectionProvider(_connection);
            var query = new RedisQuery(typeof(HashPerson)) { QueryText = "@Name:{Steve}" };

            var result = await provider.SearchAsync<HashPerson>(query);

            await _connection.Received().ExecuteAsync(
                "FT.SEARCH",
                "hash-person-idx",
                "@Name:{Steve}");

            var document = Assert.Single(result.Documents).Value;
            Assert.Equal("01FVN836BNQGYMT80V7RCVY73N", document.Id);
            Assert.Equal("Steve", document.Name);
            Assert.Null(document.Age);
            Assert.Null(document.Email);
        }

        [Fact]
        public void RedisQueryThrowsForUndecoratedTypes()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => new RedisQuery(typeof(UndecoratedDocument)));

            Assert.Equal("Type 'UndecoratedDocument' must be decorated with a DocumentAttribute to infer a RediSearch index.", exception.Message);
        }

        private sealed class SpyRedisConnectionProvider : RedisConnectionProvider
        {
            private readonly SearchResponse<Person> _response;

            internal SpyRedisConnectionProvider(IRedisConnection connection, SearchResponse<Person> response)
                : base(connection)
            {
                _response = response;
            }

            internal RedisQuery? CapturedQuery { get; private set; }

            public override Task<SearchResponse<T>> SearchAsync<T>(RedisQuery query)
            {
                CapturedQuery = query;
                return Task.FromResult((SearchResponse<T>)(object)_response);
            }
        }

        private sealed class UndecoratedDocument
        {
        }
    }
}
