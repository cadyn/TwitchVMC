using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlsUI : MonoBehaviour
{

    public GameObject mainCamera;
    public GameObject mouseSensitivity;
    
    private FreeCam mainController;
    private SliderWithText mouseSensitivitySlider;

    void Start()
    {
        mainController = mainCamera.GetComponent<FreeCam>();
        mouseSensitivitySlider = mouseSensitivity.GetComponent<SliderWithText>();
        mouseSensitivitySlider.onValueChange.AddListener(UpdateMouseSensitivity);
    }

    void UpdateMouseSensitivity()
    {
        mainController.freeLookSensitivity = mouseSensitivitySlider.slider.value;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
