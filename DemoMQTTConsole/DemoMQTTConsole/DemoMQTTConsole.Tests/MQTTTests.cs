using Xunit;
using Client1_Publisher;
using Client2_Subscriber;
using Broker;
using System.Threading.Tasks;
using MQTTnet.Server;
using MQTTnet.Client;
using System.Text;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Connecting;
using MQTTnet.Adapter;

namespace DemoMQTTConsole.Tests
{
    public class MQTTTests
    {
        private readonly MQTTBroker broker;
        private readonly Publisher publisher;
        private readonly Subscriber subscriber;

        public MQTTTests()
        {
            broker = new MQTTBroker();
            publisher = new Publisher();
            subscriber = new Subscriber();
        }

        [Fact]
        public async Task Broker_ManyClientsConnected_CanHandleConnections()
        {
            var bOptions = broker.BuildServerOptions();
            var server = broker.CreateServer();
            await server.StartAsync(bOptions.Build());
            int nn = 1;
            while(nn<10000)
            {
                var Client = subscriber.CreateClient();
                var cOptions = subscriber.BuildClientOptions().WithClientId(nn.ToString());

                var conRes = await Client.ConnectAsync(cOptions.Build());
                Assert.Equal(MqttClientConnectResultCode.Success, conRes.ResultCode);
                nn++;
            }
            await server.StopAsync();
        }

        [Fact] //0
        public async Task ValidClient_WhenConnected_GetsSuccessResultCode()
        {
            //Arrange
            var bOptions = broker.BuildServerOptions();
            var server = broker.CreateServer();
            var Client = subscriber.CreateClient();
            var cOptions = subscriber.BuildClientOptions();

            //Act
            await server.StartAsync(bOptions.Build());
            var conRes = await Client.ConnectAsync(cOptions.Build());
            await Task.Delay(10);
            await server.StopAsync();

            //Assert
            Assert.Equal(MqttClientConnectResultCode.Success, conRes.ResultCode);
        }

        [Fact] //1
        public async Task InvalidClient_WhenConnected_GetsUnAuthorisedResultCode()
        {
            //Arrange
            var bOptions = broker.BuildServerOptions();
            var server = broker.CreateServer();
            var Client = subscriber.CreateClient();
            var cOptions = subscriber.BuildClientOptions().WithCredentials("UN", "PW");

            //Act
            await server.StartAsync(bOptions.Build());

            //Assert
            var ex = await Assert.ThrowsAsync<MqttConnectingFailedException>(async () => await Client.ConnectAsync(cOptions.Build()));
            Assert.Equal(MqttClientConnectResultCode.BadUserNameOrPassword, ex.ResultCode);

            await server.StopAsync();
        }

        [Fact] //2
        public async Task Broker_WhenConnected_ReceivesCorrectClientDetails()
        {
            //Arrange
            MqttConnectionValidatorContext context = null;
            var bOptions = broker.BuildServerOptions().WithConnectionValidator(c => context = c);
            var server = broker.CreateServer();
            var Client = subscriber.CreateClient();
            var cOptions = subscriber.BuildClientOptions().WithCredentials("TestUN", "TestPW");

            //Act
            await server.StartAsync(bOptions.Build());
            await Client.ConnectAsync(cOptions.Build());
            await Task.Delay(10);
            await server.StopAsync();

            //Assert  ret code, id, un, pw, reason code
            Assert.Equal("Client-2", context.ClientId);
            Assert.Equal(MqttConnectReasonCode.Success, context.ReasonCode);
            Assert.Equal("TestUN", context.Username);
            Assert.Equal("TestPW", context.Password);
        }

        [Fact] //3
        public async Task Publisher_WhenPublished_GetsSuccessReasonCode()
        {
            //Arrange
            var bOptions = broker.BuildServerOptions();
            var server = broker.CreateServer();
            var pClient = publisher.CreateClient();
            var pcOptions = publisher.BuildClientOptions();

            //Act
            await server.StartAsync(bOptions.Build());
            await pClient.ConnectAsync(pcOptions.Build());
            var pubRes = await pClient.PublishAsync(publisher.BuildMessage("Test", "demo"));
            await Task.Delay(10);
            await server.StopAsync();

            //Assert
            Assert.Equal(MqttClientPublishReasonCode.Success, pubRes.ReasonCode);
        }

