using QT.Packaging.Base.Services;

namespace QT.Packaging.Base.Examples
{
    /// <summary>
    /// 服务使用示例
    /// 展示如何在应用程序中使用注入的服务
    /// </summary>
    public class ServiceUsageExample
    {
        /// <summary>
        /// 通过全局服务提供者使用服务的示例
        /// </summary>
        public void UseServicesViaGlobalProvider()
        {
            // 方式1: 通过全局服务提供者访问
            var logger = ServiceProvider.LogService.CreateModule("Example");
            logger.LogInfo("这是一个示例日志");

            // 获取平台信息
            var platformService = ServiceProvider.PlatformService;
            logger.LogInfo($"当前平台支持: {platformService.IsPlatformSupported}");

            // 获取 MQTT 服务状态
            var mqttService = ServiceProvider.MqttHostService;
            logger.LogInfo($"MQTT 服务状态: {mqttService.Status}");
            logger.LogInfo($"MQTT 服务地址: {mqttService.CurrentIpAddress}:{mqttService.Port}");
        }

        /// <summary>
        /// 通过依赖注入使用服务的示例
        /// </summary>
        public class ExampleController
        {
            private readonly ModuleLogger _logger;
            private readonly IPlatformService _platformService;
            private readonly MqttHostService _mqttService;

            public ExampleController(LogService logService, IPlatformService platformService, MqttHostService mqttService)
            {
                _logger = logService.CreateModule("ExampleController");
                _platformService = platformService;
                _mqttService = mqttService;
            }

            public async Task DoSomethingAsync()
            {
                _logger.LogInfo("开始执行操作...");

                try
                {
                    // 检查平台支持
                    if (!_platformService.IsPlatformSupported)
                    {
                        _logger.LogWarning("当前平台不受支持");
                        return;
                    }

                    // 检查 MQTT 服务状态
                    if (_mqttService.Status != MqttBrokerStatus.Running)
                    {
                        _logger.LogWarning("MQTT 服务未运行，尝试启动...");
                        bool started = await _mqttService.StartAsync();
                        if (!started)
                        {
                            _logger.LogError("MQTT 服务启动失败");
                            return;
                        }
                    }

                    // 获取连接的客户端
                    var clients = await _mqttService.GetConnectedClientsAsync();
                    _logger.LogInfo($"当前连接的 MQTT 客户端数量: {clients.Count}");

                    _logger.LogInfo("操作执行完成");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"操作执行失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 监听 MQTT 服务状态变化的示例
        /// </summary>
        public void MonitorMqttServiceStatus()
        {
            var logger = ServiceProvider.LogService.CreateModule("MqttMonitor");
            var mqttService = ServiceProvider.MqttHostService;

            // 订阅状态变化事件
            mqttService.StatusChanged += (sender, args) =>
            {
                logger.LogInfo($"MQTT 服务状态变更: {args.OldStatus} -> {args.NewStatus}");

                switch (args.NewStatus)
                {
                    case MqttBrokerStatus.Running:
                        logger.LogInfo($"MQTT Broker 已启动: {mqttService.CurrentIpAddress}:{mqttService.Port}");
                        break;
                    case MqttBrokerStatus.Failed:
                        logger.LogError("MQTT Broker 启动失败");
                        break;
                    case MqttBrokerStatus.Stopped:
                        logger.LogInfo("MQTT Broker 已停止");
                        break;
                }
            };
        }

        /// <summary>
        /// 许可证验证示例
        /// </summary>
        public bool ValidateLicense()
        {
            var logger = ServiceProvider.LogService.CreateModule("LicenseValidator");
            var licenseService = ServiceProvider.LicenseService;

            try
            {
                bool isValid = licenseService.CheckLicense();
                
                if (isValid)
                {
                    logger.LogInfo("许可证验证成功");
                }
                else
                {
                    logger.LogWarning("许可证验证失败");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                logger.LogError($"许可证验证异常: {ex.Message}");
                return false;
            }
        }
    }
}
