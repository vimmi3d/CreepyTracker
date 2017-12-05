using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrackerProperties : MonoBehaviour {

    private static TrackerProperties _singleton;

    public int listenPort = 55555;
    public int trackerPort = 53804;

    public string configFilename = "configSettings.txt";

    private TrackerProperties()
    {
        _singleton = this;
    }

    public static TrackerProperties Instance
    {
        get
        {
            return _singleton;
        }
    }

    void Start()
    {
        //_singleton = this;
    }
}
