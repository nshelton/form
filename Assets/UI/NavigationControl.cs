using UnityEngine;
using System.Collections;

public class NavigationControl : MonoBehaviour {

    public Transform leftController;
    public Transform rightController;

    public NewtonVR.NVRHand leftHand;
    public NewtonVR.NVRHand rightHand;
    public NewtonVR.NVRHead head;

    public float translationSpeedInv = 400;

    private bool grip_l;
    private bool grip_r;
    private bool trigger;

    private Vector3 leftControllerGrip;
    private Vector3 rightControllerGrip;

    public Matrix4x4 worldToModelTransform;
    private Matrix4x4 worldToTrackball_Position;
    private Matrix4x4 worldToTrackball_Orientation;
    private Matrix4x4 worldToModelTemporary_Translation;

    private Quaternion 	deltaRot;
    private Vector3 	deltaPos;
    private Vector3 	modelToTrackball;

    private float deltaScale;
    private float currentScale;
    private float accumulatedScale;

    int frameNum = 0;
    bool initialized;

    // Use this for initialization
    void Start () {

        initialized = false;

        grip_r = false;
		grip_l = false;
    
		deltaRot = new Quaternion(0,0,0,1);
		deltaPos = new Vector3(0,0,0);
		modelToTrackball = new Vector3(0,0,0);
		currentScale = 1;
		deltaScale = 1;
   		accumulatedScale = 1;

 		worldToModelTransform = Matrix4x4.TRS( new Vector3(0,0,0),new Quaternion(0,0,0,1),new Vector3(1,1,1));
 		worldToTrackball_Position = Matrix4x4.TRS( new Vector3(0,0,0),new Quaternion(0,0,0,1),new Vector3(1,1,1));
 		worldToTrackball_Orientation = Matrix4x4.TRS( new Vector3(0,0,0),new Quaternion(0,0,0,1),new Vector3(1,1,1));
 		worldToModelTemporary_Translation = Matrix4x4.TRS( new Vector3(0,0,0),new Quaternion(0,0,0,1),new Vector3(1,1,1));
	}

    void setGripPos() {
        leftControllerGrip = leftController.position;
        rightControllerGrip = rightController.position;

    }

    void grippedL() {
		grip_l = true;
        if (grip_r)
            setGripPos();

    }

	void grippedR() {
		grip_r = true;
        if (grip_l)
            setGripPos();
    }

	void unGrippedL() {
		grip_l = false;
		if ( grip_r )  
			SquashWorldToModelTransform();
	}

	void unGrippedR() {
		grip_r = false;
		if ( grip_l ) 
			SquashWorldToModelTransform();
	}

    public void SquashWorldToModelTransform() {

		worldToModelTransform = worldToModelTransform 
								* worldToModelTemporary_Translation
								* worldToTrackball_Position 
								* worldToTrackball_Orientation
								* worldToTrackball_Position.inverse ;

		deltaRot = new Quaternion(0,0,0,1);
		deltaPos = new Vector3(0,0,0);
		
		accumulatedScale *= currentScale * deltaScale;
		currentScale = 1;
		deltaScale = 1;

		// modelToTrackball =  new Vector3(0,0,0);
		worldToTrackball_Position = Matrix4x4.TRS( new Vector3(0,0,0),new Quaternion(0,0,0,1),new Vector3(1,1,1));
 		worldToTrackball_Orientation = Matrix4x4.TRS( new Vector3(0,0,0),new Quaternion(0,0,0,1),new Vector3(1,1,1));
 		worldToModelTemporary_Translation = Matrix4x4.TRS( new Vector3(0,0,0),new Quaternion(0,0,0,1),new Vector3(1,1,1));

	}

	void updateDeltas() {
	  	deltaPos = 
	  		(rightControllerGrip + leftControllerGrip) / 2 -
	  		(rightController.position + leftController.position) / 2 ;

	 	deltaScale =
	 		(rightControllerGrip - leftControllerGrip).magnitude / 
	 		(rightController.position - leftController.position).magnitude ;

	  	worldToModelTemporary_Translation  = Matrix4x4.TRS(
		 	deltaPos,
		 	new Quaternion(0,0,0,1),
 			new Vector3(1,1,1));

		worldToTrackball_Position = Matrix4x4.TRS(
		 	(rightController.position + leftController.position) / 2,
		 	new Quaternion(0,0,0,1),
 			new Vector3(1,1,1));

	  	deltaRot.SetFromToRotation(
	  		(rightController.position - leftController.position),
	  		(rightControllerGrip - leftControllerGrip));

	  	float s = deltaScale * currentScale;
		worldToTrackball_Orientation = Matrix4x4.TRS(
		 	new Vector3(0,0,0), deltaRot, 
		 	new Vector3(s,s,s));
		 	// new Vector3(1,1,1));

		modelToTrackball = (rightController.position + leftController.position) / 2 ;
	}

