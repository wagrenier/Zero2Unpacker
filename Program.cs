﻿using System;
using CommandLine;

namespace Zero2Unpacker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /*
             *
             * Memory location with RegisterFile: 0x003c7fdb
             *
             */

            Parser.Default.ParseArguments<ExtractOptions, DecompressOptions>(args)
            .MapResult(
                (ExtractOptions opts) => ExtractAll(opts),
                (DecompressOptions opts) => ExtractWithExistingArchives(opts),
                errs => 1);
        }

        public static int ExtractAll(ExtractOptions options)
        {
            var zero2ArchiveHandler = new Zero2ArchiveHandler(options.BinFileName, options.FolderName);

            zero2ArchiveHandler.ExtractAll();

            return 0;
        }

        public static int ExtractWithExistingArchives(DecompressOptions options)
        {
            
            var zero2ArchiveHandler = new Zero2ArchiveHandler(options.BinFileName, options.FolderName);

            var watch = System.Diagnostics.Stopwatch.StartNew();

            zero2ArchiveHandler.BuildAlreadyExistingDeLessArchive(options.ArchiveSize);

            zero2ArchiveHandler.MultiThreadExtract(12);
            zero2ArchiveHandler.ConvertAudio();

            watch.Stop();
            Console.WriteLine($"Total elapsed time: {watch.ElapsedMilliseconds}");
            

            /*
            var zeroFileStr = new ZeroFile()
            {
                FileId = 1,
                FileName = $"zeroFile1_0",
                Folder = $"{options.FolderName}/Zero/Uncompressed/audio/",
                FileHeader = new StrFile()
            };

            FileConverter.ConvertStrToWav(zeroFileStr, options.FolderName);
            */
            return 0;
        }
    }
}
