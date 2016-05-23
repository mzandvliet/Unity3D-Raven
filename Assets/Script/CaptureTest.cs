using UnityEngine;
using System.Collections;
using SharpRaven;
using SharpRaven.Logging;
using System;

public class CaptureTest : MonoBehaviour {
    [SerializeField] private string _dsnUrl = "http://pub:priv@app.getsentry.com/12345";

    static RavenClient ravenClient;

	void Start () 
	{	
		setup();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //for (int i = 0; i < 16; i++) {
                testWithoutStacktrace();
            //}
        }

        //testWithStacktrace();
    }

    void setup()
    {
        //Debug.Log("Initializing RavenClient.");
        ravenClient = new RavenClient(_dsnUrl);
        ravenClient.Logger = "C#";
        ravenClient.LogScrubber = new SharpRaven.Logging.LogScrubber();

//        Debug.Log("Sentry Uri: " + ravenClient.CurrentDSN.SentryURI);
//        Debug.Log("Port: " + ravenClient.CurrentDSN.Port);
//        Debug.Log("Public Key: " + ravenClient.CurrentDSN.PublicKey);
//        Debug.Log("Private Key: " + ravenClient.CurrentDSN.PrivateKey);
//        Debug.Log("Project ID: " + ravenClient.CurrentDSN.ProjectID);
    }

	
    void testWithoutStacktrace()
    {
//            Debug.Log("Send exception without stacktrace.");
            var id = ravenClient.CaptureException(new Exception("Test without a stacktrace"));
//            Debug.Log("Sent packet: " + id);
    }

    void testWithStacktrace()
    {
//        Debug.Log("Causing division by zero exception.");
        try
        {
            PerformDivideByZero();
        }
        catch (Exception e)
        {
//            Debug.Log("Captured: " + e.Message);
            var id = ravenClient.CaptureException(e);
//            Debug.Log("Sent packet: " + id);
        }
    }

    static void PerformDivideByZero()
    {
        int i2 = 0;
        int i = 10 / i2;
    }
	
}
