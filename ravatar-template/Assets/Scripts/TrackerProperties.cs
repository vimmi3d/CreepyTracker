using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrackerProperties : MonoBehaviour {

    private static TrackerProperties _singleton;

    int _listenPort = 55555;
    int _trackerPort = 53804;

    
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

    public int listenPort
    {
        get
        {
            return _listenPort;
        }

        set
        {
            _listenPort = value;
        }
    }

    public int trackerPort
    {
        get
        {
            return _trackerPort;
        }

        set
        {
            _trackerPort = value;
        }
    }

    void Start()
    {
        //_singleton = this;
    }
}
