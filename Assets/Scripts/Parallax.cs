using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    public Transform cameraTransform;
    public float parallaxFactor;  // This factor will determine the speed of the parallax effect. 
                                  // Smaller values mean the background moves slower

    private Vector3 previousCameraPosition;

    private void Start()
    {
        previousCameraPosition = cameraTransform.position;
    }

    private void LateUpdate() // LateUpdate because we want to make sure that the effect is applied after the camera moves.
    {
        Vector3 deltaMovement = cameraTransform.position - previousCameraPosition;
        transform.position += new Vector3(deltaMovement.x * parallaxFactor, 0, 0);  // Adjust the background position
        previousCameraPosition = cameraTransform.position;
    }
}
