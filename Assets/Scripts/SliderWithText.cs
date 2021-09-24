using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SliderWithText : MonoBehaviour
{
    public Slider slider;
    public InputField text;
    public UnityEvent onValueChange;
    public byte decimals = 2;


    void OnEnable()
    {
        text.onEndEdit.AddListener(ChangeValueText);
        slider.onValueChanged.AddListener(ChangeValue);
        ChangeValue(slider.value);
    }
    void OnDisable()
    {
        slider.onValueChanged.RemoveAllListeners();
    }

    void ChangeValueText(string value)
    {
        if(float.TryParse(value, out float result))
        {
            if (result < slider.minValue)
            {
                ChangeValue(slider.minValue);
            } 
            else if (result > slider.maxValue) 
            {
                ChangeValue(slider.maxValue);
            } 
            else
            {
                ChangeValue(result);
            }
        } 
        else 
        {
            ChangeValue(slider.value);
        }
        
        //ChangeValue(float.Parse(value));
    }

 

    public void ChangeValue(float value)
    {
        text.text = value.ToString("n" + decimals);
        slider.value = value;
        onValueChange.Invoke();
    }
}
