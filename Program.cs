using System;
using CommandLine;

namespace Zero2Unpacker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ExtractOptions, DecompressOptions>(args)
            .MapResult(
                (ExtractOptions opts) => ExtractAll(opts),
                (DecompressOptions opts) => ExtractWithExistingArchives(opts),
                errs => 1);
        }

        public static int ExtractAll(ExtractOptions options)
        {
            var zero2ArchiveHandler = new Zero2ArchiveHandler(options.BinFileName, options.FolderName);

            var watch = System.Diagnostics.Stopwatch.StartNew();

            zero2ArchiveHandler.ExtractAll(options.ThreadCount);

            if (options.ConvertFiles)
            {
                zero2ArchiveHandler.ConvertAudio();
            }

            watch.Stop();

            Console.WriteLine($"Total elapsed time: {watch.ElapsedMilliseconds}");
            return 0;
        }

        public static int ExtractWithExistingArchives(DecompressOptions options)
        {
            
            var zero2ArchiveHandler = new Zero2ArchiveHandler(options.BinFileName, options.FolderName, options.DatabaseFile);

            var watch = System.Diagnostics.Stopwatch.StartNew();

            zero2ArchiveHandler.BuildAlreadyExistingDeLessArchive(options.ArchiveSize);

            zero2ArchiveHandler.MultiThreadAction(options.ThreadCount, zero2ArchiveHandler.FileDb.ArchiveFiles, zero2ArchiveHandler.ExtractArchives);

            if (options.ConvertFiles)
            {
                zero2ArchiveHandler.ConvertAudio();
            }

            watch.Stop();
            Console.WriteLine($"Total elapsed time: {watch.ElapsedMilliseconds}");
            
            return 0;
        }
    }
}
