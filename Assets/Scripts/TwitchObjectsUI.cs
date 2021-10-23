using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UniGLTF;

//Maybe we should break this up into multiple files

//Instead of storing a bool for each flag, we use an int and bit flags, because it's what feels more natural to me.
//I'm not gonna argue this is better, but it's certainly not much worse if at all, and it's what's familiar to me which is what matters more.

//Version control plan: Every future update to the properties will get +1 to the version int, and it will be stored in a class that extends the previous version.
public class TwitchObject
{
    public int Version { get; set; }
    public string Name { get; set; }
    public int Sets { get; set; }
    public string Model { get; set; }
    public string AvatarCollisionClip { get; set; }
    public string ObjectCollisionClip { get; set; }
    public float Lifetime { get; set; }
    public float Scale { get; set; }
    public float Force { get; set; }
    public int Hitbox { get; set; }
    
}

public static class ObjectConsts
{
    public const int SET_1 = 0x1;
    public const int SET_2 = 0x2;
    public const int SET_3 = 0x4;
    public const int SET_4 = 0x8;
    public const int SET_5 = 0x10;
    public const int HITBOX_BOX = 0;
    public const int HITBOX_CAPSULE = 1;
    public const int HITBOX_MESH = 2;
}

public class CustomObject
{
    public TwitchObject objProperties;
    public GameObject obj;
    public ObjectInstance objInst;
    public TwitchObjectsUI creator;
    public ObjectInstanceUI objUI;
    public CustomObject(TwitchObject tObject, GameObject defaultObject, TwitchObjectsUI tCreator, ObjectInstanceUI objectInstanceUI)
    {
        objUI = objectInstanceUI;
        creator = tCreator;
        objProperties = tObject;
        obj = creator.newObjFromPrefab(defaultObject); //Can't use instantiate outside of a monobehaviour unfortionately.
        objInst = obj.AddComponent<ObjectInstance>();
        Rigidbody rb = obj.AddComponent<Rigidbody>();
        //rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        UpdateGameObject(defaultObject, true);
    }

    public bool TryUpdateModel(string path, TwitchObjectsUI creator, GameObject defaultObject)
    {
        GameObject newObj;
        if (path.Equals("DEFAULT"))
        {
            newObj = creator.newObjFromPrefab(defaultObject);
        }
        else
        {
            newObj = LoadModel(path);
        }
        
        if(newObj == null)
        {
            return false;
        }
        newObj.SetActive(false);
        ObjectInstance newObjInst = HelperFunctions.CopyComponent(objInst, newObj);
        newObjInst.hitboxType = -1; //Force hitbox regeneration
        HelperFunctions.CopyComponent(obj.GetComponent<Rigidbody>(), newObj);
        for (int i = 0; i < 5; i++) //Replace all objects in sets with new one.
        {
            if ((objInst.sets & (1 << i)) > 0)
            {
                creator.objectSets[i].Remove(obj);
                creator.objectSets[i].Add(newObj);
            }
        }
        creator.DeleteOldObject(obj);
        obj = newObj;
        objInst = newObjInst;
        return true;
    }

    public void NameChange(string newName, TwitchObjectsUI creator)
    {
        creator.allObjects.Remove(objInst.objectName);
        creator.allObjects[objProperties.Name] = obj;
        objInst.objectName = objProperties.Name;
    }

    public void UpdateSets(TwitchObjectsUI creator)
    {
        int diffs = (objProperties.Sets ^ objInst.sets); //Where the bits are not the same
        int removals = diffs & objInst.sets; //Where we need to remove ourselves from
        int additions = diffs & objProperties.Sets; //Where we need to add ourselves to.
        if (removals > 0)
        {
            for (int i = 0; i < 5; i++)
            {
                if ((removals & (1 << i)) > 0)
                {
                    creator.objectSets[i].Remove(obj);
                }
            }
        }
        if (additions > 0)
        {
            for (int i = 0; i < 5; i++)
            {
                if ((removals & (1 << i)) > 0)
                {
                    creator.objectSets[i].Add(obj);
                }
            }
        }
    }

    public void UpdateAudioClip(int avatarOrObject, bool returnedError, AudioClip ac)
    {
        if (avatarOrObject == 1)
        {
            if (!returnedError)
            {
                objInst.avatarCollisionClip = ac;
                objInst.avatarCollisionClipPath = objProperties.AvatarCollisionClip;
            }
            else
            {
                objProperties.AvatarCollisionClip = objInst.avatarCollisionClipPath;
                objUI.HandleError("AVATARCLIP");
            }
        }
        else
        {
            if (!returnedError)
            {
                objInst.objectCollisionClip = ac;
                objInst.objectCollisionClipPath = objProperties.ObjectCollisionClip;
            }
            else
            {
                objProperties.ObjectCollisionClip = objInst.objectCollisionClipPath;
                objUI.HandleError("OBJECTCLIP");
            }
        }
    }

