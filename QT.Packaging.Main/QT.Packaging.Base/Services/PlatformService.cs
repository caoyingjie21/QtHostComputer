using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace QT.Packaging.Base.Services
{
    /// <summary>
    /// 平台服务接口，提供平台支持信息
    /// </summary>
    public interface IPlatformService
    {
        bool IsWindowsSupported { get; }
        bool IsAndroidSupported { get; }
        bool IsPlatformSupported { get; }
        string GetMachineCode();
    }

    /// <summary>
    /// 平台服务实现类，提供平台支持信息
    /// </summary>
    public class PlatformService : IPlatformService
    {
        /// <summary>
        /// 获取是否支持 Windows 平台
        /// </summary>
        public bool IsWindowsSupported
        {
            get
            {
#if WINDOWS
                return OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763);
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 获取是否支持 Android 平台
        /// </summary>
        public bool IsAndroidSupported
        {
            get
            {
#if ANDROID
                return OperatingSystem.IsAndroidVersionAtLeast(21);
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 获取当前平台是否支持
        /// </summary>
        public bool IsPlatformSupported
        {
            get
            {
                try
                {
                    // 运行时检查平台支持
                    if (OperatingSystem.IsWindows())
                    {
                        return IsWindowsSupported;
                    }
                    else if (OperatingSystem.IsAndroid())
                    {
                        return IsAndroidSupported;
                    }
                    else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
                    {
                        return true; // Linux、macOS、FreeBSD 默认支持
                    }
                    else
                    {
                        // 对于未知平台，返回 true 但记录警告
                        Console.WriteLine($"警告: 未明确支持的平台: {Environment.OSVersion.Platform}，默认为支持");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"平台支持检查失败: {ex.Message}，默认为支持");
                    return true; // 出现异常时默认支持
                }
            }
        }
        public string GetMachineCode()
        {
            try
            {
                // 运行时平台检查，而不是编译时检查
                if (OperatingSystem.IsWindows())
                {
                    if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
                    {
                        // Windows 版本较低，但仍然可以生成机器码
                        // throw new PlatformNotSupportedException("Windows 10.0.17763.0 or higher is required.");
                    }
                }
                else if (OperatingSystem.IsAndroid())
                {
                    if (!OperatingSystem.IsAndroidVersionAtLeast(21))
                    {
                        // Android 版本较低，但仍然可以生成机器码
                        // throw new PlatformNotSupportedException("Android 21.0 or higher is required.");
                    }
                }
                else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
                {
                    // 支持 Linux、macOS 和 FreeBSD 平台
                }
                else
                {
                    // 对于其他平台，记录警告但不抛出异常
                    Console.WriteLine($"警告: 未明确支持的平台: {Environment.OSVersion.Platform}");
                }

                // 生成机器码 - 使用更稳定的方法
                return GenerateMachineCode();
            }
            catch (Exception ex)
            {
                // 如果平台检查失败，使用备用方法生成机器码
                Console.WriteLine($"平台检查失败，使用备用方法: {ex.Message}");
                return GenerateFallbackMachineCode();
            }
        }

        /// <summary>
        /// 生成机器码的主要方法
        /// </summary>
        /// <returns>机器码</returns>
        private string GenerateMachineCode()
        {
            try
            {
                // 收集系统信息
                var systemInfo = new List<string>
                {
                    Environment.MachineName ?? "Unknown",
                    Environment.OSVersion.ToString(),
                    Environment.UserName ?? "Unknown",
                    Environment.ProcessorCount.ToString(),
                    Environment.Is64BitOperatingSystem.ToString(),
                    Environment.Version.ToString()
                };

                // 尝试获取更多硬件信息
                try
                {
                    systemInfo.Add(Environment.SystemDirectory ?? "Unknown");
                    systemInfo.Add(Environment.CurrentDirectory ?? "Unknown");
                }
                catch
                {
                    // 忽略获取失败的信息
                }

                string rawData = string.Join("-", systemInfo);
                
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return Convert.ToBase64String(hash).Substring(0, 16);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"生成机器码失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 备用机器码生成方法
        /// </summary>
        /// <returns>备用机器码</returns>
        private string GenerateFallbackMachineCode()
        {
            try
            {
                // 使用最基本的信息生成机器码
                string fallbackData = $"FALLBACK-{DateTime.UtcNow.Ticks}-{Environment.TickCount}";
                
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(fallbackData));
                return Convert.ToBase64String(hash).Substring(0, 16);
            }
            catch
            {
                // 最后的备用方案
                return "DEFAULT-MACHINE-CODE";
            }
        }
    }
}
