using System.Collections;
using UnityEngine;

public class trafficlights : MonoBehaviour
{
    public GameObject[] gameObjects;
    void Start()
    {
        StartCoroutine(traffictime());
      
    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator traffictime()
    {
        gameObjects[0].SetActive(true);
     
        yield return new WaitForSeconds(16f);
        gameObjects[0].SetActive(false);
        gameObjects[1].SetActive(true);

    }
}
