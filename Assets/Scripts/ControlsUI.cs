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
        PlayerPrefs.SetFloat("mouseSensitivity", mouseSensitivitySlider.slider.value);
    }

    void LoadSettings()
    {
        if (PlayerPrefs.HasKey("mouseSensitivity"))
        {
            float sensitivity = PlayerPrefs.GetFloat("mouseSensitivity");
            mouseSensitivitySlider.ChangeValue(sensitivity);
            UpdateMouseSensitivity();
        }
        else
        {
            PlayerPrefs.SetFloat("mouseSensitivity", mouseSensitivitySlider.slider.value);
        }
    }
}
