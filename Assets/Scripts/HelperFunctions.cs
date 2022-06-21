using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

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

    //Generate random point on a unit sphere.
    //Adapted from Brian M. Scott (https://math.stackexchange.com/users/12042/brian-m-scott), How to generate random points on a sphere?, URL (version: 2015-12-22): https://math.stackexchange.com/q/1586015
    public static Vector3 RandomDir()
    {
        System.Random rand = new System.Random();
        double a = rand.NextDouble();
        double c = rand.NextDouble();
        double b = a * 2 - 1;
        double d = c * 2 * Math.PI;
        double r = Math.Sqrt(1 - b*b);
        double x = r * Math.Cos(d);
        double y = r * Math.Sin(d);
        return new UnityEngine.Vector3((float)x, (float)y, (float)b);
    }
    //Just outputs above as a Vector4, particularly for use in a matrix.
    public static Vector4 RandomDirV4()
    {
        Vector3 randDir = RandomDir();
        return new UnityEngine.Vector4(randDir.x, randDir.y, randDir.z, 0);
    }
    //Generates a random point on a unit sphere in the hemisphere about camDir
    //Adapted from: Harald Hanche-Olsen (https://math.stackexchange.com/users/23290/harald-hanche-olsen), Point on the left or right side of a plane in 3D space, URL (version: 2012-10-15): https://math.stackexchange.com/q/214194
    public static Vector3 RandomDirFromCamera(Vector3 camDir)
    {
        //Our plane passes through 0,0,0, and has a normal of camDir.
        //Generate B,C as points on this plane. This isn't particularly readable since it's all precalculated and simplified.
        //Point B is acquired by setting x and y to 1, then solving for the z coordinate given that it's on the plane.
        //Point C is similarly acquired, but instead, x is set to -1.
        Vector4 B = new UnityEngine.Vector4(1, 1, -(camDir.x + camDir.y) / camDir.z,0);
        Vector4 C = new UnityEngine.Vector4(-1, 1, (camDir.x - camDir.y) / camDir.z,0);
        Vector4 camDirV4 = new UnityEngine.Vector4(camDir.x, camDir.y, camDir.z, 0);
        Vector4 D = new UnityEngine.Vector4(0, 0, 0, 1);
        //We really care about the 3x3 matrix, but putting the 3x3 inside of a 4x4 matrix as we do here makes it so the determinant is unchanged.
        Matrix4x4 camMat = new Matrix4x4(B, C, camDirV4, D);
        int camSgn = Math.Sign(camMat.determinant);
        //In this case, the sign shows us what side of the plane the point is on.
        Vector4 dir = RandomDirV4();
        Matrix4x4 dirMat = new Matrix4x4(B, C, dir, D);
        int dirSgn = Math.Sign(dirMat.determinant);
        //Keep generating random points on the unit sphere until we get one on the same side as camDir as indicated by the determinant of the matrix.
        while(dirSgn != camSgn)
        {
            dir = RandomDirV4();
            dirMat = new Matrix4x4(B, C, dir, D);
            dirSgn = Math.Sign(dirMat.determinant);
        }
        return new UnityEngine.Vector3(dir.x, dir.y, dir.z);
    }
    public static Vector3 GetBodyPartPosition(string bodyPart)
    {
        return new UnityEngine.Vector3(0, 0, 0); //To be implemented
    }

    public static string GetPropertyString(OnChannelPointsRewardRedeemedArgs args, string property, TwitchMaster twitchMaster)
    {
        Dictionary<string, string> properties = new Dictionary<string, string>
        {
            {"USERNAME", args.RewardRedeemed.Redemption.User.DisplayName },
            {"REWARDNAME", args.RewardRedeemed.Redemption.Reward.Title },
        };
        if (!properties.ContainsKey(property))
        {
            throw new NotImplementedException();
        }
        return properties[property];
    }
    public static int GetPropertyInteger(OnChannelPointsRewardRedeemedArgs args, string property, TwitchMaster twitchMaster)
    {
        Dictionary<string, int> properties = new Dictionary<string, int>
        {
            {"USERSUBTIER", twitchMaster.GetSubTier(args.RewardRedeemed.Redemption.User.Id)},
            {"REWARDCOST", args.RewardRedeemed.Redemption.Reward.Cost}
        };
        if (!properties.ContainsKey(property))
        {
            throw new NotImplementedException();
        }
        return properties[property];
    }
    public static bool GetPropertyBool(OnChannelPointsRewardRedeemedArgs args, string property, TwitchMaster twitchMaster)
    {
        Dictionary<string, bool> properties = new Dictionary<string, bool>
        {
            {"USERSUBBED", GetPropertyInteger(args, "USERSUBTIER", twitchMaster) > 0},
            {"USERFOLLOWS", twitchMaster.IsUserFollowing(args.RewardRedeemed.Redemption.User.Id)},
        };
        if (!properties.ContainsKey(property))
        {
            throw new NotImplementedException();
        }
        return properties[property];
    }

    internal static string GetPropertyString<T>(T args, string value, TwitchMaster twitchMaster)
    {
        throw new NotImplementedException();
    }

    internal static int GetPropertyInteger<T>(T args, string value, TwitchMaster twitchMaster)
    {
        throw new NotImplementedException();
    }

    internal static float GetPropertyFloat<T>(T args, string value, TwitchMaster twitchMaster)
    {
        throw new NotImplementedException();
    }
    internal static bool GetPropertyBool<T>(T args, string value, TwitchMaster twitchMaster)
    {
        throw new NotImplementedException();
    }
}