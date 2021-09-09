using System.Collections;
using System.Collections.Generic;
using TwitchLib.Unity;
using UnityEngine;

public class TwitchMaster : MonoBehaviour
{
	private PubSub _pubSub;

	private void Start()
	{
		_pubSub = new PubSub();

		// Subscribe to Events
		_pubSub.OnWhisper += OnWhisper;
		_pubSub.OnPubSubServiceConnected += OnPubSubServiceConnected;

		// Connect
		_pubSub.Connect();
	}

	private void OnPubSubServiceConnected(object sender, System.EventArgs e)
	{
		Debug.Log("PubSubServiceConnected!");

		// On connect listen to Bits evadsent
		// Please note that listening to the whisper events requires the chat_login scope in the OAuth token.
		_pubSub.ListenToWhispers(Secrets.CHANNEL_ID_FROM_OAUTH_TOKEN);

		// SendTopics accepts an oauth optionally, which is necessary for some topics, such as bit events.
		_pubSub.SendTopics(Secrets.OAUTH_TOKEN);
	}

	private void OnWhisper(object sender, TwitchLib.PubSub.Events.OnWhisperArgs e)
	{
		Debug.Log($"{e.Whisper.Data}");
		// Do your bits logic here.
	}
}