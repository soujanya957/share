using System;
using UnityEngine;

public enum RotationReference
{
    World,
    Head,
    Controller
}

// One controller mode
public class LocomotionJoystickMode : NewControlMode
{
    [Header("References")]
    public GameObject cameraRig;
    public GameObject vignette; // Assign in Inspector if using vignette
    public Transform headTransform; // Assign main camera/head in Inspector
    public Transform leftControllerTransform;  // Assign left controller in Inspector
    public Transform rightControllerTransform; // Assign right controller in Inspector

    [Header("Locomotion Settings")]
    public float moveSpeed = 1.4f; // meters per second (human walk speed)
    public float flySpeed = 1.4f;  // meters per second (vertical)
    public bool useSnapTurn = true;
    public float snapAngle = 25f;
    public float smoothTurnSpeed = 90f; // deg/sec
    public bool vignetteEnabled = false;

    [Header("Rotation Reference")]
    public RotationReference rotationReference = RotationReference.World;

    // Internal state
    private float initialY;
    private bool hasInitialY = false;
    private bool isTriggerHeld = false;
    private float prevJoyX = 0f;

    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        if (vignette != null)
            vignette.SetActive(vignetteEnabled);

        Vector2 joystick = model.isLeft
            ? OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)
            : OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        bool trigger = model.isLeft
            ? OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > 0.5f
            : OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > 0.5f;

        if (trigger && !isTriggerHeld)
        {
            initialY = cameraRig.transform.position.y;
            hasInitialY = true;
        }
        isTriggerHeld = trigger;

        // [[ Rotation ]]
        Vector3 up;
        switch (rotationReference)
        {
            case RotationReference.Head:
                up = headTransform != null ? headTransform.up : Vector3.up;
                break;
            case RotationReference.Controller:
                Transform controllerTransform = model.isLeft ? leftControllerTransform : rightControllerTransform;
                up = controllerTransform != null ? controllerTransform.up : Vector3.up;
                break;
            default:
                up = Vector3.up;
                break;
        }

        if (!trigger)
        {
            if (joystick.magnitude > 0.1f)
            {
                Vector3 move = cameraRig.transform.forward * joystick.y + cameraRig.transform.right * joystick.x;
                move.y = 0;
                cameraRig.transform.position += move * moveSpeed * Time.deltaTime;
            }
        }
        else
        {
            // [[ Rotation ]]
            if (useSnapTurn)
            {
                if (joystick.x < -0.5f && prevJoyX >= -0.5f)
                    cameraRig.transform.RotateAround(cameraRig.transform.position, up, -snapAngle);
                else if (joystick.x > 0.5f && prevJoyX <= 0.5f)
                    cameraRig.transform.RotateAround(cameraRig.transform.position, up, snapAngle);
            }
            else
            {
                if (Mathf.Abs(joystick.x) > 0.1f)
                {
                    float angle = smoothTurnSpeed * joystick.x * Time.deltaTime;
                    cameraRig.transform.RotateAround(cameraRig.transform.position, up, angle);
                }
            }

            // [[ Fly ]]
            if (Mathf.Abs(joystick.y) > 0.1f)
            {
                Debug.Log("Flying! joystick.y: " + joystick.y + " | flySpeed: " + flySpeed);
                Vector3 pos = cameraRig.transform.position;
                pos.y += joystick.y * flySpeed * Time.deltaTime;
                cameraRig.transform.position = pos;
            }
        }

        prevJoyX = joystick.x;

        bool resetY = model.isLeft
            ? OVRInput.GetDown(OVRInput.Button.Three) // X button
            : OVRInput.GetDown(OVRInput.Button.One);  // A button

        if (resetY && hasInitialY)
        {
            Vector3 pos = cameraRig.transform.position;
            pos.y = initialY;
            cameraRig.transform.position = pos;
        }

        // [[ Labels ]]
        // A/X, B/Y, start/menu, thumbstick, trigger, gripper
        string[] labels = new string[6];
        labels[0] = model.isLeft ? (hasInitialY ? "Reset Y (X)" : "") : (hasInitialY ? "Reset Y (A)" : "");
        labels[1] = "";
        labels[2] = "";
        labels[3] = !trigger ? "Locomote" : (useSnapTurn ? "Snap Turn / Fly" : "Smooth Turn / Fly");
        labels[4] = trigger ? "Rotate/Fly" : "Hold: Rotate/Fly";
        labels[5] = "";

        model.SetLabels(labels);
    }

    public override string GetName()
    {
        return "Locomotion (Joystick)";
    }
}
