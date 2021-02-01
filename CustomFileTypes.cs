namespace Zero2Unpacker
{
    public interface IFileHeader
    {
        public int HeaderSize { get; }
        public int EndingSize { get; }
        public byte[] StartingBytes { get; }
        public byte[] EndingBytes { get; }
        public string FileExtension { get; }
    }

    public class MyFileHeader : IFileHeader
    {
        private static int _headerSize = 0x1;
        private static int _endingSize = 0x1;
        private static byte[] _startingBytes = new byte[]
        {
            0x4c, 0x45, 0x53, 0x53
        };

        private static byte[] mybytes = new byte[]
        {
            0x4c, 0x45, 0x53, 0x53
        };

        private static string _fileExtension = "abc";

        public byte[] EndingBytes => mybytes;

        public int HeaderSize => _headerSize;

        public int EndingSize => _endingSize;

        public byte[] StartingBytes => _startingBytes;

        public string FileExtension => _fileExtension;
    }

    public abstract class FileHeader
    {
        public int HeaderSize;
        public int EndingSize;
        public byte[] StartingBytes;
        public byte[] EndingBytes;
        public string FileExtension;
    }

    public class ArchiveFile
    {
        public string FileName;
        public int FileId = 0;
    }

    public class ZeroFile
    {
        public FileHeader FileHeader;
        public int FileId = 0;
        public int StartingPosition = 0;
        public int EndingPosition = 0;
        public long FileSize = 0;
        public string FileName = "zeroFile";
        public string Folder;
    }

    public class DeLESSFile : FileHeader
    {
        public DeLESSFile()
        {
            this.StartingBytes = new byte[]
            {
                0x4c, 0x45, 0x53, 0x53
            };

            this.HeaderSize = 0x8;
            this.FileExtension = "LESS";
        }
    }

    public class PssFile : FileHeader
    {
        public PssFile()
        {
            this.StartingBytes = new byte[]
            {
                0x00, 0x00, 0x01, 0xBA,
                0x44
            };

            this.EndingBytes = new byte[]
            {
                0x00, 0x00, 0x01, 0xB9
            };
            this.HeaderSize = this.StartingBytes.Length;
            this.EndingSize = this.EndingBytes.Length;
            this.FileExtension = "pss";
        }
    }

    public class Pk4File : FileHeader
    {
        // PK4 Files
        // Starting bytes   :
        // Ending bytes     :
    }

    public class StrFile : FileHeader
    {
        public StrFile()
        {
            this.StartingBytes = new byte[] { 
                0x00, 0x07, 0x77, 0x77, 
                0x77, 0x77, 0x77, 0x77, 
                0x77, 0x77, 0x77, 0x77, 
                0x77, 0x77, 0x77, 0x77
            };

            this.EndingBytes = new byte[]
            {
                0x00, 0x44, 0x58, 0x48,
                0x00, 0x10, 0x00, 0x00,
                0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
            };
            this.HeaderSize = this.StartingBytes.Length;
            this.EndingSize = this.EndingBytes.Length;
            this.FileExtension = "str";
        }
    }

    public class Tim2File : FileHeader
    {
        public Tim2File()
        {
            this.StartingBytes = new byte[]
            {
                0x54, 0x49, 0x4D, 0x32
            };

            this.HeaderSize = this.StartingBytes.Length;
            this.FileExtension = "tm2";
        }
    }
}
