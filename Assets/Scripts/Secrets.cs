using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//For people self compiling, you will have to set this.
public static class Secrets
{
	public const string CLIENT_ID = "CLIENT_ID"; //Your application's client ID, register one at https://dev.twitch.tv/dashboard
	public const string OAUTH_TOKEN = "OAUTH_TOKEN"; //A Twitch OAuth token which can be used to connect to the chat
}
