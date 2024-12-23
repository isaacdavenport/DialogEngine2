﻿using System.IO;

namespace DialogGenerator.Utilities.Model
{
    public class UserLoggerModel
    {
        public UserLoggerModel(string message,string _fileName,int line)
        {
            Message = message;
            FileName = Path.GetFileName(_fileName);
            Line = line;
        }

        public string Message { get; set; }
        public string FileName { get; set; }
        public int Line { get; set; }
    }
}
