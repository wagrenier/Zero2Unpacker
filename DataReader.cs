using System.Collections.Generic;
using System.IO;

namespace Zero2Unpacker
{
    public class DeLESSFile
    {
        public long startingPosition = 0;
        public long endingPosition = 0;
        public long fileSize = 0;
        public string fileName;
    }

    public class DataReader
    {
        public byte[] delessHeader = new byte[] {0x4c, 0x45, 0x53, 0x53};
        private string fileName;
        private string directory;
        private long img_size;
        private FileInfo fileInfo;
        private BinaryReader dataStream;
        public List<DeLESSFile> delessFiles = new List<DeLESSFile>();

        public DataReader(string fileName, string directory)
        {
            this.fileName = fileName;
            this.directory = directory;
            this.dataStream = new BinaryReader(new FileStream($"{this.directory}\\{this.fileName}", FileMode.Open, FileAccess.Read));
            this.fileInfo = new FileInfo($"{this.directory}\\{this.fileName}");
            this.img_size = this.fileInfo.Length;
        }

        public void SplitDeLESSArchives()
        {
            var currFile = new DeLESSFile
            {
                startingPosition = 0
            };

            var pattern = 

            // Ships the initial DeLESS Header
            this.dataStream.BaseStream.Position = 0x8;

            var searchPosition = 0;

            while (this.dataStream.BaseStream.Position < this.img_size) //Loop until we reach the end of the file
            {
                var latestByte = this.dataStream.ReadByte();

                if (latestByte == -1)
                {
                    break;
                }

                if (latestByte != this.delessHeader[searchPosition])
                {
                    searchPosition = 0;
                }
                else if (latestByte == this.delessHeader[searchPosition])
                {
                    searchPosition++;
                    if (searchPosition == this.delessHeader.Length)
                    {
                        currFile.endingPosition = this.dataStream.BaseStream.Position - 0x5;
                        this.delessFiles.Add(currFile);

                        currFile = new DeLESSFile()
                        {
                            startingPosition = this.dataStream.BaseStream.Position - 0x4
                        };

                        searchPosition = 0;
                    }
                }
            }

            currFile.endingPosition = this.dataStream.BaseStream.Position;
            this.delessFiles.Add(currFile);
        }

        public static void RunDeLESS()
        {
            var directoryName = "";
            var destfolderName = "";
            var destfileName = "";

            var command = $" {directoryName}\\{destfolderName.Replace("..", "")}{destfileName}";
            var path = Directory.GetCurrentDirectory();

            System.Diagnostics.Process process = new System.Diagnostics.Process();

            process.StartInfo.FileName = $"{path}\\DeLESS.exe";
            process.StartInfo.WorkingDirectory = path;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Arguments = command;
            process.Start();
            process.WaitForExit();

            var dname = $"{directoryName}\\{destfolderName.Replace("..", "")}{destfileName}";
            var temp = $"{directoryName}\\{destfolderName.Replace("..", "")}{destfileName}.LED";

            File.Delete(dname);
            File.Move(temp, dname);
        }
    }
}
