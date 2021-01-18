using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Zero2Unpacker
{
    public class DeLESSFile
    {
        public long startingPosition = 0;
        public long endingPosition = 0;
        public long fileSize = 0;
    }

    public class DataReader
    {
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
            var currFile = new DeLESSFile();
            currFile.startingPosition = 0;

            var pattern = new byte[] {0x4c, 0x45, 0x53, 0x53};

            // Ships the initial DeLESS Header
            this.dataStream.BaseStream.Position = 0x8;

            var searchPosition = 0;

            while (this.dataStream.BaseStream.Position < img_size) //Loop until we reach the end of the file
            {
                var latestbyte = this.dataStream.ReadByte();

                if (latestbyte == -1)
                {
                    break; //We have reached the end of the file
                }

                if (latestbyte != pattern[searchPosition])
                {
                    searchPosition = 0;
                    //break; //We have reached the end of the file
                }

                else if (latestbyte == pattern[searchPosition])
                {
                    searchPosition++;
                    if (searchPosition == pattern.Length)
                    {
                        currFile.endingPosition = this.dataStream.BaseStream.Position - 0x5;
                        delessFiles.Add(currFile);

                        currFile = new DeLESSFile()
                        {
                            startingPosition = this.dataStream.BaseStream.Position - 0x4
                        };

                        searchPosition = 0;
                    }
                }
            }

            currFile.endingPosition = this.dataStream.BaseStream.Position;
            delessFiles.Add(currFile);

        }
    }
}
