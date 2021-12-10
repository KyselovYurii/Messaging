using System;
using System.Collections.Generic;
using System.IO;

namespace MsmqServer
{
    public class FileConsumer : IDisposable
    {
        private const string FolderPath = @"C:\Users\Yurii_Kyselov\ServerFolder";

        private readonly Stream _stream;

        public FileConsumer(string filePath)
        {
            _stream = new FileStream($"{FolderPath}\\{filePath}", FileMode.Create, FileAccess.Write);
        }

        public void Consume(IEnumerable<byte> chunk)
        {
            foreach (byte @byte in chunk)
            {
                _stream.WriteByte(@byte);
            }
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
