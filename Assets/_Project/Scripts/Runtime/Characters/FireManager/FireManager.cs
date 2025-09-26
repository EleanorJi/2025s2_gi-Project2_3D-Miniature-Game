using UnityEngine;

public class FireManager : MonoBehaviour
{
    [Header("Related object")]
    // 在Inspector中拖拽按钮物体到这里
    public PressurePlateController pressurePlate;
    // 在Inspector中拖拽4个火焰物体到这里
    public FireController[] fires;

    void Start()
    {
        // 安全检查
        if (pressurePlate == null || fires == null || fires.Length == 0)
        {
            Debug.LogError("FireManager: 请检查按钮和火焰的关联！");
            return;
        }

        // 订阅按钮的事件
        // 当按钮的OnPlateActivated事件发生时，会调用我们这里的HandlePlateActivation方法
        pressurePlate.OnPlateActivated += HandlePlateActivation;
    }

    // 这个方法用来处理按钮的激活/取消激活事件
    private void HandlePlateActivation(bool isActivated)
    {
        if (isActivated)
        {
            // 按钮被激活，让所有火焰开始下降
            foreach (FireController fire in fires)
            {
                fire.StartDescent();
            }
        }
        else
        {
            // 按钮取消激活，你可以选择让火焰复位，或者保持下降后的状态。
            // 根据你的游戏需求，如果希望糖块拿走火焰就回去，就取消注释下面的代码。
            foreach (FireController fire in fires)
            {
                fire.ResetPosition();
            }
        }
    }

    // 取消订阅是个好习惯，防止内存泄漏
    void OnDestroy()
    {
        if (pressurePlate != null)
        {
            pressurePlate.OnPlateActivated -= HandlePlateActivation;
        }
    }
}