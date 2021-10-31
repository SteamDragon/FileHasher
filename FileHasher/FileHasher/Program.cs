using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace FileHasher
{
    internal class Program
    {
        private static readonly Mutex Mut = new Mutex();

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
                var filerToRead = File.OpenRead(filepath);
                var a = new BinaryReader(filerToRead);
                var fileSize = filerToRead.Length;
                var numberOfBlocks = fileSize / blockSize + 1;
                var watch = new Stopwatch();

                watch.Start();
                var parallelLoopResult = Parallel.For(0, numberOfBlocks, i =>
                {
                    try
                    {
                        using (var mySha256 = SHA256.Create())
                        {
                            var sizeLeft = a.BaseStream.Length - a.BaseStream.Position;
                            var dataToRead = blockSize;
                            if (sizeLeft < blockSize) dataToRead = (int)sizeLeft;
                            Mut.WaitOne();
                            var buffer = a.ReadBytes(dataToRead);
                            Mut.ReleaseMutex();
                            var hash = string.Concat(mySha256.ComputeHash(buffer)
                                .Select(item => item.ToString("x2")));
                            Console.WriteLine($"{i} : {hash}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                });
                watch.Stop();
                Console.WriteLine($"FileSize: { fileSize}");
                Console.WriteLine($"Average TimePerByte: {(double)watch.ElapsedMilliseconds / fileSize} ms");
                Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");
                Console.WriteLine(parallelLoopResult.IsCompleted);
            }

            Console.ReadKey();
        }
    }
}