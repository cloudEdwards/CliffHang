using Fusion;
using Fusion.Addons.KCC;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private KCC kcc;
    [SerializeField] private Transform camTarget;
    [SerializeField] private MeshRenderer[] modelParts;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private float lookSensitivity = 0.15f;
    [SerializeField] private Vector3 jumpInpulse = new(0f, 10f, 0f);

    // grapple
    [SerializeField] private float grappleCD = .1f;
    [SerializeField] private float grappleStrength = 12f;
    [field: SerializeField] public float AbilityRange { get; private set; } = 25f;

    public float GrappleCDFactor => (GrappleCD.RemainingTime(Runner) ?? 0f) / grappleCD;

    [Networked] private TickTimer GrappleCD { get; set; }

    [Networked] private NetworkButtons PreviousButtons { get; set; }

    private Rigidbody rb = new();

    private bool isGrabbing = false;

    public override void Spawned()
    {
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
            CheckJump(input);

            kcc.AddLookRotation(input.LookDelta * lookSensitivity, -maxPitch, maxPitch);
            UpdateCamTarget();

            // if (input.Buttons.WasPressed(PreviousButtons, InputButton.Grab))
            // {
            //     TryGrab(camTarget.forward);
            // }

            if (input.Buttons.WasPressed(PreviousButtons, InputButton.Climb))
            {
                TryClimb(camTarget.forward);
            }

            SetInputDirection(input);
            PreviousButtons = input.Buttons;

            // todo: baseLookRotation = kcc.GetLookRotation();
        }

        // if (rb != null && isGrabbing)
        // {
        //     SetGrabDirection(rb.position - transform.position);
        //     return;
        // }
    }

    public void CheckJump(NetInput input)
    {
        if (input.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && kcc.FixedData.IsGrounded)
        {
            kcc.Jump(jumpInpulse);
        }
    }

    public void SetInputDirection(NetInput input)
    {
        Vector3 worldDirection = kcc.FixedData.TransformRotation * input.Direction.X0Y();
        kcc.SetInputDirection(worldDirection);
    }
    
    // public void SetGrabDirection(Vector3 direction)
    // {
    //     Vector3 worldDirection = kcc.FixedData.TransformRotation * direction;
    //     kcc.SetInputDirection(worldDirection);
    // }

    public override void Render()
    {
        UpdateCamTarget();
    }

    private void UpdateCamTarget()
    {
        camTarget.localRotation = Quaternion.Euler(kcc.GetLookRotation().x, 0f, 0f);
    }

    public void ResetCooldowns()
    {
        GrappleCD = TickTimer.None;
    }

    public void TryClimb(Vector3 lookDirection)
    {
        // if (GrappleCD.ExpiredOrNotRunning(Runner) && Physics.Raycast(camTarget.position, lookDirection, out RaycastHit hitInfo, AbilityRange))
        if (Physics.Raycast(camTarget.position, lookDirection, out RaycastHit hitInfo, AbilityRange))
        {
            // if (hitInfo.collider.TryGetComponent(out BlockExpression _))
            if (hitInfo.transform.CompareTag("Climbable") || (hitInfo.transform.CompareTag("Vine")))
            {
                GrappleCD = TickTimer.CreateFromSeconds(Runner, grappleCD);
                Vector3 grappleVector = Vector3.Normalize(hitInfo.point - transform.position);
                // apply upwards force if looking up
                if (grappleVector.y > 0f)
                {
                    grappleVector = Vector3.Normalize(grappleVector + Vector3.up);
                }
                kcc.Jump(grappleVector * grappleStrength);
            }
        }
    }

    // public void TryGrab(Vector3 lookDirection)
    // {
    //     if (isGrabbing)
    //     {
    //         isGrabbing = false;
    //         return;
    //     }

    //     // if (GrappleCD.ExpiredOrNotRunning(Runner) && Physics.Raycast(camTarget.position, lookDirection, out RaycastHit hitInfo, AbilityRange))
    //     if (Physics.Raycast(camTarget.position, lookDirection, out RaycastHit hitInfo, AbilityRange))
    //     {

    //         // if (hitInfo.collider.TryGetComponent(out BlockExpression _))
    //         if (hitInfo.transform.CompareTag("Vine"))
    //         {
    //             isGrabbing = true;

    //             // get the collider (segment of the rope)
    //             // get position of the rigidBody
    //             // move player to that position while grabbing
    //             // if we click and we're already grbbing, we let go (grabbing is false)

    //             rb = hitInfo.collider.gameObject.GetComponent<Rigidbody>();
    //         }
    //     }

    // }

}
