using System;
using CommandLine;

namespace Zero2Unpacker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /*
             * Add this to make the extractor more enjoyable
             * https://archive.codeplex.com/?p=commandline
             *
             * 1 - Extract all DeLESS files into their own files
             * 2 - Run DeLESS on each of these files
             * 3 - Extract each files from said unarchived files
             *
             * Memory location with RegisterFile: 0x003c7fdb
             *
             */

            CommandLine.Parser.Default.ParseArguments<AddOptions, CommitOptions, CloneOptions>(args)
            .MapResult(
                (AddOptions opts) => AA(opts),
                (CommitOptions opts) => BB(opts),
                (CloneOptions opts) => CC(opts),
                errs => 1);

            
            var zero2ArchiveHandler = new Zero2ArchiveHandler("IMG_BD_US.BIN", "D:/DecompressFiles");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            //zero2ArchiveHandler.ExtractAll();

            zero2ArchiveHandler.BuildAlreadyExistingDeLESSArchive(1822);

            //dataReader.DeLESSFiles();

            zero2ArchiveHandler.MultiThreadExtract(12);

            watch.Stop();
            Console.WriteLine($"Total elapsed time: {watch.ElapsedMilliseconds}");
        }

        public static int AA(AddOptions options)
        {
            return 0;
        }

        public static int BB(CommitOptions options)
        {
            return 0;
        }

        public static int CC(CloneOptions options)
        {
            return 0;
        }

    }
}
