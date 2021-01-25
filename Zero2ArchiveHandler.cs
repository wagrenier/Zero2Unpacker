using System;
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
        public int fileID = 0;
    }

    public class Zero2ArchiveHandler
    {
        public byte[] delessHeader = new byte[] {0x4c, 0x45, 0x53, 0x53};
        private string fileName;
        private string directory;
        private long img_size;
        private FileInfo fileInfo;
        private BinaryReader dataStream;
        public List<DeLESSFile> delessFiles = new List<DeLESSFile>();
        public List<byte> fileBuffer = new List<byte>();
        public int fileID = 0;

        public Zero2ArchiveHandler(string fileName, string directory)
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
                startingPosition = 0,
                fileName = $"{this.directory}/Zero/zeroFile{this.fileID}.LESS"
            };

            // Skips the initial DeLESS Header
            this.SkipHeaderNewFile();

            var searchPosition = 0;

            //Loop until we reach the end of the file
            while (this.dataStream.BaseStream.Position < this.img_size) 
            {
                var latestByte = this.dataStream.ReadByte();

                if (latestByte == -1)
                {
                    break;
                }

                this.fileBuffer.Add(latestByte);

                if (latestByte != this.delessHeader[searchPosition])
                {
                    searchPosition = 0;
                }
                else if (latestByte == this.delessHeader[searchPosition])
                {
                    searchPosition++;
                    if (searchPosition != this.delessHeader.Length)
                    {
                        continue;
                    }

                    // When the bytes fully match the header
                    currFile.endingPosition = this.dataStream.BaseStream.Position - 0x9;
                    this.delessFiles.Add(currFile);

                    this.fileBuffer.RemoveRange(this.fileBuffer.Count - 9, 8);

                    this.fileBuffer[^1] = 0x0;

                    this.CreateNewFile();

                    // Reset the position to the beginning of the previous file
                    this.dataStream.BaseStream.Position -= 0x8;

                    currFile = new DeLESSFile()
                    {
                        startingPosition = this.dataStream.BaseStream.Position,
                        fileID = this.fileID,
                        fileName = $"{this.directory}/Zero/zeroFile{this.fileID}.LESS"
                    };

                    // Clear file buffer
                    this.fileBuffer.Clear();
                    searchPosition = 0;
                    this.SkipHeaderNewFile();
                }
            }

            currFile.endingPosition = this.dataStream.BaseStream.Position;
            this.delessFiles.Add(currFile);
            this.CreateNewFile();
        }

        public void SkipHeaderNewFile()
        {
            for (var i = 0; i < 8; i++)
            {
                this.fileBuffer.Add(this.dataStream.ReadByte());
            }
        }

        public void CreateNewFile()
        {
            Console.WriteLine($"Creating LESS archive: {this.fileID}");
            using var writer = new BinaryWriter(File.Open($"{this.directory}/Zero/zeroFile{this.fileID}.LESS", FileMode.Create));
            writer.Write(this.fileBuffer.ToArray());
            this.fileID++;
        }

        public void TrimTIM2EmptyHeader(string filename)
        {

        }

        public void RunDeLESS()
        {
            foreach (var delessFile in this.delessFiles)
            {
                Console.WriteLine($"Unarchiving file: {delessFile.fileName}");
                System.Diagnostics.Process process = new System.Diagnostics.Process();

                process.StartInfo.FileName = $"{this.directory}\\DeLESS.exe";
                process.StartInfo.WorkingDirectory = this.directory;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.Arguments = delessFile.fileName;
                
                process.Start();
                process.WaitForExit();

                try
                {
                    //File.Delete(delessFile.fileName);
                    File.Move($"{delessFile.fileName}.LED", $"{this.directory}/Zero/zeroFile{delessFile.fileID}.tim2");
                    //File.Delete($"{delessFile.fileName}.LED");
                    Console.WriteLine($"File: {delessFile.fileName}, decompressed!");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Failed to create file for {delessFile.fileName}!");
                }

                
            }
        }
    }
}
