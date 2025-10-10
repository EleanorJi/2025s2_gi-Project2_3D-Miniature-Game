using UnityEngine;

public class FireManager : MonoBehaviour
{
    [Header("Related object")]
    public PressurePlateController pressurePlate;
    public FireController[] fires;

    void Start()
    {
        // debug
        if (pressurePlate == null || fires == null || fires.Length == 0)
        {
            Debug.LogError("FireManager: Please check the connection between the button and the flame!");
            return;
        }

        // The event of the subscription button
        // When the OnPlateActivated event of the button occurs, HandlePlateActivation method will be called.
        pressurePlate.OnPlateActivated += HandlePlateActivation;
    }

    // This method is used to handle the activation/deactivation events of the buttons.
    private void HandlePlateActivation(bool isActivated)
    {
        if (isActivated)
        {
            // The button was activated, causing all the flames to start descending.
            foreach (FireController fire in fires)
            {
                fire.StartShrink();
            }
        }
        else
        {
            // The button cancels the activation, allowing the flame to reset.
            foreach (FireController fire in fires)
            {
                fire.ResetFire();
            }
        }
    }

    // Unsubscribe
    void OnDestroy()
    {
        if (pressurePlate != null)
        {
            pressurePlate.OnPlateActivated -= HandlePlateActivation;
        }
    }
}