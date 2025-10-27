using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishUI : MonoBehaviour
{
    public void GoHome()
    {
        Time.timeScale = 1f;                 // 若之前暂停过
        SceneOrderManager.Instance.LoadNextScene(); // 使用场景顺序跳转
    }
}
