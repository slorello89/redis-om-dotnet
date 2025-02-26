﻿using StackExchange.Redis;
using System;
using Xunit;

namespace Redis.OM.Unit.Tests
{
    public class ConnectionTests
    {
        private string STANDALONE_CONNECTION_STRING = "redis://localhost:6379";
        private string SENTINEL_CONNECTION_STRING = "redis://localhost:26379?sentinel_primary_name=redismaster";
        
        [Fact]
        public void TestConnectStandalone()
        {
            var hostInfo = System.Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost:6379";
            Console.WriteLine($"Current host info: {hostInfo}");
            var standaloneConnecitonString = $"redis://{hostInfo}";
            var provider = new RedisConnectionProvider(standaloneConnecitonString);
            
            var connection = provider.Connection;
            connection.Execute("SET", "Foo", "Bar");
            var res = connection.Execute("GET", "Foo");
            Assert.Equal("Bar",res);
        }

        [Fact]
        public void TestSentinel()
        {
            var hostInfo = System.Environment.GetEnvironmentVariable("SENTINLE_HOST_PORT") ?? "localhost:26379";
            Console.WriteLine($"Current host info: {hostInfo}");
            var connectionString = $"redis://{hostInfo}?sentinel_primary_name=redismaster";
            var provider = new RedisConnectionProvider(connectionString);
            var connection = provider.Connection;
            connection.Execute("SET", "Foo", "Bar");
            var res = connection.Execute("GET", "Foo");
            Assert.Equal("Bar", res);
            
        }
        
        [Fact]
        public void TestCluster()
        {
            var hostInfo = System.Environment.GetEnvironmentVariable("CLUSTER_HOST_PORT") ?? "localhost:6379";
            Console.WriteLine($"Current host info: {hostInfo}");
            var connectionString = $"redis://{hostInfo}";
            var provider = new RedisConnectionProvider(connectionString);
            var connection = provider.Connection;
            connection.Execute("SET", "Foo", "Bar");
            var res = connection.Execute("GET", "Foo");
            Assert.Equal("Bar",res);
        }

        [Fact]
        public void GivenMultiplexerConnection_WhenTestingSetCommand_ThenShouldExecuteSetCommandSuccessfully()
        {
            var hostInfo = Environment.GetEnvironmentVariable("STANDALONE_HOST_PORT") ?? "localhost:6379";
            Console.WriteLine($"Current host info: {hostInfo}");
            var multiplexer = ConnectionMultiplexer.Connect(hostInfo);
            var provider = new RedisConnectionProvider(multiplexer);

            var connection = provider.Connection;
            connection.Execute("SET", "Foo", "Bar");
            var res = connection.Execute("GET", "Foo");
            Assert.Equal("Bar", res);
        }
    }
}