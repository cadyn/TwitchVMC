using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public static class HelperFunctions
{
    public static T CopyComponent<T>(T original, GameObject destination) where T : Component
    {
        System.Type type = original.GetType();
        Component copy = destination.AddComponent(type);
        System.Reflection.FieldInfo[] fields = type.GetFields();
        foreach (System.Reflection.FieldInfo field in fields)
        {
            field.SetValue(copy, field.GetValue(original));
        }
        return copy as T;
    }

    public static IEnumerator LoadAudioFile(string path, Action<AudioClip,bool> callback)
    {
        using (var www = UnityWebRequestMultimedia.GetAudioClip($"file://{path}", AudioType.MPEG))
        {
            AudioClip ac;
            ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = true;

            yield return www.SendWebRequest();

            if ((www.result == UnityWebRequest.Result.ConnectionError) || (www.result == UnityWebRequest.Result.ProtocolError))
            {
                callback(null, true);
            }

            DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)www.downloadHandler;

            if (dlHandler.isDone)
            {
                AudioClip audioClip = dlHandler.audioClip;

                if (audioClip != null)
                {
                    ac = DownloadHandlerAudioClip.GetContent(www);
                    callback(ac,false);
                }
                else
                {
                    callback(null, true);
                }
            }
        }
    }
}