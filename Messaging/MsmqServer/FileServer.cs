using System;
using System.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace MsmqServer
{
    public static class FileServer
    {
        private static TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

        private static Task _task;

        public static Task Run(string queueName, CancellationToken token)
        {
            if (_task == null)
            {
                _task = Task.Run(() => DoWork(queueName, token), token);
            }

            return _task;
        }

        private static void DoWork(string queueName, CancellationToken token)
        {
            using (var queue = InitializeQueue(queueName))
            {
                while (!token.IsCancellationRequested)
                {
                    using (var trans = new MessageQueueTransaction())
                    {
                        FileConsumer consumer = null;
                        try
                        {
                            trans.Begin();
                            Message message;
                            while ((message = queue.Receive(WaitTimeout, trans)) != null)
                            {
                                token.ThrowIfCancellationRequested();
                                if (consumer == null)
                                {
                                    consumer = new FileConsumer(message.Label);
                                }

                                consumer.Consume((byte[])message.Body);
                                if (message.IsLastInTransaction)
                                {
                                    break;
                                }
                            }

                            consumer.Dispose();
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
        }

        private static MessageQueue InitializeQueue(string queueName)
        {
            var messageQueue = MessageQueue.Exists(queueName) ? new MessageQueue(queueName) : MessageQueue.Create(queueName, true);

            messageQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(byte[]) });
            messageQueue.MessageReadPropertyFilter = new MessagePropertyFilter
            {
                IsLastInTransaction = true,
                Label = true,
                Body = true,
            };

            return messageQueue;
        }
    }
}
