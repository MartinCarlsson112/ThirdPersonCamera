using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;


public class ThirdPersonCamera : MonoBehaviour
{
    private CameraDeadzone deadZone;
    [SerializeField]
    Transform followTarget;
    public Transform FollowTarget
    {
        get { return followTarget; }
        set
        {
            followTarget = value;
            transform.position = followTarget.position + desiredOffset;
            deadZone.transform.position = followTarget.position;
        }
    }

    SphereCollider selfCollider;

    [Header("Camera Movement")]

    //TODO: Make properties for YOffset and ZOffset to support runtime changing of these values.
    [SerializeField]
    float desiredYOffset = 2;

    [SerializeField]
    float desiredZOffset = -10;


    [SerializeField]
    float cameraRotationSpeed = 3;
    [SerializeField]
    float cameraTranslationSpeed = 25;
    [SerializeField]
    float gamepadHorizontalScalar = 100;
    [SerializeField]
    float gamepadVerticalScalar = 100;
    [SerializeField]
    float maxVerticalRotationUp = 45;
    [SerializeField]
    float maxVerticalRotationDown = 45;

    [SerializeField]
    float minAcceptableDistance = 0.2f;

    [SerializeField]
    Vector3 pivotOffset = new Vector3(0, 0.75f, 0);
    
    [SerializeField]
    float minZoom = -5;
    [SerializeField]
    float maxZoom = 15;

    RaycastHit[] raycastBuffer = new RaycastHit[5];
    Vector3 desiredOffset = new Vector3(0, 2, 15);
    //zoom total
    float zoom = 0;
    //camera horizontal input total
    float xInput;
    //camera vertical input total
    float yInput;    
    //keep track of coroutine
    bool couroutineRunning = false;

    Vector3 lastTargetPosition = new Vector3(0, 0, 0);

    void Start()
    {
        desiredOffset = new Vector3(0, desiredYOffset, desiredZOffset);
        selfCollider = GetComponent<SphereCollider>();
        deadZone = GetComponentInChildren<CameraDeadzone>();
        deadZone.transform.parent = null;
        transform.position = (followTarget.position + pivotOffset) + desiredOffset;
        deadZone.transform.position = (followTarget.position + pivotOffset);

        Cursor.lockState = CursorLockMode.Locked;
        

        //if we set a followTarget in the inspector, force it to use setter.
        if(followTarget)
        {
            FollowTarget = followTarget;
        }
    }

    void AddHorizontalInput(float input)
    {
        if (!Mathf.Approximately(input, 0))
        {
            xInput -= input * cameraRotationSpeed * Time.deltaTime;
            UpdatePosition();
        }
    }

    void AddVerticalInput(float input)
    {
        if (!Mathf.Approximately(input, 0))
        {
            yInput -= input * cameraRotationSpeed * Time.deltaTime;
            yInput = Mathf.Clamp(yInput, -maxVerticalRotationDown, maxVerticalRotationUp);
            UpdatePosition();
        }
    }

    private void Update()
    {
        float cameraHorizontalMouse = -Input.GetAxis("CameraHorizontalMouse");
        AddHorizontalInput(cameraHorizontalMouse);
        if(Mathf.Approximately(cameraHorizontalMouse, 0))
        {
            float cameraHorizontalGamepad = -Input.GetAxis("CameraHorizontalGamepad") *gamepadHorizontalScalar;
            AddHorizontalInput(cameraHorizontalGamepad);
        }

        float cameraVerticalMouse = Input.GetAxis("CameraVerticalMouse");
        AddVerticalInput(cameraVerticalMouse);
        if(Mathf.Approximately(cameraVerticalMouse, 0))
        {
            float cameraVerticalGamepad = -Input.GetAxis("CameraVerticalGamepad") * gamepadVerticalScalar;
            AddVerticalInput(cameraVerticalGamepad);
        }

        float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
        if(!Mathf.Approximately(mouseWheel, 0))
        {
            zoom += mouseWheel;
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            UpdatePosition(); 
        }
    }

    private void LateUpdate()
    {
        transform.LookAt(followTarget.position + pivotOffset + transform.right * desiredOffset.x);
    }

    Vector3 CollisionTests(Vector3 requestedPosition)
    {
        Vector3 betterPosition = requestedPosition;
        ////TODO:
        ////can the camera move to the position
        //Vector3 cameraToPosDir = requestedPosition - transform.position;
        //if (Physics.SphereCast(new Ray(transform.position, cameraToPosDir), selfCollider.radius, out RaycastHit fromCameraToPositionHitInfo, cameraToPosDir.magnitude))
        //{
        //    //how high is the blockage?
            
        //}

        //can the camera see the player from the requested position
        Vector3 targetToPosDir = requestedPosition - (followTarget.position + pivotOffset);
        int count = Physics.SphereCastNonAlloc(new Ray((followTarget.position + pivotOffset), targetToPosDir.normalized), 0.5f, raycastBuffer, targetToPosDir.magnitude, ~0, QueryTriggerInteraction.Ignore);
        if (count > 0)
        {
            float bestDistance = float.MaxValue;
            for(int i = 0; i < count; i++)
            {
                if(raycastBuffer[i].point != Vector3.zero && raycastBuffer[i].collider.gameObject != followTarget)
                {
                    if(raycastBuffer[i].distance < bestDistance)
                    {
                        bestDistance = raycastBuffer[i].distance;
                        betterPosition = raycastBuffer[i].point;
                    }
                }
            }
        }
        return betterPosition;
    }

    bool Approx(Vector3 a, Vector3 b)
    {
        Vector3 v = a - b;
        if(Mathf.Abs(v.x) > Mathf.Epsilon || Mathf.Abs(v.y) > Mathf.Epsilon || Mathf.Abs(v.z) > Mathf.Epsilon)
        {
            return false;
        }
        return true;
    }


    private IEnumerator UpdateCameraPosition(Transform followTarget)
    {
        lastTargetPosition = followTarget.position;
        couroutineRunning = true;
        do
        {
            Vector3 desiredOffsetWithZoom = (desiredOffset + Vector3.forward * zoom);
            Vector3 realOffset = Quaternion.AngleAxis(xInput, Vector3.up) * Quaternion.AngleAxis(yInput, Vector3.right) * desiredOffsetWithZoom;
            Vector3 realTarget = (followTarget.position + pivotOffset) + realOffset;

            realTarget = CollisionTests(realTarget);
            
            if (Approx(followTarget.position, lastTargetPosition) && (transform.position - realTarget).magnitude < minAcceptableDistance)
            {
                deadZone.transform.position = followTarget.position;
                break;
            }
            transform.position = Vector3.MoveTowards(transform.position, realTarget, cameraTranslationSpeed * Time.deltaTime);
            lastTargetPosition = followTarget.position;
            yield return null;
        }
        while (true);
        couroutineRunning = false;
    }

    public void UpdatePosition()
    {
        if(!couroutineRunning)
        {
            StartCoroutine(UpdateCameraPosition(followTarget));
        }
    }
}
