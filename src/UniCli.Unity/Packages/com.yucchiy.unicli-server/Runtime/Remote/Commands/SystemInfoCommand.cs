using System;
using UnityEngine;

namespace UniCli.Remote.Commands
{
    [DebugCommand("Debug.SystemInfo", "Get device and application information")]
    public sealed class SystemInfoCommand : DebugCommand<Unit, SystemInfoCommand.Response>
    {
        protected override Response ExecuteCommand(Unit request)
        {
            return new Response
            {
                platform = Application.platform.ToString(),
                unityVersion = Application.unityVersion,
                appVersion = Application.version,
                productName = Application.productName,
                companyName = Application.companyName,
                identifier = Application.identifier,
                systemLanguage = Application.systemLanguage.ToString(),
                installerName = Application.installerName,
                deviceModel = SystemInfo.deviceModel,
                deviceName = SystemInfo.deviceName,
                operatingSystem = SystemInfo.operatingSystem,
                processorType = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount,
                systemMemorySize = SystemInfo.systemMemorySize,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
                graphicsMemorySize = SystemInfo.graphicsMemorySize,
                graphicsShaderLevel = SystemInfo.graphicsShaderLevel,
                maxTextureSize = SystemInfo.maxTextureSize,
                supportsGyroscope = SystemInfo.supportsGyroscope,
                supportsLocationService = SystemInfo.supportsLocationService,
                batteryLevel = SystemInfo.batteryLevel,
                batteryStatus = SystemInfo.batteryStatus.ToString(),
                internetReachability = Application.internetReachability.ToString(),
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                screenDpi = Screen.dpi,
                qualityLevel = QualitySettings.GetQualityLevel(),
                qualityName = QualitySettings.names[QualitySettings.GetQualityLevel()],
                targetFrameRate = Application.targetFrameRate
            };
        }

        [Serializable]
        public class Response
        {
            public string platform;
            public string unityVersion;
            public string appVersion;
            public string productName;
            public string companyName;
            public string identifier;
            public string systemLanguage;
            public string installerName;
            public string deviceModel;
            public string deviceName;
            public string operatingSystem;
            public string processorType;
            public int processorCount;
            public int systemMemorySize;
            public string graphicsDeviceName;
            public string graphicsDeviceType;
            public int graphicsMemorySize;
            public int graphicsShaderLevel;
            public int maxTextureSize;
            public bool supportsGyroscope;
            public bool supportsLocationService;
            public float batteryLevel;
            public string batteryStatus;
            public string internetReachability;
            public int screenWidth;
            public int screenHeight;
            public float screenDpi;
            public int qualityLevel;
            public string qualityName;
            public int targetFrameRate;
        }
    }
}
