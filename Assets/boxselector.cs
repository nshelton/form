using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class boxselector : MonoBehaviour {

    public TotemParameterController controller;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 val = transform.localPosition;
        controller.trans = val * 10;
    }
}
