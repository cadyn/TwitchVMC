using System.Collections;
using System.Collections.Generic;
using TwitchLib.Unity;
using TwitchLib.Api;
using TwitchLib.Api.Helix;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Common;
using TwitchLib.Api.Core.Models;
using UnityEngine;
using System.Security;
using System.Net;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;
using System.Net.Http;

public class AuthTokenResponse
{
	[JsonProperty(PropertyName = "access_token")]
	public string Token { get; protected set; }
	[JsonProperty(PropertyName = "refresh_token")]
	public string Refresh { get; protected set; }
}
public class TwitchMaster : MonoBehaviour
{
	public string authURL = "https://lunar.serealia.ca/cadyn/twitch-token";
	public string clientId = "enrl7herqhjvpoc1qm23uqqh7u3mxa";
	public TwitchUI twitchUI;

	private string username = "";
	private string channelId;
	private TwitchPubSub _pubSub;
	private TwitchAPI api;
	//No this is not secure. Our auth tokens are only capable of reading publicly accessable data. Any future features which require an oAuth token with scope to do stuff will require serverside implementations.
	//In general it's better not to use oAuth tokens from a client application, but since this token does very little, it's alright.
	private SemaphoreSlim authSignal;
	private string oAuthToken;
	private string refreshToken;
	private Thread authRunner;
	private List<TwitchUserError> authErrors;
	private List<TwitchUserError> connectionErrors;

	//Initialization
	private async void Start()
	{
		authErrors = new List<TwitchUserError>();
		connectionErrors = new List<TwitchUserError>();
		_pubSub = new TwitchPubSub();
		api = new TwitchAPI();
		api.Settings.ClientId = clientId;
		await LoadSettings();
	}

	async Task LoadSettings()
	{
		if (PlayerPrefs.HasKey("oAuthToken") && PlayerPrefs.HasKey("oAuthRefresh"))
		{
			oAuthToken = PlayerPrefs.GetString("oAuthToken");
			refreshToken = PlayerPrefs.GetString("oAuthRefresh");
			if (await canConnect() && await ValidateAuthToken())
			{
				await UpdateUsername();
			}
		}
		else
        {
			AuthErrorNew();
        }
	}

	//Connection test
	private async Task<bool> canConnect()
	{
		ConnectionErrorClear();
		using (HttpClient client = new HttpClient())
		{
			var result = await client.GetAsync("https://api.twitch.tv/helix/users");
			if (result.StatusCode == HttpStatusCode.Unauthorized)
			{
				return true;
			}
			ConnectionErrorNew();
			return false;
		}
	}

	//Basic twitch stuff
	private void TwitchLoad(string channelId)
    {
		// Connect
		UnityEngine.Debug.Log(channelId);
		_pubSub.OnPubSubServiceConnected += OnPubSubServiceConnected;
		_pubSub.OnListenResponse += onListenResponse;
		_pubSub.Connect();
	}

	private static void onListenResponse(object sender, OnListenResponseArgs e)
	{
		UnityEngine.Debug.Log(e.Response.Successful);
		UnityEngine.Debug.Log(e.Response.Error);
		if (!e.Successful)
			throw new Exception($"Failed to listen! Response: {e.Response}");
	}
	public async Task UpdateUsername()
    {
		GetUsersResponse user = await api.Helix.Users.GetUsersAsync();
		string cid = user.Users[0].Id;
		TwitchLoad(cid);
	}

	//Token creation and save function
	public async void NewToken()
    {
		if(await canConnect())
        {
			await Authenticate();
			await UpdateUsername();
		}
		return;
    }

	private async Task Authenticate()
    {
		AuthThread authThread = new AuthThread();
		authThread.ThreadDone += ObtainNewAuthToken;
		authSignal = new SemaphoreSlim(0, 1);
		Thread thrd = new Thread(authThread.Run);
		thrd.Start();
		await authSignal.WaitAsync();
		thrd.Join();
		await ValidateAuthToken();
		UpdateTokenPrefs();
	}

	private void ObtainNewAuthToken(object sender, ThreadFinishedArgs args)
    {
		AuthTokenResponse response = JsonConvert.DeserializeObject<AuthTokenResponse>(args.token);
		api.Settings.AccessToken = response.Token;
		oAuthToken = response.Token;
		refreshToken = response.Refresh;
		authSignal.Release();
	}

	private void UpdateTokenPrefs()
	{
		PlayerPrefs.SetString("oAuthToken", oAuthToken);
		PlayerPrefs.SetString("oAuthRefresh", refreshToken);
	}

	//Token validation and refreshing
	private async Task<bool> ValidateAuthToken()
    {
        if (await IsAuthValid())
        {
			UnityEngine.Debug.Log("Valid Auth");
			return true;
		}
		if(await TryRefreshToken())
        {
			UpdateTokenPrefs();
			return true;
		}
		return false;
    }

