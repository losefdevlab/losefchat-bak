// Mod : Server, Des.: LC原版服务端核心类模組
// Part : 日志相关
namespace LosefDevLab.LosefChat.lcstd
{
    public partial class Server
    {
        public string logFilePath = $"log_{DateTime.Now:yyyy}{DateTime.Now:MM}{DateTime.Now:dd}.txt";
        public void Log(string message)
        {
            try
            {
                if (message.Trim() != "")
                {
                    lock (_cacheLock)
                    {
                        foreach (var line in message.Split('\n'))
                        {
                            _logCache.Add($"{DateTime.Now}: {line}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in cache log file: {ex.Message}");
                Log($"Error in cache log file: {ex.Message}");
            }
        }

        private void FlushCache(object? state)
        {
            List<string> logsToWrite;

            lock (_cacheLock)
            {
                logsToWrite = new List<string>(_logCache);
                _logCache.Clear();
            }

            if (logsToWrite.Count > 0)
            {
                try
                {
                    // 写入主日志文件
                    lock (_fileLock)
                    {
                        using (StreamWriter logFile = new StreamWriter(logFilePath, true))
                        {
                            foreach (var log in logsToWrite)
                            {
                                logFile.WriteLine(log);
                            }
                        }
                    }

                    // 写入缓存文件
                    lock (_fileLock)
                    {
                        using (StreamWriter cacheFile = new StreamWriter(scacheFilePath, true))
                        {
                            foreach (var log in logsToWrite)
                            {
                                cacheFile.WriteLine(log);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to log file: {ex.Message}");
                }
            }
        }

        public void SearchLog(string searchKeyword)
        {
            try
            {
                var logContent = File.ReadAllLines(logFilePath);
                var matchingResults = logContent.Where(line => line.Contains(searchKeyword)).ToList();

                using (StreamWriter searchResultsFile = new StreamWriter(searchFilePath, false))
                {
                    foreach (var matchingLine in matchingResults)
                    {
                        searchResultsFile.WriteLine(matchingLine);
                    }
                }

                Console.WriteLine($"找到了 {matchingResults.Count} 条关于 \"{searchKeyword}\" 的记录日志, 我已保存(\"{searchFilePath}\"), 感觉良好.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"在搜索日志时发生异常 {ex}");
                Log($"在搜索日志时发生异常 {ex}");
            }
        }
    }
}