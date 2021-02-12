﻿using CommandLine;

namespace Zero2Unpacker
{
    public class BasicOptions
    {
        [Option('f', "folder", Required = true, HelpText = "Folder where the IMG_BD.BIN is located.")]
        public string FolderName { get; set; }

        [Option('b', "bin", Default = "IMG_BD_US.BIN", Required = false, HelpText = "Name of the IMG_BD.BIN file, with extension.")]
        public string BinFileName { get; set; }

        [Option('c', "convert", Default = false, Required = false, HelpText = "Should the program convert PS2 files to common file formats.")]
        public bool ConvertFiles { get; set; }

        [Option('t', "thread", Default = 12, Required = false, HelpText = "Number of threads the application runs on.")]
        public int ThreadCount { get; set; }
    }

	[Verb("extract", HelpText = "Extract the content of IMG_BD.BIN")]
	public class ExtractOptions: BasicOptions
	{
    }

    [Verb("decompress", HelpText = "Decompresses the already extracted file from .BIN")]
    public class DecompressOptions : BasicOptions
    {
        [Option('a', "archivesize", Default = 1822, Required = false, HelpText = "Number of LESS archive files.")]
        public int ArchiveSize { get; set; }

        [Option('d', "dbfile", Required = false, HelpText = "File with path generated by a previous run of the program, contains all files information.")]
        public string DatabaseFile { get; set; }
    }
}
