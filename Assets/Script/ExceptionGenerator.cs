using UnityEngine;
using System;

public class ExceptionGenerator : MonoBehaviour {
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            Debug.Log("log test");
            Debug.LogWarning("warning test");
            Debug.LogError("error test");
            Debug.Assert(1 == 2, "assert test");

            ThrowSimple();
            ThrowNestedA();
        }
    }

    private static void ThrowSimple() {
        throw new Exception("SimpleException");
    }

    private static void ThrowNestedA() {
        ThrowNestedB();
    }

    private static void ThrowNestedB() {
        throw new Exception("Test without a stacktrace");
    }
}
