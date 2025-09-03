using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QT.Packaging.Base.Services
{
    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }

    /// <summary>
    /// 日志服务，支持多模块独立日志文件
    /// </summary>
    public class LogService
    {
        private readonly string _logDirectory;
        private readonly object _lockObject = new object();

        /// <summary>
        /// 初始化 LogService 实例，创建日志目录
        /// </summary>
        public LogService()
        {
            _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(_logDirectory);
        }

        /// <summary>
        /// 创建指定模块名的模块记录器
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <returns>模块日志记录器</returns>
        public ModuleLogger CreateModule(string moduleName)
        {
            return new ModuleLogger(this, moduleName);
        }

        /// <summary>
        /// 获取所有日志文件路径，按修改时间倒序排列
        /// </summary>
        /// <returns>日志文件路径列表</returns>
        public List<string> GetAllLogFiles()
        {
            try
            {
                return Directory.GetFiles(_logDirectory, "*_log_*.log")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取指定模块的日志文件路径
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <returns>模块日志文件路径列表</returns>
        public List<string> GetModuleLogFiles(string moduleName)
        {
            try
            {
                var pattern = $"{moduleName}_log_*.log";
                return Directory.GetFiles(_logDirectory, pattern)
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 异步读取指定日志文件内容
        /// </summary>
        /// <param name="filePath">日志文件路径</param>
        /// <returns>日志文件内容列表</returns>
        public async Task<List<string>> ReadLogFile(string filePath)
        {
            try
            {
                // 使用临时文件避免文件锁定问题
                var tempPath = Path.GetTempFileName();
                File.Copy(filePath, tempPath, true);
                var lines = await File.ReadAllLinesAsync(tempPath);
                File.Delete(tempPath);
                return lines.ToList();
            }
            catch (Exception ex)
            {
                return new List<string> { $"读取日志文件失败: {ex.Message}" };
            }
        }

        /// <summary>
        /// 内部方法：写入日志到指定模块文件
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        internal void WriteLog(string moduleName, LogLevel level, string message)
        {
            try
            {
                lock (_lockObject)
                {
                    var logFileName = $"{moduleName}_log_{DateTime.Now:yyyyMMdd}.log";
                    var logFilePath = Path.Combine(_logDirectory, logFileName);
                    
                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{moduleName}] {message}";
                    
                    File.AppendAllText(logFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                // 避免日志记录失败导致程序崩溃
                Console.WriteLine($"日志写入失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理过期日志文件
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        public void CleanupOldLogs(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var logFiles = Directory.GetFiles(_logDirectory, "*_log_*.log");
                
                foreach (var file in logFiles)
                {
                    if (File.GetCreationTime(file) < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理日志文件失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 模块日志记录器
    /// </summary>
    public class ModuleLogger
    {
        private readonly LogService _logService;
        private readonly string _moduleName;

        /// <summary>
        /// 初始化模块日志记录器
        /// </summary>
        /// <param name="logService">日志服务实例</param>
        /// <param name="moduleName">模块名称</param>
        internal ModuleLogger(LogService logService, string moduleName)
        {
            _logService = logService;
            _moduleName = moduleName;
        }

        /// <summary>
        /// 记录信息级别日志
        /// </summary>
        /// <param name="message">日志信息</param>
        public void LogInfo(string message)
        {
            _logService.WriteLog(_moduleName, LogLevel.Info, message);
        }

        /// <summary>
        /// 记录警告级别日志
        /// </summary>
        /// <param name="message">日志信息</param>
        public void LogWarning(string message)
        {
            _logService.WriteLog(_moduleName, LogLevel.Warning, message);
        }

        /// <summary>
        /// 记录错误级别日志
        /// </summary>
        /// <param name="message">日志信息</param>
        public void LogError(string message)
        {
            _logService.WriteLog(_moduleName, LogLevel.Error, message);
        }

        /// <summary>
        /// 记录异常错误级别日志
        /// </summary>
        /// <param name="ex">异常对象</param>
        public void LogError(Exception ex)
        {
            var message = $"{ex.Message}\n堆栈跟踪:\n{ex.StackTrace}";
            _logService.WriteLog(_moduleName, LogLevel.Error, message);
        }

        /// <summary>
        /// 记录调试级别日志
        /// </summary>
        /// <param name="message">日志信息</param>
        public void LogDebug(string message)
        {
            _logService.WriteLog(_moduleName, LogLevel.Debug, message);
        }

        /// <summary>
        /// 获取当前模块的日志文件列表
        /// </summary>
        /// <returns>日志文件路径列表</returns>
        public List<string> GetLogFiles()
        {
            return _logService.GetModuleLogFiles(_moduleName);
        }

        /// <summary>
        /// 异步读取指定日志文件内容
        /// </summary>
        /// <param name="filePath">日志文件路径</param>
        /// <returns>日志文件内容列表</returns>
        public Task<List<string>> ReadLogFile(string filePath)
        {
            return _logService.ReadLogFile(filePath);
        }
    }
}
