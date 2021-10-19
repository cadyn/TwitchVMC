using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwitchObjectsUI : MonoBehaviour
{
    public List<GameObject> defaultObjects;

    [HideInInspector]
    public List<GameObject> allObjects;
    public List<GameObject> allThrowable;
    public List<GameObject> allDroppable;

    //TODO LIST:
    //1. JSON Serialize important properties from the objects
    //2. Save/load to/from playerprefs (registry)
    //3. Maybe save/load from a file, or a resource pack? Could save this for later.
    void Start()
    {
        
        foreach(GameObject obj in defaultObjects)
        {
            ObjectInstance objInst = obj.GetComponent<ObjectInstance>();
            allObjects.Add(obj);
            if (objInst.throwable)
            {
                allThrowable.Add(obj);
            }
            if (objInst.droppable)
            {
                allDroppable.Add(obj);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
