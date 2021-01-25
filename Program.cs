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

            var dataReader = new Zero2ArchiveHandler("IMG_BD.BIN", "D:/DecompressFiles");

            dataReader.SplitDeLESSArchives();

            Console.WriteLine($"Total files found : {dataReader.delessFiles.Count}");

            dataReader.RunDeLESS();
        }
    }
}