    public string UpdateGameObject(GameObject defaultObject, bool justCreated = false) //Returns what caused any error when trying to update properties, or success if none.
    {
        if (justCreated) //Add to sets if we were just created.
        {
            objInst.objectName = objProperties.Name;
            creator.allObjects.Add(objProperties.Name, obj);
            for (int i = 0; i < 5; i++)
            {
                if ((objInst.sets & (1 << i)) > 0)
                {
                    creator.objectSets[i].Add(obj);
                }
            }
        }

        if (objProperties.Model.Equals("DEFAULT") && justCreated) //Just set the property if we're default anyways and were just created
        {
            objInst.modelPath = objProperties.Model;
        }
        else if (!objProperties.Model.Equals(objInst.modelPath)) //Otherwise get us a new model if our model is changed.
        {
            if (!TryUpdateModel(objProperties.Model, creator, defaultObject))
            {
                objProperties.Model = objInst.modelPath;
                return "MODEL";
            }
            objInst.modelPath = objProperties.Model;
            creator.allObjects[objInst.objectName] = obj;
        }

        if(!objProperties.Name.Equals(objInst.objectName)) //Update name if necessary.
        {
            if (creator.allObjects.ContainsKey(objProperties.Name) || objProperties.Name.Length == 0)
            {
                objProperties.Name = objInst.objectName;
                return "NAME";
            }
            NameChange(objProperties.Name, creator);
        }

        if (objProperties.Sets != objInst.sets) //Update sets if necessary.
        {
            UpdateSets(creator);
        }

        if (!objProperties.AvatarCollisionClip.Equals(objInst.avatarCollisionClipPath)) //Update Avatar Collision Clip
        {
            creator.LoadAudioClip(objProperties.AvatarCollisionClip, this, 1);
        }

        if (!objProperties.ObjectCollisionClip.Equals(objInst.objectCollisionClipPath)) //Update Object Collision Clip
        {
            creator.LoadAudioClip(objProperties.ObjectCollisionClip, this, 2);
        }

        if (objProperties.Hitbox != objInst.hitboxType) //Update hitbox
        {
            RuntimeGltfInstance gltfInstance = obj.GetComponent<RuntimeGltfInstance>();
            List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
            if (gltfInstance != null)
            {
                foreach (MeshRenderer meshRenderer in gltfInstance.MeshRenderers)
                {
                    meshRenderers.Add(meshRenderer);
                }
            }
            else
            {
                meshRenderers.Add(obj.GetComponent<MeshRenderer>());
            }
            foreach (MeshRenderer meshRenderer in meshRenderers) {
                Bounds meshBounds = meshRenderer.bounds;
                GameObject tempObj = meshRenderer.gameObject;
                Collider col = tempObj.GetComponent<Collider>();
                if (col != null)
                {
                    creator.DeleteOldObject(col);
                }
                switch (objProperties.Hitbox)
                {
                    case ObjectConsts.HITBOX_BOX: //For box, we just use the size of the bounds
                        BoxCollider boxCollider = tempObj.AddComponent<BoxCollider>();
                        //boxCollider.center = meshBounds.center;
                        boxCollider.size = meshBounds.size;
                        break;
                    case ObjectConsts.HITBOX_CAPSULE: //For capsule we need to do some calculation using the bounds to find the optimal size
                        float max = -1f, total = 0f;
                        int axis = 1;
                        for (int i = 0; i < 3; i++)
                        {
                            if (meshBounds.size[i] > max)
                            {
                                axis = i;
                                max = meshBounds.size[i];
                            }
                            total += meshBounds.extents[i];
                        }
                        CapsuleCollider capsuleCollider = tempObj.AddComponent<CapsuleCollider>();
                        capsuleCollider.center = meshBounds.center;
                        capsuleCollider.radius = (total - meshBounds.extents[axis]) * .5F;
                        capsuleCollider.height = meshBounds.size[axis];
                        capsuleCollider.direction = axis;
                        break;
                    case ObjectConsts.HITBOX_MESH:
                        MeshCollider meshCollider = tempObj.AddComponent<MeshCollider>();
                        meshCollider.convex = true;
                        break;
                }
            }
            objInst.hitboxType = objProperties.Hitbox;
        }

        if(objProperties.Force != objInst.force) //Update force
        {
            if(objProperties.Force <= 0)
            {
                objProperties.Force = objInst.force;
                return "FORCE";
            }
            objInst.force = objProperties.Force;
        }

        if (objProperties.Scale != objInst.objectScale) //Update Scale
        {
            if (objProperties.Scale <= 0)
            {
                objProperties.Scale = objInst.objectScale;
                return "SCALE";
            }
            objInst.objectScale = objProperties.Scale;
            obj.transform.localScale = new Vector3(objProperties.Scale, objProperties.Scale, objProperties.Scale);
        }

        if(objProperties.Lifetime != objInst.lifetime) //Update Lifetime
        {
            if(objProperties.Lifetime <= 0)
            {
                objProperties.Lifetime = objInst.lifetime;
                return "LIFETIME";
            }
            objInst.lifetime = objProperties.Lifetime;
        }

        return "SUCCESS";
    }

