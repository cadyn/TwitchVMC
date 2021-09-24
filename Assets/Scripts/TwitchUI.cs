using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TwitchUI : MonoBehaviour
{
    public TwitchMaster twitchMaster;
    public InputField authURL;
    public Text mainMenuErrors;
    public Text twitchMenuErrors;
    public Button authenticateButton;

    private string currentAuthURL;
    private List<TwitchUserError> errs = new List<TwitchUserError>();

    void Start()
    {
        currentAuthURL = twitchMaster.authURL;
        authURL.text = currentAuthURL;
        authenticateButton.onClick.AddListener(twitchMaster.NewToken);
        LoadSettings();
        
        authURL.onEndEdit.AddListener(UpdateAuthURL);
    }

    public void LoadSettings()
    {
        //twitchMaster.Init();
        PrefLoader.LoadString(ref currentAuthURL, "authURL");
        UpdateAuthURL(currentAuthURL);
    }

    public void UpdateAuthURL(string value)
    {
        if(value.Equals(""))
        {
            authURL.text = currentAuthURL;
        } 
        else
        {
            currentAuthURL = value;
            twitchMaster.authURL = currentAuthURL;
        }
    }

    public void ClearErrorsText()
    {
        mainMenuErrors.text = "";
        twitchMenuErrors.text = "";
    }

    public void UpdateErrors()
    {
        ClearErrorsText();
        foreach(TwitchUserError err in errs)
        {
            switch (err.displayPoint)
            {
                case TwitchUserError.DisplayPoint.MainMenu:
                    mainMenuErrors.text += $"{err.errorText}\n";
                    twitchMenuErrors.text += $"{err.errorText}\n";
                    break;
                case TwitchUserError.DisplayPoint.TwitchMenu:
                    twitchMenuErrors.text += $"{err.errorText}\n";
                    break;
            }
        }
    }
    public void AddError(TwitchUserError err)
    {
        errs.Add(err);
        UpdateErrors();
    }
    public void ClearError(TwitchUserError err)
    {
        errs.Remove(err);
        UpdateErrors();
    }

    public void ClearErrors(List<TwitchUserError> errs) //Lists should be handled seperately, so we only update the text once we're done making changes.
    {
        foreach(TwitchUserError err in errs)
        {
            errs.Remove(err);
        }
        UpdateErrors();
    }
}

public class TwitchUserError
{
    public DisplayPoint displayPoint;
    public string errorText;
    public enum DisplayPoint
    {
        MainMenu = 0,
        TwitchMenu = 1
    }
    public TwitchUserError(DisplayPoint point, string text)
    {
        displayPoint = point;
        errorText = text;
    }
}