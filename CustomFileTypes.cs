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

    public class StrFile : IFileHeader
    {
        private static int _headerSize = 0xF;
        private static int _endingSize = 0xF;

        private static readonly byte[] _startingBytes = new byte[] {
            0x00, 0x07, 0x77, 0x77,
            0x77, 0x77, 0x77, 0x77,
            0x77, 0x77, 0x77, 0x77,
            0x77, 0x77, 0x77, 0x77
        };

        private static readonly byte[] _endingBytes = new byte[]
        {
            0x00, 0x44, 0x58, 0x48,
            0x00, 0x10, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };

        private static string _fileExtension = "str";

        public byte[] StartingBytes => _startingBytes;

        public byte[] EndingBytes => _endingBytes;

        public int HeaderSize => _headerSize;

        public int EndingSize => _endingSize;

        public string FileExtension => _fileExtension;
    }

    public class Tim2File : IFileHeader
    {
        private static int _headerSize = 0x4;

        private static readonly byte[] _startingBytes = new byte[]
        {
            0x54, 0x49, 0x4D, 0x32
        };

        private static string _fileExtension = "tm2";

        public byte[] StartingBytes => _startingBytes;

        public byte[] EndingBytes => null;

        public int HeaderSize => _headerSize;

        public int EndingSize => -1;

        public string FileExtension => _fileExtension;
    }

    public class PssFile : IFileHeader
    {
        private static int _headerSize = 0x5;
        private static int _endingSize = 0x8;

        private static readonly byte[] _startingBytes = new byte[]
        {
            0x00, 0x00, 0x01, 0xBA,
            0x44
        };

        private static readonly byte[] _endingBytes = new byte[]
        {
            0x00, 0x00, 0x01, 0xB9,
            0x00, 0x00, 0x00, 0x00
        };

        private static string _fileExtension = "pss";

        public byte[] StartingBytes => _startingBytes;

        public byte[] EndingBytes => _endingBytes;

        public int HeaderSize => _headerSize;

        public int EndingSize => _endingSize;

        public string FileExtension => _fileExtension;
    }

    public class DeLESSFile : IFileHeader
    {
        // First 4 bytes indicate size when decompressed
        // Second line, bytes from 0xC to 0xF indicate compressed file size

        private static int _headerSize = 0x8;

        private static readonly byte[] _startingBytes = new byte[]
        {
            0x4c, 0x45, 0x53, 0x53
        };

        private static string _fileExtension = "LESS";

        public byte[] StartingBytes => _startingBytes;

        public byte[] EndingBytes => null;

        public int HeaderSize => _headerSize;

        public int EndingSize => 0x0;

        public string FileExtension => _fileExtension;
    }

    public class Pk4File : IFileHeader
    {
        // PK4 Files
        // Starting bytes   :
        // Ending bytes     :

        private static int _headerSize = 0xF;
        private static int _endingSize = 0xF;

        private static readonly byte[] _startingBytes = new byte[] {
            0x00, 0x07, 0x77, 0x77,
            0x77, 0x77, 0x77, 0x77,
            0x77, 0x77, 0x77, 0x77,
            0x77, 0x77, 0x77, 0x77
        };

        private static readonly byte[] _endingBytes = new byte[]
        {
            0x00, 0x44, 0x58, 0x48,
            0x00, 0x10, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };

        private static string _fileExtension = "str";

        public byte[] StartingBytes => _startingBytes;

        public byte[] EndingBytes => _endingBytes;

        public int HeaderSize => _headerSize;

        public int EndingSize => _endingSize;

        public string FileExtension => _fileExtension;
    }

    public class ZeroFile
    {
        public IFileHeader FileHeader;
        public int FileId { get; set; }
        public long StartingPosition { get; set; }
        public long EndingPosition { get; set; }
        public long FileSize { get; set; }
        public string FileName { get; set; }
        public string Folder { get; set; }

        public ZeroFile()
        {
            this.FileId = 0;
            this.StartingPosition = 0;
            this.EndingPosition = 0;
            this.FileSize = 0;
            this.FileName = "zeroFile";
        }
    }
}
