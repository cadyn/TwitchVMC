using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectInstanceUI : MonoBehaviour
{
    public InputField nameField;
    public FileInput modelField;
    public FileInput avatarField;
    public FileInput objectField;
    public List<Toggle> objectSets;
    public InputField Lifetime;
    public InputField Scale;
    public InputField Force;
    public Dropdown Hitbox;
    public Button deleteButton;
    public GameObject nameError;
    public GameObject modelError;
    public GameObject avatarError;
    public GameObject objectError;
    public GameObject lifetimeError;
    public GameObject scaleError;
    public GameObject forceError;
    [HideInInspector]
    public TwitchObject twitchObject;
    [HideInInspector]
    public event Action OnObjectChanged = delegate { };
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void resetAllErrors()
    {
        nameError.SetActive(false);
        modelError.SetActive(false);
        avatarError.SetActive(false);
        objectError.SetActive(false);
        lifetimeError.SetActive(false);
        scaleError.SetActive(false);
        forceError.SetActive(false);
    }
    public void HandleError(string error)
    {
        resetAllErrors();
        switch (error)
        {
            case "MODEL":
                modelField.UpdateString(twitchObject.Model);
                modelError.SetActive(true);
                break;
            case "NAME":
                nameField.text = twitchObject.Name;
                nameError.SetActive(true);
                break;
            case "AVATARCLIP":
                avatarField.UpdateString(twitchObject.AvatarCollisionClip);
                avatarError.SetActive(true);
                break;
            case "OBJECTCLIP":
                objectField.UpdateString(twitchObject.ObjectCollisionClip);
                objectError.SetActive(true);
                break;
            case "LIFETIME":
                Lifetime.text = twitchObject.Lifetime.ToString();
                lifetimeError.SetActive(true);
                break;
            case "SCALE":
                Scale.text = twitchObject.Scale.ToString();
                scaleError.SetActive(true);
                break;
            case "FORCE":
                Force.text = twitchObject.Force.ToString();
                forceError.SetActive(true);
                break;
        }
    }

    public void Initialize()
    {
        nameField.text = twitchObject.Name;
        modelField.UpdateString(twitchObject.Model);
        avatarField.UpdateString(twitchObject.AvatarCollisionClip);
        objectField.UpdateString(twitchObject.ObjectCollisionClip);
        for(int i = 0; i < objectSets.Count; i++)
        {
            bool objectSet = (twitchObject.Sets & 1 << i) > 0;
            objectSets[i].isOn = objectSet;
        }
        Lifetime.text = twitchObject.Lifetime.ToString();
        Scale.text = twitchObject.Scale.ToString();
        Force.text = twitchObject.Force.ToString();
        nameField.onEndEdit.AddListener(NameChanged);
        modelField.OnTextChanged += ModelChanged;
        avatarField.OnTextChanged += AvatarChanged;
        objectField.OnTextChanged += ObjectChanged;
        for(int i = 0; i < objectSets.Count; i++)
        {
            int val = i;
            objectSets[i].onValueChanged.AddListener(delegate { ObjectSetChanged(val); });
        }
        Lifetime.onEndEdit.AddListener(LifetimeChanged);
        Scale.onEndEdit.AddListener(ScaleChanged);
        Force.onEndEdit.AddListener(ForceChanged);
        Hitbox.onValueChanged.AddListener(HitboxChanged);
        modelField.extensions = new SFB.ExtensionFilter("GLTF Model", "glb", "gltf");
        avatarField.extensions = new SFB.ExtensionFilter("MP3 Audio File", "mp3");
        objectField.extensions = new SFB.ExtensionFilter("MP3 Audio File", "mp3");
    }
    
    public void NameChanged(string newString)
    {
        twitchObject.Name = newString;
        OnObjectChanged();
    }

    public void ModelChanged()
    {
        twitchObject.Model = modelField.currentString;
        OnObjectChanged();
    }

    public void AvatarChanged()
    {
        twitchObject.AvatarCollisionClip = avatarField.currentString;
        OnObjectChanged();
    }

    public void ObjectChanged()
    {
        twitchObject.ObjectCollisionClip = objectField.currentString;
        OnObjectChanged();
    }

    public void ObjectSetChanged(int i)
    {
        Debug.Log(i);
        if (objectSets[i].isOn)
        {
            twitchObject.Sets |= (1 << i);
        }
        else
        {
            twitchObject.Sets &= ~(1 << i);
        }
        OnObjectChanged();
    }

    public void LifetimeChanged(string newString)
    {
        twitchObject.Lifetime = float.Parse(newString);
        OnObjectChanged();
    }

    public void ScaleChanged(string newString)
    {
        twitchObject.Scale = float.Parse(newString);
        OnObjectChanged();
    }

    public void ForceChanged(string newString)
    {
        twitchObject.Force = float.Parse(newString);
        OnObjectChanged();
    }

    public void HitboxChanged(int option)
    {
        twitchObject.Hitbox = option;
        OnObjectChanged();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
