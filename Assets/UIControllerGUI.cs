using UnityEngine;
using System.Collections;

public class UIControllerGUI : MonoBehaviour {

	[SerializeField]
	GameObject sliders;

	[SerializeField]
	GameObject boxes;


	[SerializeField]
	TotemParameterController param;
    void OnGUI() {

		string str = string.Format("{0} Sliders", sliders.activeSelf ? "Disable" : "Enable");
        if (GUI.Button(new Rect(10, 10, 100, 30), str)) {
			sliders.SetActive(sliders.activeSelf ? false : true);
		}	
             
		str = string.Format("{0} Boxes", boxes.activeSelf ? "Disable" : "Enable");
        if (GUI.Button(new Rect(120, 10, 100, 30), str)) {
			boxes.SetActive(boxes.activeSelf ? false : true);
		}

		str = string.Format("Presets : {0}\n ", param.numPresets);

		if (param.currentPreset >= 0 && param.currentPreset < param.presets.Count)
		{
			str += string.Format("Current : {0} \n ",  param.currentPreset );
			string state = (string)param.presets[param.currentPreset];
			string[] fields = state.Split(',');
			
			for ( int i = 0; i < param.controls.Length; i ++)
			{
				str += string.Format("{0}\t{1}\n", param.controls[i], fields[i]);
			}

			str += "\nIterationTransform\n";
			Matrix4x4 mat =  param.material.GetMatrix("_IterationTransform");


			str += string.Format("rotation:\t{0:0.000}\t{1:0.000}\t{2:0.000}\t{3:0.000}\n", 
					mat.GetRotation().x,
					mat.GetRotation().y,
					mat.GetRotation().z,
					mat.GetRotation().w);

			str += string.Format("translation:\t{0:0.000}\t{1:0.000}\t{2:0.000}\n", 
					mat.GetPosition().x,
					mat.GetPosition().y,
					mat.GetPosition().z);

			str += string.Format("scale:\t{0:0.000}\n", mat.GetScale().x);



			str += "\nModelView\n";
			
			mat =  param.material.GetMatrix("_WorldToModelTransform");

			str += string.Format("rotation:\t{0:0.000}\t{1:0.000}\t{2:0.000}\t{3:0.000}\n", 
					mat.GetRotation().x,
					mat.GetRotation().y,
					mat.GetRotation().z,
					mat.GetRotation().w);

			str += string.Format("translation:\t{0:0.000}\t{1:0.000}\t{2:0.000}\n", 
					mat.GetPosition().x,
					mat.GetPosition().y,
					mat.GetPosition().z);

			str += string.Format("scale:\t{0:0.000}\n", mat.GetScale().x);

		}
		GUI.Label( new Rect(10, 50, 500, 1000) , str );
    }
}