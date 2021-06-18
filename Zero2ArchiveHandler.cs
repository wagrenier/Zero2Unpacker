#nullable enable
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
        public readonly FileDb FileDb;
        private readonly string _fileName;
        private readonly string _directory;

        public Zero2ArchiveHandler(string fileName, string directory, string? dbFile = null)
        {
            this._fileName = fileName;
            this._directory = directory;

            this.FileDb = string.IsNullOrWhiteSpace(dbFile) ? new FileDb() : JsonSerializer.Deserialize<FileDb>(File.ReadAllText(dbFile));
        }

        public void ExtractAll(int coreCount)
        {
            this.SplitArchives(new ZeroFile()
            {
                Folder = $"{this._directory}/Zero2/LESS/",
                FileName = "zeroFile",
                StartingPosition = 0,
                EndingPosition = 0,
                FileId = 0,
                FileHeader = new DeLESSFile()
            });

            this.MultiThreadAction(coreCount, this.FileDb.ArchiveFiles, this.DeLessFiles);
            this.MultiThreadAction(coreCount, this.FileDb.ArchiveFiles, this.ExtractArchives);
        }

        public void MultiThreadAction(int coreCount, List<ZeroFile> files, Action<object?> action)
        {
            // Split the list of files to handle into the number of available cores
            var listCoreSize = files.Count / coreCount;

            var threadList = new Task[coreCount];

            for (var i = 0; i < coreCount; i++)
            {
                threadList[i] = Task.Factory.StartNew(action, files.GetRange(i * listCoreSize, listCoreSize));
            }

            Task.WaitAll(threadList);

            this.FileDb.WriteDbToFile();
        }

        public void BuildAlreadyExistingDeLessArchive(int numberDeLessArchives)
        {
            for (var i = 0; i < numberDeLessArchives; i++)
            {
                var currentFile = new ZeroFile()
                {
                    FileId = i,
                    Folder = $"{this._directory}/Zero2/LESS/",
                    FileName = $"zeroFile{i}.LESS"
                };

                this.FileDb.ArchiveFiles.Add(currentFile);
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

            var currentFile = new ZeroFile
            {
                Folder = zeroFile.Folder,
                FileName = $"{zeroFile.FileName}{zeroFile.FileId}.{zeroFile.FileHeader.FileExtension}",
                FileId = zeroFile.FileId
            };

            Directory.CreateDirectory(zeroFile.Folder);

            var writer = new BinaryWriter(File.Open($"{currentFile.Folder}{currentFile.FileName}", FileMode.Create));

            // Skips the header of the first file
            writer.Write(gameArchiveBinReader.ReadBytes(0x10));

            this.FileDb.ArchiveFiles.Add(currentFile);

            // Loop until we reach the end of the file
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

                    currentFile = new ZeroFile
                    {
                        Folder = zeroFile.Folder,
                        FileName = $"{zeroFile.FileName}{zeroFile.FileId}.{zeroFile.FileHeader.FileExtension}",
                        FileId = zeroFile.FileId
                    };

                    writer = new BinaryWriter(File.Open($"{currentFile.Folder}{currentFile.FileName}", FileMode.Create));
                    this.FileDb.ArchiveFiles.Add(currentFile);
                    Console.WriteLine($"Discovered file: {currentFile.FileName}");
                }

                writer.Write(latestBytes);
            }

            writer.Close();
        }

        public void ExtractFiles(ZeroFile archiveFile, ZeroFile zeroFile, BlockingCollection<ZeroFile> files)
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

            BinaryWriter? writer = null;

            // Loop until we reach the end of the file
            while (gameArchiveBinReader.BaseStream.Position < fileSize)
            {
                // Read on line at a time
                var latestBytes = gameArchiveBinReader.ReadBytes(0x10);

                var readContainsHeader = latestBytes.FindBytesIndexInByteBuffer(currentHeaderLookUp);

                if (readContainsHeader < 0)
                {
                    if (fileFound)
                    {
                        writer?.Write(latestBytes);
                    }
                    continue;
                }

                if (fileFound)
                {
                    currentFile.EndingPosition = gameArchiveBinReader.BaseStream.Position - 0x10;

                    currentFile.FileSize = currentFile.EndingPosition - currentFile.StartingPosition;

                    zeroFile.FileId = totalFilesFound;

                    files.Add(currentFile);

                    currentHeaderLookUp = zeroFile.FileHeader.StartingBytes;

                    fileFound = false;
                    totalFilesFound++;

                    currentFile = new ZeroFile()
                    {
                        StartingPosition = gameArchiveBinReader.BaseStream.Position + 0x1,
                        EndingPosition = zeroFile.EndingPosition,
                        FileSize = zeroFile.EndingPosition - zeroFile.StartingPosition,
                        FileId = totalFilesFound,
                        FileName = zeroFile.FileName,
                        Folder = zeroFile.Folder,
                        FileHeader = zeroFile.FileHeader
                    };

                    if (zeroFile.FileHeader.EndingBytes == null)
                    {
                        writer?.Close();
                        gameArchiveBinReader.BaseStream.Position -= 0x10;
                    }
                    else
                    {
                        if (zeroFile.FileHeader.FileExtension != "pss")
                        {
                            writer?.Write(currentHeaderLookUp);
                        }
                        
                        writer?.Close();
                    }
                }
                else if (zeroFile.FileHeader.EndingBytes != null)
                {
                    currentFile.StartingPosition = gameArchiveBinReader.BaseStream.Position - 0x10;

                    currentHeaderLookUp = zeroFile.FileHeader.EndingBytes;
                    fileFound = true;

                    writer = new BinaryWriter(File.Open($"{currentFile.Folder}{currentFile.FileName}_{currentFile.FileId}.{zeroFile.FileHeader.FileExtension}", FileMode.Create));

                    gameArchiveBinReader.BaseStream.Position -= 0x10;
                }
                else
                {
                    currentFile.StartingPosition = gameArchiveBinReader.BaseStream.Position - 0x10;
                    fileFound = true;
                    writer = new BinaryWriter(File.Open($"{currentFile.Folder}{currentFile.FileName}_{currentFile.FileId}.{zeroFile.FileHeader.FileExtension}", FileMode.Create));
                    writer.Write(latestBytes);
                }
            }

            if (writer != null)
            {
                writer.Close();
            }
        }

        public void ExtractDxhFiles(ZeroFile archiveFile, ZeroFile zeroFile, BlockingCollection<ZeroFile> files)
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

        public void DeLessFiles(object? filesObj)
        {
            if (!(filesObj is List<ZeroFile> files))
            {
                return;
            }

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
            if (!(filesToExtractObj is List<ZeroFile> filesToExtract))
            {
                return;
            }

            foreach (var uncompressedFile in filesToExtract)
            {
                try
                {
                    var zeroFile = new ZeroFile()
                    {
                        FileName = $"zeroFile{uncompressedFile.FileId}",
                        Folder = $"{this._directory}/Zero2/Uncompressed/tm2/",
                        FileHeader = new Tim2File()
                    };

                    var zeroFilePss = new ZeroFile()
                    {
                        FileName = $"zeroFile{uncompressedFile.FileId}",
                        Folder = $"{this._directory}/Zero2/Uncompressed/pss/",
                        FileHeader = new PssFile()
                    };

                    var zeroFileStr = new ZeroFile()
                    {
                        FileName = $"zeroFile{uncompressedFile.FileId}",
                        Folder = $"{this._directory}/Zero2/Uncompressed/audio/",
                        FileHeader = new StrFile()
                    };

                    // Extracts cutscenes has one file instead of chunks
                    this.ExtractFiles(uncompressedFile, zeroFilePss, this.FileDb.VideoFiles);

                    // Extract Audio
                    this.ExtractDxhFiles(uncompressedFile, zeroFileStr, this.FileDb.AudioFiles);

                    // Extract Textures
                    uncompressedFile.FileName += ".LED";
                    this.ExtractFiles(uncompressedFile, zeroFile, this.FileDb.TextureFiles);

                    Console.WriteLine($"File: {uncompressedFile.FileName.Replace(".LED", "")}, extracted!");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Failed to create file for {uncompressedFile.FileName}! REASON: {e.Message}");
                }
            }
        }

        public void ConvertAudio(object? filesObj)
        {
            if (!(filesObj is List<ZeroFile> audioFiles))
            {
                return;
            }

            foreach (var audioFile in audioFiles)
            {
                FileConverter.ConvertStrToWav(audioFile);
            }
        }
    }
}
