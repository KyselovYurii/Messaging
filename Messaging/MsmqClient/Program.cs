using System;
using System.IO;
using System.Messaging;
using System.Threading;

namespace MsmqClient
{
    public class Program
    {
        private const string MessageQueueName = @".\private$\TestMessageQueue";
        private const string FolderPath = @"C:\Users\Yurii_Kyselov\ClientFolder";
        private const string FileType = "*.zip";

        private static void Main(string[] args)
        {
            using (var client = new FileClient(MessageQueueName))
            {
                client.WatchFor(FolderPath, FileType);

                Console.WriteLine("Press Enter to exit:");
                Console.ReadLine();
            }
        }
    }
}
