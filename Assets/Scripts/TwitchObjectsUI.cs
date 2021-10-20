#define SET_1 1


using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

//Instead of storing a bool for each flag, we use an int and bit flags, because it's what feels more natural to me.
//I'm not gonna argue this is better, but it's certainly not much worse if at all, and it's what's familiar to me which is what matters more.
public static class ObjectConsts
{
    public const int SET_1 = 0x1;
    public const int SET_2 = 0x2;
    public const int SET_3 = 0x4;
    public const int SET_4 = 0x8;
    public const int SET_5 = 0x10;
}

public class TwitchObject
{
    public string Name { get; set; }
    public int Sets { get; set; }
    public string Model { get; set; }
    public string AvatarCollisionClip { get; set; }
    public string ObjectCollisionClip { get; set; }
    public float lifetime { get; set; }
    
}

public class TwitchObjectsUI : MonoBehaviour
{
    public List<GameObject> defaultObjects;

    [HideInInspector]
    public List<GameObject> allObjects;
    public List<List<GameObject>> objectSets;

    //TODO LIST:
    //1. JSON Serialize important properties from the objects
    //2. Save/load to/from playerprefs (registry)
    //3. Maybe save/load from a file, or a resource pack? Could save this for later.
    void Start()
    {
        objectSets = new();
        for(int i = 0; i < 5; i++)
        {
            objectSets.Add(new List<GameObject>());
        }
        foreach(GameObject obj in defaultObjects)
        {
            ObjectInstance objInst = obj.GetComponent<ObjectInstance>();
            if (!VerifyObj(obj, objInst)) { continue; }

            allObjects.Add(obj);

            for(int i = 0; i < 5; i++)
            {
                if((objInst.sets & (1 << i)) > 0)
                {
                    objectSets[i].Add(obj);
                }
            }
        }
    }

    private bool VerifyObj(GameObject obj, ObjectInstance objInst)
    {
        return true; //To replace with actual verification that the object is valid.
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
