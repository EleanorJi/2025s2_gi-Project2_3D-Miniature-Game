using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Antventure.UI;

[RequireComponent(typeof(Collider))]
public class FinishOnPressurePlate : MonoBehaviour
{
    [Header("Source (Pressure Plate)")]
    [SerializeField] private PressurePlateController plate;
    [SerializeField] private float confirmDelay = 0.4f;   // Confirmation of the stable drop of the sugar cubes is delayed

    [Header("Win UI")]
    [SerializeField] private GameObject winPanel;   // Automatically search for the name "WinPanel"
    [SerializeField] private TMP_Text titleText;    // Find the first TMP under WinPanel
    [SerializeField] private TMP_Text subText;      // Find the second TMP (displaying the countdown) under WinPanel
    [SerializeField] private Button homeButton;     // Manual return button

    [TextArea] [SerializeField] private string winTitle = "Congratulations!";
    [TextArea] [SerializeField] private string subTemplate = "You've completed the tutorial.\nBack to Home in {0}s";

    [Header("Auto Return")]
    [SerializeField] private string homeSceneName = "StartScene";
    [SerializeField] private float autoReturnDelay = 5f;

    private bool finished;
    private float scheduleAt = -1f;
    private Coroutine autoRoutine;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void Awake()
    {
        // Automatic UI reference finding
        if (!winPanel)  winPanel = GameObject.Find("WinPanel");

        if (winPanel)
        {
            if (!titleText || !subText)
            {
                var tmps = winPanel.GetComponentsInChildren<TMP_Text>(true);
                if (!titleText && tmps.Length > 0) titleText = tmps[0];
                if (!subText && tmps.Length > 1)   subText  = tmps[tmps.Length - 1];
            }
            if (!homeButton) homeButton = winPanel.GetComponentInChildren<Button>(true);

            winPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (!plate) plate = GetComponent<PressurePlateController>();
        if (plate) plate.OnPlateActivated += HandlePlate;

        if (homeButton) homeButton.onClick.AddListener(ReturnHome);
    }

    private void OnDisable()
    {
        if (plate) plate.OnPlateActivated -= HandlePlate;
        if (homeButton) homeButton.onClick.RemoveListener(ReturnHome);
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!finished && scheduleAt > 0f && Time.time >= scheduleAt)
        {
            ShowWin();
            scheduleAt = -1f;
        }
    }

    private void HandlePlate(bool active)
    {
        if (finished) return;
        if (active) scheduleAt = Time.time + confirmDelay;
        else        scheduleAt = -1f;
    }

    private void ShowWin()
    {
        finished = true;

        if (winPanel) winPanel.SetActive(true);
        if (titleText) titleText.text = winTitle;
        if (subText)   subText.text   = string.Format(subTemplate, Mathf.CeilToInt(autoReturnDelay));

        // Pause the game, but use "unscaled time" for the countdown.
        Time.timeScale = 0f;

        // Use a unified mouse management system to display the mouse instead of directly setting it.
        if (UnifiedCursorManager.Instance != null)
        {
            UnifiedCursorManager.Instance.ShowPauseMenuCursor();
        }
        else
        {
            // Fallback solution: If UnifiedCursorManager is not available, use the original method
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (autoRoutine != null) StopCoroutine(autoRoutine);
        autoRoutine = StartCoroutine(AutoReturnRoutine());
    }

    private IEnumerator AutoReturnRoutine()
    {
        float t = autoReturnDelay;
        while (t > 0f)
        {
            if (subText) subText.text = string.Format(subTemplate, Mathf.CeilToInt(t));
            t -= Time.unscaledDeltaTime;     // Unscaled time
            yield return null;
        }
        ReturnHome();
    }

    public void ReturnHome()
    {
        Time.timeScale = 1f;

        // Before switching scenes, restore the normal state of the mouse management system
        if (UnifiedCursorManager.Instance != null)
        {
            UnifiedCursorManager.Instance.HidePauseMenuCursor();
        }

        if (!Application.CanStreamedLevelBeLoaded(homeSceneName))
        {
            Debug.LogError($"[Finish] Scene '{homeSceneName}' is NOT in Build Settings.");
            // Fallback: Try to return to the first scene in the Build Settings.
            if (SceneManager.sceneCountInBuildSettings > 0)
                SceneManager.LoadScene(0);
            return;
        }

        SceneOrderManager.Instance.LoadNextScene();
    }
}
