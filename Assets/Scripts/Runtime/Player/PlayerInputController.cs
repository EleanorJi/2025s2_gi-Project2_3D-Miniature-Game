using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    private bool isInputEnabled = true;
    
    // The method provided for other scripts to call
    public void EnableInput()
    {
        isInputEnabled = true;
        Debug.Log($"Input enabled - Time: {Time.time}");
    }
    
    public void DisableInput()
    {
        isInputEnabled = false;
        Debug.Log($"Input disabled - Time: {Time.time}");
    }
    
    public bool IsInputEnabled()
    {
        return isInputEnabled;
    }
}