using UnityEngine;
using System.Collections;
using SharpRaven;
using SharpRaven.Logging;
using System;

public class CaptureTest : MonoBehaviour {
    [SerializeField] private string _dsnUrl = "http://pub:priv@app.getsentry.com/12345";

    private RavenClient _ravenClient;

	void Start () 
	{	
		setup();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
                testWithoutStacktrace();
                testWithStacktrace();
        }
    }

    void setup()
    {
        _ravenClient = new RavenClient(_dsnUrl, this);
        _ravenClient.Logger = "C#";
        _ravenClient.LogScrubber = new LogScrubber();
    }

	
    void testWithoutStacktrace()
    {
            _ravenClient.CaptureException(new Exception("Test without a stacktrace"));
    }

    void testWithStacktrace()
    {
        try
        {
            PerformDivideByZero();
        }
        catch (Exception e)
        {
            _ravenClient.CaptureException(e);
        }
    }

    static void PerformDivideByZero()
    {
        int i2 = 0;
        int i = 10 / i2;
    }
	
}
