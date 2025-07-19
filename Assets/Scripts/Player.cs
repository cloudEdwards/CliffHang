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
                return;
            }

            Vector3 worldDirection = kcc.TransformRotation * new Vector3(input.Direction.x, 0f, input.Direction.y);
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




    private void HandleClimbing(NetInput input)
    {
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.Jump))
        {
            ExitClimb();
            Debug.Log("exiting climbing");
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
                ClimbTarget = hit.point + hit.normal * 0.3f;
                Debug.Log("entering climbing");
                EnterClimb();
            }
            else
            {
                Debug.Log("no hit on climbable");
            }
        }
    }


    private void EnterClimb()
    {
        IsClimbing = true;
        // kcc.SetGravity(0f);
        // kcc.SetVelocity(Vector3.zero);
    }

    private void ExitClimb()
    {
        IsClimbing = false;
        kcc.SetGravity(Physics.gravity.y * 2f);
    }


}
