using UnityEngine;
using System.Collections.Generic;

public class PressurePlateController : MonoBehaviour
{
    // 用来记录当前在按钮上的糖块数量，防止一个糖块离开导致其他糖块还在上面时火焰就停止了
    private HashSet<GameObject> sugarCubesOnPlate = new HashSet<GameObject>();

    // 公开一个事件，当触发状态改变时发出通知
    // 我们使用System.Action，参数是bool（是否被激活）
    public System.Action<bool> OnPlateActivated;

    void OnTriggerEnter(Collider other)
    {
        // 只检测属于ButtonTrigger层的物体
        if (other.gameObject.layer == LayerMask.NameToLayer("ButtonTrigger"))
        {
            // 通过父物体获取糖块主体
            GameObject sugarParent = other.transform.parent.gameObject;

            // 检查进入触发器的物体是否是糖块
            if (sugarParent.CompareTag("Sugar"))
            {
                sugarCubesOnPlate.Add(sugarParent);
                if (sugarCubesOnPlate.Count == 1)
                {
                    // 只要有一个糖块上来，就激活按钮（true）
                    OnPlateActivated?.Invoke(true);
                    Debug.Log("按钮被激活！");
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("ButtonTrigger"))
        {
            // 通过父物体获取糖块主体
            GameObject sugarParent = other.transform.parent.gameObject;

            // 检查离开触发器的物体是否是糖块
            if (sugarParent.CompareTag("Sugar"))
            {
                // 将这个糖块从集合中移除
                sugarCubesOnPlate.Remove(sugarParent);

                // 当所有糖块都离开时，才取消激活按钮（false）
                if (sugarCubesOnPlate.Count == 0)
                {
                    OnPlateActivated?.Invoke(false);
                    Debug.Log("按钮取消激活。");
                }
            }
        }
        
    }
}