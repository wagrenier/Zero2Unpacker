using System;

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
             */

            var zero2ArchiveHandler = new Zero2ArchiveHandler("IMG_BD_US.BIN", "D:/DecompressFiles");

            //dataReader.SplitArchives();

            zero2ArchiveHandler.BuildAlreadyExistingDeLESSArchive(1822);

            Console.WriteLine($"Total files found : {zero2ArchiveHandler.DelessFiles.Count}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            //dataReader.DeLESSFiles();

            zero2ArchiveHandler.MultiThreadExtract();

            watch.Stop();
            Console.WriteLine($"Total elapsed time: {watch.ElapsedMilliseconds}");
        }
    }
}
