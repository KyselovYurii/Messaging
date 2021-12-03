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
        private const string FolderPath = @"C:\Users\Yurii_Kyselov\ServerFolder";

        private static MessageQueue _messageQueue;

        private static void Main(string[] args)
        {
            _messageQueue = MessageQueue.Exists(MessageQueueName) ? new MessageQueue(MessageQueueName) : MessageQueue.Create(MessageQueueName, true);

            _messageQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(byte[]) });
            _messageQueue.MessageReadPropertyFilter = new MessagePropertyFilter
            {
                IsLastInTransaction = true,
                Label = true,
                Body = true,
            };

            Task.Run(Worker);

            Console.ReadLine();

        }

        static void Worker()
        {
            while (true)
            {
                using (var trans = new MessageQueueTransaction())
                {
                    Stream stream = null;
                    try
                    {
                        var fileName = string.Empty;

                        trans.Begin();
                        Message message;
                        while ((message = _messageQueue.Receive(trans)) != null)
                        {
                            if(stream == null)
                            {
                                fileName = message.Label;
                                Console.WriteLine($"Start reading ({fileName})");
                                stream = new FileStream($"{FolderPath}\\{fileName}", FileMode.Create, FileAccess.Write);
                            }

                            WriteToStream(stream, (byte[])message.Body);
                            if (message.IsLastInTransaction)
                            {
                                break;
                            }
                        }

                        Console.WriteLine($"End reading ({fileName})");

                        stream.Dispose();
                        trans.Commit();
                    }
                    catch (MessageQueueException e)
                    {
                        if (e.MessageQueueErrorCode == MessageQueueErrorCode.TransactionUsage)
                        {
                            Console.WriteLine("Something is wrong. Queue is not in transactional mode.");
                        }

                        trans.Abort();
                    }
                }
            }
        }

        private static void WriteToStream(Stream stream, IEnumerable<byte> bytes)
        {
            foreach (byte @byte in bytes)
            {
                stream.WriteByte(@byte);
            }
        }
    }
}
