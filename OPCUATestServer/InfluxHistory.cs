using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.UaFx;
using Opc.UaFx.Server;

namespace OPCUATestServer
{
    internal partial class InfluxHistory<T> where T : OpcHistoryValue
    {
        private readonly Repository repository;

        private InfluxHistory(Repository repository) : base()
        {
            this.repository = repository;
        }

        private static bool IsModifiedHistory
        {
            get => typeof(T) == typeof(OpcModifiedHistoryValue);
        }

        public static InfluxHistory<T> Create(string measurement, OpcNodeId nodeId)
        {
            if (typeof(T) != typeof(OpcHistoryValue) && typeof(T) != typeof(OpcModifiedHistoryValue))
                throw new ArgumentException();

            return new InfluxHistory<T>(new Repository(measurement, nodeId));
        }

        public bool Contains(DateTime timestamp)
        {
            return this.repository.Exists(timestamp);
        }
        public void Add(T value)
        {
            if (!this.repository.Create(value))
            {
                throw new ArgumentException(string.Format("An item with the timestamp '{0}' already exists.", value.Timestamp));
            }
        }
        public IEnumerable<T> Enumerate(DateTime? startTime, DateTime? endTime)
        {
            AdjustRange(ref startTime, ref endTime);
            foreach (var value in this.repository.Read(startTime, endTime))
            {
                Console.WriteLine(value.Value);
                Console.WriteLine(value.Timestamp);
                yield return value;
            }
        }

        public void Replace(T value)
        {
            if (!this.repository.Update(value))
            {
                throw new ArgumentException(string.Format("An item with the timestamp '{0}' does not exist.", value.Timestamp));
            }
        }


        private static void AdjustRange(ref DateTime? startTime, ref DateTime? endTime)
        {
            if (startTime == DateTime.MinValue || startTime == DateTime.MaxValue)
            {
                startTime = null;
            }
            if (endTime == DateTime.MinValue || endTime == DateTime.MaxValue)
            {
                endTime = null;
            }
            if (startTime > endTime)
            {
                var tempTime = startTime;
                startTime = endTime;
                endTime = tempTime;
            }
        }
    }
}
