using System;
using System.Collections.Generic;
using System.IO;
using System.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace MsmqServer
{
    public class Program
    {
        private const string MessageQueueName = @".\private$\TestMessageQueue";

        private static MessageQueue _messageQueue;

        private static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var task = FileServer.Run(MessageQueueName, token);

            Console.WriteLine("Press ANY key to exit:");
            Console.ReadLine();
            cts.Cancel();

            try
            {
                task.Wait();
            }
            catch (TaskCanceledException)
            {
                // igone
            }
            finally
            {
                Console.WriteLine("Server ended");
            }
        }
    }
}
