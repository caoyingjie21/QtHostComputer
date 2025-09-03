using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QT.Packaging.Base.Extensions;
using QT.Packaging.Base.Services;

namespace QT.Packaging.Base
{
    /// <summary>
    /// 全局服务提供者
    /// 提供对注册服务的便捷访问
    /// </summary>
    public static class ServiceProvider
    {
        private static IServiceProvider? _serviceProvider;
        private static IHost? _host;

        /// <summary>
        /// 当前服务提供者实例
        /// </summary>
        public static IServiceProvider Current
        {
            get
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("服务提供者尚未初始化。请先调用 Initialize() 方法。");
                }
                return _serviceProvider;
            }
        }

        /// <summary>
        /// 当前主机实例
        /// </summary>
        public static IHost? Host => _host;

        /// <summary>
        /// 初始化服务提供者
        /// </summary>
        /// <param name="mqttPort">MQTT 端口</param>
        /// <returns>主机实例</returns>
        public static IHost Initialize(int mqttPort = 1883)
        {
            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
            
            // 添加包装机服务
            builder.Services.AddPackagingServices(mqttPort);
            
            _host = builder.Build();
            _serviceProvider = _host.Services;
            
            return _host;
        }

        /// <summary>
        /// 初始化服务提供者（带配置）
        /// </summary>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>主机实例</returns>
        public static IHost Initialize(Action<PackagingServiceOptions> configureOptions)
        {
            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
            
            // 添加包装机服务
            builder.Services.AddPackagingServices(configureOptions);
            
            _host = builder.Build();
            _serviceProvider = _host.Services;
            
            return _host;
        }

        /// <summary>
        /// 使用现有服务提供者初始化
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 获取指定类型的服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public static T GetService<T>() where T : class
        {
            return Current.GetRequiredService<T>();
        }

        /// <summary>
        /// 尝试获取指定类型的服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例或 null</returns>
        public static T? GetServiceOrNull<T>() where T : class
        {
            return Current.GetService<T>();
        }

        /// <summary>
        /// 获取日志服务
        /// </summary>
        public static LogService LogService => GetService<LogService>();

        /// <summary>
        /// 获取平台服务
        /// </summary>
        public static IPlatformService PlatformService => GetService<IPlatformService>();

        /// <summary>
        /// 获取许可证服务
        /// </summary>
        public static LicenseService LicenseService => GetService<LicenseService>();

        /// <summary>
        /// 获取 MQTT 主机服务
        /// </summary>
        public static MqttHostService MqttHostService => GetService<MqttHostService>();

        /// <summary>
        /// 启动所有后台服务
        /// </summary>
        /// <returns></returns>
        public static async Task StartAsync()
        {
            if (_host != null)
            {
                await _host.StartAsync();
            }
        }

        /// <summary>
        /// 停止所有服务
        /// </summary>
        /// <returns></returns>
        public static async Task StopAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
                _host = null;
                _serviceProvider = null;
            }
        }
    }
}
