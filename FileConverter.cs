using System;
using System.Diagnostics;

namespace Zero2Unpacker
{
    public static class FileConverter
    {
        public static void ConvertStrToWav(ZeroFile zeroFile)
        {
            var orig = $"{zeroFile.Folder}{zeroFile.FileName}_{zeroFile.FileId}.{zeroFile.FileHeader.FileExtension}".Replace("/", "\\");
            var dest = $"{zeroFile.Folder}{zeroFile.FileName}_{zeroFile.FileId}.wav".Replace("/", "\\");
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

            if (!process.WaitForExit(5000))
            {
                try
                {
                    process.Kill(true);
                }
                catch (InvalidOperationException)
                {
                    // The process already finished by itself, so use the counter value.
                    //process.WaitForExit();
                }
            }
        }
    }
}
