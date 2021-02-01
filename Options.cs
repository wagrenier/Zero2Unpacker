using CommandLine;

namespace Zero2Unpacker
{
	[Verb("extract", HelpText = "Extract the content of IMG_BD.BIN")]
	public class ExtractOptions
	{
        [Option('f', "folder", Required = true, HelpText = "Folder where the IMG_BD.BIN is located.")]
        public string FolderName { get; set; }

        [Option('b', "bin", Default = "IMG_BD_US.BIN", Required = false, HelpText = "Name of the IMG_BD.BIN file, with extension.")]
        public string BinFileName { get; set; }
    }

    [Verb("decompress", HelpText = "Extract the content of IMG_BD.BIN")]
    public class DecompressOptions
    {
        [Option('f', "folder", Required = true, HelpText = "Folder where the IMG_BD.BIN is located.")]
        public string FolderName { get; set; }

        [Option('b', "bin", Default = "IMG_BD_US.BIN", Required = false, HelpText = "Name of the IMG_BD.BIN file, with extension.")]
        public string BinFileName { get; set; }

        [Option('a', "archivesize", Default = 1822, Required = false, HelpText = "Number of LESS archive files.")]
        public int ArchiveSize { get; set; }
    }
}
