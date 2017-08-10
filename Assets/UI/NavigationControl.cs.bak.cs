using UnityEngine;
using System.Collections;

public class NavigationControlOne : MonoBehaviour {

    public Transform leftController;
    public Transform rightController;

    public SteamVR_TrackedController leftControllerButtons;
    public SteamVR_TrackedController rightControllerButtons;

    private bool grip_l;
    private bool grip_r;

    private Vector3 leftControllerGrip;
    private Vector3 rightControllerGrip;

    private Vector3 currentPos;
    private Quaternion currentRot;
    private Vector3 deltaPos;
    private Quaternion deltaRot;
    private Vector3 rotationPoint;
    private Vector3 lastRotationPoint;


    private float deltaScale;
    private float currentScale;

	int frameNum = 0;

	// Use this for initialization
	void Start () {
		lastRotationPoint = new Vector3(0,0,0);
		grip_r = false;
		grip_l = false;
		currentPos = new Vector3(0,0,0);
		rotationPoint = new Vector3(0,0,0);
		currentRot = new Quaternion(0,0,0,1);
		currentScale = 1;

		deltaPos = new Vector3(0,0,0);
		deltaRot = new Quaternion(0,0,0,1);
		deltaScale = 1;

		leftControllerButtons.TriggerClicked  += grippedL;
		rightControllerButtons.TriggerClicked += grippedR;
		
		leftControllerButtons.TriggerUnclicked  += unGrippedL;
		rightControllerButtons.TriggerUnclicked += unGrippedR;

	}
	
	void moveTrackBall() {
		if (rotationPoint.magnitude == 0)
			return;

		Vector3 d = ((rightController.position + leftController.position) / 2 );

		// d = currentRot * (d - rotationPoint); // displacement in the world frame (with trackball rotation)

		// currentPos = currentRot * currentPos;
		// currentPos += 
		// currentPos = Quaternion.Inverse(currentRot) * currentPos;
 
		Debug.Log((d).ToString("F4"));
		updateDeltas();
	}

	void grippedL(object sender, ClickedEventArgs a) {
		leftControllerGrip = leftController.position;
		grip_l = true;

 		if (grip_r)
 			moveTrackBall();
	}

	void grippedR(object sender, ClickedEventArgs a) {
		rightControllerGrip = rightController.position;
		grip_r = true;

		if (grip_l)
			moveTrackBall();
	}

	void unGrippedL(object sender, ClickedEventArgs a) {
		grip_l = false;
		if ( grip_r )  
			accumulateDeltasAndReset();
	}

	void unGrippedR(object sender, ClickedEventArgs a) {
		grip_r = false;
		if ( grip_l ) 
			accumulateDeltasAndReset();
	}

	void accumulateDeltasAndReset() {
		updateDeltas();

		// squash deltas into the current state
		currentRot 		= deltaRot * currentRot;
		currentPos		+= deltaPos ;
		currentScale 	*= deltaScale;

		 // the translation is in the frame of referenct with this "rotation point" as origin
		 // need to change the state here to be in the world frame. Make this look wight with rotationPoint=000

		deltaPos = new Vector3(0,0,0);
		deltaRot = new Quaternion(0,0,0,1);
		deltaScale = 1;

		// rotationPoint = new Vector3(0,0,0);
	}

	void updateDeltas() {
	  	deltaRot.SetFromToRotation(
	  		(rightControllerGrip - leftControllerGrip),
	  		(rightController.position - leftController.position));

	  	deltaPos = 
	  		(rightController.position + leftController.position) / 2 -
	  		(rightControllerGrip + leftControllerGrip) / 2 ;

	 	deltaScale = ((rightController.position - leftController.position).magnitude / 
	 		(rightControllerGrip - leftControllerGrip).magnitude) ;

		rotationPoint = (rightController.position + leftController.position) / 2;

	}

	void updateTrackballState() {

		// GetComponent<Renderer>().material.SetVector ("_RotationPoint", rotationPoint );
		GetComponent<Renderer>().material.SetVector ("_ModelToTrackball", rotationPoint - (currentPos + deltaPos) );

		// GetComponent<Renderer>().material.SetFloat ("_scale", currentScale * deltaScale);
		GetComponent<Renderer>().material.SetFloat ("_scale", 1);

 		Matrix4x4 m = Matrix4x4.TRS(
			new Vector3(0,0,0),
			// deltaPos + currentPos,
 			deltaRot * currentRot, 
 			// new Quaternion(0,0,0,1),
 			new Vector3(1,1,1));

		GetComponent<Renderer>().material.SetMatrix ("_FractalModelView", m);
		
		Vector3 t = deltaPos + currentPos;
		GetComponent<Renderer>().material.SetVector ("_WorldToModel", t);

	}

	// Update is called once per frame
	void Update () {

		if (grip_r && grip_l) {
			updateDeltas();

			GetComponent<Renderer>().material.SetFloat ("_InteractiveTransform", 1);
 		} else {
			GetComponent<Renderer>().material.SetFloat ("_InteractiveTransform", 0);			
 		}

		updateTrackballState();
		frameNum++;

	}	
}
