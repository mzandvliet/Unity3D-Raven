using UnityEngine;
using System.Collections;
using SharpRaven;
using SharpRaven.Logging;
using System;

public class CaptureTest : MonoBehaviour {
	
	//const string dsnUrl = "add-your-sentry-dns-here";
	const string dsnUrl = "https://c2854807ceba4725856a9ded504305d5:1d256f1ca71b4ee3a6ebc82d31810f3d@app.getsentry.com/79295";	
    static SharpRaven.RavenClient ravenClient;

	void Start () 
	{	
		setup();
        testWithoutStacktrace();
        testWithStacktrace();
    }

    static void setup()
    {
        Debug.Log("Initializing RavenClient.");
        ravenClient = new RavenClient(dsnUrl);
        ravenClient.Logger = "C#";
        ravenClient.LogScrubber = new SharpRaven.Logging.LogScrubber();

        Debug.Log("Sentry Uri: " + ravenClient.CurrentDSN.SentryURI);
        Debug.Log("Port: " + ravenClient.CurrentDSN.Port);
        Debug.Log("Public Key: " + ravenClient.CurrentDSN.PublicKey);
        Debug.Log("Private Key: " + ravenClient.CurrentDSN.PrivateKey);
        Debug.Log("Project ID: " + ravenClient.CurrentDSN.ProjectID);
    }

	
    static void testWithoutStacktrace()
    {
            Debug.Log("Send exception without stacktrace.");
            var id = ravenClient.CaptureException(new Exception("Test without a stacktrace."));
            Debug.Log("Sent packet: " + id);
    }

    static void testWithStacktrace()
    {
        Debug.Log("Causing division by zero exception.");
        try
        {
            PerformDivideByZero();
        }
        catch (Exception e)
        {
            Debug.Log("Captured: " + e.Message);
            var id = ravenClient.CaptureException(e);
            Debug.Log("Sent packet: " + id);
        }
    }

    static void PerformDivideByZero()
    {
        int i2 = 0;
        int i = 10 / i2;
    }
	
}
