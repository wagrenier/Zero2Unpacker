using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;

namespace Zero2Unpacker
{
    public class FileDb
    {
        public List<ArchiveFile> ArchiveFiles { get; set; }
        public BlockingCollection<ZeroFile> VideoFiles { get; set; }
        public BlockingCollection<ZeroFile> TextureFiles { get; set; }
        public BlockingCollection<ZeroFile> AudioFiles { get; set; }

        public FileDb()
        {
            ArchiveFiles = new List<ArchiveFile>();
            VideoFiles = new BlockingCollection<ZeroFile>();
            TextureFiles = new BlockingCollection<ZeroFile>();
            AudioFiles = new BlockingCollection<ZeroFile>();
        }

        public void WriteDbToFile()
        {
            System.IO.File.WriteAllText(@"D:\zeroDB.json", JsonSerializer.Serialize(this));
        }
    }
}
