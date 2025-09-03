using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Server;

namespace QT.Packaging.Base.Services
{
    /// <summary>
    /// MQTT Broker 主机服务
    /// 提供 MQTT 代理服务器的启动、停止、监控和自动重试功能
    /// </summary>
    public class MqttHostService : BackgroundService
    {
        private readonly ModuleLogger _logger;
        private readonly int _defaultPort = 1883;
        private readonly int _maxRetryAttempts = 5;
        private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(10);
        
        private MqttServer? _mqttServer;
        private string? _currentIpAddress;
        private bool _isRunning = false;
        private readonly object _lockObject = new object();

        /// <summary>
        /// MQTT Broker 状态变化事件
        /// </summary>
        public event EventHandler<MqttBrokerStatusChangedEventArgs>? StatusChanged;

        /// <summary>
        /// 当前 MQTT Broker 状态
        /// </summary>
        public MqttBrokerStatus Status { get; private set; } = MqttBrokerStatus.Stopped;

        /// <summary>
        /// 当前使用的 IP 地址
        /// </summary>
        public string? CurrentIpAddress => _currentIpAddress;

        /// <summary>
        /// 当前使用的端口
        /// </summary>
        public int Port { get; private set; } = 1883;

        /// <summary>
        /// 初始化 MQTT 主机服务
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="port">MQTT 端口，默认 1883</param>
        public MqttHostService(LogService logService, int port = 1883)
        {
            _logger = logService.CreateModule("MqttHost");
            Port = port;
            _defaultPort = port;
        }

        /// <summary>
        /// 后台服务执行方法
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInfo("MQTT Broker 服务启动中...");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_isRunning)
                    {
                        await StartBrokerWithRetry(stoppingToken);
                    }
                    
