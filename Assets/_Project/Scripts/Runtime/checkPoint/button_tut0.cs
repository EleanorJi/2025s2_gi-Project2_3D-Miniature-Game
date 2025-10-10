using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishUI : MonoBehaviour
{
    public void GoHome()
    {
        Time.timeScale = 1f;                 // 若之前暂停过
        SceneManager.LoadScene("StartScene"); // 注意：名字必须和 Build Settings 里的完全一致
    }
}
