using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using JetBrains.Annotations;

namespace Testcontainer.InfluxDB
{
    [PublicAPI]
    public abstract class InfluxDBTestBase : IAsyncLifetime
    {
        protected const string AdminToken = "my-super-secret-admin-token";

        protected TestcontainersContainer TestContainer { get; private set; } = null!;
        protected InfluxDBClient InfluxDbClient { get; private set; } = null!;
        protected Bucket DefaultBucket { get; private set; } = null!;
        protected ushort Port { get; private set; }

        async Task IAsyncLifetime.InitializeAsync()
        {
            var environment = new Dictionary<string, string>
            {
                { "DOCKER_INFLUXDB_INIT_MODE", "setup" },
                { "DOCKER_INFLUXDB_INIT_USERNAME" , "User" },
                { "DOCKER_INFLUXDB_INIT_PASSWORD", "Password"},
                { "DOCKER_INFLUXDB_INIT_ORG", "Org" },
                { "DOCKER_INFLUXDB_INIT_BUCKET", "Bucket" },
                { "DOCKER_INFLUXDB_INIT_ADMIN_TOKEN", AdminToken }
            };

            TestContainer = new TestcontainersBuilder<TestcontainersContainer>()
                .WithName(Guid.NewGuid().ToString())
                // .WithPortBinding(8086, true)
                .WithPortBinding(49227, 8086)
                .WithEnvironment(environment)
                .WithImage("influxdb:latest")
                .Build();

            await TestContainer.StartAsync();
            await TestContainer.ExecAsync(new[] { "influx" });

            Port = TestContainer.GetMappedPublicPort(8086);

            var address = $"http://localhost:{Port}";

            var optionsBuilder = InfluxDBClientOptions.Builder.CreateNew()
                .Url(address)
                .Org("Org")
                .AuthenticateToken(AdminToken);

            InfluxDbClient = new InfluxDBClient(optionsBuilder.Build());

            for (int i = 0; i < 100; i++)
            {
                var alive = await InfluxDbClient.PingAsync();
                if (alive)
                    break;
                await Task.Delay(100);
            }

            var bucketsApi = InfluxDbClient.GetBucketsApi();

            DefaultBucket = await bucketsApi.FindBucketByNameAsync("Bucket");

            Assert.NotNull(DefaultBucket);
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            InfluxDbClient.Dispose();
            await TestContainer.DisposeAsync();
        }
    }
}