                    // 每30秒检查一次服务状态
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    
                    if (_isRunning)
                    {
                        await CheckBrokerHealth();
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInfo("MQTT Broker 服务正在停止...");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"MQTT Broker 服务运行异常: {ex.Message}");
                    await Task.Delay(_retryDelay, stoppingToken);
                }
            }
        }

        /// <summary>
        /// 带重试机制的启动 Broker
        /// </summary>
        private async Task StartBrokerWithRetry(CancellationToken cancellationToken)
        {
            for (int attempt = 1; attempt <= _maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInfo($"尝试启动 MQTT Broker (第 {attempt} 次尝试)...");
                    
                    // 获取可用的 IP 地址
                    var availableIp = await GetAvailableIpAddress();
                    if (string.IsNullOrEmpty(availableIp))
                    {
                        throw new InvalidOperationException("未找到可用的网络接口");
                    }

                    // 检查端口是否已被占用
                    if (await IsPortInUse(availableIp, Port))
                    {
                        _logger.LogWarning($"端口 {Port} 在 IP {availableIp} 上已被占用");
                        
                        // 对于默认的 1883 端口，直接杀掉占用的进程
                        if (Port == 1883)
                        {
                            _logger.LogInfo("检测到 1883 端口被占用，尝试终止占用该端口的进程...");
                            if (await KillProcessUsingPort(Port))
                            {
                                _logger.LogInfo("成功终止占用 1883 端口的进程");
                                // 等待一小段时间让端口释放
                                await Task.Delay(2000, cancellationToken);
                            }
                            else
                            {
                                _logger.LogError("无法终止占用 1883 端口的进程，尝试使用其他端口");
                                Port = await FindAvailablePort(availableIp, _defaultPort + 1);
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"端口 {Port} 已被占用，尝试查找其他端口...");
                            Port = await FindAvailablePort(availableIp, _defaultPort);
                        }
                    }

                    // 启动 MQTT Broker
                    await StartMqttBroker(availableIp, Port);
                    
                    _currentIpAddress = availableIp;
                    _isRunning = true;
                    SetStatus(MqttBrokerStatus.Running);
                    
                    _logger.LogInfo($"MQTT Broker 启动成功: {availableIp}:{Port}");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"启动 MQTT Broker 失败 (第 {attempt} 次尝试): {ex.Message}");
                    
                    if (attempt < _maxRetryAttempts)
                    {
                        _logger.LogInfo($"等待 {_retryDelay.TotalSeconds} 秒后重试...");
                        await Task.Delay(_retryDelay, cancellationToken);
                    }
                    else
                    {
                        SetStatus(MqttBrokerStatus.Failed);
                        _logger.LogError("已达到最大重试次数，MQTT Broker 启动失败");
                    }
                }
            }
        }

        /// <summary>
        /// 启动 MQTT Broker
        /// </summary>
        private async Task StartMqttBroker(string ipAddress, int port)
        {
            var factory = new MqttFactory();
            
            var options = factory.CreateServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointBoundIPAddress(IPAddress.Parse(ipAddress))
                .WithDefaultEndpointPort(port)
                .Build();

            _mqttServer = factory.CreateMqttServer(options);
            
            // 订阅事件
            _mqttServer.ValidatingConnectionAsync += OnValidatingConnection;
            _mqttServer.ClientConnectedAsync += OnClientConnected;
            _mqttServer.ClientDisconnectedAsync += OnClientDisconnected;
            _mqttServer.InterceptingPublishAsync += OnInterceptingPublish;
            _mqttServer.ApplicationMessageNotConsumedAsync += OnMessageNotConsumed;
            
            await _mqttServer.StartAsync();
        }

        /// <summary>
        /// 获取可用的 IP 地址
        /// 优先级：以太网适配器 > WLAN适配器 > 回环地址
        /// </summary>
        private async Task<string?> GetAvailableIpAddress()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .ToList();

                // 按优先级排序的适配器候选列表
                var candidates = new List<(string IpAddress, string AdapterName, int Priority)>();

                foreach (var networkInterface in networkInterfaces)
                {
                    var ipProperties = networkInterface.GetIPProperties();
                    var ipAddresses = ipProperties.UnicastAddresses
                        .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
                        .Select(ua => ua.Address.ToString())
                        .ToList();

                    foreach (var ipAddress in ipAddresses)
                    {
                        var priority = GetAdapterPriority(networkInterface, ipAddress);
                        if (priority > 0) // 只考虑有效的适配器
                        {
                            candidates.Add((ipAddress, networkInterface.Name, priority));
                            _logger.LogDebug($"发现网络适配器: {networkInterface.Name} ({GetAdapterTypeDescription(networkInterface)}) - IP: {ipAddress}, 优先级: {priority}");
                        }
                    }
                }

                // 按优先级排序（优先级越高越好）
                candidates = candidates.OrderByDescending(c => c.Priority).ToList();

                // 测试每个候选 IP 地址的可用性
                foreach (var candidate in candidates)
                {
                    if (await IsIpAddressAvailable(candidate.IpAddress))
                    {
                        _logger.LogInfo($"选择网络适配器: {candidate.AdapterName} - IP: {candidate.IpAddress} (优先级: {candidate.Priority})");
                        return candidate.IpAddress;
                    }
                    else
                    {
                        _logger.LogWarning($"网络适配器不可用: {candidate.AdapterName} - IP: {candidate.IpAddress}");
                    }
                }

                // 如果没有找到任何可用的 IP，使用本地回环地址
                _logger.LogWarning("未找到可用的网络接口，使用本地回环地址");
                return "127.0.0.1";
            }
            catch (Exception ex)
            {
                _logger.LogError($"获取 IP 地址时发生错误: {ex.Message}");
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// 获取适配器优先级
        /// </summary>
        /// <param name="networkInterface">网络接口</param>
        /// <param name="ipAddress">IP地址</param>
        /// <returns>优先级（越高越优先，0表示不考虑）</returns>
        private int GetAdapterPriority(NetworkInterface networkInterface, string ipAddress)
        {
            // 跳过回环地址（除非作为最后选择）
            if (ipAddress == "127.0.0.1" || ipAddress.StartsWith("127."))
            {
                return 1; // 最低优先级，但仍然有效
            }

            // 跳过链路本地地址（169.254.x.x）
            if (ipAddress.StartsWith("169.254."))
            {
                return 0; // 不考虑
            }

            var interfaceType = networkInterface.NetworkInterfaceType;
            var interfaceName = networkInterface.Name.ToLowerInvariant();
            var description = networkInterface.Description.ToLowerInvariant();

            // 以太网适配器 - 最高优先级
            if (interfaceType == NetworkInterfaceType.Ethernet ||
                interfaceType == NetworkInterfaceType.GigabitEthernet ||
                interfaceType == NetworkInterfaceType.FastEthernetT ||
                interfaceType == NetworkInterfaceType.FastEthernetFx ||
                interfaceName.Contains("ethernet") ||
                interfaceName.Contains("以太网") ||
                description.Contains("ethernet") ||
                description.Contains("以太网"))
            {
                return 100;
            }

            // WLAN/WiFi 适配器 - 中等优先级
            if (interfaceType == NetworkInterfaceType.Wireless80211 ||
                interfaceName.Contains("wi-fi") ||
                interfaceName.Contains("wifi") ||
                interfaceName.Contains("wlan") ||
                interfaceName.Contains("无线") ||
                description.Contains("wi-fi") ||
                description.Contains("wifi") ||
                description.Contains("wireless") ||
                description.Contains("wlan") ||
                description.Contains("无线"))
            {
                return 50;
            }

            // PPP 连接（如拨号、VPN等）- 较低优先级
            if (interfaceType == NetworkInterfaceType.Ppp ||
                interfaceName.Contains("ppp") ||
                interfaceName.Contains("vpn") ||
                description.Contains("ppp") ||
                description.Contains("vpn"))
            {
                return 20;
            }

            // 隧道接口 - 很低优先级
            if (interfaceType == NetworkInterfaceType.Tunnel ||
                interfaceName.Contains("tunnel") ||
                interfaceName.Contains("隧道") ||
                description.Contains("tunnel") ||
                description.Contains("隧道"))
            {
                return 10;
            }

            // 回环接口 - 最低优先级但仍有效
            if (interfaceType == NetworkInterfaceType.Loopback)
            {
                return 1;
            }

            // 其他未知类型的接口 - 低优先级
            return 5;
        }

        /// <summary>
        /// 获取适配器类型描述
        /// </summary>
        /// <param name="networkInterface">网络接口</param>
        /// <returns>适配器类型描述</returns>
        private string GetAdapterTypeDescription(NetworkInterface networkInterface)
        {
            var interfaceType = networkInterface.NetworkInterfaceType;
            var interfaceName = networkInterface.Name.ToLowerInvariant();
            var description = networkInterface.Description.ToLowerInvariant();

            if (interfaceType == NetworkInterfaceType.Ethernet ||
                interfaceType == NetworkInterfaceType.GigabitEthernet ||
                interfaceName.Contains("ethernet") ||
                interfaceName.Contains("以太网"))
            {
                return "以太网适配器";
            }

            if (interfaceType == NetworkInterfaceType.Wireless80211 ||
                interfaceName.Contains("wi-fi") ||
                interfaceName.Contains("wlan") ||
                interfaceName.Contains("无线"))
            {
                return "WLAN适配器";
            }

            if (interfaceType == NetworkInterfaceType.Loopback)
            {
                return "回环适配器";
            }

            if (interfaceType == NetworkInterfaceType.Ppp ||
                interfaceName.Contains("vpn"))
            {
                return "PPP/VPN适配器";
            }

            return $"未知适配器({interfaceType})";
        }

        /// <summary>
        /// 检查 IP 地址是否可用
        /// </summary>
        private async Task<bool> IsIpAddressAvailable(string ipAddress)
        {
            try
            {
                var ping = new Ping();
                var reply = await ping.SendPingAsync(IPAddress.Parse(ipAddress), 1000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 终止占用指定端口的进程
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>是否成功终止进程</returns>
        private async Task<bool> KillProcessUsingPort(int port)
        {
            try
            {
                var processIds = await GetProcessIdsUsingPort(port);
                
                if (processIds.Count == 0)
                {
                    _logger.LogInfo($"未找到占用端口 {port} 的进程");
                    return true; // 端口已经空闲
                }

                bool allKilled = true;
                foreach (var processId in processIds)
                {
                    try
                    {
                        var process = Process.GetProcessById(processId);
                        _logger.LogInfo($"尝试终止进程: PID={processId}, 名称={process.ProcessName}");
                        
                        // 首先尝试优雅关闭
                        if (!process.CloseMainWindow())
                        {
                            // 如果优雅关闭失败，强制终止
                            process.Kill();
                        }
                        
                        // 等待进程退出
                        if (!process.WaitForExit(5000)) // 等待5秒
                        {
                            process.Kill(); // 强制终止
                            process.WaitForExit(2000); // 再等待2秒
                        }
                        
                        _logger.LogInfo($"成功终止进程: PID={processId}");
                    }
                    catch (ArgumentException)
                    {
                        // 进程已经不存在
                        _logger.LogInfo($"进程 PID={processId} 已经不存在");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"终止进程 PID={processId} 失败: {ex.Message}");
                        allKilled = false;
                    }
                }

                return allKilled;
            }
            catch (Exception ex)
            {
                _logger.LogError($"终止占用端口 {port} 的进程时发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取占用指定端口的进程ID列表
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>进程ID列表</returns>
        private async Task<List<int>> GetProcessIdsUsingPort(int port)
        {
            var processIds = new List<int>();
            
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    processIds.AddRange(await GetProcessIdsUsingPortWindows(port));
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    processIds.AddRange(await GetProcessIdsUsingPortLinux(port));
                }
                else
                {
                    _logger.LogWarning("当前平台不支持进程端口查询");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"获取占用端口 {port} 的进程ID时发生错误: {ex.Message}");
            }

            return processIds;
        }

        /// <summary>
        /// Windows 平台获取占用端口的进程ID
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>进程ID列表</returns>
        private async Task<List<int>> GetProcessIdsUsingPortWindows(int port)
        {
            var processIds = new List<int>();
            
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.Contains($":{port} ") && (line.Contains("LISTENING") || line.Contains("ESTABLISHED")))
                        {
                            var parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0 && int.TryParse(parts[^1], out int pid))
                            {
                                processIds.Add(pid);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Windows 平台获取端口 {port} 占用进程失败: {ex.Message}");
            }

            return processIds;
        }

        /// <summary>
        /// Linux 平台获取占用端口的进程ID
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>进程ID列表</returns>
        private async Task<List<int>> GetProcessIdsUsingPortLinux(int port)
        {
            var processIds = new List<int>();
            
            try
            {
                // 使用 lsof 命令查找占用端口的进程
                var startInfo = new ProcessStartInfo
                {
                    FileName = "lsof",
                    Arguments = $"-t -i:{port}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (int.TryParse(line.Trim(), out int pid))
                            {
                                processIds.Add(pid);
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(error))
                    {
                        _logger.LogWarning($"lsof 命令执行失败: {error}");
                        
                        // 备用方法：使用 netstat
                        processIds.AddRange(await GetProcessIdsUsingPortLinuxNetstat(port));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Linux 平台获取端口 {port} 占用进程失败: {ex.Message}");
                
                // 备用方法：使用 netstat
                try
                {
                    processIds.AddRange(await GetProcessIdsUsingPortLinuxNetstat(port));
                }
                catch (Exception netstatEx)
                {
                    _logger.LogError($"备用方法也失败: {netstatEx.Message}");
                }
            }

            return processIds;
        }

        /// <summary>
        /// Linux 平台使用 netstat 获取占用端口的进程ID
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>进程ID列表</returns>
        private async Task<List<int>> GetProcessIdsUsingPortLinuxNetstat(int port)
        {
            var processIds = new List<int>();
            
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-tlnp",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.Contains($":{port} "))
                        {
                            var parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            var lastPart = parts[^1];
                            if (lastPart.Contains('/'))
                            {
                                var pidPart = lastPart.Split('/')[0];
                                if (int.TryParse(pidPart, out int pid))
                                {
                                    processIds.Add(pid);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"使用 netstat 获取端口 {port} 占用进程失败: {ex.Message}");
            }

            return processIds;
        }

        /// <summary>
        /// 检查端口是否被占用
        /// </summary>
        private async Task<bool> IsPortInUse(string ipAddress, int port)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(IPAddress.Parse(ipAddress), port);
                var timeoutTask = Task.Delay(1000);
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == connectTask && !connectTask.IsFaulted)
                {
                    _logger.LogWarning($"端口 {port} 在 {ipAddress} 上已被占用");
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 查找可用端口
        /// </summary>
        private async Task<int> FindAvailablePort(string ipAddress, int startPort)
        {
            for (int port = startPort; port < startPort + 100; port++)
            {
                if (!await IsPortInUse(ipAddress, port))
                {
                    _logger.LogInfo($"找到可用端口: {port}");
                    return port;
                }
            }
            
            throw new InvalidOperationException($"在 {ipAddress} 上未找到可用端口 (范围: {startPort}-{startPort + 100})");
        }

        /// <summary>
        /// 检查 Broker 健康状态
        /// </summary>
        private async Task CheckBrokerHealth()
        {
            try
            {
                if (_mqttServer?.IsStarted == true)
                {
                    var clientCount = (await _mqttServer.GetClientsAsync()).Count;
                    _logger.LogDebug($"MQTT Broker 健康检查通过，当前连接客户端数: {clientCount}");
                }
                else
                {
                    _logger.LogWarning("MQTT Broker 似乎已停止，尝试重启...");
                    _isRunning = false;
                    SetStatus(MqttBrokerStatus.Stopped);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"MQTT Broker 健康检查失败: {ex.Message}");
                _isRunning = false;
                SetStatus(MqttBrokerStatus.Failed);
            }
        }

        /// <summary>
        /// 验证连接事件处理
        /// </summary>
        private Task OnValidatingConnection(ValidatingConnectionEventArgs args)
        {
            _logger.LogInfo($"客户端连接验证: {args.ClientId} from {args.Endpoint}");
            args.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.Success;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 客户端连接事件处理
        /// </summary>
        private Task OnClientConnected(ClientConnectedEventArgs args)
        {
            _logger.LogInfo($"MQTT 客户端已连接: {args.ClientId} from {args.Endpoint}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 客户端断开连接事件处理
        /// </summary>
        private Task OnClientDisconnected(ClientDisconnectedEventArgs args)
        {
            _logger.LogInfo($"MQTT 客户端已断开: {args.ClientId}, 原因: {args.DisconnectType}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 拦截发布消息事件处理
        /// </summary>
        private Task OnInterceptingPublish(InterceptingPublishEventArgs args)
        {
            try
            {
                var payload = args.ApplicationMessage.PayloadSegment.Count > 0 
                    ? Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment) 
                    : "<empty>";
                _logger.LogDebug($"消息拦截: Topic={args.ApplicationMessage.Topic}, Payload={payload}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"消息拦截处理异常: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 消息未被消费事件处理
        /// </summary>
        private Task OnMessageNotConsumed(ApplicationMessageNotConsumedEventArgs args)
        {
            _logger.LogWarning($"MQTT 消息未被消费: Topic={args.ApplicationMessage.Topic}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 设置状态并触发事件
        /// </summary>
        private void SetStatus(MqttBrokerStatus status)
        {
            lock (_lockObject)
            {
                if (Status != status)
                {
                    var oldStatus = Status;
                    Status = status;
                    
                    _logger.LogInfo($"MQTT Broker 状态变更: {oldStatus} -> {status}");
                    StatusChanged?.Invoke(this, new MqttBrokerStatusChangedEventArgs(oldStatus, status));
                }
            }
        }

        /// <summary>
        /// 手动启动 MQTT Broker
        /// </summary>
        public async Task<bool> StartAsync()
        {
            try
            {
                if (_isRunning)
                {
                    _logger.LogWarning("MQTT Broker 已在运行中");
                    return true;
                }

                SetStatus(MqttBrokerStatus.Starting);
                await StartBrokerWithRetry(CancellationToken.None);
                return _isRunning;
            }
            catch (Exception ex)
            {
                _logger.LogError($"手动启动 MQTT Broker 失败: {ex.Message}");
                SetStatus(MqttBrokerStatus.Failed);
                return false;
            }
        }

        /// <summary>
        /// 手动停止 MQTT Broker
        /// </summary>
        public async Task<bool> StopAsync()
        {
            try
            {
                if (!_isRunning || _mqttServer == null)
                {
                    _logger.LogWarning("MQTT Broker 未在运行中");
                    return true;
                }

                SetStatus(MqttBrokerStatus.Stopping);
                
                await _mqttServer.StopAsync();
                _mqttServer.Dispose();
                _mqttServer = null;
                
                _isRunning = false;
                _currentIpAddress = null;
                
                SetStatus(MqttBrokerStatus.Stopped);
                _logger.LogInfo("MQTT Broker 已停止");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"停止 MQTT Broker 失败: {ex.Message}");
                SetStatus(MqttBrokerStatus.Failed);
                return false;
            }
        }

        /// <summary>
        /// 获取当前连接的客户端信息
        /// </summary>
        public async Task<List<MqttClientInfo>> GetConnectedClientsAsync()
        {
            try
            {
                if (_mqttServer?.IsStarted != true)
                {
                    return new List<MqttClientInfo>();
                }

                var clients = await _mqttServer.GetClientsAsync();
                return clients.Select(c => new MqttClientInfo
                {
                    ClientId = c.Id,
                    Endpoint = c.Endpoint,
                    ConnectedAt = DateTime.Now,
                    IsConnected = true
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"获取客户端信息失败: {ex.Message}");
                return new List<MqttClientInfo>();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Dispose()
        {
            _mqttServer?.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// MQTT Broker 状态枚举
    /// </summary>
    public enum MqttBrokerStatus
    {
        /// <summary>已停止</summary>
        Stopped,
        /// <summary>启动中</summary>
        Starting,
        /// <summary>运行中</summary>
        Running,
        /// <summary>停止中</summary>
        Stopping,
        /// <summary>失败</summary>
        Failed
    }

    /// <summary>
    /// MQTT Broker 状态变化事件参数
    /// </summary>
    public class MqttBrokerStatusChangedEventArgs : EventArgs
    {
        public MqttBrokerStatus OldStatus { get; }
        public MqttBrokerStatus NewStatus { get; }

        public MqttBrokerStatusChangedEventArgs(MqttBrokerStatus oldStatus, MqttBrokerStatus newStatus)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }

    /// <summary>
    /// MQTT 客户端信息
    /// </summary>
    public class MqttClientInfo
    {
        /// <summary>客户端 ID</summary>
        public string ClientId { get; set; } = string.Empty;
        
        /// <summary>连接端点</summary>
        public string Endpoint { get; set; } = string.Empty;
        
        /// <summary>连接时间</summary>
        public DateTime ConnectedAt { get; set; }
        
        /// <summary>是否已连接</summary>
        public bool IsConnected { get; set; }
    }
}
