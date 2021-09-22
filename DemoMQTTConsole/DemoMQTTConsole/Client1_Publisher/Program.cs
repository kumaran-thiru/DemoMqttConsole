using System.Threading.Tasks;

namespace Client1_Publisher
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var publisher = new Publisher();
            await publisher.publisher();
        }
    }
}