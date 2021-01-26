using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Zero2Unpacker
{
    public class DeLESSFile
    {
        public long StartingPosition = 0;
        public long EndingPosition = 0;
        public long FileSize = 0;
        public string FileName;
        public int FileId = 0;
    }

    public class Zero2ArchiveHandler
    {
        public int delessHeaderSize = 0x8;
        public byte[] delessHeader = new byte[] {0x4c, 0x45, 0x53, 0x53};
        public byte[] tim2Header = new byte[] { 0x54, 0x49, 0x4D, 0x32 };
        private string fileName;
        private string directory;
        private long img_size;
        private FileInfo fileInfo;
        public List<DeLESSFile> delessFiles = new List<DeLESSFile>();
        public int fileID = 0;

        public Zero2ArchiveHandler(string fileName, string directory)
        {
            this.fileName = fileName;
            this.directory = directory;
            this.fileInfo = new FileInfo($"{this.directory}/{this.fileName}");
            this.img_size = this.fileInfo.Length;
        }

        public void BuildAlreadyExistingDeLESSArchive(int numberDeLESSArchives)
        {
            for (var i = 0; i < numberDeLESSArchives; i++)
            {
                var currentFile = new DeLESSFile()
                {
                    FileId = this.fileID,
                    FileName = $"{this.directory}/Zero/zeroFile{this.fileID}.LESS"
                };

                this.delessFiles.Add(currentFile);
                this.fileID++;
            }
        }

        public void SplitArchives()
        { 
            using var gameArchiveBinReader = new BinaryReader(new FileStream($"{this.directory}/{this.fileName}", FileMode.Open, FileAccess.Read));
            var fileBuffer = new List<byte>();

            var currentFile = new DeLESSFile
            {
                StartingPosition = 0,
                FileName = $"{this.directory}/Zero/zeroFile{this.fileID}.LESS"
            };

            // Skips the initial DeLESS Header
            this.SkipHeaderNewFile(gameArchiveBinReader, this.delessHeaderSize, fileBuffer);

            var searchPosition = 0;

            //Loop until we reach the end of the file
            while (gameArchiveBinReader.BaseStream.Position < this.img_size) 
            {
                var latestByte = gameArchiveBinReader.ReadByte();

                if (latestByte == -1)
                {
                    break;
                }

                fileBuffer.Add(latestByte);

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
                    currentFile.EndingPosition = gameArchiveBinReader.BaseStream.Position - 0x9;
                    this.delessFiles.Add(currentFile);

                    fileBuffer.RemoveRange(fileBuffer.Count - 9, this.delessHeaderSize);

                    fileBuffer[^1] = 0x0;

                    this.CreateNewFile(fileBuffer.ToArray(), $"{this.directory}/Zero/zeroFile{this.fileID}.LESS");

                    // Reset the position to the beginning of the previous file
                    gameArchiveBinReader.BaseStream.Position -= this.delessHeaderSize;

                    currentFile = new DeLESSFile()
                    {
                        StartingPosition = gameArchiveBinReader.BaseStream.Position,
                        FileId = this.fileID,
                        FileName = $"{this.directory}/Zero/zeroFile{this.fileID}.LESS"
                    };

                    // Clear file buffer
                    fileBuffer.Clear();
                    searchPosition = 0;
                    this.SkipHeaderNewFile(gameArchiveBinReader, this.delessHeaderSize, fileBuffer);
                }
            }

            currentFile.EndingPosition = gameArchiveBinReader.BaseStream.Position;
            this.delessFiles.Add(currentFile);
            this.CreateNewFile(fileBuffer.ToArray(), $"{this.directory}/Zero/zeroFile{this.fileID}.LESS");
        }

        public void SkipHeaderNewFile(BinaryReader dataStream, int headerSize, List<byte> fileBuffer)
        {
            for (var i = 0; i < headerSize; i++)
            {
                fileBuffer.Add(dataStream.ReadByte());
            }
        }

        public void CreateNewFile(byte[] fileBuffer, string newFileName)
        {
            Console.WriteLine($"Creating LESS archive: {this.fileID}");
            using var writer = new BinaryWriter(File.Open(newFileName, FileMode.Create));
            writer.Write(fileBuffer);
            this.fileID++;
        }

        public void TrimTIM2EmptyHeader(string filename, string extensionFilename)
        {
            var fileBytes = File.ReadAllBytes(filename);
            var searchPosition = 0;
            var fileBuf = new List<byte>();
            var headerFileBuf = new List<byte>();
            var fileFound = false;
            var totalFilesFound = 0;

            foreach (var currByte in fileBytes)
            {
                if (fileFound)
                {
                    fileBuf.Add(currByte);
                }
                else
                {
                    headerFileBuf.Add(currByte);
                }

                if (currByte != this.tim2Header[searchPosition])
                {
                    searchPosition = 0;
                }
                else if (currByte == this.tim2Header[searchPosition])
                {
                    searchPosition++;
                    if (searchPosition == this.tim2Header.Length)
                    {
                        if (fileFound)
                        {
                            fileBuf.RemoveRange(fileBuf.Count - 4, 4);

                            using var writer = new BinaryWriter(File.Open($"{extensionFilename}A{totalFilesFound}.tim2", FileMode.Create));
                            writer.Write(fileBuf.ToArray());
                            fileBuf.Clear();
                            totalFilesFound++;
                        }

                        fileBuf.AddRange(this.tim2Header);
                        fileFound = true;
                        searchPosition = 0;
                    }
                }
            }

            if (!fileFound)
            {
                return;
            }

            if (headerFileBuf.Count > this.tim2Header.Length)
            {
                headerFileBuf.RemoveRange(headerFileBuf.Count - 4, 4);
                using var writerHeader = new BinaryWriter(File.Open($"{extensionFilename}HEADER", FileMode.Create));
                writerHeader.Write(headerFileBuf.ToArray());
            }

            using var writerFile = new BinaryWriter(File.Open($"{extensionFilename}A{totalFilesFound}.tim2", FileMode.Create));

            writerFile.Write(fileBuf.ToArray());
        }

        public void RunDeLESS()
        {
            foreach (var delessFile in this.delessFiles)
            {
                Console.WriteLine($"Unarchiving file: {delessFile.FileName}");
                System.Diagnostics.Process process = new System.Diagnostics.Process();

                process.StartInfo.FileName = $"{this.directory}/DeLESS.exe";
                process.StartInfo.WorkingDirectory = this.directory;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.Arguments = delessFile.FileName;
                
                process.Start();
                process.WaitForExit();

                try
                {
                    this.TrimTIM2EmptyHeader($"{delessFile.FileName}.LED",
                        $"{this.directory}/Zero/Uncompressed/zeroFile{delessFile.FileId}");

                    Console.WriteLine($"File: {delessFile.FileName}, decompressed!");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Failed to create file for {delessFile.FileName}!");
                }
            }
        }
    }
}
