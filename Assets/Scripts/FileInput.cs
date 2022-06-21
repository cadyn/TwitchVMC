using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SFB;

public class FileInput : MonoBehaviour
{
    public event Action OnTextChanged= delegate { };
    public InputField textInput;
    public Button fileSelectButton;
    public ExtensionFilter extensions = new ExtensionFilter("All files", "*");
    [HideInInspector]
    public string currentString = "";

    private SemaphoreSlim finishedSignal;
    // Start is called before the first frame update
    void Start()
    {
        fileSelectButton.onClick.AddListener(OnSelectButton);
        textInput.onEndEdit.AddListener(InputChanged);
    }

    public async void OnSelectButton()
    {
        FileSelectThread thread = new FileSelectThread();
        thread.ThreadDone += SelectFinished;
        Thread thrd = new Thread(() => thread.Run(new ExtensionFilter[] { extensions }, currentString));
        finishedSignal = new SemaphoreSlim(0, 1);
        thrd.Start();
        await finishedSignal.WaitAsync();
        thrd.Join();
        UpdateStringInternal(currentString);
    }

    private void SelectFinished(object sender, ThreadFinishedArgs args)
    {
        finishedSignal.Release();
        if (!args.output.Equals("ERROR"))
        {
            currentString = args.output;
        }
    }

    public void UpdateString(string newString)
    {
        currentString = newString;
        textInput.text = newString;
    }

    private void UpdateStringInternal(string newString)
    {
        UpdateString(newString);
        OnTextChanged();
    }

    private void InputChanged(string newString)
    {
        currentString = newString;
        OnTextChanged();
    }
}

class FileSelectThread
{
    public event EventHandler<ThreadFinishedArgs> ThreadDone;
    public void Run(ExtensionFilter[] extensions, string currentString)
    {
        string path = "";
        if(currentString.Length > 0)
        {
            path = Path.GetFullPath(Path.Combine(currentString, @"..\"));
        }
        string output = "";
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", path,extensions,false);

        if(paths.Length == 0)
        {
            output = "ERROR";
        }
        else
        {
            output = paths[0];
        }

        if (ThreadDone != null)
        {
            ThreadDone(this, new ThreadFinishedArgs(output));
        }
    }
}