    public GameObject LoadModel(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        Debug.LogFormat("{0}", path);
        var ext = Path.GetExtension(path).ToLower();
        switch (ext)
        {
            case ".glb":
                {
                    var data = new GlbFileParser(path).Parse();

                    using (var loader = new UniGLTF.ImporterContext(data, materialGenerator: new GltfMaterialDescriptorGenerator()))
                    {
                        var loaded = loader.Load();
                        loaded.ShowMeshes();
                        loaded.EnableUpdateWhenOffscreen();
                        
                        return loaded.gameObject;
                    }
                }

            case ".gltf":
                {
                    var data = new GltfFileWithResourceFilesParser(path).Parse();

                    using (var loader = new UniGLTF.ImporterContext(data, materialGenerator: new GltfMaterialDescriptorGenerator()))
                    {
                        var loaded = loader.Load();
                        loaded.ShowMeshes();
                        loaded.EnableUpdateWhenOffscreen();
                        return loaded.gameObject;
                    }
                }

            case ".zip":
                {
                    var data = new ZipArchivedGltfFileParser(path).Parse();

                    using (var loader = new UniGLTF.ImporterContext(data, materialGenerator: new GltfMaterialDescriptorGenerator()))
                    {
                        var loaded = loader.Load();
                        loaded.ShowMeshes();
                        loaded.EnableUpdateWhenOffscreen();
                        return loaded.gameObject;
                    }
                }

            default:
                Debug.LogWarningFormat("unknown file type: {0}", path);
                return null;
        }
    }
}

public class TwitchObjectsUI : MonoBehaviour
{
    public GameObject interfacePrefab;
    public GameObject defaultObjectPrefab;
    public AudioClip defaultAvatarClip;
    public AudioClip defaultObjectClip;
    public List<GameObject> defaultObjects;
    public Transform scrollContent;
    public Button newObjButton;
    public RectTransform newObjButtonTransform;
    public Button testButton;
    public Button saveButton;
    public Button loadButton;

    [HideInInspector]
    public Dictionary<string,GameObject> allObjects;
    [HideInInspector]
    public List<List<GameObject>> objectSets;
    [HideInInspector]
    public List<CustomObject> customObjects;

    //TODO LIST:
    //1. JSON Serialize important properties from the objects
    //2. Save/load to/from playerprefs (registry)
    //3. Maybe save/load from a file, or a resource pack? Could save this for later.
    public void LoadAudioClip(string path, CustomObject co, int avatarOrObject = 0)
    {
        if(avatarOrObject > 0 && path=="DEFAULT")
        {
            if(avatarOrObject == 1)
            {
                co.UpdateAudioClip(avatarOrObject, false, defaultAvatarClip);
                return;
            }
            co.UpdateAudioClip(avatarOrObject, false, defaultObjectClip);
            return;
        }
        StartCoroutine(HelperFunctions.LoadAudioFile(path, (AudioClip ac, bool err) =>
        {
            co.UpdateAudioClip(avatarOrObject,err,ac);
        }));
    }
    public void DeleteOldObject(Object obj)
    {
        Destroy(obj);
    }

    public GameObject newObjFromPrefab(GameObject obj)
    {
        GameObject newObj = Instantiate(obj);
        newObj.SetActive(false);
        return newObj;
    }

    public void SaveObjects()
    {
        List<TwitchObject> tObjs = new List<TwitchObject>();
        foreach(CustomObject co in customObjects)
        {
            tObjs.Add(co.objProperties);
        }
        string output = JsonConvert.SerializeObject(tObjs);
        PlayerPrefs.SetString("objectSettings", output);
    }

