using UnityEngine;
using System.Collections.Generic;

public class PressurePlateController : MonoBehaviour
{
    // It is used to record the current number of sugar cubes on the button, 
    // to prevent the fire from stopping when one sugar cube leaves while the others are still on the button.
    private HashSet<GameObject> sugarCubesOnPlate = new HashSet<GameObject>();

    // Publish an event and send a notification when the triggering state changes.
    public System.Action<bool> OnPlateActivated;

    void OnTriggerEnter(Collider other)
    {
        // Only detect objects belonging to the ButtonTrigger layer
        if (other.gameObject.layer == LayerMask.NameToLayer("ButtonTrigger"))
        {
            // Obtain the main body of the candy from the parent object
            GameObject sugarParent = other.transform.parent.gameObject;

            // Check whether the object entering the trigger is a sugar cube
            if (sugarParent.CompareTag("Sugar"))
            {
                sugarCubesOnPlate.Add(sugarParent);
                if (sugarCubesOnPlate.Count == 1)
                {
                    // As long as a sugar cube is placed on it, the button will be activated (true)
                    OnPlateActivated?.Invoke(true);
                    Debug.Log("The button has been activated!");
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("ButtonTrigger"))
        {
            // Obtain the main body of the candy from the parent object
            GameObject sugarParent = other.transform.parent.gameObject;

            // Check whether the object that triggers the exit is a sugar cube.
            if (sugarParent.CompareTag("Sugar"))
            {
                // remove this sugar
                sugarCubesOnPlate.Remove(sugarParent);

                // The activation button (false) will be deactivated only when all the sugar cubes have moved away.
                if (sugarCubesOnPlate.Count == 0)
                {
                    OnPlateActivated?.Invoke(false);
                    Debug.Log("The button cancels the activation.");
                }
            }
        }
        
    }
}