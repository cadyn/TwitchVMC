using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInstance : MonoBehaviour
{
    public bool throwable = true;
    public bool droppable = false;
    public bool initializeOnStart = false;
    public float lifetime = 30;
    public AudioClip avatarCollisionClip;
    public AudioClip objectCollisionClip;

    private float lifeStarted;
    private AudioSource audioSourceAvatar;
    private AudioSource audioSourceObject;
    
    // Start is called before the first frame update
    void Start()
    {
        if (initializeOnStart)
        {
            Initialize();
        }
    }

    public void Initialize()
    {
        lifeStarted = Time.time;
        audioSourceAvatar = gameObject.AddComponent<AudioSource>();
        audioSourceAvatar.clip = avatarCollisionClip;
        audioSourceObject = gameObject.AddComponent<AudioSource>();
        audioSourceObject.clip = objectCollisionClip;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.tag.Equals("Avatar"))
        {
            audioSourceAvatar.Play();
        }
        else
        {
            audioSourceObject.Play();
        }
        
    }
    // Update is called once per frame
    void Update()
    {
        if(lifeStarted + lifetime < Time.time)
        {
            Destroy(gameObject);
        }
    }
}