    public void TryLoadObjects()
    {
        if (!PlayerPrefs.HasKey("objectSettings"))
        {
            return;
        }
        string input = PlayerPrefs.GetString("objectSettings");
        List<TwitchObject> tObjs = JsonConvert.DeserializeObject<List<TwitchObject>>(input);
        //First get rid of all the current entries.
        while (customObjects.Count > 0)
        {
            CustomObject co = customObjects[0];
            DeleteObject(co, co.obj);
        }
        //Then we populate it with the entries from the save
        foreach(TwitchObject to in tObjs)
        {
            NewObject(to);
        }
    }

    void Start()
    {
        allObjects = new Dictionary<string, GameObject>();
        customObjects = new List<CustomObject>();
        objectSets = new List<List<GameObject>>();
        for(int i = 0; i < 5; i++)
        {
            objectSets.Add(new List<GameObject>());
        }
        foreach(GameObject obj in defaultObjects)
        {
            ObjectInstance objInst = obj.GetComponent<ObjectInstance>();
            if (!VerifyObj(obj, objInst)) { continue; }

            allObjects.Add(objInst.name,obj);

            for(int i = 0; i < 5; i++)
            {
                if((objInst.sets & (1 << i)) > 0)
                {
                    objectSets[i].Add(obj);
                }
            }
        }
        TryLoadObjects();
        if (testButton)
        {
            testButton.onClick.AddListener(Test);
        }
        newObjButton.onClick.AddListener(delegate { NewObject(); });
        saveButton.onClick.AddListener(SaveObjects);
        loadButton.onClick.AddListener(TryLoadObjects);
    }

    public void Test()
    {
        int i = 10;
        foreach (KeyValuePair<string, GameObject> entry in allObjects)
        {
            GameObject go = Instantiate(entry.Value, new Vector3(0, i, 0), Quaternion.identity);
            go.SetActive(true);
            go.GetComponent<ObjectInstance>().Initialize();
            i += 10;
        }
    }

    public void NewObject(TwitchObject tObj = null)
    {
        TwitchObject twitchObject = tObj;
        if (twitchObject == null) {
            twitchObject = new TwitchObject();
            string name = "Object";
            int i = 1;
            while (allObjects.ContainsKey(name = $"Object{i}"))
            {
                i++;
            }
            twitchObject.Name = name;
            twitchObject.Model = "DEFAULT";
            twitchObject.AvatarCollisionClip = "DEFAULT";
            twitchObject.ObjectCollisionClip = "DEFAULT";
            twitchObject.Sets = ObjectConsts.SET_1;
            twitchObject.Force = 1;
            twitchObject.Lifetime = 30;
            twitchObject.Scale = 1;
            twitchObject.Hitbox = ObjectConsts.HITBOX_BOX;
            twitchObject.Version = 1;
        }
        GameObject interfaceObj = Instantiate(interfacePrefab,scrollContent);
        RectTransform rt = interfaceObj.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(-700, 275 - (75 * customObjects.Count));
        
        ObjectInstanceUI interfaceUI = interfaceObj.GetComponent<ObjectInstanceUI>();
        interfaceUI.twitchObject = twitchObject;
        interfaceUI.Initialize();
        
        CustomObject co = new CustomObject(twitchObject, defaultObjectPrefab, this, interfaceUI);
        customObjects.Add(co);
        UpdateButtonPos();
        interfaceUI.OnObjectChanged += delegate { ObjectChanged(co, interfaceUI); };
        interfaceUI.deleteButton.onClick.AddListener(delegate { DeleteObject(co, co.obj); });
    }

    public void UpdateButtonPos()
    {
        newObjButtonTransform.anchoredPosition = new Vector2(newObjButtonTransform.anchoredPosition.x, 275 - (75 * customObjects.Count));
    }

    public void DeleteObject(CustomObject co, GameObject go)
    {
        customObjects.Remove(co);
        for(int i = 0; i < 5; i++)
        {
            if((co.objInst.sets & (1 << i)) > 0)
            {
                objectSets[i].Remove(go);
            }
        }
        allObjects.Remove(co.objInst.objectName);
        Destroy(go);
        Destroy(co.objUI.gameObject);
        UpdateButtonPos();
        co = null;
    }

    public void ObjectChanged(CustomObject co, ObjectInstanceUI interfaceUI)
    {
        string output = co.UpdateGameObject(defaultObjectPrefab);
        interfaceUI.HandleError(output);
        
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
