using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Opc.UaFx;
using Opc.UaFx.Server;

namespace OPCUATestServer
{
    internal partial class InfluxHistory<T>
    {
        private class Repository
        {
            private readonly object syncRoot;
            private readonly string measurement;
            private readonly OpcNodeId nodeId;

            public Repository(string measurement, OpcNodeId nodeId)
            {
                this.syncRoot = new object();
                this.measurement = measurement;
                this.nodeId = nodeId;
            }

            public bool Create(T value)
            {
                lock (this.syncRoot)
                {
                    if (!this.Exists(value.Timestamp))
                    {
                        string query = $"{this.measurement},nodeId=\"{this.nodeId.ToString().Replace("=","\\=")}\",statusCode={0} value={value.Value} {((DateTimeOffset)value.Timestamp).ToUnixTimeMilliseconds()}";
                        
                        if (value is OpcModifiedHistoryValue modifiedvalue)
                        {
                            query = $"{this.measurement},nodeId={this.nodeId},statusCode={0},modificationTime={modifiedvalue.ModificationTime},modificationTimeValue={modifiedvalue.ModificationTime.Ticks},modificationType={modifiedvalue.ModificationType},modificationUsername={modifiedvalue.ModificationUserName} value={value.Value} {value.Timestamp.Ticks}";
                        }
                        Console.WriteLine(query);
                        InfluxConnector.InfluxWrite(query);
                        return this.Exists(value.Timestamp);
                    }
                }
                return false;
            }

            public bool Exists(DateTime timestamp)
            {
                lock (this.syncRoot)
                {
                    Console.WriteLine("1");
                    Console.WriteLine(((DateTimeOffset)timestamp).ToUnixTimeSeconds().ToString());
                    Console.WriteLine(((DateTimeOffset)(timestamp.AddSeconds(1))).ToUnixTimeSeconds().ToString());
                    InfluxConnector.InfluxRead(((DateTimeOffset)timestamp).ToUnixTimeSeconds().ToString(), ((DateTimeOffset)timestamp.AddSeconds(1)).ToUnixTimeSeconds().ToString(), this.measurement).Wait();
                    var count = InfluxConnector.GetResults().Count;
                    Console.WriteLine(count == 1);
                    return count == 1;
                }
            }

            public IEnumerable<T> Read(DateTime? startTime, DateTime? endTime)
            {
                lock (this.syncRoot)
                {
                    var startTimeIN = (startTime ?? DateTime.MinValue);
                    var endTimeIN = (endTime ?? DateTime.MaxValue);

                    //Conert datetime to unix seconds
                    var uxStart = ((DateTimeOffset)startTimeIN).ToUnixTimeSeconds().ToString();
                    var uxEnd = ((DateTimeOffset)endTimeIN).ToUnixTimeSeconds().ToString();

                    InfluxConnector.InfluxRead(uxStart, uxEnd, this.measurement).Wait();

                    List<InfluxObject> list = InfluxConnector.GetResults();
                    for (int i = 0; i < list.Count; i++)
                    {
                        yield return CreateValue(list[i]);
                    }
                   
                }
            }


            public bool Update(T value)
            {
                lock(this.syncRoot)
                {
                    string query = $"{this.measurement},nodeId=\"{this.nodeId.ToString().Replace("=", "\\=")}\",statusCode={0} value={value.Value} {((DateTimeOffset)value.Timestamp).ToUnixTimeMilliseconds()}";

                    if (value is OpcModifiedHistoryValue modifiedvalue)
                    {
                        query = $"{this.measurement},nodeId={this.nodeId},statusCode={0},modificationTime={modifiedvalue.ModificationTime},modificationTimeValue={modifiedvalue.ModificationTime.Ticks},modificationType={modifiedvalue.ModificationType},modificationUsername={modifiedvalue.ModificationUserName} value={value.Value} {value.Timestamp.Ticks}";
                    }
                    Console.WriteLine(query);
                    InfluxConnector.InfluxWrite(query);
                    return this.Exists(value.Timestamp);
                }
            }


            private static T CreateValue(InfluxObject item)
            {
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(Int64.Parse(item.Time)).UtcDateTime;
                
                var value = item.Value;
                var statusCode = (OpcStatusCode)Convert.ToInt64(item.StatusCode);
                

                OpcHistoryModificationInfo modificationInfo = null;
                if (IsModifiedHistory)
                {
                    var time = new DateTime(Convert.ToInt64(item.ModificationTimeValue));
                    var type = (OpcHistoryModificationType)Convert.ToInt32(item.ModificationType);
                    var userName = Convert.ToString(item.ModificationUserName);
                    modificationInfo = new OpcHistoryModificationInfo(type, userName, time);
                }

                if (modificationInfo == null)
                {
                    return (T)new OpcHistoryValue(value, timestamp, statusCode);
                }
                return (T)(object)new OpcModifiedHistoryValue(value, timestamp, statusCode, modificationInfo);
            }
        }
    }
}
