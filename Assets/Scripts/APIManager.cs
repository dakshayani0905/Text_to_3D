using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using GLTFast;
using System;
using Debug = UnityEngine.Debug;
using System.IO;
using UnityEngine.UI;
public class APIManager : MonoBehaviour
{
    void Update()
    {
        if(isGenerating &&
           generationTimeText != null)
        {
            float elapsed =
                Time.time -
                generationStartTime;

            generationTimeText.text =
                "Time: " +
                elapsed.ToString("F1") +
                " s";
        }
    }
    [Header("API")]
    public string apiUrl = "http://127.0.0.1:8000/generate";

    [Header("UI")]
    public TMP_InputField promptInput;
    public TMP_Text statusText;
    public GameObject canvasUI;
    public TMP_Dropdown qualityDropdown;
    public Slider progressBar;
    public Button cancelButton;
    private float generationStartTime;
    private bool isGenerating = false;
    Coroutine progressRoutine;
    public TMP_Text generationTimeText;
    

    // ==========================================
    // REQUEST DATA
    // ==========================================

    [System.Serializable]
    public class PromptData
    {
        public string prompt;

        public string quality;
    }
    [System.Serializable]
    public class ResponseData
    {
        public string status;
        public string model_url;
        public string error;
    }
    [System.Serializable]
    public class ResultData
    {
        public string status;
        public string model_url;
        public string file_path;
        public string error;
    }
    
