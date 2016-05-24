using System;
using UnityEngine;
using System.Collections.Generic;
using SharpRaven;
using UnityEditor;

/* Todo:
 *  - log error messages too
 *  - handle messages from other threads with Application.logMessageReceivedThreaded
 */

public class ErrorReporter : MonoBehaviour {
    [SerializeField] private string _dsnUrl = "http://pub:priv@app.getsentry.com/12345";

    private RavenClient _ravenClient;
    private Queue<UnityLogEvent> _messageQueue;
    private Dictionary<string, string> _clientInfo;

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
        Debug.Log("Initializing RavenClient.");
        _ravenClient = new RavenClient(_dsnUrl, this);
        _ravenClient.Logger = "C#";
        _ravenClient.LogScrubber = new SharpRaven.Logging.LogScrubber();

        Debug.Log("Sentry Uri: " + _ravenClient.Dsn.SentryURI);
        Debug.Log("Port: " + _ravenClient.Dsn.Port);
        Debug.Log("Public Key: " + _ravenClient.Dsn.PublicKey);
        Debug.Log("Private Key: " + _ravenClient.Dsn.PrivateKey);
        Debug.Log("Project ID: " + _ravenClient.Dsn.ProjectID);

        // Todo: Include Volo Airsport version string
        // Todo: Since these tags are always included, please just cache them in the jsonpacket or something.

        _clientInfo = new Dictionary<string, string>();

        _clientInfo.Add("Version", "v3.6.1");
        _clientInfo.Add("UnityVersion", Application.unityVersion);

        _clientInfo.Add("OS", SystemInfo.operatingSystem);

        _clientInfo.Add("ProcessorType", SystemInfo.processorType);
        _clientInfo.Add("ProcessorCount", SystemInfo.processorCount.ToString());
        
        _clientInfo.Add("MemorySize", SystemInfo.systemMemorySize.ToString());
        _clientInfo.Add("Screen-Resolution", Screen.currentResolution.ToString()); // Note: can change at runtime

        _clientInfo.Add("GPU-Memory", SystemInfo.graphicsMemorySize.ToString());
        _clientInfo.Add("GPU-Name", SystemInfo.graphicsDeviceName);
        _clientInfo.Add("GPU-Vendor", SystemInfo.graphicsDeviceVendor);
        _clientInfo.Add("GPU-VendorID", SystemInfo.graphicsDeviceVendorID.ToString());
        _clientInfo.Add("GPU-id", SystemInfo.graphicsDeviceID.ToString());
        _clientInfo.Add("GPU-Version", SystemInfo.graphicsDeviceVersion);
        _clientInfo.Add("GPU-ShaderLevel", SystemInfo.graphicsShaderLevel.ToString());

        // Todo: vr info
    }

    private void Update() {
        while (_messageQueue.Count > 0) {
            var message = _messageQueue.Dequeue();
            var packet = _ravenClient.CreatePacket(message);
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
