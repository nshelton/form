using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class boxrotator : MonoBehaviour
{

    [SerializeField]

    public TotemParameterController controller;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        controller.rot = transform.localRotation.eulerAngles;
    }
}