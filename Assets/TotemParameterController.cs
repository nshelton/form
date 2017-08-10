using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

//[ExecuteInEditMode]
public class TotemParameterController : MonoBehaviour
{

    #region Serialized Variables

    public NewtonVR.NVRHand leftHand;
    public NewtonVR.NVRHand rightHand;
    public bool menuWasPressed = false;
    public bool rPadPressed = false;
    public bool lPadPressed = false;

    [SerializeField]
    public Vector3 rot;

    [SerializeField]
    public Vector3 trans;
    #endregion

    public GameObject controllerParent;

    public NewtonVR.NVRSlider[] sliders;
    public string[] controls;
    public float[] ranges;
    public float[] offsets;

    public const int nControls = 10;

    public ArrayList presets;
    public int numPresets = 0;
    public int currentPreset = -1;

    public Material material;

    void tryInitControllers()
    {
        if ( GameObject.Find("RightHand") != null)
            rightHand = GameObject.Find("RightHand").GetComponent<NewtonVR.NVRHand>();
            
        if ( GameObject.Find("LeftHand") != null)
             leftHand = GameObject.Find("LeftHand").GetComponent<NewtonVR.NVRHand>();
    }
    
    public string presetFileName() 
    {
        return Application.dataPath + "/presets.txt";
    }
    void Start()
    {
        material = GetComponent<Renderer>().material;
        presets = new ArrayList();
        controls = new string[nControls] { "_palette", "_InnerRad", "_FoldScale", "fs", "fu",
                                       "_Threshold", "_StepRatio", "_fractalType", "fc", "fd"};
        ranges = new float[nControls] { 1, 5, 10, 10, 10, 10, 5, 1, 3, 3 };
        offsets = new float[nControls] { 0, 0, -5, -3, 0, 0, 0, 0, 0, 0 };

        sliders = new NewtonVR.NVRSlider[controls.Length];

        GameObject[] sliderObjects;
        sliderObjects = GameObject.FindGameObjectsWithTag("slider");
        int i = 0;
        foreach (GameObject s in sliderObjects)
        {

            if (i < sliders.Length)
                sliders[i] = s.GetComponent<NewtonVR.NVRSlider>();

            TextMesh label = s.GetComponentInChildren<TextMesh>();
            label.text = controls[i];

            i++;
        }

        // parse the presets
        FileInfo theSourceFile = new FileInfo(presetFileName());
        StreamReader reader = theSourceFile.OpenText();

        string text;

        do
        {
            text = reader.ReadLine();
            presets.Add(text);
            Debug.Log(text);
            numPresets++;
        } while (text != null);


    }

    void loadPreset(int index)
    {

        string pre = (string)presets[index];
        Debug.Log("loading" + pre);
        string[] fields = pre.Split(',');

        for (int i = 0; i < nControls; i++)
        {
            float val = float.Parse(fields[i]);
            material.SetFloat(controls[i], val);

            sliders[i].CurrentValue = (val - offsets[i]) / ranges[i];
        }

        Debug.Log("line" + (index+1));

        Matrix4x4 matrix = Matrix4x4.identity;

        Vector3 ppos = new Vector3(
            float.Parse(fields[nControls + 0]),
            float.Parse(fields[nControls + 1]),
            float.Parse(fields[nControls + 2]));
        trans = ppos;
        Quaternion prot = new Quaternion(
            float.Parse(fields[nControls + 3]),
            float.Parse(fields[nControls + 4]),
            float.Parse(fields[nControls + 5]),
            float.Parse(fields[nControls + 6]));
        rot = prot.eulerAngles;

        float s = float.Parse(fields[nControls + 7]);
        Vector3 scale = new Vector3(s, s, s);

        matrix.SetTRS(ppos, prot, scale);
        material.SetMatrix("_IterationTransform", matrix);

        ppos = new Vector3(
            float.Parse(fields[nControls + 8]),
            float.Parse(fields[nControls + 9]),
            float.Parse(fields[nControls + 10]));

        prot = new Quaternion(
            float.Parse(fields[nControls + 11]),
            float.Parse(fields[nControls + 12]),
            float.Parse(fields[nControls + 13]),
            float.Parse(fields[nControls + 14]));

        s = float.Parse(fields[nControls + 15]);
        scale = new Vector3(s, s, s);

        matrix.SetTRS(ppos, prot, scale);
        material.SetMatrix("_WorldToModelTransform", matrix);

        NavigationControl n = GetComponent<NavigationControl>();
        n.worldToModelTransform = matrix;
        n.SquashWorldToModelTransform();
    }

