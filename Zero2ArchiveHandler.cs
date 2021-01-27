using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

    public class FileHeader
    {
        public int HeaderSize;
        public byte[] StartingBytes;
        public byte[] EndingBytes;
        public string FileType;
        public string FileExtension;
    }

    public class PssFile : FileHeader
    {

    }

    public class Pk4File : FileHeader
    {

    }

    public class Zero2ArchiveHandler
    {
        public int delessHeaderSize = 0x8;
        public byte[] delessHeader = new byte[] {0x4c, 0x45, 0x53, 0x53};
        public byte[] tim2Header = new byte[] { 0x54, 0x49, 0x4D, 0x32 };
        public List<DeLESSFile> DelessFiles = new List<DeLESSFile>();
        private string fileName;
        private string directory;
        private long img_size;
        private FileInfo fileInfo;
        public int FileId = 0;
        private readonly int NumCores = 6;

        public Zero2ArchiveHandler(string fileName, string directory)
        {
            this.fileName = fileName;
            this.directory = directory;
            this.fileInfo = new FileInfo($"{this.directory}/{this.fileName}");
            this.img_size = this.fileInfo.Length;
        }

        public void MultiThreadExtract()
        {
            // Split the list of files to handle into the number of available cores
            // construct two threads for our demonstration;  
            var listCoreSize = this.DelessFiles.Count / this.NumCores;

            var threadList = new Task[this.NumCores];

            for (var i = 0; i < this.NumCores; i++)
            {
                threadList[i] = Task.Factory.StartNew(ExtractArchives, this.DelessFiles.GetRange(i * listCoreSize, listCoreSize));
            }

            Task.WaitAll(threadList);
        }

        public void ExtractAll()
        {
            this.SplitArchives();
            this.DeLESSFiles();
            this.MultiThreadExtract();
        }

        public void BuildAlreadyExistingDeLESSArchive(int numberDeLESSArchives)
        {
            for (var i = 0; i < numberDeLESSArchives; i++)
            {
                var currentFile = new DeLESSFile()
                {
                    FileId = this.FileId,
                    FileName = $"{this.directory}/Zero/zeroFile{this.FileId}.LESS"
                };

                this.DelessFiles.Add(currentFile);
                this.FileId++;
            }
        }

        /// <summary>
        /// Extracts all DeLESS archives from the bin file
        /// !! MUST NOT BE RAN IN MULTI THREADED CONTEXT !!
        /// </summary>
        public void SplitArchives()
        { 
            using var gameArchiveBinReader = new BinaryReader(new FileStream($"{this.directory}/{this.fileName}", FileMode.Open, FileAccess.Read));
            var fileBuffer = new List<byte>();

            var currentFile = new DeLESSFile
            {
                StartingPosition = 0,
                FileName = $"{this.directory}/Zero/zeroFile{this.FileId}.LESS"
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
                    this.DelessFiles.Add(currentFile);

                    fileBuffer.RemoveRange(fileBuffer.Count - 9, this.delessHeaderSize);

                    fileBuffer[^1] = 0x0;

                    this.CreateNewFile(fileBuffer.ToArray(), $"{this.directory}/Zero/zeroFile{this.FileId}.LESS");

                    // Reset the position to the beginning of the previous file
                    gameArchiveBinReader.BaseStream.Position -= this.delessHeaderSize;

                    currentFile = new DeLESSFile()
                    {
                        StartingPosition = gameArchiveBinReader.BaseStream.Position,
                        FileId = this.FileId,
                        FileName = $"{this.directory}/Zero/zeroFile{this.FileId}.LESS"
                    };

                    // Clear file buffer
                    fileBuffer.Clear();
                    searchPosition = 0;
                    this.SkipHeaderNewFile(gameArchiveBinReader, this.delessHeaderSize, fileBuffer);
                }
            }

            currentFile.EndingPosition = gameArchiveBinReader.BaseStream.Position;
            this.DelessFiles.Add(currentFile);
            this.CreateNewFile(fileBuffer.ToArray(), $"{this.directory}/Zero/zeroFile{this.FileId}.LESS");
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
            Console.WriteLine($"Creating LESS archive: {this.FileId}");
            using var writer = new BinaryWriter(File.Open(newFileName, FileMode.Create));
            writer.Write(fileBuffer);
            this.FileId++;
        }

        public void SplitTim2Files(string filename, string extensionFilename)
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

                            using var writer = new BinaryWriter(File.Open($"{extensionFilename}A{totalFilesFound}.tm2", FileMode.Create));
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

            using var writerFile = new BinaryWriter(File.Open($"{extensionFilename}A{totalFilesFound}.tm2", FileMode.Create));

            writerFile.Write(fileBuf.ToArray());
        }

        public void ExtractFiles(byte[] fileHeader, byte[] fileEnd)
        {
            // PSS Files
            // Starting bytes   :    var bytes = new bytes[]{00 00 01 BA 44 00 04 00 04};
            // Ending bytes     :    var bytes = new bytes[]{FF FF FF FF FF 00 00 01 B9);

            // PK4 Files
            // Starting bytes   :
            // Ending bytes     :

            /*
             * 1) Find the file HEADER
             * 2) Flip to record data
             * 3) Look for file END
             * 4) Save file
             *
             */
        }

        public void ExtractArchives(object? filesToExtractObj)
        {
            var filesToExtract = filesToExtractObj as List<DeLESSFile>;
            foreach (var delessFile in filesToExtract)
            {
                try
                {
                    this.SplitTim2Files($"{delessFile.FileName}.LED",
                        $"{this.directory}/Zero/Uncompressed/zeroFile{delessFile.FileId}");

                    Console.WriteLine($"File: {delessFile.FileName}, decompressed!");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Failed to create file for {delessFile.FileName}!");
                }
            }
        }

        public void DeLESSFiles()
        {
            foreach (var delessFile in this.DelessFiles)
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
            }
        }
    }
}
