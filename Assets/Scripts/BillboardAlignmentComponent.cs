using System;
using UnityEngine;

public class BillboardAlignmentComponent : MonoBehaviour
{
    private Camera mainCamera;
    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera)
        {
            transform.forward = mainCamera.transform.forward;    
        }
    }
}