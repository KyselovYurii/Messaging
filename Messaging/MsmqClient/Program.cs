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
        private const int BufferSize = 2 * 1024 * 1024;

        private static MessageQueue _messageQueue;

        private static void Main(string[] args)
        {
            _messageQueue = MessageQueue.Exists(MessageQueueName) ? new MessageQueue(MessageQueueName) : MessageQueue.Create(MessageQueueName, true);
            _messageQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(byte[]) });

            FileSystemWatcher watcher = new FileSystemWatcher(FolderPath)
            {
                EnableRaisingEvents = true,
                Filter = "*.zip"
            };

            watcher.Created += SendFileToQueue;

            Console.WriteLine("Press Enter to exit:");
            Console.ReadLine();
        }

        static void SendFileToQueue(object sender, FileSystemEventArgs e)
        {
            int retryCount = 0;
            //try to read file
            while (retryCount < 3)
            {
                try
                {
                    if (_messageQueue.Transactional == true)
                    {
                        MessageQueueTransaction myTransaction = new MessageQueueTransaction();
                        myTransaction.Begin();

                        byte[] buffer = new byte[BufferSize];

                        using (var stream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            long remaining = stream.Length;
                            int bytesRead;
                            while (remaining > 0 && (bytesRead = stream.Read(buffer, 0, (int)Math.Min(remaining, BufferSize))) > 0)
                            {
                                byte[] arrayToSent;
                                if (bytesRead < BufferSize)
                                {
                                    arrayToSent = new byte[bytesRead];
                                    Array.Copy(buffer, arrayToSent, bytesRead);
                                }
                                else
                                {
                                    arrayToSent = buffer;
                                }

                                _messageQueue.Send(arrayToSent, e.Name, myTransaction);

                                remaining -= bytesRead;
                            }
                        }

                        myTransaction.Commit();
                        return;
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine($"Try #{retryCount++}");
                    Thread.Sleep(200);
                }
            }
        }
    }
}
