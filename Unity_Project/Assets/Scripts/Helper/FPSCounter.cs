using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCounter : MonoBehaviour { 

    private float startTime = 0.0f;
    public float duration = 10;
    private int frameCounter = 0;
    private bool running = false;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (running)
        {
            frameCounter += 1;
            if (Time.time - startTime > duration)
            {
                Debug.LogFormat("{0} Frames in {1} seconds: {2} average FPS", frameCounter, duration, frameCounter / duration);
                running = false;
            }
            
        }
    }

    public void startCounting()
    {
        if (!running)
        {
            frameCounter = 0;
            startTime = Time.time;
            running = true;
            Debug.LogFormat("FPS Measure started. Duration {0}s", duration); ;
        } 
    }
}
