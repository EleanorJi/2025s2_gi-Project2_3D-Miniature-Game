using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class SoftAirWall : MonoBehaviour
{
    [Header("谁会被挡")]
    public string playerTag = "Player";

    [Header("推回设置")]
    [Tooltip("从空气墙中心到玩家的“水平向外”推力")]
    public float pushBackStrength = 18f;
    [Tooltip("每帧最多的水平速度（避免弹飞）")]
    public float maxHorizontalSpeed = 6f;
    [Tooltip("附加的阻尼，进区时让速度迅速变小")]
    public float damping = 12f;

    [Header("提示UI")]
    public CanvasGroup hintGroup;   // 拖你的提示面板
    public TMP_Text hintText;
    [TextArea] public string message = "不能再靠近了，不然会被水花拍死。";
    public float fade = 0.2f;
    public float stay = 1.5f;

    Rigidbody _rb;
    Coroutine _uiCo;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _rb = other.attachedRigidbody ? other.attachedRigidbody : other.GetComponent<Rigidbody>();
        if (_rb == null) return;

        ShowHintOnce();
    }

    void OnTriggerStay(Collider other)
    {
        if (_rb == null || !other.CompareTag(playerTag)) return;

        // 水平推回：从空气墙中心 -> 玩家 的方向
        Vector3 center = GetComponent<Collider>().bounds.center;
        Vector3 toPlayer = other.transform.position - center;
        toPlayer.y = 0f; // 只管水平
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        Vector3 dir = toPlayer.normalized;

        // 先给点阻尼，防止硬闯
        Vector3 v = _rb.linearVelocity;
        Vector3 hv = new Vector3(v.x, 0f, v.z);
        hv = Vector3.Lerp(hv, Vector3.zero, 1f - Mathf.Exp(-damping * Time.deltaTime));

        // 再加一个向外的推力
        hv += dir * (pushBackStrength * Time.deltaTime);

        // 限制水平速度
        if (hv.magnitude > maxHorizontalSpeed) hv = hv.normalized * maxHorizontalSpeed;

        _rb.linearVelocity = new Vector3(hv.x, v.y, hv.z);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _rb = null;
    }

    void ShowHintOnce()
    {
        if (!hintGroup) return;
        if (_uiCo != null) StopCoroutine(_uiCo);
        _uiCo = StartCoroutine(HintRoutine());
    }

    IEnumerator HintRoutine()
    {
        if (hintText) hintText.text = message;

        // 淡入
        for (float t=0; t<fade; t+=Time.unscaledDeltaTime)
        {
            hintGroup.alpha = Mathf.Lerp(0f, 1f, t/fade);
            yield return null;
        }
        hintGroup.alpha = 1f;

        // 停留
        yield return new WaitForSecondsRealtime(stay);

        // 淡出
        for (float t=0; t<fade; t+=Time.unscaledDeltaTime)
        {
            hintGroup.alpha = Mathf.Lerp(1f, 0f, t/fade);
            yield return null;
        }
        hintGroup.alpha = 0f;
        _uiCo = null;
    }
}