	void updateTrackballState() {
		GetComponent<Renderer>().material.SetFloat ("_scale", accumulatedScale * deltaScale);
		// GetComponent<Renderer>().material.SetFloat ("_scale", 1);

		Matrix4x4 m =  	worldToModelTransform 
						* worldToModelTemporary_Translation
		     			* worldToTrackball_Position
						* worldToTrackball_Orientation 
						* worldToTrackball_Position.inverse;

		GetComponent<Renderer>().material.SetVector ("_ModelToTrackball", m.MultiplyPoint3x4(modelToTrackball));

        GetComponent<Renderer>().material.SetMatrix("_WorldToModelTransform", m);
        //Debug.Log(m);
        //GetComponent<Renderer>().material.SetMatrix("_WorldToModelTransform_inv", m.inverse);

    }

    void tryInitControllers()
    {
        if ( GameObject.Find("RightHand") != null)
            rightHand = GameObject.Find("RightHand").GetComponent<NewtonVR.NVRHand>();
            
        if ( GameObject.Find("LeftHand") != null)
         leftHand = GameObject.Find("LeftHand").GetComponent<NewtonVR.NVRHand>();

        head = GameObject.Find("Head").GetComponent<NewtonVR.NVRHead>();

        if (leftHand && rightHand)
        {
            rightController = rightHand.transform;
            leftController = leftHand.transform;

            initialized = true;
        }
    }


    // Update is called once per frame
    void Update () {
        if (!initialized)
        {
            tryInitControllers();
            return;
        }

        if (!grip_r && 
            rightHand.CurrentHandState == NewtonVR.HandState.GripDownNotInteracting)
        {
            grippedR();
        }

        if (grip_r &&
            rightHand.CurrentHandState != NewtonVR.HandState.GripDownNotInteracting)
        {
            unGrippedR();
        }
        if (!grip_l &&
            leftHand.CurrentHandState == NewtonVR.HandState.GripDownNotInteracting)
        {
            grippedL();
        }

        if (grip_l &&
            leftHand.CurrentHandState != NewtonVR.HandState.GripDownNotInteracting)
        {
            unGrippedL();
        }

        if (leftHand.Inputs[NewtonVR.NVRButtons.Trigger].IsPressed)
        {
            float handDist = 5 *(head.transform.position - leftController.transform.position).magnitude;

            worldToModelTemporary_Translation = Matrix4x4.TRS(
               Mathf.Pow(handDist, 2) * leftHand.CurrentForward / translationSpeedInv,
               new Quaternion(0, 0, 0, 1),
               new Vector3(1, 1, 1));

            SquashWorldToModelTransform();
            updateTrackballState();
            if (!rightHand.Inputs[NewtonVR.NVRButtons.Trigger].IsPressed)
                return;
        }

        if (rightHand.Inputs[NewtonVR.NVRButtons.Trigger].IsPressed)
        {
            float handDist = 5 * (head.transform.position - rightController.transform.position).magnitude;

            worldToModelTemporary_Translation = Matrix4x4.TRS(
               - Mathf.Pow(handDist, 2) * rightHand.CurrentForward / translationSpeedInv,
               new Quaternion(0, 0, 0, 1),
               new Vector3(1, 1, 1));

            SquashWorldToModelTransform();
            updateTrackballState();
            return;

        }

        // Debug.Log(leftHand.CurrentHandState + " " + rightHand.CurrentHandState);


        if (grip_r && grip_l ) {
			updateDeltas();

			GetComponent<Renderer>().material.SetFloat ("_InteractiveTransform", 1);
 		} else {
			GetComponent<Renderer>().material.SetFloat ("_InteractiveTransform", 0);			
 		}

		updateTrackballState();
		frameNum++;

	}	
}
