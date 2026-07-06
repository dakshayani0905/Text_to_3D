using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class BackendManager : MonoBehaviour
{
    public static BackendManager Instance;

    Process backendProcess;

    bool backendReady = false;

    public bool BackendReady => backendReady;

    string BackendFolder =>
        Path.Combine(
            Application.streamingAssetsPath,
            "UnityBackend"
        );

    string PythonPath =>
        Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            "backend",
            "venv",
            "Scripts",
            "python.exe"
        );

    string MainPy =>
        Path.Combine(
            BackendFolder,
            "main.py"
        );

    //------------------------------------------------

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //------------------------------------------------

    IEnumerator Start()
    {
        yield return StartCoroutine(StartBackend());
    }

    //------------------------------------------------

    IEnumerator StartBackend()
    {
        if (backendReady)
            yield break;

        if (!File.Exists(PythonPath))
        {
            Debug.LogError("Python not found:\n" + PythonPath);
            yield break;
        }

        if (!File.Exists(MainPy))
        {
            Debug.LogError("main.py not found:\n" + MainPy);
            yield break;
        }

        Debug.Log("Starting TRELLIS Backend...");

        backendProcess = new Process();

        backendProcess.StartInfo.FileName = PythonPath;

        backendProcess.StartInfo.Arguments = $"\"{MainPy}\"";

        backendProcess.StartInfo.WorkingDirectory =
            BackendFolder;

        backendProcess.StartInfo.UseShellExecute = false;

        backendProcess.StartInfo.CreateNoWindow = true;

        backendProcess.StartInfo.RedirectStandardOutput = true;

        backendProcess.StartInfo.RedirectStandardError = true;

        backendProcess.EnableRaisingEvents = true;

        backendProcess.OutputDataReceived +=
            (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.Log("[Backend] " + e.Data);
            };

        backendProcess.ErrorDataReceived +=
            (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.LogError("[Backend] " + e.Data);
            };

        backendProcess.Exited +=
            (sender, e) =>
            {
                backendReady = false;
                Debug.LogError("TRELLIS Backend Closed");
            };

        backendProcess.Start();
        

        backendProcess.BeginOutputReadLine();

        backendProcess.BeginErrorReadLine();

        float timeout = 180f;

        while (timeout > 0)
        {
            UnityWebRequest request =
                UnityWebRequest.Get("http://127.0.0.1:8000/");

            yield return request.SendWebRequest();

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                backendReady = true;

                Debug.Log("TRELLIS Backend Ready");

                yield break;
            }

            timeout -= 1f;

            yield return new WaitForSeconds(1f);
        }

        Debug.LogError("Backend startup timeout.");
    }

    //------------------------------------------------

    public IEnumerator WaitUntilReady()
    {
        while (!backendReady)
            yield return null;
    }

    //------------------------------------------------

    public void StopBackend()
    {
        if (backendProcess == null)
            return;

        try
        {
            if (!backendProcess.HasExited)
            {
                backendProcess.Kill();
                backendProcess.WaitForExit();
            }

            backendProcess.Dispose();
                backendProcess = null;
            
        }
        catch
        {
        }

        backendReady = false;
    }
    public void RestartBackend()
    {
        StopBackend();

        backendReady = false;

        StartCoroutine(StartBackend());
    }
    //------------------------------------------------

    void OnApplicationQuit()
    {
        StopBackend();
    }

    void OnDestroy()
    {
        StopBackend();
    }
}