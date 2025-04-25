using System;
using System.IO;
using System.Text;

namespace lcstd
{
    // Mod : FileLockManager, Des.: 文件锁读写管理器
    // 进行文件读写同时使用文件锁防止进程读写冲突
    public class FileLockManager
    {
        private readonly string filePath;

        public FileLockManager(string filePath)
        {
            this.filePath = filePath;
        }

        public void WriteToFile(string content)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None))
            {
                fs.Lock(0, fs.Length);
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.WriteLine(content);
                }
                fs.Unlock(0, fs.Length);
            }
        }

        public string ReadFromFile()
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Lock(0, fs.Length);
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    string content = sr.ReadToEnd();
                    fs.Unlock(0, fs.Length);
                    return content;
                }
            }
        }
    }
}