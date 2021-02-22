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

        public SampleHistorian(OpcNodeManager owner, OpcVariableNode node) : base()
        {
            this.Owner = owner;
            this.Node = node;

            this.Node.AccessLevel |= OpcAccessLevel.HistoryReadOrWrite;
            this.Node.UserAccessLevel |= OpcAccessLevel.HistoryReadOrWrite;
            this.Node.IsHistorizing = true;

            this.syncRoot = new object();

            this.History = 
        }

       
    }
}
