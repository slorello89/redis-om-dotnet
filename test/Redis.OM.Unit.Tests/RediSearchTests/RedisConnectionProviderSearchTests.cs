using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Redis.OM.Aggregation;
using Redis.OM.Aggregation.AggregationPredicates;
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
        public async Task SearchAsyncExecutesProjectionQueryWithSelectedFields()
        {
            var projectionReply = new RedisReply[]
            {
                new(1),
                new("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
                new(new RedisReply[]
                {
                    "Name",
                    "Steve",
                }),
            };

            _connection.ClearSubstitute();
            _connection.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(projectionReply);

            var provider = new RedisConnectionProvider(_connection);

            var result = await provider.SearchAsync<string>(
                "person-idx",
                "@Name:{Steve}",
                new ReturnFields(new[] { "Name" }));

            await _connection.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "@Name:{Steve}",
                "RETURN",
                "1",
                "Name");

            Assert.Single(result.Documents);
            Assert.Equal("Steve", result.Documents.Values.First());
        }

        [Fact]
        public async Task SearchAsyncRoutesProjectionQueriesThroughRedisQueryOverload()
        {
            var provider = new SpyRedisConnectionProvider(_connection, new SearchResponse<Person>(_mockReply));

            var result = await provider.SearchAsync<Person>(
                "person-idx",
                "@Name:{Steve}",
                new ReturnFields(new[] { "Name", "Age" }));

            Assert.NotNull(provider.CapturedQuery);
            Assert.NotNull(provider.CapturedQuery!.Return);
            Assert.Equal(new[] { "RETURN", "2", "Name", "Age" }, provider.CapturedQuery.Return.SerializeArgs);
            Assert.Equal(1, result.DocumentCount);
        }

        [Fact]
        public async Task AggregateAsyncExecutesAggregationAgainstProvidedIndex()
        {
            var aggregationReply = new RedisReply[]
            {
                new(1),
                new(new RedisReply[]
                {
                    "Department",
                    "Engineering",
                    "COUNT",
                    "3",
                }),
            };

            _connection.ClearSubstitute();
            _connection.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(aggregationReply);

            var provider = new RedisConnectionProvider(_connection);
            var aggregation = new RedisAggregation("person-idx")
            {
                RawQuery = "@Department:{Engineering}",
            };

            aggregation.Predicates.Push(new ZeroArgumentReduction(ReduceFunction.COUNT));
            aggregation.Predicates.Push(new GroupBy(new[] { "Department" }));

            var result = await provider.AggregateAsync<Person>(aggregation);

            await _connection.Received().ExecuteAsync(
                "FT.AGGREGATE",
                "person-idx",
                "@Department:{Engineering}",
                "GROUPBY",
                "1",
                "@Department",
                "REDUCE",
                "COUNT",
                "0",
                "AS",
                "COUNT");

            var row = Assert.Single(result);
            Assert.Equal("Engineering", row["Department"].ToString());
            Assert.Equal("3", row["COUNT"].ToString());
        }

        [Fact]
        public async Task AggregateAsyncRoutesStringOverloadThroughRedisAggregationOverload()
        {
            var provider = new SpyRedisConnectionProvider(_connection, new SearchResponse<Person>(_mockReply))
            {
                AggregationResponse = new[] { CreateAggregationResultReply("COUNT", "3") },
            };

            var result = await provider.AggregateAsync<Person>("person-idx", "@Department:{Engineering}");

            Assert.NotNull(provider.CapturedAggregation);
            Assert.Equal("person-idx", provider.CapturedAggregation!.IndexName);
            Assert.Equal("@Department:{Engineering}", provider.CapturedAggregation.RawQuery);
            Assert.Single(result);
            Assert.Equal("3", result[0]["COUNT"].ToString());
        }

        [Fact]
        public async Task AggregateAsyncMaterializesAggregationRowsIntoTypedDto()
        {
            var aggregationReply = new RedisReply[]
            {
                new(1),
                new(new RedisReply[]
                {
                    "DepartmentName",
                    "Engineering",
                    "AverageAge",
                    "32.5",
                }),
            };

            _connection.ClearSubstitute();
            _connection.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(aggregationReply);

            var provider = new RedisConnectionProvider(_connection);
            var result = await provider.AggregateAsync<Person>("person-idx", "@Department:{Engineering}");

            var row = Assert.Single(result);
            var summary = row.Hydrate<DepartmentSummary>();

            Assert.Equal("Engineering", summary.DepartmentName);
            Assert.Equal(32.5d, summary.AverageAge);
        }

        [Fact]
        public async Task AggregateAsyncMaterializesAggregationRowsIntoTypedDtoUsingRedisFieldAttribute()
        {
            var aggregationReply = new RedisReply[]
            {
                new(1),
                new(new RedisReply[]
                {
                    "Department",
                    "Engineering",
                    "COUNT",
                    "3",
                }),
            };

            _connection.ClearSubstitute();
            _connection.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(aggregationReply);

            var provider = new RedisConnectionProvider(_connection);
            var aggregation = new RedisAggregation("person-idx")
            {
                RawQuery = "@Department:{Engineering}",
            };

            aggregation.Predicates.Push(new ZeroArgumentReduction(ReduceFunction.COUNT));
            aggregation.Predicates.Push(new GroupBy(new[] { "Department" }));

            var result = await provider.AggregateAsync<Person>(aggregation);

            var row = Assert.Single(result);
            var summary = row.Hydrate<DepartmentCountProjection>();

            Assert.Equal("Engineering", summary.DepartmentName);
            Assert.Equal(3, summary.TotalCount);
            Assert.Null(summary.MissingField);
        }

        [Fact]
        public async Task SearchAsyncExecutesParameterizedProjectionQueryWithSelectedFields()
        {
            var projectionReply = new RedisReply[]
            {
                new(1),
                new("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
                new(new RedisReply[]
                {
                    "Name",
                    "Steve",
                }),
            };

            _connection.ClearSubstitute();
            _connection.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(projectionReply);

            var provider = new RedisConnectionProvider(_connection);

            var result = await provider.SearchAsync<string>(
                "person-idx",
                "@Name:{$name}",
                new { name = "Steve" },
                new ReturnFields(new[] { "Name" }));

            await _connection.Received().ExecuteAsync(
                "FT.SEARCH",
                "person-idx",
                "@Name:{$name}",
                "PARAMS",
                2,
                "name",
                "Steve",
                "DIALECT",
                2,
                "RETURN",
                "1",
                "Name");

            Assert.Single(result.Documents);
            Assert.Equal("Steve", result.Documents.Values.First());
        }

        [Fact]
        public async Task SearchAsyncMaterializesProjectedFieldsIntoTypedDto()
        {
            var projectionReply = new RedisReply[]
            {
                new(1),
                new("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
                new(new RedisReply[]
                {
                    "DisplayName",
                    "Steve",
                    "YearsOld",
                    "32",
                }),
            };

            _connection.ClearSubstitute();
            _connection.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(projectionReply);

            var provider = new RedisConnectionProvider(_connection);

            var result = await provider.SearchAsync<PersonProjection>(
                "person-idx",
                "@Name:{Steve}",
                new ReturnFields(new[]
                {
                    new ReturnField("Name", "DisplayName"),
                    new ReturnField("Age", "YearsOld"),
                }));

            var document = Assert.Single(result.Documents).Value;
            Assert.Equal("Steve", document.DisplayName);
            Assert.Equal(32, document.YearsOld);
            Assert.Null(document.UnrelatedField);
        }

        [Fact]
        public async Task SearchAsyncMaterializesProjectedFieldsIntoTypedDtoUsingRedisFieldAttribute()
        {
            var projectionReply = new RedisReply[]
            {
                new(1),
                new("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
                new(new RedisReply[]
                {
                    "Name",
                    "Steve",
                }),
            };

            _connection.ClearSubstitute();
            _connection.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(projectionReply);

            var provider = new RedisConnectionProvider(_connection);

            var result = await provider.SearchAsync<PersonNameOnlyProjection>(
                "person-idx",
                "@Name:{Steve}",
                new ReturnFields(new[] { "Name" }));

            var document = Assert.Single(result.Documents).Value;
            Assert.Equal("Steve", document.DisplayName);
            Assert.Null(document.MissingField);
        }

        [Fact]
        public async Task SearchAsyncMaterializesProjectedFieldsIntoSearchProjection()
        {
            var projectionReply = new RedisReply[]
            {
                new(1),
                new("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
                new(new RedisReply[]
                {
                    "DisplayName",
                    "Steve",
                    "YearsOld",
                    "32",
                }),
            };

            _connection.ClearSubstitute();
            _connection.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(projectionReply);

            var provider = new RedisConnectionProvider(_connection);

            var result = await provider.SearchAsync<SearchProjection>(
                "person-idx",
                "@Name:{Steve}",
                new ReturnFields(new[]
                {
                    new ReturnField("Name", "DisplayName"),
                    new ReturnField("Age", "YearsOld"),
                }));

            var row = Assert.Single(result.Documents).Value;
            Assert.Equal("Steve", row["DisplayName"]);
            Assert.True(row.ContainsKey("displayname"));
            Assert.True(row.TryGetValue("YearsOld", out var yearsOld));
            Assert.Equal("32", yearsOld);
            Assert.Equal(32, row.GetValue<int>("YearsOld"));
        }

        [Fact]
        public async Task SearchAsyncMaterializesParameterizedProjectionIntoSearchProjection()
        {
            var projectionReply = new RedisReply[]
            {
                new(1),
                new("Redis.OM.Unit.Tests.RediSearchTests.Person:01FVN836BNQGYMT80V7RCVY73N"),
                new(new RedisReply[]
                {
                    "Name",
                    "Steve",
                }),
            };

            _connection.ClearSubstitute();
            _connection.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(projectionReply);

            var provider = new RedisConnectionProvider(_connection);

            var result = await provider.SearchAsync<SearchProjection>(
                "person-idx",
                "@Name:{$name}",
                new { name = "Steve" },
                new ReturnFields(new[] { "Name" }));

            var row = Assert.Single(result.Documents).Value;
            Assert.Equal("Steve", row["Name"]);
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

            internal AggregationResult<Person>[] AggregationResponse { get; init; } = Array.Empty<AggregationResult<Person>>();

            internal RedisAggregation? CapturedAggregation { get; private set; }

            public override Task<SearchResponse<T>> SearchAsync<T>(RedisQuery query)
            {
                CapturedQuery = query;
                return Task.FromResult((SearchResponse<T>)(object)_response);
            }

            public override Task<AggregationResult<T>[]> AggregateAsync<T>(RedisAggregation aggregation)
            {
                CapturedAggregation = aggregation;
                return Task.FromResult((AggregationResult<T>[])(object)AggregationResponse);
            }
        }

        private sealed class UndecoratedDocument
        {
        }

        private sealed class PersonProjection
        {
            public string? DisplayName { get; set; }

            public int? YearsOld { get; set; }

            public string? UnrelatedField { get; set; }
        }

        private sealed class PersonNameOnlyProjection
        {
            [RedisField(PropertyName = "Name")]
            public string? DisplayName { get; set; }

            public string? MissingField { get; set; }
        }

        private sealed class DepartmentSummary
        {
            public string DepartmentName { get; set; }

            public double AverageAge { get; set; }
        }

        private sealed class DepartmentCountProjection
        {
            [RedisField(PropertyName = "Department")]
            public string? DepartmentName { get; set; }

            [RedisField(PropertyName = "COUNT")]
            public int TotalCount { get; set; }

            public string? MissingField { get; set; }
        }

        private static AggregationResult<Person> CreateAggregationResultReply(string key, string value)
        {
            var reply = new RedisReply[]
            {
                new(1),
                new(new RedisReply[]
                {
                    key,
                    value,
                }),
            };

            return AggregationResult<Person>.FromRedisResult(reply).Single();
        }
    }
}
