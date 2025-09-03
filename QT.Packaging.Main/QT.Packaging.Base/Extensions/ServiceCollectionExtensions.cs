using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QT.Packaging.Base.Services;

namespace QT.Packaging.Base.Extensions
{
    /// <summary>
    /// 服务集合扩展方法
    /// 用于注册 QT.Packaging.Base 中的所有服务
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加包装机基础服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="mqttPort">MQTT 端口，默认 1883</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddPackagingServices(this IServiceCollection services, int mqttPort = 1883)
        {
            // 注册基础服务（按依赖顺序）
            services.AddSingleton<LogService>();
            services.AddSingleton<IPlatformService, PlatformService>();
            services.AddSingleton<LicenseService>();
            
            // 注册 MQTT 主机服务（作为后台服务）
            services.AddSingleton<MqttHostService>(provider =>
            {
                var logService = provider.GetRequiredService<LogService>();
                return new MqttHostService(logService, mqttPort);
            });
            
            // 将 MQTT 主机服务注册为后台服务
            services.AddHostedService<MqttHostService>(provider => 
                provider.GetRequiredService<MqttHostService>());

            return services;
        }

        /// <summary>
        /// 添加包装机基础服务（带自定义配置）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddPackagingServices(this IServiceCollection services, 
            Action<PackagingServiceOptions> configureOptions)
        {
            var options = new PackagingServiceOptions();
            configureOptions(options);

            return services.AddPackagingServices(options.MqttPort);
        }
    }

    /// <summary>
    /// 包装机服务配置选项
    /// </summary>
    public class PackagingServiceOptions
    {
        /// <summary>
        /// MQTT 端口，默认 1883
        /// </summary>
        public int MqttPort { get; set; } = 1883;

        /// <summary>
        /// 是否启用调试日志，默认 false
        /// </summary>
        public bool EnableDebugLogging { get; set; } = false;

        /// <summary>
        /// 日志保留天数，默认 30 天
        /// </summary>
        public int LogRetentionDays { get; set; } = 30;
    }
}