        [Fact] //4
        public async Task Subscriber_WhenSubscribedToTopic_GetsSuccessResultCode()
        {
            //Arrange
            var bOptions = broker.BuildServerOptions();
            var server = broker.CreateServer();
            var sClient = subscriber.CreateClient();
            var scOptions = subscriber.BuildClientOptions();

            //Act
            await server.StartAsync(bOptions.Build());
            await sClient.ConnectAsync(scOptions.Build());
            var subRes = await sClient.SubscribeAsync(subscriber.BuildSubscribeOptions("Test"));
            await Task.Delay(10);
            await server.StopAsync();

            //Assert
            Assert.Equal(MqttClientSubscribeResultCode.GrantedQoS2, subRes.Items[0].ResultCode);
        }

        [Fact] //5
        public async Task Subscriber_WhenSubscribedToTopic_GetsCorrectMessageWhenPublished()
        {
            //Arrange
            var msgCount = 0;
            MqttApplicationMessage receivedMessage = null;
            var bOptions = broker.BuildServerOptions();
            var server = broker.CreateServer();
            var pClient = publisher.CreateClient();
            var pcOptions = publisher.BuildClientOptions();
            var sClient = subscriber.CreateClient();
            var scOptions = subscriber.BuildClientOptions();
            sClient.UseApplicationMessageReceivedHandler(e => { receivedMessage = e.ApplicationMessage; msgCount++; });

            //Act
            await server.StartAsync(bOptions.Build());
            await sClient.ConnectAsync(scOptions.Build());
            await sClient.SubscribeAsync(subscriber.BuildSubscribeOptions("Test"));
            await pClient.ConnectAsync(pcOptions.Build());
            var pub = await pClient.PublishAsync(publisher.BuildMessage("Test", "demo"));
            await Task.Delay(10);
            await server.StopAsync();

            //Assert
            Assert.NotNull(receivedMessage);
            Assert.Equal("Test", receivedMessage.Topic);
            Assert.Equal(MqttQualityOfServiceLevel.ExactlyOnce, receivedMessage.QualityOfServiceLevel);
            Assert.Equal("demo", Encoding.UTF8.GetString(receivedMessage.Payload));
            Assert.Equal(1, msgCount);
        }
         
        [Fact] //6
        public async Task Subscriber_WhenSubscribedToTopic_GetsAllMessagesWhenPublished()
        {
            //Arrange
            var msgCount = 0;
            MqttApplicationMessage receivedMessage = null;
            var bOptions = broker.BuildServerOptions();
            var server = broker.CreateServer();
            var pClient = publisher.CreateClient();
            var pcOptions = publisher.BuildClientOptions();
            var sClient = subscriber.CreateClient();
            var scOptions = subscriber.BuildClientOptions();
            sClient.UseApplicationMessageReceivedHandler(e => { receivedMessage = e.ApplicationMessage; msgCount++; });

            //Act
            await server.StartAsync(bOptions.Build());
            await sClient.ConnectAsync(scOptions.Build());
            await sClient.SubscribeAsync(subscriber.BuildSubscribeOptions("Test"));
            await pClient.ConnectAsync(pcOptions.Build());
            await pClient.PublishAsync(publisher.BuildMessage("Test", "demo"));
            await pClient.PublishAsync(publisher.BuildMessage("Test", "demo1"));
            await pClient.PublishAsync(publisher.BuildMessage("Test", "demo2"));
            await Task.Delay(10);
            await server.StopAsync();

            //Assert

            Assert.Equal(3, msgCount);
        }

        [Fact] //7
        public async Task Subscriber_NotSubscribedToTopic_GetsNoMessageWhenPublished()
        {
            //Arrange
            var msgCount = 0;
            MqttApplicationMessage receivedMessage = null;
            var bOptions = broker.BuildServerOptions();
            var server = broker.CreateServer();
            var pClient = publisher.CreateClient();
            var pcOptions = publisher.BuildClientOptions();
            var sClient = subscriber.CreateClient();
            var scOptions = subscriber.BuildClientOptions();
            sClient.UseApplicationMessageReceivedHandler(e => { receivedMessage = e.ApplicationMessage; msgCount++; });

            //Act
            await server.StartAsync(bOptions.Build());
            await sClient.ConnectAsync(scOptions.Build());
            await sClient.SubscribeAsync(subscriber.BuildSubscribeOptions("test"));
            await pClient.ConnectAsync(pcOptions.Build());
            await pClient.PublishAsync(publisher.BuildMessage("Test", "demo"));
            await Task.Delay(10);
            await server.StopAsync();

            //Assert
            Assert.Null(receivedMessage);
            Assert.Equal(0, msgCount);
        }

