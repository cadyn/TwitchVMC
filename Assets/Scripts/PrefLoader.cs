using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefLoader
{
    //Gonna keep this funny relic of me trying to do this with pointers :) The code works, but I realized it was gonna be more trouble than it's worth once I got to strings. If anyone has a cool way of accomplishing this for strings, I would love you.
    /*public unsafe void LoadFloats(float*[] pointers, string[] names)
    {
        if(pointers.Length != names.Length)
        {
            throw new System.Exception("Pointers array must be of same size as names array");
        }
        for(int i = 0; i < pointers.Length; i++)
        {
            if (PlayerPrefs.HasKey(names[i]))
            {
                *pointers[i] = PlayerPrefs.GetFloat(names[i]);
            } 
            else
            {
                PlayerPrefs.SetFloat(names[i],*pointers[i]);
            }
        }
    }*/
   public static void LoadFloat(ref float reference, string name)
    {
        if (PlayerPrefs.HasKey(name))
        {
            reference = PlayerPrefs.GetFloat(name);
        }
        else
        {
            PlayerPrefs.SetFloat(name, reference);
        }
    }

    public static void LoadInt(ref int reference, string name)
    {
        if (PlayerPrefs.HasKey(name))
        {
            reference = PlayerPrefs.GetInt(name);
        }
        else
        {
            PlayerPrefs.SetInt(name, reference);
        }
    }

    public static void LoadBool(ref bool reference, string name)
    {
        if (PlayerPrefs.HasKey(name))
        {
            reference = PlayerPrefs.GetInt(name)==1;
        }
        else
        {
            PlayerPrefs.SetInt(name, reference ? 1 : 0);
        }
    }

    public static void LoadString(ref string reference, string name)
    {
        if (PlayerPrefs.HasKey(name))
        {
            reference = PlayerPrefs.GetString(name);
        }
        else
        {
            PlayerPrefs.SetString(name, reference);
        }
    }
}
