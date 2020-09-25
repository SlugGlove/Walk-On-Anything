using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    //public bool FollowTargetRotation;
    [Header("FollowSpeed")]
    public float FollowRotSpeed = 0.5f;
    public float FollowRotSpeedFlying = 10f;
    public float GravityFollowSpeed = 0.1f;
    private Vector3 LookDirection;

    public Transform target;
    public Transform FollowTarget;
    public Transform YPivot;

    private Transform pivot;
    private Transform FollowRotationPivot;
    public Transform camTransform;
    private Camera CamUnit;

    private Vector3 LookAtPos;
    [Header("Mouse Speeds")]
    public float MouseSpeed = 2;
    public float turnSmoothing = 0.1f;
    public float minAngle = -35;
    public float maxAngle = 35;
    public float LookDirectionSpeed = 2f;

    public float DistanceFromPlayer;
    private float CurrentDis;

    float smoothX;
    float smoothXvelocity;
    float smoothY;
    float smoothYvelocity;
    private float lookAngle;
    private float tiltAngle;

    float delta;

    //setup objects
    void Awake()
    {
        transform.parent = null;

        pivot = camTransform.parent;
        LookAtPos = target.position;
        CurrentDis = DistanceFromPlayer;

        tiltAngle = 10f;

        LookDirection = transform.forward;

        CamUnit = GetComponentInChildren<Camera>();
    }
    private void Update()
    {
        transform.position = FollowTarget.position;
    }

    private void FixedUpdate()
    {
        delta = Time.deltaTime;

        if (!target)
        {
            return;
        }
        Tick(delta);
    }

    public void Tick(float d)
    {
        float h = Input.GetAxis("CamHorizontal");
        float v = Input.GetAxis("CamVertical");
        float rotateSpeed = MouseSpeed;

        HandleRotation(d, v, h, rotateSpeed);
        handlePivotPosition();

        //look at player
        
        
        LookAtPos = target.position;

        Vector3 LerpDir = Vector3.Lerp(transform.up, target.up, d * FollowRotSpeed);
        transform.rotation = Quaternion.FromToRotation(transform.up, LerpDir) * transform.rotation;
    }

    void handlePivotPosition()
    {
        float targetZ = DistanceFromPlayer;

        CurrentDis = Mathf.Lerp(CurrentDis, targetZ, delta * 5f);

        Vector3 tp = Vector3.zero;
        tp.z = CurrentDis;
        camTransform.localPosition = tp;
    }

    void HandleRotation(float d, float v, float h, float speed)
    {
        if (turnSmoothing > 0)
        {
            smoothY = Mathf.SmoothDamp(smoothY, v, ref smoothYvelocity, turnSmoothing);
            smoothX = Mathf.SmoothDamp(smoothX, h, ref smoothXvelocity, turnSmoothing);
        }
        else
        {
            smoothX = h;
            smoothY = v;
        }

        tiltAngle -= smoothY * speed;
        tiltAngle = Mathf.Clamp(tiltAngle, minAngle, maxAngle);
        pivot.localRotation = Quaternion.Euler(tiltAngle, 0, 0);

        lookAngle += smoothX * speed;
        if (lookAngle > 360)
            lookAngle = 0;
        else if (lookAngle < 0)
            lookAngle = 360;

        if (smoothX != 0)
        {
            transform.RotateAround(transform.position, transform.up, ((smoothX * speed) * 30f) * d);
        }
    }
}