using System;
using System.IO;
using System.Threading;

namespace DialogGenerator.UI.Helpers
{
    public static class FileHelper
    {
        public static void ClearDirectory(string path)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            DirectoryInfo _directoryInfo = new DirectoryInfo(path);

            foreach (FileInfo file in _directoryInfo.GetFiles())
            {
                bool _isDeleted = false;
                int counter = 0;

                do
                {
                    try
                    {
                        if (File.Exists(file.FullName))
                        {
                            File.Delete(file.FullName);
                        }
                        _isDeleted = true;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(500);
                        counter++;
                    }
                }
                while (!_isDeleted && counter <= 16); // wait 8 seconds to delete file 
            }

            foreach (DirectoryInfo dir in _directoryInfo.GetDirectories())
            {
                bool _isDeleted = false;
                int counter = 0;

                do
                {
                    try
                    {
                        if (Directory.Exists(dir.FullName))
                        {
                            dir.Delete(true);
                        }

                        _isDeleted = true;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(500);
                        counter++;
                    }
                }
                while (!_isDeleted && counter <= 16); // wait 8 seconds to delete file 
            }
        }
    }
}
