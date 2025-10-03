using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    private bool isInputEnabled = true;
    
    // 提供给其他脚本调用的方法
    public void EnableInput()
    {
        isInputEnabled = true;
        Debug.Log($"输入已启用 - 时间: {Time.time}");
    }
    
    public void DisableInput()
    {
        isInputEnabled = false;
        Debug.Log($"输入已禁用 - 时间: {Time.time}");
    }
    
    public bool IsInputEnabled()
    {
        return isInputEnabled;
    }
}