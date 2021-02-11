using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Zero2Unpacker
{
    public static class ByteExtensionMethods
    {
        public static byte[] EmptyHeader = new byte[]
        {
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };

        /// <summary>
        /// Find a byte pattern in a byte buffer in reverse.
        /// </summary>
        /// <param name="fileBuffer"></param>
        /// <param name="bytesToFind"></param>
        /// <param name="startingIndex"></param>
        /// <returns>Index of the beginning of the bytesToFind array in the fileBuffer.</returns>
        public static int FindBytesIndexBackWardInByteBuffer(this byte[] fileBuffer, byte[] bytesToFind, int startingIndex = 0)
        {
            var searchPosition = 0;
            for (var i = startingIndex; i > 0; i--)
            {
                if (fileBuffer[i] != bytesToFind[searchPosition])
                {
                    searchPosition = bytesToFind.Length - 1;
                }
                else if (fileBuffer[i] == bytesToFind[searchPosition])
                {
                    searchPosition--;
                    if (searchPosition != -1)
                    {
                        continue;
                    }

                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Find a byte pattern in a byte buffer.
        /// </summary>
        /// <param name="fileBuffer"></param>
        /// <param name="bytesToFind"></param>
        /// <param name="startingIndex"></param>
        /// <returns>Index of the beginning of the bytesToFind array in the fileBuffer.</returns>
        public static int FindBytesIndexInByteBuffer(this byte[] fileBuffer, byte[] bytesToFind, int startingIndex = 0)
        {
            var searchPosition = 0;
            for (var i = startingIndex; i < fileBuffer.Length; i++)
            {
                if (fileBuffer[i] != bytesToFind[searchPosition])
                {
                    searchPosition = 0;
                }
                else if (fileBuffer[i] == bytesToFind[searchPosition])
                {
                    searchPosition++;
                    if (searchPosition != bytesToFind.Length)
                    {
                        continue;
                    }

                    return i;
                }
            }
            return -1;
        }

        public static long BinaryStreamFindArrayBackwards(this BinaryReader binReader, byte[] bytesToFind, long fileSize)
        {
            while (binReader.BaseStream.Position < fileSize && binReader.BaseStream.Position > 0)
            {
                var latestBytes = binReader.ReadBytes(0x10);

                var index = latestBytes.FindBytesIndexInByteBuffer(EmptyHeader);

                if (index > -1)
                {
                    return binReader.BaseStream.Position - 0x10;
                }

                binReader.BaseStream.Position -= 0x20;
            }

            return -1;
        }

        /// <summary>
        /// Writes a buffer range to a new file.
        /// </summary>
        /// <param name="fileBuffer"></param>
        /// <param name="zeroFile"></param>
        /// <param name="files"></param>
        public static void WriteBufferRangeToFile(this byte[] fileBuffer, ZeroFile zeroFile, BlockingCollection<ZeroFile> files)
        {
            files.Add(new ZeroFile()
            {
                StartingPosition = zeroFile.StartingPosition,
                EndingPosition = zeroFile.EndingPosition,
                FileSize = zeroFile.EndingPosition - zeroFile.StartingPosition,
                FileId = zeroFile.FileId,
                FileName = zeroFile.FileName,
                Folder = zeroFile.Folder,
                FileHeader = zeroFile.FileHeader
            });

            Directory.CreateDirectory(zeroFile.Folder);
            using var writer = new BinaryWriter(File.Open($"{zeroFile.Folder}{zeroFile.FileName}_{zeroFile.FileId}.{zeroFile.FileHeader.FileExtension}", FileMode.Create));

            for (var i = zeroFile.StartingPosition; i < zeroFile.EndingPosition; i++)
            {
                writer.Write(fileBuffer[i]);
            }
        }
    }
}