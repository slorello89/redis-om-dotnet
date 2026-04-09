using Redis.OM;
using Redis.OM.Aggregation;
using Redis.OM.Aggregation.AggregationPredicates;
using Redis.OM.Contracts;
using Redis.OM.Modeling;
using Redis.OM.Searching;
using Redis.OM.Searching.Query;

var redisUrl = Environment.GetEnvironmentVariable("REDIS_OM_EXAMPLE_REDIS_URL") ?? "redis://localhost:6379";
var provider = new RedisConnectionProvider(redisUrl);
var connection = provider.Connection;
var collection = provider.RedisCollection<DapperPerson>();

var insertedPeople = new List<DapperPerson>();
var runId = $"dapperexample{Guid.NewGuid():N}";

Console.WriteLine($"Connecting to {redisUrl}");
Console.WriteLine($"Using run id: {runId}");

try
{
    ResetIndex(connection);
    SeedPeople(collection, insertedPeople, runId);
    await WaitForIndexedDocumentsAsync(provider, runId, insertedPeople.Count);

    await RunBasicSearchExampleAsync(provider, runId);
    await RunParameterizedProjectionExampleAsync(provider, runId);
    await RunAggregationExampleAsync(provider, runId);

    Console.WriteLine("Dapper interoperability example completed successfully.");
}
finally
{
    Cleanup(collection, insertedPeople);
}

static void ResetIndex(IRedisConnection connection)
{
    try
    {
        connection.DropIndex(typeof(DapperPerson));
    }
    catch
    {
        // Ignore missing-index failures so the example can be rerun locally.
    }

    connection.CreateIndex(typeof(DapperPerson));
}

static void SeedPeople(IRedisCollection<DapperPerson> collection, List<DapperPerson> insertedPeople, string runId)
{
    insertedPeople.AddRange(new[]
    {
        new DapperPerson
        {
            Id = Guid.NewGuid().ToString("N"),
            RunId = runId,
            FirstName = "Ada",
            LastName = "Lovelace",
            Department = "Engineering",
            YearsOfExperience = 11,
            Skills = new[] { "csharp", "redis", "design" },
        },
        new DapperPerson
        {
            Id = Guid.NewGuid().ToString("N"),
            RunId = runId,
            FirstName = "Grace",
            LastName = "Hopper",
            Department = "Engineering",
            YearsOfExperience = 15,
            Skills = new[] { "redis", "compiler", "navy" },
        },
        new DapperPerson
        {
            Id = Guid.NewGuid().ToString("N"),
            RunId = runId,
            FirstName = "Margaret",
            LastName = "Hamilton",
            Department = "Engineering",
            YearsOfExperience = 9,
            Skills = new[] { "apollo", "redis", "testing" },
        },
        new DapperPerson
        {
            Id = Guid.NewGuid().ToString("N"),
            RunId = runId,
            FirstName = "Katherine",
            LastName = "Johnson",
            Department = "Research",
            YearsOfExperience = 12,
            Skills = new[] { "math", "analysis" },
        },
    });

    foreach (var person in insertedPeople)
    {
        collection.Insert(person);
    }
}

static async Task WaitForIndexedDocumentsAsync(RedisConnectionProvider provider, string runId, int expectedCount)
{
    const int attempts = 20;
    for (var attempt = 0; attempt < attempts; attempt++)
    {
        var response = await provider.SearchAsync<DapperPerson>(
            "dapper-person-idx",
            "@RunId:{$runId}",
            new { runId });

        if (response.DocumentCount >= expectedCount)
        {
            return;
        }

        await Task.Delay(250);
    }

    throw new InvalidOperationException("Timed out waiting for the example documents to be indexed.");
}

static async Task RunBasicSearchExampleAsync(RedisConnectionProvider provider, string runId)
{
    Console.WriteLine();
    Console.WriteLine("=== Basic search example ===");

    var response = await provider.SearchAsync<DapperPerson>(
        "dapper-person-idx",
        $"@RunId:{{{runId}}} @Department:{{Engineering}} @YearsOfExperience:[10 +inf]");

    Ensure(response.DocumentCount == 2, $"Expected 2 senior engineers, found {response.DocumentCount}.");

    foreach (var person in response.Documents.Values.OrderBy(x => x.LastName))
    {
        Console.WriteLine($"{person.FirstName} {person.LastName} ({person.YearsOfExperience} years)");
    }
}

