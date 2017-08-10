using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;


public class CustomCamera : MonoBehaviour {

    public float renderScale;

	// Use this for initialization
	void Start () {
        renderScale = 0.6f;
	}
	
	// Update is called once per frame
	void Update () {
        VRSettings.renderScale = renderScale;

    }
}
