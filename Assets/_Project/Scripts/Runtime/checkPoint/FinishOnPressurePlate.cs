using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class FinishOnPressurePlate : MonoBehaviour
{
    [Header("Source (Pressure Plate)")]
    [SerializeField] private PressurePlateController plate;
    [SerializeField] private float confirmDelay = 0.4f;   // 糖块稳定落盘的确认延迟

    [Header("Win UI")]
    [SerializeField] private GameObject winPanel;   // 可不拖；会自动找名为 "WinPanel"
    [SerializeField] private TMP_Text titleText;    // 可不拖；找 WinPanel 下第一个 TMP
    [SerializeField] private TMP_Text subText;      // 可不拖；找 WinPanel 下第二个 TMP（显示倒计时）
    [SerializeField] private Button homeButton;     // 备用：手动返回按钮（可不拖）

    [TextArea] [SerializeField] private string winTitle = "Congratulations!";
    [TextArea] [SerializeField] private string subTemplate = "You’ve completed the tutorial.\nBack to Home in {0}s";

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
        // 自动找 UI 引用
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

        // 暂停游戏，但用“未缩放时间”做倒计时
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (autoRoutine != null) StopCoroutine(autoRoutine);
        autoRoutine = StartCoroutine(AutoReturnRoutine());
    }

    private IEnumerator AutoReturnRoutine()
    {
        float t = autoReturnDelay;
        while (t > 0f)
        {
            if (subText) subText.text = string.Format(subTemplate, Mathf.CeilToInt(t));
            t -= Time.unscaledDeltaTime;     // 关键：未缩放时间
            yield return null;
        }
        ReturnHome();
    }

    public void ReturnHome()
    {
        Time.timeScale = 1f;

        if (!Application.CanStreamedLevelBeLoaded(homeSceneName))
        {
            Debug.LogError($"[Finish] Scene '{homeSceneName}' is NOT in Build Settings.");
            // 兜底：尝试回到 Build Settings 中第一个场景
            if (SceneManager.sceneCountInBuildSettings > 0)
                SceneManager.LoadScene(0);
            return;
        }

        SceneManager.LoadScene(homeSceneName);
    }
}
