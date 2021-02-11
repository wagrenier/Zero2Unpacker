using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Zero2Unpacker
{
    public class Zero2ArchiveHandler
    {
        public List<ArchiveFile> ArchiveFiles = new List<ArchiveFile>();
        private readonly FileDb _fileDb = new FileDb();
        private readonly string _fileName;
        private readonly string _directory;

        public Zero2ArchiveHandler(string fileName, string directory)
        {
            this._fileName = fileName;
            this._directory = directory;
        }

        public void ExtractAll(int coreCount = 12)
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

            this.DeLessFiles(this.ArchiveFiles);
            this.MultiThreadExtract(coreCount);
        }

        public void MultiThreadExtract(int numberCores)
        {
            // Split the list of files to handle into the number of available cores
            var listCoreSize = this.ArchiveFiles.Count / numberCores;

            var threadList = new Task[numberCores];

            for (var i = 0; i < numberCores; i++)
            {
                threadList[i] = Task.Factory.StartNew(this.ExtractArchives, this.ArchiveFiles.GetRange(i * listCoreSize, listCoreSize));
            }

            Task.WaitAll(threadList);

            this._fileDb.WriteDbToFile();
        }

        public void BuildAlreadyExistingDeLessArchive(int numberDeLESSArchives)
        {
            for (var i = 0; i < numberDeLESSArchives; i++)
            {
                var currentFile = new ArchiveFile()
                {
                    FileId = i,
                    Folder = $"{this._directory}/Zero/LESS/",
                    FileName = $"zeroFile{i}.LESS"
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
                Folder = zeroFile.Folder,
                FileName = $"{zeroFile.FileName}{zeroFile.FileId}.{zeroFile.FileHeader.FileExtension}",
                FileId = zeroFile.FileId
            };

            Directory.CreateDirectory(zeroFile.Folder);

            var writer = new BinaryWriter(File.Open($"{currentFile.Folder}{currentFile.FileName}", FileMode.Create));

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
                        Folder = zeroFile.Folder,
                        FileName = $"{zeroFile.FileName}{zeroFile.FileId}.{zeroFile.FileHeader.FileExtension}",
                        FileId = zeroFile.FileId
                    };

                    writer = new BinaryWriter(File.Open($"{currentFile.Folder}{currentFile.FileName}", FileMode.Create));
                    this.ArchiveFiles.Add(currentFile);
                }

                writer.Write(latestBytes);
            }

            writer.Close();
        }

        public void ExtractFiles(ZeroFile zeroFile, byte[] fileBuffer, BlockingCollection<ZeroFile> files)
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
                        fileBuffer.WriteBufferRangeToFile(zeroFile, files);

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
            fileBuffer.WriteBufferRangeToFile(zeroFile, files);
        }

        public void ExtractDxhFiles(ArchiveFile archiveFile, ZeroFile zeroFile, BlockingCollection<ZeroFile> files)
        {
            var fileSize = new FileInfo($"{archiveFile.Folder}{archiveFile.FileName}").Length;
            using var gameArchiveBinReader = new BinaryReader(new FileStream($"{archiveFile.Folder}{archiveFile.FileName}", FileMode.Open, FileAccess.Read));

            Directory.CreateDirectory(zeroFile.Folder);

            var totalFilesFound = 0;
            var fileFound = false;

            var currentFile = new ZeroFile()
            {
                StartingPosition = 0,
                FileSize = zeroFile.EndingPosition - zeroFile.StartingPosition,
                FileId = totalFilesFound,
                FileName = zeroFile.FileName,
                Folder = zeroFile.Folder,
                FileHeader = zeroFile.FileHeader
            };

            var currentHeaderLookUp = zeroFile.FileHeader.StartingBytes;

            //Loop until we reach the end of the file
            while (gameArchiveBinReader.BaseStream.Position < fileSize)
            {
                // Read on line at a time
                var latestBytes = gameArchiveBinReader.ReadBytes(0x10);

                var readContainsHeader = latestBytes.FindBytesIndexInByteBuffer(currentHeaderLookUp);

                if (readContainsHeader < 0)
                {
                    continue;
                }

                if (fileFound)
                {
                    var temp = gameArchiveBinReader.BaseStream.Position - 0x10;
                    gameArchiveBinReader.BaseStream.Position = currentFile.EndingPosition;

                    currentFile.EndingPosition = temp;

                    gameArchiveBinReader.BaseStream.Position =
                        gameArchiveBinReader.BinaryStreamFindArrayBackwards(ByteExtensionMethods.EmptyHeader,
                            fileSize);


                    currentFile.StartingPosition = (int)gameArchiveBinReader.BaseStream.Position;

                    currentFile.FileSize = currentFile.EndingPosition - currentFile.StartingPosition;

                    zeroFile.FileId = totalFilesFound;

                    using var writer = new BinaryWriter(File.Open($"{currentFile.Folder}{currentFile.FileName}_{currentFile.FileId}.{zeroFile.FileHeader.FileExtension}", FileMode.Create));
                    writer.Write(gameArchiveBinReader.ReadBytes((int)currentFile.EndingPosition - (int)currentFile.StartingPosition));
                    writer.Close();
                    files.Add(currentFile);

                    currentHeaderLookUp = zeroFile.FileHeader.StartingBytes;

                    fileFound = false;
                    totalFilesFound++;

                    currentFile = new ZeroFile()
                    {
                        StartingPosition = (int)gameArchiveBinReader.BaseStream.Position + 1,
                        EndingPosition = zeroFile.EndingPosition,
                        FileSize = zeroFile.EndingPosition - zeroFile.StartingPosition,
                        FileId = totalFilesFound,
                        FileName = zeroFile.FileName,
                        Folder = zeroFile.Folder,
                        FileHeader = zeroFile.FileHeader
                    };

                }
                else if (zeroFile.FileHeader.EndingBytes != null)
                {
                    currentFile.EndingPosition = (int)gameArchiveBinReader.BaseStream.Position - 0x10;

                    currentHeaderLookUp = zeroFile.FileHeader.EndingBytes;
                    fileFound = true;
                }
            }
        }

        public void ExtractDxhFiles(ZeroFile zeroFile, byte[] fileBuffer, BlockingCollection<ZeroFile> files)
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

                        zeroFile.StartingPosition = fileBuffer.FindBytesIndexBackWardInByteBuffer(ByteExtensionMethods.EmptyHeader, (int)zeroFile.StartingPosition);

                        zeroFile.FileId = totalFilesFound;

                        fileBuffer.WriteBufferRangeToFile(new ZeroFile()
                        {
                            StartingPosition = zeroFile.StartingPosition,
                            EndingPosition = zeroFile.EndingPosition,
                            FileSize = zeroFile.EndingPosition - zeroFile.StartingPosition,
                            FileId = totalFilesFound,
                            FileName = zeroFile.FileName,
                            Folder = zeroFile.Folder,
                            FileHeader = zeroFile.FileHeader
                        }, files);

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

        public void DeLessFiles(List<ArchiveFile> files)
        {
            foreach (var file in files)
            {
                Console.WriteLine($"Unarchiving file: {file.FileName}");
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = $"DeLESS.exe",
                        WorkingDirectory = this._directory,
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        Arguments = $"{file.Folder}{file.FileName}"
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
                    //var fileBytes = File.ReadAllBytes($"{uncompressedFile.Folder}{uncompressedFile.FileName}");
                    //var fileBytesLED = File.ReadAllBytes($"{uncompressedFile.Folder}{uncompressedFile.FileName}.LED");

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
                    //this.ExtractFiles(zeroFilePss, fileBytes, this._fileDb.VideoFiles);
                    //this.ExtractDxhFiles(zeroFileStr, fileBytes, this._fileDb.AudioFiles);
                    this.ExtractDxhFiles(uncompressedFile, zeroFileStr, this._fileDb.AudioFiles);

                    Console.WriteLine($"File: {uncompressedFile.FileName}, extracted!");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Failed to create file for {uncompressedFile.FileName}! REASON: {e.Message}");
                }
            }
        }

        public void ConvertAudio()
        {
            foreach (var audioFile in this._fileDb.AudioFiles)
            {
                FileConverter.ConvertStrToWav(audioFile);
            }
        }
    }
}
