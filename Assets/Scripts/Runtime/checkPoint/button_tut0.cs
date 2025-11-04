using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishUI : MonoBehaviour
{
    public void GoHome()
    {
        Time.timeScale = 1f;                 // If there was a pause before
        SceneOrderManager.Instance.LoadNextScene(); // Use scene sequence navigation
    }
}
