using DialogGenerator.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DialogGenerator.Utilities
{
    public static class ProcessHandler
    {
        private static readonly Dictionary<string, Process> msDictionary = new Dictionary<string, Process>();
        private static ProcessKeysIndexer msIndexer;

        public sealed class ProcessKeysIndexer
        {
            public Process this[string key]
            {
                get { return Get(key); }
                set { Set(key, value); }
            }
        }

        private static void _process_Exited(object sender, System.EventArgs e)
        {
            Process proccess = sender as Process;
            string arguments = proccess.StartInfo.Arguments;

            if (!string.IsNullOrEmpty(arguments))
            {
                string key = Path.GetFileName(arguments);
                msDictionary.Remove(key);
                File.Delete(Path.Combine(ApplicationData.Instance.EditorTempDirectory,key));
            }
        }

        public static void Remove(string key)
        {
            if (!Contains(key))
                return;

            var process = Get(key);
            if(!process.HasExited)
                process.Kill();
        }

        public static void Set(string key, Process value)
        {
            if (msDictionary.ContainsKey(key))
            {
                if (value != null)
                    msDictionary[key] = value;
                else
                    msDictionary.Remove(key);
            }
            else if (value != null)
            {
                msDictionary.Add(key, value);
            }

            value.Exited += _process_Exited;
        }

        public static Process Get(string key)
        {
            if (msDictionary.ContainsKey(key))
            {
                return msDictionary[key];
            }

            return null;
        }

        public static void ClearAll()
        {
            foreach(var _processKey in msDictionary.Keys)
            {
                Remove(_processKey);
            }

            msDictionary.Clear();
        }

        public static bool HasActiveProcess
        {
            get { return msDictionary.Count > 0; }
        }

        public static bool Contains(string key)
        {
            return msDictionary.ContainsKey(key);
        }

        public static ProcessKeysIndexer Keys
        {
            get { return msIndexer ?? (msIndexer = new ProcessKeysIndexer()); }
        }
    }


}