    [System.Serializable]
    public class ProgressData
    {
        public string message;
        public int percent;
    }
    
   
    IEnumerator GetProgress()
    {
        while (gameObject.activeInHierarchy)
        {
            UnityWebRequest request =
                UnityWebRequest.Get(
                    "http://127.0.0.1:8000/progress"
                );

            yield return request.SendWebRequest();

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                string json =
                    request.downloadHandler.text;

                if(string.IsNullOrEmpty(json))
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                ProgressData data =
                    JsonUtility.FromJson<ProgressData>(
                        json
                    );

                if (data != null)
                {
                    statusText.text =
                        data.message +
                        " (" +
                        data.percent +
                        "%)";

                    if (progressBar != null)
                    {
                        progressBar.value =
                            data.percent;
                    }
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
    // ==========================================
    // GENERATE BUTTON
    // ==========================================

    public void GenerateModel()
    {
        if(isGenerating)
        {
            return;
        }

        // RESET UI AFTER CANCEL
        statusText.text = "Starting Generation...";

        if(progressBar != null)
        {
            progressBar.value = 0;
        }

        if(generationTimeText != null)
        {
            generationTimeText.text = "TIME: 0.0 S";
        }

        generationStartTime = Time.time;
        isGenerating = true;

        if(cancelButton != null)
        {
            cancelButton.gameObject.SetActive(true);
        }
        if(progressRoutine != null)
        {
            StopCoroutine(progressRoutine);
        }

        progressRoutine =
            StartCoroutine(GetProgress());
        StartCoroutine(GenerateRoutine());
    }
    IEnumerator GenerateRoutine()
    {
        if (BackendManager.Instance == null)
        {
            statusText.text = "Backend Manager Missing";

            isGenerating = false;

            yield break;
        }

        yield return BackendManager.Instance.WaitUntilReady();

        yield return SendPrompt();
    }

    // ==========================================
    // SEND PROMPT
    // ==========================================

    IEnumerator SendPrompt()
    {
        if(promptInput == null)
        {
            statusText.text="Prompt Input Missing";

            isGenerating=false;

            yield break;
        }

        if(string.IsNullOrWhiteSpace(promptInput.text))
        {
            statusText.text="Enter Prompt";

            isGenerating=false;

            yield break;
        }

        PromptData data =
            new PromptData();

        data.prompt =
            promptInput.text;
        
        if(qualityDropdown == null)
        {
            Debug.LogError("Quality Dropdown Missing");
            isGenerating=false;
            yield break;
        }
        data.quality =
            qualityDropdown.options[
                qualityDropdown.value
            ].text;
        Debug.Log(
            "Quality = " +
            data.quality
        );

        string jsonData =
            JsonUtility.ToJson(data);

        UnityWebRequest request =
            new UnityWebRequest(
                apiUrl,
                "POST"
            );

        byte[] bodyRaw =
            System.Text.Encoding.UTF8
                .GetBytes(jsonData);

        request.uploadHandler =
            new UploadHandlerRaw(bodyRaw);

        request.downloadHandler =
            new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        yield return request.SendWebRequest();

        if (request.result !=
            UnityWebRequest.Result.Success)
        {
            statusText.text =
                "API Error";

            Debug.LogError(
                request.error
            );
            isGenerating=false;

            if(progressRoutine!=null)
            {
                StopCoroutine(progressRoutine);
                progressRoutine=null;
            }

            if(cancelButton!=null)
            {
                cancelButton.gameObject.SetActive(false);
            }
            yield break;
        }

        string responseText =
            request.downloadHandler.text;

        Debug.Log($"API RESPONSE : {responseText}");

        ResponseData response=null;

        try
        {
            response=
                JsonUtility.FromJson<ResponseData>(
                    responseText
                );
        }
        catch
        {
            statusText.text="Invalid JSON";

            isGenerating=false;

            yield break;
        }

        if (response == null)
        {
            statusText.text =
                "Invalid Response";
            isGenerating=false;
            yield break;
        }

        if (response.status != "started")
        {
            statusText.text =
                "Generation Failed";
            isGenerating=false;
            yield break;
        }

        StartCoroutine(
            WaitForResult()
        );
    }
    IEnumerator WaitForResult()
    {
        while(isGenerating)
        {
            UnityWebRequest request =
                UnityWebRequest.Get(
                    "http://127.0.0.1:8000/result"
                );

            yield return request.SendWebRequest();

            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                string json =
                    request.downloadHandler.text;

                if(string.IsNullOrEmpty(json))
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                ResultData result =
                    JsonUtility.FromJson<ResultData>(
                        json
                    );

                if (result.status ==
                    "completed")
                {
                    Debug.Log(
                        "STARTING LOAD MODEL"
                    );
                    isGenerating = false;
                    generationStartTime = 0f;
                    
                    if(progressRoutine != null)
                    {
                        StopCoroutine(progressRoutine);
                        progressRoutine = null;
                    }
                    if(cancelButton != null)
                    {
                        cancelButton.gameObject.SetActive(false);
                    }
                    StartCoroutine(
                        LoadModel(
                            result.model_url
                        )
                    );

                    yield break;
                }

                if (result.status ==
                    "failed")
                {
                    isGenerating = false;
                    if(progressRoutine != null)
                    {
                        StopCoroutine(progressRoutine);
                        progressRoutine = null;
                    }
                    if(cancelButton != null)
                    {
                        cancelButton.gameObject.SetActive(false);
                    }
                    statusText.text =
                        "Generation Failed";

                    yield break;
                }
                if (result.status == "cancelled")
                {
                    isGenerating = false;
                    if(progressRoutine != null)
                    {
                        StopCoroutine(progressRoutine);
                        progressRoutine = null;
                    }
                    if(cancelButton != null)
                    {
                        cancelButton.gameObject.SetActive(false);
                    }

                    if(progressBar != null)
                    {
                        progressBar.value = 0;
                    }

                    statusText.text =
                        "Generation Cancelled";

                    yield break;
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }
    // ==========================================
    // LOAD MODEL
    // ==========================================

    IEnumerator LoadModel(string url)
    {
        // DELETE OLD MODEL
        
        if(ModelLoader.Instance!=null)
        {
            ModelLoader.Instance.ClearModel();
        }
        

        // CREATE GENERATED MODELS FOLDER
        string folderPath =
            Path.Combine(
                Application.persistentDataPath,
                "GeneratedModels"
            );

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(
                folderPath
            );
        }

        // SAVE PATH
        string filePath =
            Path.Combine(
                folderPath,
                "GeneratedModel.glb"
            );
        Debug.Log("DOWNLOADING GLB...");
        Debug.Log(url);
        // DOWNLOAD GLB
        using (UnityWebRequest www =
            UnityWebRequest.Get(url))
        {
            www.downloadHandler =
                new DownloadHandlerFile(
                    filePath
                );
            
            yield return www.SendWebRequest();
            Debug.Log(
                "FILE EXISTS: " +
                File.Exists(filePath)
            );

            Debug.Log(
                "PATH: " +
                filePath
            );
            if (www.result !=
                UnityWebRequest.Result.Success)
            {
                statusText.text =
                    "Download Failed";

                UnityEngine.Debug.LogError(
                    www.error
                );

                yield break;
            }
        }

        statusText.text =
            "Importing Model...";

        // LOAD GLB
        GltfImport gltf =
            new GltfImport();

        var loadTask =
            gltf.Load(filePath);

        while (!loadTask.IsCompleted)
        {
            yield return null;
        }

        bool success =
            loadTask.Result;
        Debug.Log(
            "GLTF LOAD RESULT = " +
            success
        );
        if (!success)
        {
            statusText.text =
                "GLB Import Failed";

            UnityEngine.Debug.LogError(
                "FAILED TO LOAD GLB"
            );

            yield break;
        }

        // CREATE MODEL ROOT
        GameObject model =
            new GameObject(
                "GeneratedModel"
            );

// INSTANTIATE MODEL
        var instantiateTask =
            gltf.InstantiateMainSceneAsync(
                model.transform
            );

        while (!instantiateTask.IsCompleted)
        {
            yield return null;
        }

        bool instantiateSuccess =
            instantiateTask.Result;
        Debug.Log(
            "INSTANTIATE RESULT = " +
            instantiateSuccess
        );
        if (instantiateSuccess)
        {
            if(model.GetComponent<AutoRotate>()==null)
            {
                model.AddComponent<AutoRotate>();
            }

            statusText.text =
                "Done";
        }
        else
        {
            statusText.text =
                "Load Failed";

            yield break;
        }

        // CHECK MODEL
        if (model.transform.childCount == 0)
        {
            statusText.text =
                "No Mesh Created";

            yield break;
        }

        // GET RENDERERS
        Renderer[] renderers =
            model.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            statusText.text =
                "No Renderers Found";

            yield break;
        }


        // CALCULATE BOUNDS
        Bounds bounds =
            renderers[0].bounds;

        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(
                r.bounds
            );
        }

        // CENTER MODEL
        Vector3 center =
            bounds.center;

        model.transform.position =
            -center;

        // PLACE ABOVE GROUND
        model.transform.position +=
            new Vector3(
                0,
                bounds.extents.y,
                0
            );

        // SCALE MODEL
        float maxSize =
            Mathf.Max(
                bounds.size.x,
                bounds.size.y,
                bounds.size.z
            );

        if (maxSize < 0.01f)
        {
            maxSize = 1f;
        }

        float targetSize =
            2f;

        float scaleFactor =
            targetSize / maxSize;

        model.transform.localScale =
            Vector3.one * scaleFactor;

        // CAMERA
        if (Camera.main != null)
        {
            Camera.main.transform.position =
                new Vector3(
                    0,
                    1.5f,
                    -5f
                );

            Camera.main.transform.LookAt(
                model.transform
            );
        }

        // REFRESH UNITY
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        // HIDE UI
        if(UIManager.Instance!=null)
        {
            UIManager.Instance.HideUI();
        }

        statusText.text =
            "MODEL LOADED SUCCESSFULLY";

        UnityEngine.Debug.Log(
            "MODEL LOADED SUCCESSFULLY"
        );
    }
    public void CancelGeneration()
    {
        Debug.Log("Cancelling Generation");

        isGenerating = false;

        if (progressRoutine != null)
        {
            StopCoroutine(progressRoutine);
            progressRoutine = null;
        }

        if(BackendManager.Instance!=null)
        {
            BackendManager.Instance.RestartBackend();
        }

        if (progressBar != null)
        {
            progressBar.value = 0;
        }

        if (statusText != null)
        {
            statusText.text =
                "Generation Cancelled";
        }

        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(false);
        }
    }

    
    
}