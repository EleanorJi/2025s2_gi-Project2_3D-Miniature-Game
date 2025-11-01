using UnityEngine;

public class CameraIntroThird : MonoBehaviour
{
    public GameObject[] gameObjects;
    public Animator animator;

    public string targetStateName = "third";
    private bool hasTriggered;

    private void Awake()
    {
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (animator == null || hasTriggered)
        {
            return;
        }
           

        var info = animator.GetCurrentAnimatorStateInfo(0);

        // 当当前状态是目标状态且已经执行结束，且不在过渡中时触发
        if (info.IsName(targetStateName) && info.normalizedTime >= 1f && !animator.IsInTransition(0))
        {
            hasTriggered = true;
            OnAnimatorEnd();
        }
    }

    void OnAnimatorEnd()
    {
        Debug.Log("动画播放完成 " + targetStateName);
        gameObjects[0].SetActive(false);
        gameObjects[1].SetActive(true);
       
    }
}
