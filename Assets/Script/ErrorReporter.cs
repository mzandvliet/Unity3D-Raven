using System;
using UnityEngine;
using System.Collections.Generic;
using SharpRaven;

/* 
 * Note: Don't worry too much about garbage and performance! When Unity throws an exception it by-default calls StackTraceUtility.PostProcesssStacktrace,
 * which up to 5ms and allocates 18KB of garbage. So whatever bits of garbage you're adding are just drops in the ocean.
 * 
 * Todo:
 * 
 * - How generic do we want this? Maintain feature parity with Raven, or say fuck it and simplify?
 * - Simplify! It'll make serialization cheaper as well. I only care about making this work for my own Unity games atm.
 * 
 *  - log error messages too
 *  - handle messages from other threads with Application.logMessageReceivedThreaded
 *  
 *  - Application.stackTraceLogType http://docs.unity3d.com/ScriptReference/Application-stackTraceLogType.html (Can include unmanaged stack as well. Never used it though, so don't bother yet.)
 */

public class ErrorReporter : MonoBehaviour {
    [SerializeField] private string _dsnUrl = "http://pub:priv@app.getsentry.com/12345";

    private RavenClient _ravenClient;
    private Queue<UnityLogEvent> _messageQueue;
    private Dictionary<string, string> _clientInfoTags;

    
    private void Awake()
	{
		DontDestroyOnLoad(gameObject);
        _messageQueue = new Queue<UnityLogEvent>();
        CreateRavenClient();
    }

    private void OnEnable() {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable() {
        Application.logMessageReceived -= HandleLog;
    }

    private void CreateRavenClient() {
        _ravenClient = new RavenClient(_dsnUrl, this);
        _ravenClient.Logger = "Unity";
        _ravenClient.LogScrubber = new SharpRaven.Logging.LogScrubber();

        // Todo: Include Volo Airsport version string
        // Todo: Since these tags are always included, please just cache them in the jsonpacket or something.

        _clientInfoTags = new Dictionary<string, string>();

        _clientInfoTags.Add("Version", "v3.6.1");
        _clientInfoTags.Add("UnityVersion", Application.unityVersion);

        _clientInfoTags.Add("OS", SystemInfo.operatingSystem);

        _clientInfoTags.Add("ProcessorType", SystemInfo.processorType);
        _clientInfoTags.Add("ProcessorCount", SystemInfo.processorCount.ToString());
        
        _clientInfoTags.Add("MemorySize", SystemInfo.systemMemorySize.ToString());
        _clientInfoTags.Add("Screen-Resolution", Screen.currentResolution.ToString()); // Note: can change at runtime

        _clientInfoTags.Add("GPU-Memory", SystemInfo.graphicsMemorySize.ToString());
        _clientInfoTags.Add("GPU-Name", SystemInfo.graphicsDeviceName);
        _clientInfoTags.Add("GPU-Vendor", SystemInfo.graphicsDeviceVendor);
        _clientInfoTags.Add("GPU-VendorID", SystemInfo.graphicsDeviceVendorID.ToString());
        _clientInfoTags.Add("GPU-id", SystemInfo.graphicsDeviceID.ToString());
        _clientInfoTags.Add("GPU-Version", SystemInfo.graphicsDeviceVersion);
        _clientInfoTags.Add("GPU-ShaderLevel", SystemInfo.graphicsShaderLevel.ToString());

        // Todo: vr info
    }

    private void Update() {
        while (_messageQueue.Count > 0) {
            var message = _messageQueue.Dequeue();
            var packet = _ravenClient.CreatePacket(message);
            packet.Tags = _clientInfoTags;
            _ravenClient.Send(packet);
        }
    }

    private void HandleLog(string log, string stack, LogType type) {
        switch (type) {
            case LogType.Error:
                CaptureUnityLog(log, stack, type);
                break;
            case LogType.Assert:
                break;
            case LogType.Warning:
                break;
            case LogType.Log:
                break;
            case LogType.Exception:
                CaptureUnityLog(log, stack, type);
                break;
            default:
                throw new ArgumentOutOfRangeException("type", type, null);
        }
    }

    private void CaptureUnityLog(string message, string stack, LogType logType) {
        _messageQueue.Enqueue(new UnityLogEvent() {
            LogType = logType,
            Message = message,
            StackTrace = stack
        });
    }
}
