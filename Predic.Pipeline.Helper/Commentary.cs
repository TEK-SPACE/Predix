using System;
using System.IO;

namespace Predic.Pipeline.Helper
{
    public static class Commentary
    {
        public static bool WriteToFile { get; set; }

        private static string FilePath { get; set; } =
            $"{Path.Combine("logs", Utility.ActiveBin, "logs", DateTime.Now.ToString("yyyyMMddhhmmss"))}.txt";

        public static void Print(string message, bool tabSpace = false)
        {
            var printMessage = tabSpace
                ? $"\t{DateTime.Now:G} => {message}"
                : $"{DateTime.Now:G} => {message}";
            Console.WriteLine(printMessage);
            if (!Directory.Exists(Path.Combine(Utility.ActiveBin, "logs")))
                Directory.CreateDirectory(Path.Combine(Utility.ActiveBin, "logs"));
            if (!WriteToFile) return;
            if (!File.Exists(FilePath))
            {
                using (var filestream = File.Create(FilePath))
                {
                    filestream.Close();
                }
            }

            using (var streamWriter = File.AppendText(FilePath))
            {
                streamWriter.WriteLine(printMessage);
                streamWriter.Close();
            }
        }
    }
}
