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
            var args = $"/IF44100 /IC2 /II1000 /IH0 /OTWAVU /OF44100 /OC2 /OI0 \"{orig}\" \"{dest}\"";

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

            if (!process.WaitForExit(10000))
            {
                try
                {
                    process.Kill(true);
                    Console.WriteLine($"Extracting audio file: {zeroFile.FileName}.{zeroFile.FileHeader.FileExtension} took too long, it was killed!");
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
