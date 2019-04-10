using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.Extensions.CommandLineUtils;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NetworkSpeedMonitor
{
    struct SampelingData
    {
        public int Id;
        public float Up;
        public float Down;
    }
    class Program
    {
        static BufferBlock<SampelingData> buffer = new BufferBlock<SampelingData>(new DataflowBlockOptions() { BoundedCapacity = 1 });
        static void Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.Name = "NetworkSpeedMonitor";
            app.HelpOption("-?|-h|--help");
            var snmpServerOption = app.Option("-ss|--snmpserver <ip-address>", "ip address of the snmp server", CommandOptionType.SingleValue, true);
            var snmpCommunityOption = app.Option("-sc|--snmpcommunity <string>", "the snmp community (rocommunity or public)", CommandOptionType.SingleValue);
            var snmpDownOption = app.Option("-sd|--snmpdownoid <snmp-oid>", "the snmp download baseoid", CommandOptionType.SingleValue);
            var snmpUpOption = app.Option("-su|--snmpupoid <snmp-oid>", "the snmp upload baseoid", CommandOptionType.SingleValue);

            var mqttServerOption = app.Option("-ms|--mqttserver <ip-address>", "ip address of the mqtt server", CommandOptionType.SingleValue);
            var mqttTopicOption = app.Option("-mt|--mqtttopic <string>", "mqtt topic to push values to", CommandOptionType.SingleValue);
            app.OnExecute(() => {
                Console.WriteLine("Starting send task");
                new Thread(() => SendDataTask(mqttServerOption.Value(), mqttTopicOption.Value())).Start();
                Console.WriteLine("Starting to collect data");
                GatherData(snmpServerOption.Value(), snmpCommunityOption.Value(), snmpDownOption.Value(), snmpUpOption.Value());
                return 0;
            });
            app.Execute(args);

        }

        public static void GatherData(string snmpServer, string snmpCommunity, string downOID, string upOID)
        {
            var id = 0;
            /*var downID = "1.3.6.1.2.1.2.2.1.10.10";
            var upID = "1.3.6.1.2.1.2.2.1.16.10";*/
            long lastDownValue = GetValue(snmpServer, snmpCommunity, downOID);
            long lastUpValue = GetValue(snmpServer, snmpCommunity, upOID);
            var sleepTime = 1000;
            while (true)
            {
                Thread.Sleep(sleepTime);
                var watch = Stopwatch.StartNew();
                var newDownValue = GetValue(snmpServer, snmpCommunity, downOID);
                var newUpValue = GetValue(snmpServer, snmpCommunity, upOID);
                var up = (newUpValue - lastUpValue) / 1024f / 1024f;
                var down = (newDownValue - lastDownValue) / 1024f / 1024f;
                lastDownValue = newDownValue;
                lastUpValue = newUpValue;
                buffer.Post(new SampelingData() { Id = id++, Down = down, Up = up });
                watch.Stop();
                sleepTime = (int)Math.Max(0, 1000 - watch.ElapsedMilliseconds);
            }
        }

        public static long GetValue(string server, string snmpCommunity, string id)
        {
            var result = Messenger.Get(VersionCode.V2,
                           new IPEndPoint(IPAddress.Parse(server), 161),
                           new OctetString(snmpCommunity),
                           new List<Variable> { new Variable(new ObjectIdentifier(id)) },
                           10000);

            return long.Parse(result[0].Data.ToString());
        }

        public static void SendDataTask(string mqttServer, string mqttTopic)
        {
            Task.Run(async () =>
            {
                Console.WriteLine("Creating MQTT client");
                var options = new MqttClientOptionsBuilder().WithClientId("NetworkSpeedMonitor")
                            .WithTcpServer(mqttServer)
                            .WithCleanSession()
                            .Build();

                var factory = new MqttFactory();
                var mqttClient = factory.CreateMqttClient();
                var i = 0;
                while (true)
                {
                    i++;
                    var value = await buffer.ReceiveAsync();
                    if (i % (60 * 60) == 0)
                    {
                        Console.WriteLine($"down: {value.Down} MB/s, up: {value.Up} MB/s");
                    }

                    if (!mqttClient.IsConnected)
                    {
                        Console.WriteLine("Connecting to mqtt server");
                        await mqttClient.ConnectAsync(options);
                    }
                    if (mqttClient.IsConnected)
                    {
                        await mqttClient.PublishAsync(new MqttApplicationMessage() { Topic = mqttTopic + "/Up", Payload = Encoding.UTF8.GetBytes(value.Up.ToString()) },
                                                      new MqttApplicationMessage() { Topic = mqttTopic + "/Down", Payload = Encoding.UTF8.GetBytes(value.Down.ToString()) });
                        //Console.WriteLine("Mqtt message sent");
                    }
                    else
                    {
                        Console.WriteLine("Failed to connect, dumping message!");
                    }
                }
            });
        }
    }
}
