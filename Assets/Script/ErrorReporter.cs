using System;
using UnityEngine;
using System.Collections.Generic;
using SharpRaven;

public class ErrorReporter : MonoBehaviour {
    [SerializeField] private string _dsnUrl = "http://pub:priv@app.getsentry.com/12345";

    private RavenClient _ravenClient;
    private Dictionary<string, string> _clientInfo;

    void Awake()
	{
		DontDestroyOnLoad(gameObject);
		setup();
	    Application.logMessageReceived += HandleLog;
	}

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }
	
    void setup()
    {
        Debug.Log("Initializing RavenClient.");
        _ravenClient = new RavenClient(_dsnUrl, this);
        _ravenClient.Logger = "C#";
        _ravenClient.LogScrubber = new SharpRaven.Logging.LogScrubber();

        Debug.Log("Sentry Uri: " + _ravenClient.CurrentDSN.SentryURI);
        Debug.Log("Port: " + _ravenClient.CurrentDSN.Port);
        Debug.Log("Public Key: " + _ravenClient.CurrentDSN.PublicKey);
        Debug.Log("Private Key: " + _ravenClient.CurrentDSN.PrivateKey);
        Debug.Log("Project ID: " + _ravenClient.CurrentDSN.ProjectID);

        // Todo: Include Volo Airsport version string
        // Todo: Since these tags are always included, please just cache them in the jsonpacket or something.

        _clientInfo = new Dictionary<string, string>();

        _clientInfo.Add("Version", "v3.6.1");

        _clientInfo.Add("ProcessorType", SystemInfo.processorType);
        _clientInfo.Add("ProcessorCount", SystemInfo.processorCount.ToString());

        _clientInfo.Add("Device-Uid", SystemInfo.deviceUniqueIdentifier);
        _clientInfo.Add("Device-Model", SystemInfo.deviceModel);
        _clientInfo.Add("Device-Name", SystemInfo.deviceName);
        _clientInfo.Add("OS", SystemInfo.operatingSystem);
        _clientInfo.Add("MemorySize", SystemInfo.systemMemorySize.ToString());

        _clientInfo.Add("GPU-Memory", SystemInfo.graphicsMemorySize.ToString());
        _clientInfo.Add("GPU-Name", SystemInfo.graphicsDeviceName);
        _clientInfo.Add("GPU-Vendor", SystemInfo.graphicsDeviceVendor);
        _clientInfo.Add("GPU-VendorID", SystemInfo.graphicsDeviceVendorID.ToString());
        _clientInfo.Add("GPU-id", SystemInfo.graphicsDeviceID.ToString());
        _clientInfo.Add("GPU-Version", SystemInfo.graphicsDeviceVersion);
        _clientInfo.Add("GPU-ShaderLevel", SystemInfo.graphicsShaderLevel.ToString());

        //_clientInfo.Add("playTime", Time.realtimeSinceStartup.ToString());
        //dic.Add("GPU-PixelFillrate", SystemInfo.graphicsPixelFillrate.ToString());		
    }

    private void HandleLog(string log, string stack, LogType type) {
	    bool send = false;

        // Todo: make reporting level configurable
	    switch (type) {
	        case LogType.Error:
	            send = true;
	            break;
	        case LogType.Assert:
	            break;
	        case LogType.Warning:
	            send = true;
	            break; 
	        case LogType.Log:
	            break;
	        case LogType.Exception:
                send = true;
                break;
	        default:
	            throw new ArgumentOutOfRangeException("type", type, null);
	    }

        Debug.Log("received: " + log + "\n" + stack);

	    if (send) {
	        _ravenClient.CaptureUnityLog(log, stack, type, _clientInfo);
        }
    }
}
