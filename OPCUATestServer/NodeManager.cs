using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Opc.UaFx;
using Opc.UaFx.Server;

namespace OPCUATestServer
{
    internal class NodeManager : OpcNodeManager
    {
        private OpcNodeHistorian hcsHistorian;
        private OpcAlarmConditionNode temperatureCriticalNode;
        private OpcAnalogItemNode<int> tempDataNode1;
        public NodeManager() : base("http://vhgsystems.com/")
        {
        }
        protected override IEnumerable<IOpcNode> CreateNodes(OpcNodeReferenceCollection references)
        {

            this.tempDataNode1 = new OpcAnalogItemNode<int>("Value", 25);
            this.hcsHistorian = new OpcNodeHistorian(this, tempDataNode1); 
            hcsHistorian.AutoUpdateHistory = true; 
            

            var tempDataNode2 = new OpcDataVariableNode<string>("State", "ON");
            var lightDataNode = new OpcDataVariableNode<string>("State", "ON");
            var doorDataNode = new OpcDataVariableNode<string>("State", "LOCKED");

            var temperatureNode = new OpcObjectNode("Temperature-Sensor", this.tempDataNode1, tempDataNode2);
            var lightNode = new OpcObjectNode("Light", lightDataNode);
            var doorNode = new OpcObjectNode("Door", doorDataNode);

            
            this.temperatureCriticalNode = new OpcAlarmConditionNode(temperatureNode, "TemperatureCritical");

            // Define the alarm as the notifier of the machineOne node.
            temperatureNode.AddNotifier(this.SystemContext, this.temperatureCriticalNode);


            var rootNode = new OpcFolderNode(new OpcName("Home Control System", this.DefaultNamespaceIndex), temperatureNode, lightNode, doorNode);

            references.Add(rootNode, OpcObjectTypes.ObjectsFolder);
            CreateHistoryEntries(hcsHistorian);
            
            yield return rootNode;
        }
        protected override IOpcNodeHistoryProvider RetrieveNodeHistoryProvider(IOpcNode node)
        {
            if (this.hcsHistorian.Node == node)
                return hcsHistorian;

            return base.RetrieveNodeHistoryProvider(node);
        }

        private static void CreateHistoryEntries(OpcNodeHistorian historian)
        {
            for (int second = 0; second < 3600; second++)
            {
                var value = new OpcHistoryValue(
                        1000 + second, DateTime.UtcNow.Date.AddHours(6).AddSeconds(second));

                if ((second % 30) == 0)
                {
                    historian.ModifiedHistory.Add(value.CreateModified(
                            OpcHistoryModificationType.Delete, "Anonymous", value.Timestamp));
                }
                else
                {
                    historian.History.Add(value);
                }
            }
        }





        public void Simulate(SemaphoreSlim semaphore)
        {
            // By default we define each condition as acknowledged, because we will change it 
            // depending on outcome of the evaluations bound to the alarms.
            //          this.positionLimitNode.ChangeIsAcked(this.SystemContext, true);
            this.temperatureCriticalNode.ChangeIsAcked(this.SystemContext, true);

            var run = 0;
            var random = new Random(45);

            while (!semaphore.Wait(1000))
            {
                // Only perform "job"-simulation in case the "machine" is active.
                //if (!this.isActiveNode.Value)
                //    continue;

                //            this.SimulatePosition(run, random);
                this.SimulateTemperature(run, random);
                //            this.SimulateStatus(run, random);

                run = unchecked(run + 1);
            }
        }


        private void SimulateTemperature(int run, Random random)
        {
            //var temperatureValue = run;
            var temperatureValue = random.Next(12, 20 * (((run % 7) / 4) + 1));
            this.tempDataNode1.Value = temperatureValue;

            // This will trigger DataChange notification being send to DataChange subscriptions.
            this.tempDataNode1.ApplyChanges(this.SystemContext);

            if (temperatureValue <= 20)
            {
                this.temperatureCriticalNode.ChangeIsActive(this.SystemContext, false);
            }
            else
            {
                var message = "The temperature is higher than 20°C!";
                var severity = OpcEventSeverity.Low;

                if (temperatureValue > 30)
                {
                    message = "The temperature is higher than 30°C!";
                    severity = OpcEventSeverity.Medium;
                }
                else if (temperatureValue > 40 && temperatureValue <= 50)
                {
                    message = "The temperature is higher than 40°C!";
                    severity = OpcEventSeverity.High;
                }
                else
                {
                    message = "The temperature exceeded 50°C!";
                    severity = OpcEventSeverity.Max;
                }

                this.temperatureCriticalNode.Message = message;

                this.temperatureCriticalNode.ReceiveTime = DateTime.UtcNow;
                this.temperatureCriticalNode.Time = DateTime.UtcNow;

                this.temperatureCriticalNode.ChangeSeverity(this.SystemContext, severity);
                this.temperatureCriticalNode.ChangeIsActive(this.SystemContext, true);

            }
            this.temperatureCriticalNode.ReportEventFrom(
                    this.SystemContext, this.tempDataNode1);
        }

    }
}

