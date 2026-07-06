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
    private int qualityIndex = 1;

    private string[] qualities =
    {
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

        if (
            GUILayout.Button(
                "Generate",
                GUILayout.Height(40)
            )
        )
        {
            Generate();
        }
    }
    private async Task<bool> StartBackend()
{
    if (backendStarted)
        return true;

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

    backendProcess.OutputDataReceived += (s, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
            Debug.Log("[Backend] " + e.Data);
    };

    backendProcess.ErrorDataReceived += (s, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
            Debug.LogError("[Backend] " + e.Data);
    };

    backendProcess.Start();

    backendProcess.BeginOutputReadLine();
    backendProcess.BeginErrorReadLine();

    for (int i = 0; i < 30; i++)
    {
        await Task.Delay(1000);

        UnityWebRequest request =
            UnityWebRequest.Get("http://127.0.0.1:8000/");

        var op = request.SendWebRequest();

        while (!op.isDone)
            await Task.Yield();

        if (request.result == UnityWebRequest.Result.Success)
        {
            backendStarted = true;
            Debug.Log("Backend Started");
            return true;
        }
    }

    Debug.LogError("Backend did not respond.");
    return false;
}
    
    private async void Generate()
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            status = "Enter Prompt";
            Repaint();
            return;
        }
        if(generationRunning)
        {
            status = "Generation Already Running";
            Repaint();
            return;
        }
        status = "Starting Backend...";
        Repaint();

        bool started = await StartBackend();

        if (!started)
        {
            status = "Backend Failed To Start";
            Repaint();
            return;
        }
       

        PromptData data = new PromptData();

        data.prompt = prompt;
        data.quality = qualities[qualityIndex];

        string json =
            JsonUtility.ToJson(data);

        byte[] body =
            Encoding.UTF8.GetBytes(json);

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

        var operation =
            request.SendWebRequest();

        operation.completed += _ =>
        {
            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                status =
                    "Generation Started";
                generationRunning = true;

                progress = 0;
                progressMessage =
                    "Starting";

                EditorApplication.update -= CheckResult;
                EditorApplication.update -= CheckProgress;

                EditorApplication.update += CheckResult;
                EditorApplication.update += CheckProgress;
            }
            else
            {
                status = request.error;

                UnityEngine.Debug.LogError(request.error);

                if (request.downloadHandler != null)
                    UnityEngine.Debug.LogError(request.downloadHandler.text);
            }

            Repaint();
        };
        
        
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
            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                ResultData result =
                    JsonUtility.FromJson<ResultData>(
                        request.downloadHandler.text
                    );

                if (result.status ==
    "completed")
                {
                    status =
                        "Model Imported Successfully";

                    progress = 100;

                    progressMessage =
                        "Completed";
                    ImportGeneratedModel();
                    EditorApplication.update -= CheckResult;
                    EditorApplication.update -= CheckProgress;
                    generationRunning = false;
                    Repaint();

                    return;
                }

                if (result.status ==
                    "failed")
                {
                    status =
                        "Generation Failed";

                    EditorApplication.update -=
                        CheckResult;

                    EditorApplication.update -=
                        CheckProgress;
                    generationRunning = false;
                    Repaint();
                }
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
            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                ProgressData data =
                    JsonUtility.FromJson<ProgressData>(
                        request.downloadHandler.text
                    );

                progress =
                    data.percent;

                progressMessage =
                    data.message;

                Repaint();
            }
        };
    }
    private void ImportGeneratedModel()
    {
        
        string folder =
            "Assets/GeneratedModels";

        if (
            !AssetDatabase.IsValidFolder(
                folder
            )
        )
        {
            AssetDatabase.CreateFolder(
                "Assets",
                "GeneratedModels"
            );
        }

        string sourcePath =
            Path.Combine(
                Application.streamingAssetsPath,
                "UnityBackend",
                "generated",
                "output.glb"
            );

        string targetPath =
            "Assets/GeneratedModels/output.glb";

        if (File.Exists(sourcePath))
        {
            if(File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            try
            {
                File.Copy(sourcePath,targetPath,true);
            }
            catch(System.Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
            }

            AssetDatabase.Refresh();

            EditorSceneManager.MarkAllScenesDirty();

            UnityEngine.Debug.Log(
                "GLB Imported Successfully"
            );
        }
        else
        {
            UnityEngine.Debug.LogError(
                "output.glb not found"
            );
        }
    }
    private void OnDestroy()
    {
        EditorApplication.update -= CheckProgress;
        EditorApplication.update -= CheckResult;
    }
}