using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace FileHasher
{
    internal class Program
    {
        private static readonly Mutex Mut = new Mutex();
        private static FileStream _fileToRead;

        private static BinaryReader _fileBinaryReader;

        private static void Main()
        {
            Console.WriteLine("File to Read: ");
            var filepath = Console.ReadLine();
            if ((filepath ?? string.Empty).Contains('\"')) filepath = filepath?.Replace("\"", string.Empty);
            Console.WriteLine("Block size: ");
            int blockSize;
            try
            {
                blockSize = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                blockSize = 256;
            }

            if (filepath != null)
            {
                _fileToRead = File.OpenRead(filepath);
                _fileBinaryReader = new BinaryReader(_fileToRead);
                var fileSize = _fileToRead.Length;
                var numberOfBlocks = fileSize / blockSize + 1;
                var watch = new Stopwatch();
                var pool = new List<Thread>();
                watch.Start();
                for (var i = 0; i < numberOfBlocks; i++)
                {
                    var i1 = i;
                    var thread = new Thread(() => { CalculateHash(i1, blockSize); });
                    thread.Start();
                    pool.Add(thread);
                }

                foreach (var thread in pool) thread.Join();

                watch.Stop();
                Console.WriteLine($"FileSize: {fileSize}");
                Console.WriteLine($"Average TimePerByte: {(double)watch.ElapsedMilliseconds / fileSize} ms");
                Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");
            }

            Console.ReadKey();
        }

        public static void CalculateHash(int blockNumber, int blockSize)
        {
            try
            {
                using (var mySha256 = SHA256.Create())
                {
                    var sizeLeft = _fileBinaryReader.BaseStream.Length - _fileBinaryReader.BaseStream.Position;
                    var dataToRead = blockSize;
                    if (sizeLeft < blockSize) dataToRead = (int)sizeLeft;
                    Mut.WaitOne();
                    var buffer = _fileBinaryReader.ReadBytes(dataToRead);
                    Mut.ReleaseMutex();
                    var hash = string.Concat(mySha256.ComputeHash(buffer)
                        .Select(item => item.ToString("x2")));
                    Console.WriteLine($"{blockNumber} : {hash}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}