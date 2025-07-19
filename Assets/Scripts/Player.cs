using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.SimpleKCC;
using Unity.VisualScripting;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private SimpleKCC kcc;
    [SerializeField] private Transform camTarget;
    [SerializeField] private MeshRenderer[] modelParts;
    [SerializeField] private float lookSensitivity = 0.15f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpInpulse = 10f;

    [Networked] private NetworkButtons PreviousButtons { get; set; }


    // [SerializeField] private Camera playerCamera;
    [SerializeField] private float grabDistance = 2.5f;
    [SerializeField] private LayerMask climbableMask;

    [Networked] private bool IsClimbing { get; set; }
    [Networked] private Vector3 ClimbTarget { get; set; }


    private ConfigurableJoint climbJoint;
    [SerializeField] private float climbMaxDistance = 1.5f;
    [SerializeField] private float climbSpring = 1000f;
    [SerializeField] private float climbDamper = 50f;

    [SerializeField] private float climbSpeed = 2f;
    [SerializeField] private float climbRadius = 1.5f;




    public override void Spawned()
    {
        kcc.SetGravity(Physics.gravity.y * 2f);

        if (HasInputAuthority)
        {
            // make player model seethrough for player camera
            foreach (MeshRenderer part in modelParts)
            {
                part.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
            CameraFollow.Singleton.SetTarget(camTarget);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetInput input))
        {
            kcc.AddLookRotation(input.LookDelta * lookSensitivity);
            UpdateCamTarget();

            // Call climbing state checks here
            if (IsClimbing)
            {
                HandleClimbing(input);
                // return;
            }

            Vector3 worldDirection;

            if (IsClimbing)
            {
                // TODO: set a timer and run out of stamina, then can't move up
                worldDirection = kcc.TransformRotation * new Vector3(input.Direction.x, input.Direction.y, 0f);
            }
            else
            {
                kcc.SetGravity(Physics.gravity.y * 2f);
                worldDirection = kcc.TransformRotation * new Vector3(input.Direction.x, 0f, input.Direction.y);
            }
            
            float jump = 0f;

            if (input.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && kcc.IsGrounded)
            {
                jump = jumpInpulse;
            }

            kcc.Move(worldDirection.normalized * speed, jump);


            // Add climbing attempt at the end
            TryEnterClimb(input);


            PreviousButtons = input.Buttons;
        }
    }

    public override void Render()
    {
        UpdateCamTarget();
    }

    private void UpdateCamTarget()
    {
        camTarget.localRotation = Quaternion.Euler(kcc.GetLookRotation().x, 0f, 0f);
    }




    // private void HandleClimbing(NetInput input)
    // {
    //     if (input.Buttons.WasPressed(PreviousButtons, InputButton.Jump))
    //     {
    //         ExitClimb();
    //         Debug.Log("exiting climbing");
    //     }
    // }
    
    private void HandleClimbing(NetInput input)
    {
        Vector3 climbInput = kcc.TransformRotation * new Vector3(input.Direction.x, input.Direction.y, input.Direction.y);

        // Use vertical axis for climbing (W/S = up/down)
        Vector3 climbDelta = new Vector3(0, input.Direction.y, 0) * climbSpeed * Runner.DeltaTime;

        // Calculate target position
        Vector3 targetPosition = kcc.Object.transform.position + climbDelta;

        // Constrain within climb radius
        // if (Vector3.Distance(targetPosition, ClimbTarget) <= climbRadius)
        // {
        //     kcc.SetPosition(targetPosition);
        // }

        // Jump to exit climb
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.Jump))
        {
            ExitClimb();
            // kcc.AddJump(jumpInpulse);
        }
    }


    private void TryEnterClimb(NetInput input)
    {
        if (!HasInputAuthority || IsClimbing == true)
            return;

        if (input.Buttons.WasPressed(PreviousButtons, InputButton.Fire))
        {
            Debug.Log("click on climbing");

            Vector3 rayOrigin = Camera.main.transform.position;
            Vector3 rayDirection = Camera.main.transform.forward;

            // Draw the ray line in Game view (visible if Gizmos are on)
            Debug.DrawRay(rayOrigin, rayDirection * grabDistance, Color.green, 1f);

            Ray ray = new Ray(rayOrigin, rayDirection);
            if (Physics.Raycast(ray, out RaycastHit hit, grabDistance, climbableMask))
            {
                if (hit.transform.CompareTag("Climbable"))
                {
                    ClimbTarget = hit.point + hit.normal * 0.3f;
                    Debug.Log("entering climbing");
                    Debug.Log($"entering climbing {hit.transform}");
                    EnterClimb(hit);   
                }
            }
            else
            {
                Debug.Log("no hit on climbable");
            }
        }
    }


    private void EnterClimb(RaycastHit hit)
    {
        IsClimbing = true;
        CreateClimbJoint(hit.point);
    }

    private void CreateClimbJoint(Vector3 anchor)
    {
        // Remove any existing joint
        if (climbJoint != null)
            Destroy(climbJoint);

        Rigidbody rb = kcc.GetComponent<Rigidbody>();

        climbJoint = rb.gameObject.AddComponent<ConfigurableJoint>();
        climbJoint.autoConfigureConnectedAnchor = false;
        climbJoint.connectedAnchor = anchor;


        // Restrict all motion but allow movement within a limit
        climbJoint.xMotion = ConfigurableJointMotion.Limited;
        climbJoint.yMotion = ConfigurableJointMotion.Limited;
        climbJoint.zMotion = ConfigurableJointMotion.Limited;

        SoftJointLimit limit = new SoftJointLimit { limit = climbMaxDistance };
        climbJoint.linearLimit = limit;

        JointDrive drive = new JointDrive
        {
            positionSpring = climbSpring,
            positionDamper = climbDamper,
            maximumForce = Mathf.Infinity
        };

        climbJoint.xDrive = drive;
        climbJoint.yDrive = drive;
        climbJoint.zDrive = drive;

        climbJoint.anchor = Vector3.zero;
        climbJoint.rotationDriveMode = RotationDriveMode.Slerp;
    }


    private void ExitClimb()
    {
        IsClimbing = false;

        if (climbJoint != null)
        {
            Destroy(climbJoint);
            climbJoint = null;
        }
        
        kcc.SetGravity(Physics.gravity.y * 2f);
    }


}