	private async Task<bool> IsAuthValid()
    {
		using (var httpClient = new HttpClient())
		{
			using (var requestMessage =
				new HttpRequestMessage(HttpMethod.Get, "https://id.twitch.tv/oauth2/validate"))
			{
				requestMessage.Headers.Add(HttpRequestHeader.Authorization.ToString(), $"Bearer {TwitchLib.Api.Core.Common.Helpers.FormatOAuth(oAuthToken)}");

				var response = await httpClient.SendAsync(requestMessage);
                if (response.IsSuccessStatusCode)
                {
					api.Settings.AccessToken = oAuthToken;
					AuthErrorClear();
					return true;
                }
				return false;
			}
		}
	}

	private async Task<bool> TryRefreshToken()
    {
		var resp = new WebClient().DownloadString(authURL + "/refresh.php?refresh_token=" + refreshToken);
		if(resp.Contains("400 Bad Request"))
        {
			AuthErrorNew();
			return false;
        }
		UnityEngine.Debug.Log(resp);
		AuthTokenResponse authResponse = JsonConvert.DeserializeObject<AuthTokenResponse>(resp);
		oAuthToken = authResponse.Token;
		refreshToken = authResponse.Refresh;
		if(await IsAuthValid())
        {
			return true;
        }
		return false;
    }

	//Pubsub stuff
	private void OnPubSubServiceConnected(object sender, System.EventArgs e)
	{
		UnityEngine.Debug.Log("PubSubServiceConnected!");
		_pubSub.ListenToFollows(channelId);
		_pubSub.OnChannelPointsRewardRedeemed += OnChannelPoints;

		_pubSub.SendTopics();
	}

	public void OnChannelPoints(object sender, OnChannelPointsRewardRedeemedArgs args)
	{
		UnityEngine.Debug.Log("Success!");
	}

	//Error handling
	private void AuthErrorNew()
	{
		TwitchUserError newErr = new TwitchUserError(TwitchUserError.DisplayPoint.MainMenu, "Unable to authenticate with Twitch");
		twitchUI.AddError(newErr);
		authErrors.Add(newErr);
	}

	private void ConnectionErrorNew()
	{
		TwitchUserError newErr = new TwitchUserError(TwitchUserError.DisplayPoint.MainMenu, "Unable to connect to Twitch");
		twitchUI.AddError(newErr);
		authErrors.Add(newErr);
	}

	private void AuthErrorClear()
	{
		if (authErrors.Count > 0)
		{
			twitchUI.ClearErrors(authErrors);
			authErrors.Clear();
		}
	}

	private void ConnectionErrorClear()
	{
		if (connectionErrors.Count > 0)
		{
			twitchUI.ClearErrors(connectionErrors);
			connectionErrors.Clear();
		}
	}
}
public class StreamString
{
	private Stream ioStream;
	private UnicodeEncoding streamEncoding;

	public StreamString(Stream ioStream)
	{
		this.ioStream = ioStream;
		streamEncoding = new UnicodeEncoding();
	}

	public string ReadString()
	{
		int len = 0;

		len = ioStream.ReadByte() * 256;
		len += ioStream.ReadByte();
		byte[] inBuffer = new byte[len];
		ioStream.Read(inBuffer, 0, len);

		return streamEncoding.GetString(inBuffer);
	}

	public int WriteString(string outString)
	{
		byte[] outBuffer = streamEncoding.GetBytes(outString);
		int len = outBuffer.Length;
		if (len > UInt16.MaxValue)
		{
			len = (int)UInt16.MaxValue;
		}
		ioStream.WriteByte((byte)(len / 256));
		ioStream.WriteByte((byte)(len & 255));
		ioStream.Write(outBuffer, 0, len);
		ioStream.Flush();

		return outBuffer.Length + 2;
	}
}

class ThreadFinishedArgs : EventArgs
{
	public string token;
	public ThreadFinishedArgs(string input)
    {
		token = input;
    }
}
class AuthThread
{
	public event EventHandler<ThreadFinishedArgs> ThreadDone;

	public void Run()
	{
		string authClientLocation = Path.Combine(UnityEngine.Application.streamingAssetsPath, "AuthClient/TwitchVMCAuthClient.exe");
		string output;
		using (NamedPipeServerStream pipeServer =
			new NamedPipeServerStream("testpipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte))
		{
			System.Diagnostics.Process.Start(authClientLocation);
			pipeServer.WaitForConnection();
			var ss = new StreamString(pipeServer);
			string s = ss.ReadString();
			if (s == "SYNC")
			{
				UnityEngine.Debug.Log("Connected and pipe functional");
			}
			output = ss.ReadString();
		}

		if (ThreadDone != null)
			ThreadDone(this, new ThreadFinishedArgs(output));
	}
}