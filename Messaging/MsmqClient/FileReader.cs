using System;
using System.IO;
using System.Threading;

namespace MsmqClient
{
    public class FileReader
    {
        private const int BufferSize = 2 * 1024 * 1024;

        public Action<byte[], string> ChunkRead { get; set; }

        public bool TryReadFile(string path)
        {
            int retryCount = 0;
            //try to read file
            while (retryCount < 3)
            {
                try
                {
                    ReadFile(path);

                    return true;
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine($"Try #{retryCount++}");
                    Thread.Sleep(200);
                }
            }

            return false;
        }

        private void ReadFile(string path)
        {
            byte[] buffer = new byte[BufferSize];

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var fileName = Path.GetFileName(path);

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

                    ChunkRead?.Invoke(arrayToSent, fileName);

                    remaining -= bytesRead;
                }
            }
        }
    }
}
