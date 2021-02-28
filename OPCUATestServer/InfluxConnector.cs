using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using InfluxDB.Client.Writes;
using Task = System.Threading.Tasks.Task;

namespace OPCUATestServer
{
    class InfluxConnector
    {
        private static InfluxDBClient influxDBClient = null;
        private static readonly char[] Token = "Ph687w0QCY0fXRBFoCDH1PU_sFjau22NmWdwlofxPKEi9Yv851X03TCsTDm3Y_j10Wkg-Jzg5Iuyrn-K8YIQuA==".ToCharArray();
        private static List<InfluxObject> list = null;

        private InfluxConnector()
        {
        }

        public static void InitializeClient()
        {
            if (influxDBClient == null)
            {
                influxDBClient = InfluxDBClientFactory.Create("http://localhost:4081", Token);
            }
        }

        public static void InfluxWrite(string query)
        {
            using (var writeApi = influxDBClient.GetWriteApi())
            {
                writeApi.WriteRecord("mainbucket", "emerson", WritePrecision.Ms, query);
            }
        }

        public static async Task InfluxRead()
        {
            if (list == null)
            {
                list = new List<InfluxObject>();
            }
            else
            {
                list.Clear();
            }

            var flux = "from(bucket:\"mainbucket\") |> range(start: -48h)";

            var fluxTables = await influxDBClient.GetQueryApi().QueryAsync(flux, "emerson");
            fluxTables.ForEach(fluxTable =>
            {
                var fluxRecords = fluxTable.Records;
                fluxRecords.ForEach(fluxRecord =>
                {
                    list.Add(new InfluxObject() { Time = fluxRecord.GetTime().ToString(), Value = fluxRecord.GetValue().ToString() });
                    //Console.WriteLine($"{fluxRecord.GetTime()}: {fluxRecord.GetValue()}");
                });
            });
        }
        public static async Task InfluxRead(string timeStart, string timeEnd, string measurement)
        {
            if (list == null)
            {
                list = new List<InfluxObject>();
            }
            else
            {
                list.Clear();
            }

            var flux = $"from(bucket:\"mainbucket\") |> range(start: { timeStart }, stop: { timeEnd }) |> filter(fn: (r) => r._measurement==\"{ measurement }\")";
            Console.WriteLine(flux);
            var fluxTables = await influxDBClient.GetQueryApi().QueryAsync(flux, "emerson");

            fluxTables.ForEach(fluxTable =>
            {
                var fluxRecords = fluxTable.Records;
                fluxRecords.ForEach(fluxRecord =>
                {
                    list.Add(new InfluxObject()
                    {
                        Time = fluxRecord.GetTime().Value.ToUnixTimeMilliseconds().ToString(),
                        Value = fluxRecord.GetValue().ToString(),
                        StatusCode = (fluxRecord.GetValueByKey("statusCode") ?? "").ToString(),
                        ModificationTimeValue = (fluxRecord.GetValueByKey("modificationTimeValue") ?? "").ToString(),
                        ModificationType = (fluxRecord.GetValueByKey("modificationType") ?? "").ToString(),
                        ModificationUserName = (fluxRecord.GetValueByKey("modificationUsername") ?? "").ToString()
                    });
                });
            });
        }


        public static List<InfluxObject> GetResults()
        {
            return list;
        }

        public static void DisposeClient()
        {
            influxDBClient.Dispose();
        }
    }

    class InfluxObject
    {
        public string Time { get; set; }
        public string Value { get; set; }
        public string StatusCode { get; set; }
        public string ModificationTimeValue { get; set; }
        public string ModificationType { get; set; }
        public string ModificationUserName { get; set; }
    }
}
