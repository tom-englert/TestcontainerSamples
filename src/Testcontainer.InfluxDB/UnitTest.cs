using System.Text;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;
using JetBrains.Annotations;

namespace Testcontainer.InfluxDB
{
    [UsesVerify]
    public class UnitTest : InfluxDBTestBase
    {
        [Fact]
        public async Task PointsCanBeWrittenAndQueried()
        {
            var influxDbClient = InfluxDbClient;
            var bucket = DefaultBucket;
            var now = DateTime.UtcNow;

            var points = Enumerable.Range(1, 5)
                .Select(i => PointData.Builder.Measurement("syslog")
                    .Tag("tag1", $@"{i}")
                    .Tag("tag2", $@"Tag.Test.{i + 1}")
                    .Field("field1", $@"Data 1 {i}")
                    .Field("field2", $@"Data 2 {i}")
                    .Timestamp(now + TimeSpan.FromMilliseconds(i), WritePrecision.Ms)
                    .ToPointData())
                .ToList();

            await influxDbClient.GetWriteApiAsync().WritePointsAsync(points, bucket.Name, bucket.OrgID);

            const string query = @"from(bucket: ""Bucket"")
  |> range(start: -1h)
  |> filter(fn: (r) => r[""_measurement""] == ""syslog"")
  |> group(columns: [""_time""])
";
            var result = new List<QueryResult>();

            await influxDbClient.GetQueryApi().QueryAsync(query, record =>
            {
                result.Add(new QueryResult(record));
            });

            //Console.WriteLine($"... Port: {Port}");
            //Console.ReadKey();

            await Verify(result);
        }

        [PublicAPI]
        private class QueryResult
        {
            public QueryResult(FluxRecord record)
            {
                Table = record.Table;
                var values = record.Values;

                Time = ((NodaTime.Instant)values["_time"]).ToDateTimeUtc();
                Field = (string)values["_field"];
                Value = values["_value"];
                Tag1 = (string)values["tag1"];
                Tag2 = (string)values["tag2"];
            }

            public DateTime Time { get; }
            public int Table { get; }
            public string Field { get; }
            public object Value { get; }
            public string Tag1 { get; }
            public string Tag2 { get; }
        }
    }
}