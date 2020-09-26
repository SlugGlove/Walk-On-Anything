using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public enum WorldState
    {
        Grounded, //on ground
        InAir, //in the air
    }

    [HideInInspector]
    public WorldState States;
    private Transform Cam;
    private Transform CamY;
    //public ControlsPivot AxisPivot;
    private CameraFollow CamFol;

    private DetectCollision Colli;
    [HideInInspector]
    public Rigidbody Rigid;

    float delta;

    [Header("Physics")]
    public Transform[] GroundChecks;
    public float DownwardPush; //what is applied to the player when on a surface to stick to it
    public float GravityAmt;    //how much we are pulled downwards when we are on a wall
    public float GravityBuildSpeed; //how quickly we build our gravity speed
    private float ActGravAmt; //the actual gravity applied to our character

    public LayerMask GroundLayers; //what layers the ground can be
    public float GravityRotationSpeed = 10f; //how fast we rotate to a new gravity direction

    [Header("Stats")]
    public float Speed = 15f; //max speed for basic movement
    public float Acceleration = 4f; //how quickly we build speed
    public float turnSpeed = 2f;
    private Vector3 MovDirection, movepos, targetDir, GroundDir; //where to move to

    [Header("Jumps")]
    public float JumpAmt;
    private bool HasJumped;

    // Start is called before the first frame update
    void Awake()
    {
        Rigid = GetComponentInChildren<Rigidbody>();
        Colli = GetComponent<DetectCollision>();
        GroundDir = transform.up;
        SetGrounded();
        
        Cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        CamY = Cam.transform.parent.parent.transform;
        CamFol = Cam.GetComponentInParent<CameraFollow>();

        //detatch rigidbody so it can move freely 
        Rigid.transform.parent = null;
    }

    private void Update()   //inputs
    {
        transform.position = Rigid.position;

        //check for jumping
        if (States == WorldState.Grounded)
        {
            if (Input.GetButtonDown("Jump"))
            {
                //if the player can jump, isnt attacking and isnt using an item
                if (!HasJumped)
                {              

                    StopCoroutine(JumpUp(JumpAmt));
                    StartCoroutine(JumpUp(JumpAmt));
                    return;
                }
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()  //world movement
    {
        delta = Time.deltaTime;

        if (States == WorldState.Grounded)
        {
            float Spd = Speed;

           if (Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Vertical") == 0)
            {
                //we are not moving, lerp to a walk speed
                Spd = 0f;
            }
          
            
            MoveSelf(delta, Spd, Acceleration);

            //switch to air
            bool Ground = Colli.CheckGround(-GroundDir);

            if (!Ground)
            {
                SetInAir();
                return;
            }

        }
        else if (States == WorldState.InAir)
        {        
            if (HasJumped) //only return back to ground once jump state is over
                return;

            FallingCtrl(delta, Speed, Acceleration);

            //check for ground
            bool Ground = Colli.CheckGround(-GroundDir);

            if (Ground)
            {
                SetGrounded();
                return;
            }
        }
    }

    //transition to ground
    public void SetGrounded()
    {
        ActGravAmt = 5f; //our gravity is returned to normal

        States = WorldState.Grounded;
    }
    //transition to air
    void SetInAir()
    {
        States = WorldState.InAir;
    }
    //jump up
    IEnumerator JumpUp(float UpwardsAmt)
    {
        HasJumped = true;

        Rigid.velocity = Vector3.zero;

        SetInAir();

        if (UpwardsAmt != 0)
            Rigid.AddForce((transform.up * UpwardsAmt), ForceMode.Impulse);

        ActGravAmt = 0;

        yield return new WaitForSecondsRealtime(0.3f);
        HasJumped = false;
    }     
    //check the angle of the floor we are stood on
    Vector3 FloorAngleCheck()
    {
        RaycastHit HitFront;
        RaycastHit HitCentre;
        RaycastHit HitBack;

        Physics.Raycast(GroundChecks[0].position, -GroundChecks[0].transform.up, out HitFront, 10f, GroundLayers);
        Physics.Raycast(GroundChecks[1].position, -GroundChecks[1].transform.up, out HitCentre, 10f, GroundLayers);
        Physics.Raycast(GroundChecks[2].position, -GroundChecks[2].transform.up, out HitBack, 10f, GroundLayers);

        Vector3 HitDir = transform.up;

        if (HitFront.transform != null)
        {
            HitDir += HitFront.normal;
        }
        if (HitCentre.transform != null)
        {
            HitDir += HitCentre.normal;
        }
        if (HitBack.transform != null)
        {
            HitDir += HitBack.normal;
        }

        Debug.DrawLine(transform.position, transform.position + (HitDir.normalized * 5f), Color.red);

        return HitDir.normalized;
    }
    
    //move our character
    void MoveSelf(float d, float Speed, float Accel)
    {
        float _xMov = Input.GetAxis("Horizontal");
        float _zMov = Input.GetAxis("Vertical");
        bool MoveInput = false;

        Vector3 screenMovementForward = CamY.transform.forward;
        Vector3 screenMovementRight = CamY.transform.right;

        Vector3 h = screenMovementRight * _xMov;
        Vector3 v = screenMovementForward * _zMov;

        Vector3 moveDirection = (v + h).normalized;

        if (_xMov == 0 && _zMov == 0)
        {
            targetDir = transform.forward;
        }
        else
        {
            targetDir = moveDirection;
            MoveInput = true;
        }

        if (targetDir == Vector3.zero)
        {
            targetDir = transform.forward;
        }

        Quaternion lookDir = Quaternion.LookRotation(targetDir);
        float TurnSpd = turnSpeed;

        Vector3 SetGroundDir = FloorAngleCheck();
        GroundDir = Vector3.Lerp(GroundDir, SetGroundDir, d * GravityRotationSpeed);

        //lerp mesh slower when not on ground
        RotateSelf(SetGroundDir, d, GravityRotationSpeed);
        RotateMesh(d, targetDir, TurnSpd);

        //move character
        float Spd = Speed;
        Vector3 curVelocity = Rigid.velocity;

        if (!MoveInput) //if we are not pressing a move input we move towards velocity //or are crouching
        {
            Spd = Speed * 0.8f; //less speed is applied to our character
            MovDirection = Vector3.Lerp(transform.forward, Rigid.velocity.normalized, 12f * d);
        }
        else
        {
            MovDirection = transform.forward;
        }

        Vector3 targetVelocity = MovDirection * Spd;

        //push downwards in downward direction of mesh
        targetVelocity -= SetGroundDir * DownwardPush;

        Vector3 dir = Vector3.Lerp(curVelocity, targetVelocity, d * Accel);
        Rigid.velocity = dir;
    }

    //move once we are in air
    void FallingCtrl(float d, float Speed, float Accel)
    {
        //control our direction slightly when falling
        float _xMov = Input.GetAxis("Horizontal");
        float _zMov = Input.GetAxis("Vertical");

        Vector3 screenMovementForward = CamY.transform.forward;
        Vector3 screenMovementRight = CamY.transform.right;

        Vector3 h = screenMovementRight * _xMov;
        Vector3 v = screenMovementForward * _zMov;

        Vector3 moveDirection = (v + h).normalized;

        if (_xMov != 0 || _zMov != 0)
        {
            targetDir = moveDirection;
        }
        else
        {
            targetDir = transform.forward;
        }

        Quaternion lookDir = Quaternion.LookRotation(targetDir);

        Vector3 SetGroundDir = FloorAngleCheck();
        GroundDir = Vector3.Lerp(GroundDir, SetGroundDir, d * GravityRotationSpeed);

        //lerp mesh slower when not on ground
        RotateSelf(GroundDir, d, GravityRotationSpeed);
        RotateMesh(d, transform.forward, turnSpeed);

        //move character
        MovDirection = targetDir;
        float Spd = Speed;
        Vector3 curVelocity = Rigid.velocity;

        Vector3 targetVelocity = MovDirection;

        //fall from the air
        if (ActGravAmt < GravityAmt - 0.5f)
            ActGravAmt = Mathf.Lerp(ActGravAmt, GravityAmt, GravityBuildSpeed * d);

        //move either upwards or downwards with gravity
        targetVelocity -= GroundDir * ActGravAmt;

        Vector3 dir = Vector3.Lerp(curVelocity, targetVelocity, d * Accel);
        Rigid.velocity = dir;
    }
    //rotate the direction we face down
    void RotateSelf(Vector3 Direction, float d, float GravitySpd)
    {
        Vector3 LerpDir = Vector3.Lerp(transform.up, Direction, d * GravitySpd);
        transform.rotation = Quaternion.FromToRotation(transform.up, LerpDir) * transform.rotation;
    }
    //rotate the direction we face forwards
    void RotateMesh(float d, Vector3 LookDir, float spd)
    {
        Quaternion SlerpRot = Quaternion.LookRotation(LookDir, transform.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, SlerpRot, spd * d);
    }
}
