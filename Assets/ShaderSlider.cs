using UnityEngine;
using System.Collections;

namespace NewtonVR.Example
{
    public class ShaderSlider : MonoBehaviour
    {
        public Material materialToModify;

        public NVRSlider Slider;

        private void Update()
        {
            materialToModify.SetFloat("_palette", Slider.CurrentValue);
        }
    }
}