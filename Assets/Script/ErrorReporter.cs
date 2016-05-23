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

        _clientInfo = new Dictionary<string, string>();
        _clientInfo.Add("playTime", Time.realtimeSinceStartup.ToString());
        _clientInfo.Add("time", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff"));

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
        //dic.Add("GPU-PixelFillrate", SystemInfo.graphicsPixelFillrate.ToString());		
    }
	
	private void HandleLog(string log, string stack, LogType type)
	{
		//ravenClient.CaptureMessage(log, SharpRaven.Data.ErrorLevel.error, dic, null);
		_ravenClient.CaptureUnityLog(log, stack, type, _clientInfo);
	}	
}