        [Fact] //8
        public async Task Broker_WhenSubscribed_ReceivesCorrectTopic()
        {
            //Arrange
            string topic = null;
            var bOptions = broker.BuildServerOptions();
            var server = broker.CreateServer();
            var sClient = subscriber.CreateClient();
            var scOptions = subscriber.BuildClientOptions();
            server.ClientSubscribedTopicHandler = new MqttServerClientSubscribedHandlerDelegate(x => topic = x.TopicFilter.Topic);

            //Act
            await server.StartAsync(bOptions.Build());
            await sClient.ConnectAsync(scOptions.Build());
            await sClient.SubscribeAsync(subscriber.BuildSubscribeOptions("Test"));
            await Task.Delay(10);
            await server.StopAsync();

            //Assert
            Assert.NotNull(topic);
            Assert.Equal("Test", topic);
        }

        [Fact] //9
        public async Task Broker_WhenSubscribed_ReceivesAllTopics()
        {
            //Arrange
            var topicCount = 0;
            var bOptions = broker.BuildServerOptions();
            var server = broker.CreateServer();
            var sClient = subscriber.CreateClient();
            var scOptions = subscriber.BuildClientOptions();
            server.ClientSubscribedTopicHandler = new MqttServerClientSubscribedHandlerDelegate(x => topicCount++);

            //Act
            await server.StartAsync(bOptions.Build());
            await sClient.ConnectAsync(scOptions.Build());
            await sClient.SubscribeAsync(subscriber.BuildSubscribeOptions("Test"));
            await sClient.SubscribeAsync(subscriber.BuildSubscribeOptions("Test2"));
            await sClient.SubscribeAsync(subscriber.BuildSubscribeOptions("Test4"));
            await Task.Delay(10);
            await server.StopAsync();

            //Assert
            Assert.Equal(3, topicCount);
        }

        [Fact] //10
        public async Task Broker_WhenPublished_ReceivesCorrectMessage()
        {
            //Arrange
            MqttApplicationMessage receivedMessage = null;
            var bOptions = broker.BuildServerOptions().WithApplicationMessageInterceptor(x => receivedMessage = x.ApplicationMessage);
            var server = broker.CreateServer();
            var pClient = publisher.CreateClient();
            var pcOptions = publisher.BuildClientOptions();

            //Act
            await server.StartAsync(bOptions.Build());
            await pClient.ConnectAsync(pcOptions.Build());
            await pClient.PublishAsync(publisher.BuildMessage("Test", "demo"));
            await Task.Delay(10);
            await server.StopAsync();

            //Assert
            Assert.NotNull(receivedMessage);
            Assert.Equal("Test", receivedMessage.Topic);
            Assert.Equal(MqttQualityOfServiceLevel.ExactlyOnce, receivedMessage.QualityOfServiceLevel);
            Assert.Equal("demo", Encoding.UTF8.GetString(receivedMessage.Payload));
        }

        [Fact] //11
        public async Task Broker_WhenPublished_ReceivesAllMessages()
        {
            //Arrange
            var msgCount = 0;
            MqttApplicationMessage receivedMessage = null;
            var bOptions = broker.BuildServerOptions().WithApplicationMessageInterceptor(x => { receivedMessage = x.ApplicationMessage; msgCount++; });
            var server = broker.CreateServer();
            var pClient = publisher.CreateClient();
            var pcOptions = publisher.BuildClientOptions();

            //Act
            await server.StartAsync(bOptions.Build());
            await pClient.ConnectAsync(pcOptions.Build());
            await pClient.PublishAsync(publisher.BuildMessage("Test", "demo"));
            await pClient.PublishAsync(publisher.BuildMessage("Test", "demo1"));
            await Task.Delay(10);
            await server.StopAsync();

            //Assert            
            Assert.Equal(2, msgCount);
        }
    }
}

//    client.Connected += async(s, e) =>
//        {
//            // Subscribe to a topic
//            await client.SubscribeAsync(new TopicFilterBuilder().WithTopic("topic123").Build());

//            var payload = "{\"value\":\"" + GetRandomNumber(1, 200).ToString() + "\"}";

//    var message = new MqttApplicationMessageBuilder()
//        .WithTopic("topic123")
//        .WithPayload(payload)
//        .WithExactlyOnceQoS()
//        .WithRetainFlag()

//        .Build();

//    await client.PublishAsync(message);

//};