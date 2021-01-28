using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Zero2Unpacker
{
    public class Zero2ArchiveHandler
    {
        public List<ArchiveFile> ArchiveFiles = new List<ArchiveFile>();
        private readonly string _fileName;
        private readonly string _directory;

        public Zero2ArchiveHandler(string fileName, string directory)
        {
            this._fileName = fileName;
            this._directory = directory;
        }

        public void ExtractAll()
        {

            this.SplitArchives(new ZeroFile()
            {
                Folder = $"{this._directory}/Zero/LESS/",
                FileName = "zeroFile",
                StartingPosition = 0,
                EndingPosition = 0,
                FileId = 0,
                FileHeader = new DeLESSFile()
            });

            this.DeLESSFiles();
            this.MultiThreadExtract(12);
        }

        public void MultiThreadExtract(int numberCores)
        {
            // Split the list of files to handle into the number of available cores
            // construct two threads for our demonstration;  
            var listCoreSize = this.ArchiveFiles.Count / numberCores;

            var threadList = new Task[numberCores];

            for (var i = 0; i < numberCores; i++)
            {
                threadList[i] = Task.Factory.StartNew(this.ExtractArchives, this.ArchiveFiles.GetRange(i * listCoreSize, listCoreSize));
            }

            Task.WaitAll(threadList);
        }

        public void BuildAlreadyExistingDeLESSArchive(int numberDeLESSArchives)
        {
            for (var i = 0; i < numberDeLESSArchives; i++)
            {
                var currentFile = new ArchiveFile()
                {
                    FileId = i,
                    FileName = $"{this._directory}/Zero/zeroFile{i}.LESS"
                };

                this.ArchiveFiles.Add(currentFile);
            }
        }

        /// <summary>
        /// Extracts all DeLESS archives from the bin file
        /// !! MUST NOT BE RAN IN MULTI THREADED CONTEXT !!
        /// </summary>
        public void SplitArchives(ZeroFile zeroFile)
        {
            var fileSize = new FileInfo($"{this._directory}/{this._fileName}").Length;;
            using var gameArchiveBinReader = new BinaryReader(new FileStream($"{this._directory}/{this._fileName}", FileMode.Open, FileAccess.Read));

            var currentFile = new ArchiveFile
            {
                FileName = $"{this._directory}/Zero/zeroFile{zeroFile.FileId}.LESS",
                FileId = zeroFile.FileId
            };

            var writer = new BinaryWriter(File.Open($"{currentFile.FileName}", FileMode.Create));

            // Skips the header of the first file
            writer.Write(gameArchiveBinReader.ReadBytes(0x10));

            this.ArchiveFiles.Add(currentFile);

            //Loop until we reach the end of the file
            while (gameArchiveBinReader.BaseStream.Position < fileSize)
            {
                // Read on line at a time
                var latestBytes = gameArchiveBinReader.ReadBytes(0x10);

                var readContainsHeader = latestBytes.FindBytesIndexInByteBuffer(zeroFile.FileHeader.StartingBytes);

                if (readContainsHeader > 0)
                {
                    // A new file has been found, close the stream a write to a new file
                    writer.Close();
                    
                    zeroFile.FileId++;

                    currentFile = new ArchiveFile
                    {
                        FileName = $"{this._directory}/Zero/zeroFile{zeroFile.FileId}.LESS",
                        FileId = zeroFile.FileId
                    };

                    writer = new BinaryWriter(File.Open($"{currentFile.FileName}", FileMode.Create));
                    this.ArchiveFiles.Add(currentFile);
                }

                writer.Write(latestBytes);
            }

            writer.Close();
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

                        zeroFile.StartingPosition = fileBuffer.FindBytesIndexBackWardInByteBuffer(ByteExtensionMethods.EmptyHeader, zeroFile.StartingPosition);


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
            foreach (var file in this.ArchiveFiles)
            {
                Console.WriteLine($"Unarchiving file: {file.FileName}");
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = $"{this._directory}/DeLESS.exe",
                        WorkingDirectory = this._directory,
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
            if (!(filesToExtractObj is List<ArchiveFile> filesToExtract))
            {
                return;
            }

            foreach (var uncompressedFile in filesToExtract)
            {
                try
                {
                    var fileBytes = File.ReadAllBytes($"{uncompressedFile.FileName}");
                    //var fileBytesLED = File.ReadAllBytes($"{uncompressedFile.FileName}.LED");

                    var zeroFile = new ZeroFile()
                    {
                        FileName = $"zeroFile{uncompressedFile.FileId}",
                        Folder = $"{this._directory}/Zero/Uncompressed/tm2/",
                        FileHeader = new Tim2File()
                    };

                    var zeroFilePss = new ZeroFile()
                    {
                        FileName = $"zeroFile{uncompressedFile.FileId}",
                        Folder = $"{this._directory}/Zero/Uncompressed/pss/",
                        FileHeader = new PssFile()
                    };

                    var zeroFileStr = new ZeroFile()
                    {
                        FileName = $"zeroFile{uncompressedFile.FileId}",
                        Folder = $"{this._directory}/Zero/Uncompressed/audio/",
                        FileHeader = new StrFile()
                    };

                    //this.ExtractFiles(zeroFile, fileBytesLED);
                    this.ExtractFiles(zeroFilePss, fileBytes);
                    //this.ExtractDxhFiles(zeroFileStr, fileBytes);

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
