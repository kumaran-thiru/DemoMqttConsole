using System.Threading.Tasks;

namespace Broker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var broker = new MQTTBroker();
            await broker.broker();
        }        
    }
}