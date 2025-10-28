using UnityEngine;

public class FireManager : MonoBehaviour
{
    [Header("Related object")]
    public PressurePlateController pressurePlate;
    public FireController[] fires;

    void Start()
    {
        if (pressurePlate == null || fires == null || fires.Length == 0)
        {
            Debug.LogError("FireManager: Please check the connection between the button and the flame!");
            return;
        }

        pressurePlate.OnPlateActivated += HandlePlateActivation;
    }

    private void HandlePlateActivation(bool isActivated)
    {
        if (isActivated)
        {
            foreach (FireController fire in fires)
            {
                fire.StartShrink();
            }
        }
        else
        {
            foreach (FireController fire in fires)
            {
                fire.StartReset();
            }
        }
    }

    void OnDestroy()
    {
        if (pressurePlate != null)
        {
            pressurePlate.OnPlateActivated -= HandlePlateActivation;
        }
    }
}