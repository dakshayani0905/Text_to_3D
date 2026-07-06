using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ModelLoader : MonoBehaviour
{
    public static ModelLoader Instance;

    [Header("Generated Model")]
    public string relativeModelPath =
        "StreamingAssets/UnityBackend/generated/output.glb";

    [Header("Spawn Position")]
    public Vector3 spawnPosition = Vector3.zero;

    private GameObject currentModel;

    void Awake()
    {
        Instance = this;
    }

    public void LoadGeneratedModel()
    {
        StartCoroutine(ImportRoutine());
    }

    IEnumerator ImportRoutine()
    {
        string assetPath =
            "Assets/" + relativeModelPath;

        if (!File.Exists(assetPath))
        {
            Debug.LogError(
                "Generated model not found:\n" +
                assetPath
            );
            yield break;
        }

        AssetDatabase.Refresh();

        yield return new WaitForSeconds(1f);

        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                assetPath
            );

        if (prefab == null)
        {
            Debug.LogError(
                "Unity hasn't imported the GLB yet."
            );

            yield break;
        }

        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        currentModel =
            Instantiate(
                prefab,
                spawnPosition,
                Quaternion.identity
            );

        currentModel.name = "Generated Model";

        Debug.Log(
            "Model Loaded Successfully"
        );
    }

    public void ClearModel()
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
            currentModel = null;
        }
    }
}