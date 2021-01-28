using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Zero2Unpacker
{
    public class Zero2ArchiveHandler
    {
        public int delessHeaderSize = 0x8;
        public byte[] delessHeader = new byte[] { 0x4c, 0x45, 0x53, 0x53 };
        public byte[] tim2Header = new byte[] { 0x54, 0x49, 0x4D, 0x32 };
        public List<DeLESSFile> DelessFiles = new List<DeLESSFile>();
        private string fileName;
        private string directory;
        private long img_size;
        private FileInfo fileInfo;
        public int FileId = 0;

        public Zero2ArchiveHandler(string fileName, string directory)
        {
            this.fileName = fileName;
            this.directory = directory;
            this.fileInfo = new FileInfo($"{this.directory}/{this.fileName}");
            this.img_size = this.fileInfo.Length;
        }

        public void MultiThreadExtract(int numberCores)
        {
            // Split the list of files to handle into the number of available cores
            // construct two threads for our demonstration;  
            var listCoreSize = this.DelessFiles.Count / numberCores;

            var threadList = new Task[numberCores];

            for (var i = 0; i < numberCores; i++)
            {
                threadList[i] = Task.Factory.StartNew(ExtractArchives, this.DelessFiles.GetRange(i * listCoreSize, listCoreSize));
            }

            Task.WaitAll(threadList);
        }

        public void ExtractAll()
        {
            this.SplitArchives();
            this.DeLESSFiles();
            this.MultiThreadExtract(6);
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

        public void ExtractTim2Files(string filename, string extensionFilename)
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

                            using var writer = new BinaryWriter(File.Open($"{extensionFilename}_{totalFilesFound}.tm2", FileMode.Create));
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
                //headerFileBuf.RemoveRange(headerFileBuf.Count - 4, 4);
                //using var writerHeader = new BinaryWriter(File.Open($"{extensionFilename}HEADER", FileMode.Create));
                //writerHeader.Write(headerFileBuf.ToArray());
            }

            using var writerFile = new BinaryWriter(File.Open($"{extensionFilename}_{totalFilesFound}.tm2", FileMode.Create));

            writerFile.Write(fileBuf.ToArray());
        }

        public void WriteBufferRangeToFile(ZeroFile zeroFile, byte[] fileBuffer)
        {
            Directory.CreateDirectory(zeroFile.Folder);
            using var writer = new BinaryWriter(File.Open($"{zeroFile.Folder}{zeroFile.FileName}_{zeroFile.FileId}.{zeroFile.FileHeader.FileExtension}", FileMode.Create));

            for (var i = zeroFile.StartingPosition; i < zeroFile.EndingPosition; i++)
            {
                writer.Write(fileBuffer[i]);
            }
        }

        public void ExtractFiles(ZeroFile zeroFile, byte[] fileBuffer)
        {
            /*
             * 1) Find the file HEADER
             * 2) Flip to record data
             * 3) Look for file END
             * 4) Save file
             * 5) GOTO 1) Until end of file
             */

            var searchPosition = 0;
            var totalFilesFound = 0;
            var fileFound = false;
            var currentHeaderLookUp = zeroFile.FileHeader.StartingBytes;
            var currentHeaderLookUpSize = zeroFile.FileHeader.HeaderSize;

            for (var i = 0; i < fileBuffer.Length; i++)
            {
                if (fileBuffer[i] != currentHeaderLookUp[searchPosition])
                {
                    searchPosition = 0;
                }
                else if (fileBuffer[i] == currentHeaderLookUp[searchPosition])
                {
                    searchPosition++;
                    if (searchPosition != currentHeaderLookUp.Length)
                    {
                        continue;
                    }

                    if (fileFound)
                    {
                        zeroFile.EndingPosition = i + 1;
                        zeroFile.FileId = totalFilesFound;
                        this.WriteBufferRangeToFile(zeroFile, fileBuffer);

                        currentHeaderLookUp = zeroFile.FileHeader.StartingBytes;
                        currentHeaderLookUpSize = zeroFile.FileHeader.HeaderSize;
                        fileFound = false;
                        totalFilesFound++;
                    }
                    else if(zeroFile.FileHeader.EndingBytes != null)
                    {
                        zeroFile.StartingPosition = i - currentHeaderLookUpSize + 1;
                        currentHeaderLookUp = zeroFile.FileHeader.EndingBytes;
                        currentHeaderLookUpSize = zeroFile.FileHeader.EndingSize;
                        fileFound = true;
                    }
                    else
                    {
                        zeroFile.StartingPosition = i - currentHeaderLookUpSize + 1;
                        fileFound = true;
                        totalFilesFound++;
                    }

                    searchPosition = 0;
                }
            }

            if (!fileFound)
            {
                return;
            }

            zeroFile.EndingPosition = fileBuffer.Length;
            zeroFile.FileId = totalFilesFound;
            this.WriteBufferRangeToFile(zeroFile, fileBuffer);
        }

        public void ExtractDxhFiles(ZeroFile zeroFile, byte[] fileBuffer)
        {
            /*
             * At first sigh, a few file types require this type of logic, everything with DXH after
             *
             *  1) Trouver cette ligne
             *  0x00, 0x07, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77, 0x77
             *
             *  2) Le file va se rendre jusqu'au header DXH
             *  00 44 58 48 00 10 00 00 02 00 00 00 00 00 00 00
             *
             *  3) Mettre le curseur actuel sur la ligne 1)
             *
             *  4) Le audio file sera donc la premiere ligne (inclue) de 00 avant 1) jusqu'au byte avant 2) 
             *  00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
             */

            var searchPosition = 0;
            var totalFilesFound = 0;
            var fileFound = false;
            var currentHeaderLookUp = zeroFile.FileHeader.StartingBytes;
            var currentHeaderLookUpSize = zeroFile.FileHeader.HeaderSize;

            for (var i = 0; i < fileBuffer.Length; i++)
            {
                if (fileBuffer[i] != currentHeaderLookUp[searchPosition])
                {
                    searchPosition = 0;
                }
                else if (fileBuffer[i] == currentHeaderLookUp[searchPosition])
                {
                    searchPosition++;
                    if (searchPosition != currentHeaderLookUp.Length)
                    {
                        continue;
                    }

                    if (fileFound)
                    {
                        zeroFile.EndingPosition = i - currentHeaderLookUpSize + 1;

                        zeroFile.StartingPosition = fileBuffer.FindBytesIndexBackWardInByteBuffer(ByteExtensionMethods.emptyHeader, zeroFile.StartingPosition);


                        zeroFile.FileId = totalFilesFound;
                        this.WriteBufferRangeToFile(zeroFile, fileBuffer);

                        currentHeaderLookUp = zeroFile.FileHeader.StartingBytes;
                        currentHeaderLookUpSize = zeroFile.FileHeader.HeaderSize;
                        fileFound = false;
                        totalFilesFound++;
                    }
                    else if (zeroFile.FileHeader.EndingBytes != null)
                    {
                        zeroFile.StartingPosition = i;
                        currentHeaderLookUp = zeroFile.FileHeader.EndingBytes;
                        currentHeaderLookUpSize = zeroFile.FileHeader.EndingSize;
                        fileFound = true;
                    }

                    searchPosition = 0;
                }
            }

            if (!fileFound)
            {
                return;
            }

            zeroFile.EndingPosition = fileBuffer.Length;
            zeroFile.FileId = totalFilesFound;
            //this.WriteBufferRangeToFile(zeroFile, fileBuffer);
        }

        public void DeLESSFiles()
        {
            foreach (var file in this.DelessFiles)
            {
                Console.WriteLine($"Unarchiving file: {file.FileName}");
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = $"{this.directory}/DeLESS.exe",
                        WorkingDirectory = this.directory,
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        Arguments = file.FileName
                    }
                };

                process.Start();
                process.WaitForExit();
            }
        }

        public void ExtractArchives(object? filesToExtractObj)
        {
            if (!(filesToExtractObj is List<DeLESSFile> filesToExtract))
            {
                return;
            }

            foreach (var uncompressedFile in filesToExtract)
            {
                try
                {
                    var fileBytes = File.ReadAllBytes($"{uncompressedFile.FileName}");

                    var zeroFile = new ZeroFile()
                    {
                        FileName = $"zeroFile{uncompressedFile.FileId}",
                        Folder = $"{this.directory}/Zero/Uncompressed/tm2/",
                        FileHeader = new Tim2File()
                    };

                    var zeroFilePss = new ZeroFile()
                    {
                        FileName = $"zeroFile{uncompressedFile.FileId}",
                        Folder = $"{this.directory}/Zero/Uncompressed/pss/",
                        FileHeader = new PssFile()
                    };

                    var zeroFileStr = new ZeroFile()
                    {
                        FileName = $"zeroFile{uncompressedFile.FileId}",
                        Folder = $"{this.directory}/Zero/Uncompressed/audio/",
                        FileHeader = new StrFile()
                    };

                    //this.ExtractFiles(zeroFile, fileBytes);
                    this.ExtractFiles(zeroFilePss, fileBytes);
                    //this.ExtractDxhFiles(zeroFileStr, fileBytes);

                    //this.ExtractTim2Files($"{uncompressedFile.FileName}.LED", $"{this.directory}/Zero/Uncompressed/timtest/zeroFile{uncompressedFile.FileId}");

                    Console.WriteLine($"File: {uncompressedFile.FileName}, extracted!");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Failed to create file for {uncompressedFile.FileName}! REASON: {e.Message}");
                }
            }
        }
    }
}
