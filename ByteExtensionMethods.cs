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
        public static int FindBytesIndexBackWardInByteBuffer(this byte[] fileBuffer, byte[] bytesToFind, int startingIndex)
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
        public static int FindBytesIndexInByteBuffer(this byte[] fileBuffer, byte[] bytesToFind, int startingIndex)
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
    }
}