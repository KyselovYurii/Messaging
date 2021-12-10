using System;
using System.IO;
using System.Messaging;
using System.Threading;

namespace MsmqClient
{
    public class FileClient : IDisposable
    {
        private MessageQueue _messageQueue;
        private FileSystemWatcher _watcher;

        public FileClient(string messageQueueName)
        {
            _messageQueue = MessageQueue.Exists(messageQueueName) ? new MessageQueue(messageQueueName) : MessageQueue.Create(messageQueueName, true);
            _messageQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(byte[]) });
        }

        public void WatchFor(string path, string extension)
        {
            _watcher = new FileSystemWatcher(path)
            {
                EnableRaisingEvents = true,
                Filter = extension
            };

            _watcher.Created += SendFileToQueue;
        }


        private void SendFileToQueue(object sender, FileSystemEventArgs e)
        {
            if (_messageQueue.Transactional == true)
            {
                using (MessageQueueTransaction myTransaction = new MessageQueueTransaction())
                {
                    myTransaction.Begin();

                    var fileReader = new FileReader();
                    fileReader.ChunkRead += (a, n) => { _messageQueue.Send(a, n, myTransaction); };
                    if (fileReader.TryReadFile(e.FullPath))
                    {
                        myTransaction.Commit();
                    }
                    else
                    {
                        myTransaction.Abort();
                    }
                }
            }
        }

        public void Dispose()
        {
            _watcher.Created -= SendFileToQueue;
            _watcher.Dispose();
            _messageQueue.Dispose();
        }
    }
}
