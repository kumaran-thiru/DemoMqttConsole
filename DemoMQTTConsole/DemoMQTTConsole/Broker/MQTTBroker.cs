using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

//mosquitto -c "C:\Program Files\Mosquitto\pass.conf" -v
namespace Broker
{
    public class MQTTBroker
    {
        private static int MessageCounter;

        public async Task broker()
        {
            Console.WriteLine("-------  BROKER -------");
            Console.WriteLine();
            try
            {
                // Create the options for our MQTT Broker
                var options = BuildServerOptions();

                // creates a new mqtt server     
                IMqttServer mqttServer = CreateServer();

                // handle client disconnection
                mqttServer.UseClientDisconnectedHandler(e =>
                {
                    Console.WriteLine();
                    Console.WriteLine("CONNECTION CLOSED - \n");
                    Console.WriteLine($"MQTT Client with Client_ID : {e.ClientId} disconnected.");
                    Console.WriteLine();
                });

                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine("*****  STARTING BROKER  *****");

                // start the server with options  
                await mqttServer.StartAsync(options.Build());

                Thread.Sleep(TimeSpan.FromSeconds(1));
                Console.WriteLine("####  STARTED  ####");
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine();

                // keep application running until user press a key
                Console.ReadLine();

                // stop server
                await mqttServer.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public IMqttServer CreateServer()
        {
            return new MqttFactory().CreateMqttServer();
        }

        public MqttServerOptionsBuilder BuildServerOptions()
        {
            return new MqttServerOptionsBuilder()
                                        // set endpoint to localhost
                                        .WithDefaultEndpoint()
                                        // set default port
                                        .WithDefaultEndpointPort(1883)
                                        // handler for new connections
                                        .WithConnectionValidator(OnNewConnection)
                                        // set number of queue connections
                                        .WithConnectionBacklog(100)
                                        // handler for new messages
                                        .WithApplicationMessageInterceptor(OnNewMessage)
                                        // handler for new subscriptions
                                        .WithSubscriptionInterceptor(OnNewSubscription);
    }

        private static void OnNewConnection(MqttConnectionValidatorContext context)
        {
            if(!UserCred.users.Exists(x => x.Username == context.Username && x.Password == context.Password))
            {
                context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return;
            }
            Console.WriteLine();
            Console.WriteLine($"NEW CONNECTION - \n\n" +
                              $"    ClientId : {context.ClientId}\n" +
                              $"    Endpoint : {context.Endpoint}");
            Console.WriteLine();
            context.ReasonCode = MqttConnectReasonCode.Success;
        }

        private static void OnNewMessage(MqttApplicationMessageInterceptorContext context)
        {
            MessageCounter++;
            Console.WriteLine();
            Console.WriteLine("NEW MESSAGE - \n");
            Console.WriteLine($"    Message Number : {MessageCounter}\n" +
                              $"    TimeStamp : {DateTime.Now}\n" +
                               "    Message Details \n" +
                              $"        ClientId    : {context.ClientId}\n" +
                              $"        Topic       : {context.ApplicationMessage.Topic}\n" +
                              $"        QoS         : {context.ApplicationMessage.QualityOfServiceLevel}\n" +
                              $"        Retain-Flag : {context.ApplicationMessage.Retain}");
            Console.WriteLine();
        }

        private static void OnNewSubscription(MqttSubscriptionInterceptorContext context)
        {
            Console.WriteLine();
            Console.WriteLine("NEW SUBSCRIPTION - \n");
            Console.WriteLine($"    TimeStamp: {DateTime.Now}\n" +
                               "    Subscription Details  \n" +
                              $"        ClientId : {context.ClientId}\n" +
                              $"        Topic    : {context.TopicFilter.Topic}\n" +
                              $"        QoS      : {context.TopicFilter.QualityOfServiceLevel}");
            Console.WriteLine();
        }
    }
}
