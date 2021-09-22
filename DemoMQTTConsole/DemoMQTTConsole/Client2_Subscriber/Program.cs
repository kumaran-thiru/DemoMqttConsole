using System.Threading.Tasks;

namespace Client2_Subscriber
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var subscriber = new Subscriber();
            await subscriber.subscriber();
        }
        
    }
}