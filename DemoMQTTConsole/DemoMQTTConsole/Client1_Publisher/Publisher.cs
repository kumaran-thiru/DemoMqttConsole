﻿using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace Client1_Publisher
{
    public class Publisher
    {
        private static IMqttClient mqttClient;

        public async Task publisher()
        {
            Console.WriteLine("-------  CLIENT 1 -------");
            Console.WriteLine();
            try
            {
                Console.WriteLine("Press any key to start....");
                Console.ReadLine();
                Console.WriteLine();
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine("*****  CONNECTING TO BROKER  *****");

                var conOptions = BuildClientOptions();

                //create client
                mqttClient = CreateClient();

                //connection handler
                mqttClient.UseConnectedHandler(e =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    Console.WriteLine("####  CONNECTED  ####");
                    Console.WriteLine("-----------------------------------------------------------------------");
                    Console.WriteLine();
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                });

                //handle reconnecting
                mqttClient.UseDisconnectedHandler(async e =>
                {
                    Console.WriteLine("####  DISCONNECTED FROM BROKER  ####");
                    Console.WriteLine("-----------------------------------------------------------------------");
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    Console.WriteLine("####  RECONNECTING  ####");
                    try
                    {
                        await mqttClient.ConnectAsync(conOptions.Build(), CancellationToken.None);
                    }
                    catch
                    {
                        Console.WriteLine("### RECONNECTING FAILED ###");
                    }
                });

                //receive incoming msg
                mqttClient.UseApplicationMessageReceivedHandler(OnMessageReceived);

                //connect to broker
                await mqttClient.ConnectAsync(conOptions.Build());

                //subscribing to topic
                await SubscribeAsync("my/topic2");

                //publish message
                int n = 0;
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    Console.WriteLine("-  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  - ");
                    Console.WriteLine("Press any key to publish the message....");
                    Console.WriteLine();
                    Console.ReadLine();
                    var obj = new { Message_No = ++n, Sender = "Client 1", Sent_Time = DateTime.Now.ToString() };
                    string json = JsonSerializer.Serialize(obj);
                    
                    var message = BuildMessage("my/topic1", json);

                    Console.WriteLine("-----------------------------------------------------------------------");
                    Console.WriteLine($"*****  PUBLISHING MESSAGE TO 'my/topic1' at {DateTime.Now}  *****");

                    await mqttClient.PublishAsync(message);
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    Console.WriteLine("###  PUBLISHED  ###");
                    Console.WriteLine("-----------------------------------------------------------------------");
                    Console.WriteLine();
                    //Console.WriteLine();
                    //Task.Delay(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public MqttApplicationMessage BuildMessage(string topic, string json)
        {
            return new MqttApplicationMessageBuilder()
                                    .WithTopic(topic)
                                    .WithPayload(json)
                                    .WithExactlyOnceQoS()
                                    .WithRetainFlag()
                                    .Build();
        }

        public IMqttClient CreateClient()
        {
            var factory = new MqttFactory();
            return factory.CreateMqttClient();
        }

        public MqttClientOptionsBuilder BuildClientOptions()
        {
            return new MqttClientOptionsBuilder()
                                .WithClientId("Client-1")
                                .WithTcpServer("127.0.0.1", 1883)
                                .WithCleanSession()
                                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                                .WithCredentials("User1", "Pass@1");
        }

        public MqttClientSubscribeOptions BuildSubscribeOptions(string topic)
        {
            return new MqttClientSubscribeOptionsBuilder()
                            .WithTopicFilter(topic, MqttQualityOfServiceLevel.ExactlyOnce)
                            .Build();
        }

        public async Task SubscribeAsync(string topic)
        {
            //Console.WriteLine("Press any key to subscribe to topic....");
            //Console.ReadLine();
            //Console.WriteLine();
            Console.WriteLine("-----------------------------------------------------------------------");
            Console.WriteLine("*****  SUBSCRIBING TO TOPIC 'my/topic2'  *****");

            var subOptions = BuildSubscribeOptions(topic);

            Thread.Sleep(TimeSpan.FromSeconds(1));
            await mqttClient.SubscribeAsync(subOptions);
            Console.WriteLine("### SUBSCRIBED ###");
            Console.WriteLine("-----------------------------------------------------------------------");
            Console.WriteLine();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            //Console.WriteLine();
        }
        
        private static void OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            //var obj = JsonSerializer.Deserialize<MyClass>(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));

            Console.WriteLine();
            Console.WriteLine("***********************************************************************");
            Console.WriteLine("#####  INCOMING MESSAGE  #####");
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Console.WriteLine();
            Console.WriteLine($"  Topic   : {e.ApplicationMessage.Topic}");
            Console.WriteLine($"  Message : {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
            Console.WriteLine($"  QoS     : {e.ApplicationMessage.QualityOfServiceLevel}");
            //Console.WriteLine($"  Retain  : {e.ApplicationMessage.Retain}");
            Console.WriteLine();
            Console.WriteLine("####  END OF MESSAGE  ####");
            Console.WriteLine("***********************************************************************");
            Console.WriteLine();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Console.WriteLine("-  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  -  - ");
            Console.WriteLine("Press any key to publish the message....");
            Console.WriteLine();
        }

        //var clientOptions = new ManagedMqttClientOptionsBuilder()
        //    .WithAutoReconnectDelay(TimeSpan.FromSeconds(30))
        //    .WithClientOptions(conOptions)
        //    .Build();
    }
}
