using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DissolveSphere : MonoBehaviour
{

    //Material mat;



    //private void Start()
    //{
    //    mat = GetComponent<Renderer>().material;
    //}

    //private void Update()
    //{
    //    mat.SetFloat("_DissolveAmount", Mathf.Sin(Time.time) / 2 + 0.5f);

    //}

    private Material mat;
    private bool isActive = false;
    private float dissolveStartTime;
    private float dissolveSpeed = 1.0f;

    void Start()
    {
       
        Renderer renderer = GetComponent<Renderer>();
        mat = new Material(renderer.material);
        renderer.material = mat;

      
        mat.SetFloat("_DissolveAmount", 0);
    }

  
    public void StartDissolve(float speed = 1.0f)
    {
        isActive = true;
        dissolveStartTime = Time.time;
        dissolveSpeed = speed;
    }

   
    public void StopDissolve()
    {
        isActive = false;
        mat.SetFloat("_DissolveAmount", 0);
    }

    void Update()
    {
        if (isActive)
        {
          
            float dissolveValue = Mathf.Sin((Time.time - dissolveStartTime) * dissolveSpeed) / 2 + 0.5f;
            mat.SetFloat("_DissolveAmount", dissolveValue);
        }
    }




}