﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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

        /// <summary>
        /// Loads the character from the ZIP file with the changed header.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="file"></param>
        public static void LoadCharacter(string directory, string fileName) 
        {
            // Header content.
            string headerContent = "TOYS2LIFE_THE_DIALOG_GENERATOR";
            int headerLength = headerContent.Length;

            // Open file.
            byte[] initialFileBytes = File.ReadAllBytes(fileName);

            // Read header.
            byte[] headerBytes = new byte[headerLength];
            Array.Copy(initialFileBytes, 0, headerBytes, 0, headerLength);
            char[] headerChars = new char[headerLength];
            for(int i = 0; i < headerLength; i++)
            {
                headerChars[i] = Convert.ToChar(headerBytes[i]);
            }

            string header = new string(headerChars);
            if(!header.Equals(headerContent))
            {
                throw (new Exception("Wrong file format!"));
            }

            // Save the rest as temporary file (zip file).
            byte[] tempFileBytes = new byte[initialFileBytes.Length - headerLength];
            Array.Copy(initialFileBytes, headerLength, tempFileBytes, 0, tempFileBytes.Length);

            //// Extract file name.
            string onlyDir = Path.GetDirectoryName(fileName);
            //// Temporary file name.
            string tempFileName = onlyDir + "\\tempfile";
            if (File.Exists(tempFileName))
            {
                File.Delete(tempFileName);
            }

            FileStream sb = new FileStream(tempFileName, FileMode.OpenOrCreate);
            sb.Write(tempFileBytes, 0, tempFileBytes.Length);
            sb.Close();

            // Unpack the temp zip file to directory.
            ZipFile.ExtractToDirectory(tempFileName, directory);

            // Delete temp file.
            File.Delete(tempFileName);
        }

        /// <summ
        /// Exports the character data to a zip file and changes the header of zip file.
        /// </summary>
        /// <param name="directory">Directory whree the all files are contained.</param>
        /// <param name="fileName">Name of the destination zip file.</param>
        public static void ExportCharacter(string directory, string fileName)
        {
            // Header content.
            string headerContent = "TOYS2LIFE_THE_DIALOG_GENERATOR";
            int headerLength = headerContent.Length;

            // Extract file name.
            string onlyDir = Path.GetDirectoryName(fileName);

            // Temporary file name.
            string tempFileName = onlyDir + "\\tempfile";
            if(File.Exists(tempFileName))
            {
                File.Delete(tempFileName);
            }

            // Create ZIP file.
            ZipFile.CreateFromDirectory(directory, tempFileName);

            // Create addition to the ZIP file header.
            byte[] headerInBytes = new byte[headerContent.Length];
            char[] headerInChars = headerContent.ToArray();
            for(int i = 0; i < headerInChars.Length; i ++)
            {
                headerInBytes[i] = Convert.ToByte(headerInChars[i]);
            }

            // Get contents of temporary ZIP file.
            byte[] tempFileBytes = File.ReadAllBytes(tempFileName);

            // Create new file with the customized header.
            FileStream sb = new FileStream(fileName, FileMode.OpenOrCreate);
            sb.Write(headerInBytes, 0, headerInBytes.Length);
            sb.Write(tempFileBytes, 0, tempFileBytes.Length);
            sb.Close();
           
            // Clean.
            File.Delete(tempFileName);
        }
    }
}
