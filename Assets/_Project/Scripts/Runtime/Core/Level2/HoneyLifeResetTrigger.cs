using UnityEngine;

/// 把它挂到“重生点”位置的一个小 BoxCollider(IsTrigger✔) 上
/// 作用：玩家重生/路过时，重置“本命蜂蜜计数”为 0（又有两次自救机会）
[RequireComponent(typeof(Collider))]
public class HoneyLifeResetTrigger : MonoBehaviour
{
    public bool onlyWhenPlayerTag = true;
    public string playerTag = "Player";
    bool _doneThisFrame;

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void LateUpdate() { _doneThisFrame = false; } // 允许下一帧再次触发一次

    void OnTriggerEnter(Collider other) { TryReset(other); }
    void OnTriggerStay(Collider other)  { TryReset(other); }

    void TryReset(Collider other)
    {
        if (_doneThisFrame) return;
        var t = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (onlyWhenPlayerTag && !t.CompareTag(playerTag)) return;

        StickyGooMashR.ResetGlobalHoneyCounter();
        _doneThisFrame = true;
        // 可选：Debug.Log("[Honey] reset counter at checkpoint");
    }
}
