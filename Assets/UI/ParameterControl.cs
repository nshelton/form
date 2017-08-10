using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class ParameterControl : MonoBehaviour {

    public Transform leftController;
    public Transform rightController;

    public NewtonVR.NVRHand leftHand;
    public NewtonVR.NVRHand rightHand;

    public SteamVR_TrackedController leftControllerButtons;
    public SteamVR_TrackedController rightControllerButtons;

	public Material wireframeMaterial;
	public GameObject targetObject;

    private bool grip_l;
    private bool grip_r;

    private Vector3 leftControllerGrip;
    private Vector3 rightControllerGrip;

 	private List<GameObject> controlObjects;

 	private int currentlyModifyingIndex;
 	
 	private string[] indexMapping;

    private int numControls = 5;

    private string presetFileName = Application.persistentDataPath + "/presets.txt";
    private ArrayList presets;

    void tryInitControllers()
    {
        rightHand = GameObject.Find("RightHand").GetComponent<NewtonVR.NVRHand>();
        leftHand = GameObject.Find("LeftHand").GetComponent<NewtonVR.NVRHand>();
    }

        void Start () {
		indexMapping = new string[5] {"_Threshold", "_StepRatio", "_FoldScale", "fs", "fu"};
		currentlyModifyingIndex = -1;
		controlObjects = new List<GameObject>();
		grip_r = false;
		grip_l = false;

 		leftControllerButtons.TriggerClicked  += grippedL;
		rightControllerButtons.TriggerClicked += grippedR;
		
		leftControllerButtons.TriggerUnclicked  += unGrippedL;
		rightControllerButtons.TriggerUnclicked += unGrippedR;

		float s = 0.01f;
		float val = 0.1f;


		for ( int i = 0; i < numControls; i ++) {

			controlObjects.Add(GameObject.CreatePrimitive(PrimitiveType.Cube));

	        controlObjects[i].transform.position = new Vector3(i * 0.015f,0,0);
	    	// controlObjects[i].transform.localScale  = new Vector3(s, val, s);
	        controlObjects[i].GetComponent<Renderer>().material = wireframeMaterial;
		 
	        controlObjects[i].transform.SetParent(this.transform);

            Mesh mesh = controlObjects[i].GetComponent<MeshFilter>().mesh;
			Vector3[] vertices = mesh.vertices;

	        int j = 0;
	        Vector3 offs = new Vector3(0.0f, 0.5f, 0.0f);

	        while (j < vertices.Length) {
	        	vertices[j] += offs;
	            vertices[j] *= s;
	            j++;
	        }
	        mesh.vertices = vertices;
	        mesh.RecalculateBounds();
		}

        // parse the presets
        using (System.IO.StringReader reader = new System.IO.StringReader(presetFileName)) {
            presets.Add(reader.ReadLine());
        }


    }

    void loadPreset(int index)
    {
        Material mat = targetObject.GetComponent<Renderer>().material;

        string pre = (string)presets[index];
        string[] fields = pre.Split(',');

        for (int i = 0; i < numControls; i++)
        {
            mat.SetFloat(indexMapping[i], float.Parse(fields[i]));
        }

        Matrix4x4 matrix = Matrix4x4.identity;

        Vector3 pos = new Vector3(
            float.Parse(fields[numControls + 0]),
            float.Parse(fields[numControls + 1]),
            float.Parse(fields[numControls + 2]));

        Quaternion rot = new Quaternion(
            float.Parse(fields[numControls + 3]),
            float.Parse(fields[numControls + 4]),
            float.Parse(fields[numControls + 5]),
            float.Parse(fields[numControls + 6]));

        float s = float.Parse(fields[numControls + 7]);
        Vector3 scale = new Vector3(s, s, s);

        matrix.SetTRS(pos, rot, scale);
        mat.SetMatrix("_IterationTransform", matrix);
    }

    void savePreset()
    {
        string text2write = "";
        Material mat = targetObject.GetComponent<Renderer>().material;
        //floats
        for (int i = 0; i < numControls; i++)
        {
            float m = mat.GetFloat(indexMapping[i]);
            text2write = string.Concat(text2write, "{0},", m);
        }

        Matrix4x4 matrix = mat.GetMatrix("_IterationTransform");

        text2write = string.Concat(text2write, string.Format(
           "{0},{1},{2},{3},{4},{5},{6}/n",
          matrix.GetPosition().x,
          matrix.GetPosition().y,
          matrix.GetPosition().z,

          matrix.GetRotation().x,
          matrix.GetRotation().y,
          matrix.GetRotation().z,
          matrix.GetRotation().w,

          matrix.GetScale().x));
        Debug.Log(text2write);
        System.IO.File.AppendAllText(presetFileName, text2write);
    }


    void grippedL(object sender, ClickedEventArgs a) {
		leftControllerGrip = leftController.position;
		grip_l = true;

		for(int i = 0; i < controlObjects.Count; i ++) {
  			if(controlObjects[i].GetComponent<Collider>().bounds.Contains(leftController.position)) {
  				currentlyModifyingIndex = i;
  			}
		}
	}

	void grippedR(object sender, ClickedEventArgs a) {
		rightControllerGrip = rightController.position;
		grip_r = true;
	}

	void unGrippedL(object sender, ClickedEventArgs a) {
		grip_l = false;
		currentlyModifyingIndex = -1;
	}

	void unGrippedR(object sender, ClickedEventArgs a) {
		grip_r = false;
	}

	void copyTransform(Transform t_out, Transform t_in) {
		t_out.position = new Vector3( 
		 	t_in.position.x,
		 	t_in.position.y,
		 	t_in.position.z ); 

		t_out.rotation = new Quaternion(
			t_in.rotation.x, 
			t_in.rotation.y, 
			t_in.rotation.z,
			t_in.rotation.w);

	}

	void ScaleMesh(GameObject bar, float d) {

		Mesh mesh = bar.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int i = 0;
        while (i < vertices.Length) {
            if(vertices[i].y > 0  )
            	vertices[i].y = d;
            i++;
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();

        BoxCollider col = (BoxCollider)bar.GetComponent<Collider>();
		col.size = new Vector3(0.01f, d, 0.01f);

    }



	// Update is called once per frame
	void Update () {
        if (!leftController || !rightController)
        {
            tryInitControllers();
            return;
        }

        Debug.Log("sdfg");
        if (leftHand.Inputs[NewtonVR.NVRButtons.ApplicationMenu].IsPressed)
        {
            savePreset();
        }

            if (grip_r  ) {
				copyTransform(this.transform, rightController);
		}

/*		
		 Matrix4x4 m = Matrix4x4.TRS(
		  	new Vector3(0,0,0), 
		  	new Quaternion(1,0,0,1), 
		  	new Vector3(s,s,s));
            */
		// GetComponent<Renderer>().material.SetMatrix ("_IterationTransform", m);

		if(currentlyModifyingIndex > -1 ) {
			float d = (leftController.position - leftControllerGrip).magnitude;
			ScaleMesh( controlObjects[currentlyModifyingIndex], d);

			targetObject.GetComponent<Renderer>().material.SetFloat(indexMapping[currentlyModifyingIndex], d * 100.0f);

		}
	}	
}
