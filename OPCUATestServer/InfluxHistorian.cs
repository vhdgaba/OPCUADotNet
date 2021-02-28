using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.UaFx;
using Opc.UaFx.Server;

namespace OPCUATestServer
{
    internal class InfluxHistorian : IOpcNodeHistoryProvider
    {
        private readonly object syncRoot;
        private bool autoUpdateHistory;
        
        public OpcVariableNode Node { get; }
        public OpcNodeManager Owner { get; }
        public InfluxHistory<OpcHistoryValue> History { get; }
        public InfluxHistory<OpcModifiedHistoryValue> ModifiedHistory { get; }


        public InfluxHistorian(OpcNodeManager owner, OpcVariableNode node) : base()
        {
            this.Owner = owner;
            this.Node = node;

            this.Node.AccessLevel |= OpcAccessLevel.HistoryReadOrWrite;
            this.Node.UserAccessLevel |= OpcAccessLevel.HistoryReadOrWrite;
            this.Node.IsHistorizing = true;

            this.syncRoot = new object();

            this.History = InfluxHistory<OpcHistoryValue>.Create("trialhistory6",node.Id);
            this.ModifiedHistory = InfluxHistory<OpcModifiedHistoryValue>.Create("trialhistory6", node.Id);

        }


        public bool AutoUpdateHistory
        {
            get
            {
                lock (this.syncRoot)
                {
                    return this.autoUpdateHistory;
                }
            }

            set
            {
                lock (this.syncRoot)
                {
                    if (this.autoUpdateHistory != value)
                    {
                        this.autoUpdateHistory = value;
                        if (this.autoUpdateHistory)
                        {
                            this.Node.BeforeApplyChanges += this.HandleNodeBeforeApplyChanges;
                        }
                        else
                        {
                            this.Node.BeforeApplyChanges -= this.HandleNodeBeforeApplyChanges;
                        }
                    }
                }
            }
        }


        public OpcStatusCollection CreateHistory(OpcContext context, OpcHistoryModificationInfo modificationInfo, OpcValueCollection values)
        {
            var results = OpcStatusCollection.Create(OpcStatusCode.Good, values.Count);
            lock (this.syncRoot)
            {
                var expectedDataType = this.Node.DataTypeId;
                for (int index = 0; index < values.Count; index++)
                {
                    var result = results[index];
                    var value = OpcHistoryValue.Create(values[index]);

                    if (value.DataTypeId == expectedDataType)
                    {
                        if (this.History.Contains(value.Timestamp))
                        {
                            result.Update(OpcStatusCode.BadEntryExists);
                        }
                        else
                        {
                            this.History.Add(value);

                            var modifiedValue = value.CreateModified(modificationInfo);
                            this.ModifiedHistory.Add(modifiedValue);

                            result.Update(OpcStatusCode.GoodEntryInserted);
                        }
                    }
                    else
                    {
                        result.Update(OpcStatusCode.BadTypeMismatch);
                    }
                }
            }
            return results;
        }

        public OpcStatusCollection DeleteHistory(OpcContext context, OpcHistoryModificationInfo modificationInfo, IEnumerable<DateTime> times)
        {
            throw new NotImplementedException();
        }

        public OpcStatusCollection DeleteHistory(OpcContext context, OpcHistoryModificationInfo modificationInfo, OpcValueCollection values)
        {
            throw new NotImplementedException();
        }

        public OpcStatusCollection DeleteHistory(OpcContext context, OpcHistoryModificationInfo modificationInfo, DateTime? startTime, DateTime? endTime, OpcDeleteHistoryOptions options)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<OpcHistoryValue> ReadHistory(OpcContext context, DateTime? startTime, DateTime? endTime, OpcReadHistoryOptions options)
        {
            lock (this.syncRoot)
            {
                if (options.HasFlag(OpcReadHistoryOptions.Modified))
                {
                    return this.ModifiedHistory.Enumerate(startTime, endTime).Cast<OpcHistoryValue>().ToArray();
                }
                return this.History.Enumerate(startTime, endTime).ToArray();
            }
        }

        public OpcStatusCollection ReplaceHistory(OpcContext context, OpcHistoryModificationInfo modificationInfo, OpcValueCollection values)
        {
            throw new NotImplementedException();
        }

        public OpcStatusCollection UpdateHistory(OpcContext context, OpcHistoryModificationInfo modificationInfo, OpcValueCollection values)
        {
            throw new NotImplementedException();
        }



        private void HandleNodeBeforeApplyChanges(object sender, OpcNodeChangesEventArgs e)
        {
            var timestamp = this.Node.Timestamp;

            if(timestamp != null && e.IsChangeOf(OpcNodeChanges.Value))
            {
                var value = new OpcHistoryValue(this.Node.Value, timestamp.Value);

                if (this.History.Contains(value.Timestamp))
                    this.History.Replace(value);
                else
                    this.History.Add(value);
            }
        }
    }
}
