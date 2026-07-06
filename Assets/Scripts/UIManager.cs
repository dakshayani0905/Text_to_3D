using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI References")]
    public TMP_InputField promptInput;
    public TMP_Text statusText;
    public Button generateButton;

    [Header("Panels")]
    public GameObject canvasUI;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void HideUI()
    {
        if (canvasUI != null)
        {
            canvasUI.SetActive(false);
        }
    }

    public void ShowUI()
    {
        if (canvasUI != null)
        {
            canvasUI.SetActive(true);
        }
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public string GetPrompt()
    {
        if (promptInput == null)
            return "";

        return promptInput.text;
    }

    public void EnableGenerateButton(bool enable)
    {
        if (generateButton != null)
        {
            generateButton.interactable = enable;
        }
    }
}