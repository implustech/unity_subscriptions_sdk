using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ApptilausSDK;

public class Apptilaus : MonoBehaviour
{

    public string AppId = "{AppId}";
    public string AppToken = "{AppToken}";
    public bool EnableSessionTracking = false;

    void Start()
    {
        if (string.IsNullOrEmpty(AppId) || string.IsNullOrEmpty(AppToken)) {
            Debug.LogError("[Apptilaus] Invalid initializer parameters");
            return;
        }
        ApptilausManager.Shared.Setup(AppId, AppToken, EnableSessionTracking);
    }
}
