using System.Text;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace Testcontainer.InfluxDB
{
    [UsesVerify]
    public class UnitTest : InfluxDBTestBase
    {
        [Fact]
        public async Task PointsCanBeWrittenAndQueried()
        {
            var influxDbClient = InfluxDBClient;
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
    }
}