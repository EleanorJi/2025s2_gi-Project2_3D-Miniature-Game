using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialGate : MonoBehaviour
{
    TutorialManager manager;
    TutorialManager.Step gateFor;

    public void Init(TutorialManager mgr, TutorialManager.Step step)
    {
        manager = mgr;
        gateFor = step;
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && manager)
        {
            manager.CompleteStepFromGate(gateFor);
        }
    }
}
