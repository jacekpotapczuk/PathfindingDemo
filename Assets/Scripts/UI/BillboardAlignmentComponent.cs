using UnityEngine;

namespace PathfindingDemo
{
    /// <summary>
    /// Aligns transform to always face the main camera for billboard-style UI elements.
    /// </summary>
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
                transform.forward = mainCamera.transform.forward;
        }
    }
}