    void savePreset()
    {
        string text2write = "";
        //floats
        for (int i = 0; i < nControls; i++)
        {
            float m = material.GetFloat(controls[i]);
            text2write = string.Concat(text2write, m + ",");
        }

        Matrix4x4 matrix = material.GetMatrix("_IterationTransform");

        text2write = string.Concat(text2write, string.Format(
           "{0:000.0000000000},{1:000.0000000000},{2:000.0000000000},{3:000.0000000000},{4:000.0000000000},{5:000.0000000000},{6:000.0000000000},{7:000.0000000000},",
          matrix.GetPosition().x,
          matrix.GetPosition().y,
          matrix.GetPosition().z,

          matrix.GetRotation().x,
          matrix.GetRotation().y,
          matrix.GetRotation().z,
          matrix.GetRotation().w,

          matrix.GetScale().x));

        matrix = material.GetMatrix("_WorldToModelTransform");
        text2write = string.Concat(text2write, string.Format(
         "{0:000.0000000000},{1:000.0000000000},{2:000.0000000000},{3:000.0000000000},{4:000.0000000000},{5:000.0000000000},{6:000.0000000000},{7}\n",
        matrix.GetPosition().x,
        matrix.GetPosition().y,
        matrix.GetPosition().z,

        matrix.GetRotation().x,
        matrix.GetRotation().y,
        matrix.GetRotation().z,
        matrix.GetRotation().w,

        matrix.GetScale().x));

        Debug.Log(text2write);
        System.IO.File.AppendAllText(presetFileName(), text2write);
    }


    void Update()
    {

        if (!leftHand || !rightHand)
        {
            tryInitControllers();
            return;
        }

        if (leftHand.Inputs[NewtonVR.NVRButtons.ApplicationMenu].IsPressed && !menuWasPressed)
        {
            savePreset();
        }
        menuWasPressed = leftHand.Inputs[NewtonVR.NVRButtons.ApplicationMenu].IsPressed;

        if (leftHand.Inputs[NewtonVR.NVRButtons.Touchpad].IsPressed && !lPadPressed)
        {
            currentPreset = (currentPreset + 1) % numPresets;
            loadPreset(currentPreset);
        }
        lPadPressed = leftHand.Inputs[NewtonVR.NVRButtons.Touchpad].IsPressed;

        /*        if (rightHand.Inputs[NewtonVR.NVRButtons.Touchpad].IsPressed && !rPadPressed)
                {
                    currentPreset = (currentPreset - 1) % numPresets;
                    loadPreset(currentPreset);

                }
                rPadPressed = rightHand.Inputs[NewtonVR.NVRButtons.Touchpad].IsPressed;
                */
        Vector3 dr = new Vector3(
            Mathf.Sin(Time.time / 10) * 30,
            Mathf.Sin(Time.time / 17) * 30,
            Mathf.Sin(Time.time / 23) * 30);

        Quaternion q = Quaternion.Euler(rot + dr);
        Vector3 trans_abs = new Vector3(
            Mathf.Abs(trans.x),
            Mathf.Abs(trans.y),
            Mathf.Abs(trans.z));
        Matrix4x4 m = Matrix4x4.TRS(trans_abs, q, Vector3.one);
        material.SetMatrix("_IterationTransform", m);

        for (int i = 0; i < sliders.Length; i++)
        {
            material.SetFloat(controls[i], sliders[i].CurrentValue * ranges[i] + offsets[i]);

        }


    }
}
