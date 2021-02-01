using System;
using System.Diagnostics;

namespace Zero2Unpacker
{
    public static class FileConverter
    {
        public static void ConvertStrToWav(ZeroFile zeroFile, string dir)
        {
            var orig = $"{zeroFile.Folder}{zeroFile.FileName}.{zeroFile.FileHeader.FileExtension}".Replace("/", "\\");
            var dest = $"{zeroFile.Folder}{zeroFile.FileName}.wav".Replace("/", "\\");
            var args = $"/IF41000 /IC2 /II800 /IH0 /OTWAVU /OF41000 /OC2 /OI0 \"{orig}\" \"{dest}\"";

            Console.WriteLine($"Extracting audio file: {zeroFile.FileName}.{zeroFile.FileHeader.FileExtension}");
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "MFAudio.exe",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    Arguments = args
                }
            };

            process.Start();
            process.WaitForExit();
        }
    }
}
