using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;

namespace Zero2Unpacker
{
    public class FileDb
    {
        public List<ZeroFile> ArchiveFiles { get; set; }
        public BlockingCollection<ZeroFile> VideoFiles { get; set; }
        public BlockingCollection<ZeroFile> TextureFiles { get; set; }
        public BlockingCollection<ZeroFile> AudioFiles { get; set; }

        public FileDb()
        {
            this.ArchiveFiles = new List<ZeroFile>();
            this.VideoFiles = new BlockingCollection<ZeroFile>();
            this.TextureFiles = new BlockingCollection<ZeroFile>();
            this.AudioFiles = new BlockingCollection<ZeroFile>();
        }

        public void WriteDbToFile()
        {
            System.IO.File.WriteAllText(@"zeroDB.json", JsonSerializer.Serialize(this));
        }
    }
}
