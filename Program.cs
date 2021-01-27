using System;

namespace Zero2Unpacker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /*
             *
             * 1 - Extract all DeLESS files into their own files
             * 2 - Run DeLESS on each of these files
             * 3 - Extract each files from said unarchived files
             *
             */

            var dataReader = new Zero2ArchiveHandler("IMG_BD_US.BIN", "D:/DecompressFiles");

            //dataReader.SplitArchives();

            dataReader.BuildAlreadyExistingDeLESSArchive(1822);

            Console.WriteLine($"Total files found : {dataReader.DelessFiles.Count}");

            var watch = System.Diagnostics.Stopwatch.StartNew();

            dataReader.MultiThreadExtract();

            //dataReader.DeLESSFiles();

            watch.Stop();
            Console.WriteLine($"Total elapsed time: {watch.ElapsedMilliseconds}");
        }
    }
}
