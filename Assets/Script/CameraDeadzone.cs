using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraDeadzone : MonoBehaviour
{
    private new ThirdPersonCamera camera;

    private void Awake()
    {
        camera = GetComponentInParent<ThirdPersonCamera>();
    }

    private void OnTriggerExit(Collider other)
    {

        if (other.gameObject == camera.FollowTarget.gameObject)
        {
            camera.UpdatePosition();
        }
    }

}
