using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSLimiter : MonoBehaviour {

    public int fps = 24;

	// Use this for initialization
	void Start () {
        Application.targetFrameRate = fps;

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
