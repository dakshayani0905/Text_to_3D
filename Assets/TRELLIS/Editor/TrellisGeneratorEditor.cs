using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.IO;
using Process = System.Diagnostics.Process;
using UnityEditor.SceneManagement;
using System.Threading.Tasks;
public class TrellisGeneratorEditor : EditorWindow
{
    private string prompt = "";
    private int qualityIndex = 0;

    private string[] qualities =
    {
        "Auto",
        "Fast",
        "Balanced",
        "Ultra"
    };

    private string status = "Ready";
    private float progress = 0f;
    private string progressMessage = "Ready";
    private Process backendProcess;
    private bool backendStarted = false;
    private bool generationRunning = false;
    private bool startingBackend = false;
    [System.Serializable]
    public class PromptData
    {
        public string prompt;
        public string quality;
    }
    [System.Serializable]
    public class ResultData
    {
        public string status;
        public string model_url;
        public string error;
    }

    [System.Serializable]
    public class ProgressData
    {
        public string message;
        public int percent;
    }
    [MenuItem("Tools/TRELLIS Generator")]
    public static void ShowWindow()
    {
        GetWindow<TrellisGeneratorEditor>(
            "TRELLIS Generator"
        );
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        GUILayout.Label(
            "TRELLIS Text To 3D",
            EditorStyles.boldLabel
        );

        GUILayout.Space(10);

        prompt =
            EditorGUILayout.TextField(
                "Prompt",
                prompt
            );

        qualityIndex =
            EditorGUILayout.Popup(
                "Quality",
                qualityIndex,
                qualities
            );

        GUILayout.Space(10);

        EditorGUILayout.LabelField(
    progressMessage
);

        EditorGUI.ProgressBar(
            GUILayoutUtility.GetRect(
                300,
                20
            ),
            progress / 100f,
            progress + "%"
        );

        EditorGUILayout.HelpBox(
            status,
            MessageType.Info
        );

        GUILayout.Space(10);

        if (startingBackend)
        {
            GUI.enabled = false;

            GUILayout.Button(
                "Starting Backend...",
                GUILayout.Height(40)
            );

            GUI.enabled = true;
        }
        else if (!generationRunning)
        {
            if (GUILayout.Button("Generate", GUILayout.Height(40)))
            {
                Generate();
            }
        }
        else
        {
            if (GUILayout.Button("Cancel Generation", GUILayout.Height(40)))
            {
                CancelGeneration();
            }
        }

    }   // <-- THIS closes OnGUI()


    private async Task<bool> StartBackend()
{
    //--------------------------------------------------
    // Check if backend is already running
   //--------------------------------------------------

   try
   {
       using (UnityWebRequest request =
              UnityWebRequest.Get("http://127.0.0.1:8000/"))
       {
           var operation = request.SendWebRequest();

           while (!operation.isDone)
               await Task.Yield();

           if (request.result == UnityWebRequest.Result.Success)
           {
               backendStarted = true;

               Debug.Log("TRELLIS Backend Already Running");

               return true;
           }
       }
   }
   catch
   {
   }
    //--------------------------------------------------
    // Backend already running?
    //--------------------------------------------------
    if (backendProcess != null)
    {
        try
        {
            if (!backendProcess.HasExited)
            {
                backendStarted = true;
                return true;
            }

            backendProcess.Dispose();
            backendProcess = null;
            backendStarted = false;
        }
        catch
        {
            backendProcess = null;
            backendStarted = false;
        }
    }

    //--------------------------------------------------
    // Paths
    //--------------------------------------------------

    string pythonPath =
        Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            "backend",
            "venv",
            "Scripts",
            "python.exe"
        );

    string mainPy =
        Path.Combine(
            Application.streamingAssetsPath,
            "UnityBackend",
            "main.py"
        );

    if (!File.Exists(pythonPath))
    {
        Debug.LogError("Python not found:\n" + pythonPath);
        return false;
    }

    if (!File.Exists(mainPy))
    {
        Debug.LogError("main.py not found:\n" + mainPy);
        return false;
    }

    //--------------------------------------------------
    // Start Python Backend
    //--------------------------------------------------

    backendProcess = new Process();

    backendProcess.StartInfo.FileName = pythonPath;

    backendProcess.StartInfo.Arguments = $"\"{mainPy}\"";

    backendProcess.StartInfo.WorkingDirectory =
        Path.Combine(
            Application.streamingAssetsPath,
            "UnityBackend"
        );

    backendProcess.StartInfo.UseShellExecute = false;
    backendProcess.StartInfo.CreateNoWindow = true;
    backendProcess.StartInfo.RedirectStandardOutput = true;
    backendProcess.StartInfo.RedirectStandardError = true;

    backendProcess.EnableRaisingEvents = true;

    backendProcess.OutputDataReceived += (sender, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
            Debug.Log("[Backend] " + e.Data);
    };

    backendProcess.ErrorDataReceived += (sender, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
            Debug.LogError("[Backend] " + e.Data);
    };

    backendProcess.Exited += (sender, e) =>
    {
        backendStarted = false;

        backendProcess?.Dispose();
        backendProcess = null;

        Debug.LogWarning("Backend Process Exited");
    };

    backendProcess.Start();

    backendProcess.BeginOutputReadLine();
    backendProcess.BeginErrorReadLine();

    //--------------------------------------------------
    // Wait for FastAPI
    //--------------------------------------------------
    