static async Task RunParameterizedProjectionExampleAsync(RedisConnectionProvider provider, string runId)
{
    Console.WriteLine();
    Console.WriteLine("=== Parameterized projection example ===");

    var returnFields = new ReturnFields(new[]
    {
        new ReturnField("FirstName", "GivenName"),
        new ReturnField("LastName", "Surname"),
        new ReturnField("YearsOfExperience", "ExperienceYears"),
    });

    var projectionResponse = await provider.SearchAsync<EngineerProjection>(
        "dapper-person-idx",
        "@RunId:{$runId} @Department:{$department}",
        new { runId, department = "Engineering" },
        returnFields);

    Ensure(projectionResponse.DocumentCount == 3, $"Expected 3 projected engineers, found {projectionResponse.DocumentCount}.");

    foreach (var row in projectionResponse.Documents.Values.OrderBy(x => x.Surname))
    {
        Console.WriteLine($"{row.GivenName} {row.Surname} ({row.ExperienceYears} years)");
    }

    var adHocProjectionResponse = await provider.SearchAsync<SearchProjection>(
        "dapper-person-idx",
        "@RunId:{$runId} @Department:{$department}",
        new { runId, department = "Engineering" },
        returnFields);

    var first = adHocProjectionResponse.Documents.Values.FirstOrDefault()
        ?? throw new InvalidOperationException("Expected at least one ad hoc projection row.");

    Ensure(first.ContainsKey("GivenName"), "Ad hoc projection did not contain the GivenName alias.");
    Ensure(first.GetValue<int>("ExperienceYears") >= 9, "Projected ExperienceYears value did not convert correctly.");

    Console.WriteLine($"Anonymous-like projection sample: {first["GivenName"]} {first["Surname"]}");
}

static async Task RunAggregationExampleAsync(RedisConnectionProvider provider, string runId)
{
    Console.WriteLine();
    Console.WriteLine("=== Aggregation example ===");

    var aggregation = new RedisAggregation("dapper-person-idx")
    {
        RawQuery = $"@RunId:{{{runId}}}",
    };

    aggregation.Predicates.Push(new ZeroArgumentReduction(ReduceFunction.COUNT));
    aggregation.Predicates.Push(new GroupBy(new[] { "Department" }));

    var rows = await provider.AggregateAsync<DapperPerson>(aggregation);
    Ensure(rows.Length == 2, $"Expected 2 aggregation rows, found {rows.Length}.");

    var summaries = rows
        .Select(x => x.Hydrate<DepartmentCount>())
        .OrderByDescending(x => x.TotalCount)
        .ThenBy(x => x.DepartmentName)
        .ToList();

    Ensure(summaries[0].DepartmentName == "Engineering" && summaries[0].TotalCount == 3,
        "Engineering aggregation count did not match the seeded data.");

    foreach (var summary in summaries)
    {
        Console.WriteLine($"{summary.DepartmentName}: {summary.TotalCount}");
    }
}

static void Cleanup(IRedisCollection<DapperPerson> collection, List<DapperPerson> insertedPeople)
{
    if (insertedPeople.Count == 0)
    {
        return;
    }

    try
    {
        collection.Delete(insertedPeople);
    }
    catch
    {
        // Cleanup should not mask a more useful example failure.
    }
}

static void Ensure(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

[Document(StorageType = StorageType.Json, IndexName = "dapper-person-idx", Prefixes = new[] { "DapperExamplePerson" })]
public class DapperPerson
{
    [RedisIdField]
    [Indexed]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    public string RunId { get; set; } = string.Empty;

    [Indexed]
    public string FirstName { get; set; } = string.Empty;

    [Indexed]
    public string LastName { get; set; } = string.Empty;

    [Indexed]
    public string Department { get; set; } = string.Empty;

    [Indexed(Sortable = true)]
    public int YearsOfExperience { get; set; }

    [Indexed]
    public string[] Skills { get; set; } = Array.Empty<string>();
}

public class EngineerProjection
{
    public string GivenName { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public int ExperienceYears { get; set; }
}

public class DepartmentCount
{
    [RedisField(PropertyName = "Department")]
    public string DepartmentName { get; set; } = string.Empty;

    [RedisField(PropertyName = "COUNT")]
    public int TotalCount { get; set; }
}