    for (int i = 0; i < 30; i++)
    {
        await Task.Delay(1000);

        using (UnityWebRequest request =
               UnityWebRequest.Get("http://127.0.0.1:8000/"))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                backendStarted = true;

                Debug.Log("TRELLIS Backend Started");

                return true;
            }
        }
    }

    Debug.LogError("Backend did not respond.");

    KillBackend();

    return false;

    
}
    private void KillBackend()
    {
        EditorApplication.update -= CheckProgress;
        EditorApplication.update -= CheckResult;

        try
        {
            if (backendProcess != null)
            {
                if (!backendProcess.HasExited)
                {
                    backendProcess.Kill();
                    backendProcess.WaitForExit();
                }

                backendProcess.Dispose();
                backendProcess = null;
            }
        }
        catch
        {
        }

        backendStarted = false;
        generationRunning = false;
        startingBackend = false;

        Debug.Log("TRELLIS Backend Terminated");
    }

    private async void Generate()
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            status = "Enter Prompt";
            Repaint();
            return;
        }

        if (generationRunning || startingBackend)
        {
            return;
        }

        startingBackend = true;

        status = "Starting Backend...";

        Repaint();

        bool started = await StartBackend();

        if (!started)
        {
            startingBackend = false;

            status = "Backend Failed To Start";

            Repaint();

            return;
        }

        startingBackend = false;

        PromptData data = new PromptData();

        data.prompt = prompt;
        data.quality = qualities[qualityIndex];

        string json = JsonUtility.ToJson(data);

        byte[] body = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request =
            new UnityWebRequest(
                "http://127.0.0.1:8000/generate",
                "POST"
            );

        request.uploadHandler =
            new UploadHandlerRaw(body);

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        var operation = request.SendWebRequest();

        operation.completed += _ =>
        {
            try
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    status = "Generation Started";

                    generationRunning = true;

                    progress = 0;

                    progressMessage = "Starting";

                    EditorApplication.update -= CheckResult;
                    EditorApplication.update -= CheckProgress;

                    EditorApplication.update += CheckResult;
                    EditorApplication.update += CheckProgress;
                }
                else
                {
                    status = request.error;

                    generationRunning = false;

                    startingBackend = false;

                    KillBackend();

                    Debug.LogError(request.error);

                    if (request.downloadHandler != null)
                        Debug.LogError(request.downloadHandler.text);
                }

                Repaint();
            }
            finally
            {
                request.Dispose();
            }
        };
    }

    private void CancelGeneration()
    {
        EditorApplication.update -= CheckProgress;
        EditorApplication.update -= CheckResult;

        generationRunning = false;
        startingBackend = false;

        progress = 0;
        progressMessage = "Cancelled";
        status = "Generation Cancelled";

        KillBackend();

        Repaint();
    }
    private void CheckResult()
{
    UnityWebRequest request =
        UnityWebRequest.Get(
            "http://127.0.0.1:8000/result"
        );

    var operation =
        request.SendWebRequest();

    operation.completed += _ =>
    {
        try
        {
            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                ResultData result =
                    JsonUtility.FromJson<ResultData>(
                        request.downloadHandler.text
                    );

                if (result == null)
                    return;

                if (result.status == "completed")
                {
                    status = "Model Imported Successfully";

                    progress = 100;

                    progressMessage = "Completed";

                    ImportGeneratedModel();

                    KillBackend();

                    generationRunning = false;
                    startingBackend = false;

                    EditorApplication.update -= CheckResult;
                    EditorApplication.update -= CheckProgress;

                    Repaint();

                    return;
                }

                if (result.status == "cancelled")
                {
                    status = "Generation Cancelled";

                    generationRunning = false;
                    startingBackend = false;

                    progress = 0;

                    progressMessage = "Cancelled";

                    KillBackend();

                    EditorApplication.update -= CheckResult;
                    EditorApplication.update -= CheckProgress;

                    Repaint();

                    return;
                }

                if (result.status == "failed")
                {
                    status = "Generation Failed";

                    generationRunning = false;
                    startingBackend = false;

                    KillBackend();

                    EditorApplication.update -= CheckResult;
                    EditorApplication.update -= CheckProgress;

                    Repaint();

                    return;
                }
            }
        }
        finally
        {
            request.Dispose();
        }
    };
}

    private void CheckProgress()
    {
        UnityWebRequest request =
            UnityWebRequest.Get(
                "http://127.0.0.1:8000/progress"
            );

        var operation =
            request.SendWebRequest();

        operation.completed += _ =>
        {
            try
            {
                if (request.result ==
                    UnityWebRequest.Result.Success)
                {
                    ProgressData data =
                        JsonUtility.FromJson<ProgressData>(
                            request.downloadHandler.text
                        );

                    if (data != null)
                    {
                        progress =
                            data.percent;

                        progressMessage =
                            data.message;

                        Repaint();
                    }
                }
            }
            finally
            {
                request.Dispose();
            }
        };
    }
    private async void ImportGeneratedModel()
    {
        string glbPath = "Assets/Prefabs/output.glb";

        // Refresh only once
        AssetDatabase.Refresh();

        // Wait for UnityGLTF to finish importing
        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(500);

            GameObject model =
                AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);

            if (model != null)
            {
                string prefabPath = "Assets/Prefabs/output.prefab";

                PrefabUtility.SaveAsPrefabAsset(
                    model,
                    prefabPath
                );

                EditorSceneManager.MarkAllScenesDirty();

                Debug.Log("Prefab created successfully.");

                return;
            }
        }

        Debug.LogError("Unity never imported output.glb");
    }
    private void OnDestroy()
    {
        EditorApplication.update -= CheckProgress;
        EditorApplication.update -= CheckResult;

        KillBackend();
    }
}