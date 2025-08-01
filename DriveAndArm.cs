using System;
using UnityEngine;

/// <summary>
/// Single controller mode: Use index trigger to switch between Drive/Rotate and Arm control.
/// - Index trigger held: Arm mode (absolute or relative, selectable in Inspector)
/// - Index trigger not held: Drive/Rotate mode (joystick for drive, hold grip for rotate)
/// </summary>
public class DriveAndArm : NewControlMode
{
    public enum ArmControlMode
    {
        Absolute,
        Relative
    }

    [Header("Arm Control Mode")]
    public ArmControlMode armControlMode = ArmControlMode.Absolute;

    // For relative mode anchoring
    private Vector3 initialControllerPosition;
    private Quaternion initialControllerRotation;
    private Vector3 initialGripperPosition;
    private Quaternion initialGripperRotation;
    private bool isRelativeModeActive = false;

    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        // Use the same controller (left or right)
        bool isLeft = model.isLeft;

        // Input mappings
        var indexTrigger = isLeft ? OVRInput.Button.PrimaryIndexTrigger : OVRInput.Button.SecondaryIndexTrigger;
        var gripButton = isLeft ? OVRInput.Button.PrimaryHandTrigger : OVRInput.Button.SecondaryHandTrigger;
        var joystickAxis = isLeft ? OVRInput.Axis2D.PrimaryThumbstick : OVRInput.Axis2D.SecondaryThumbstick;

        // Input states
        bool isIndexHeld = OVRInput.Get(indexTrigger);
        bool indexPressed = OVRInput.GetDown(indexTrigger);
        bool isGripHeld = OVRInput.Get(gripButton);
        bool gripPressed = OVRInput.GetDown(gripButton);
        Vector2 joystick = OVRInput.Get(joystickAxis);

        // UI labels
        string thumbstickLabel = "";
        string triggerLabel = "";
        string gripLabel = "";

        if (isIndexHeld)
        {
            // === Arm Mode ===
            if (armControlMode == ArmControlMode.Absolute)
            {
                // Snap gripper to controller
                spot.SetGripperPos(model.anchor.transform);
                isRelativeModeActive = false;
            }
            else // Relative mode
            {
                if (!isRelativeModeActive)
                {
                    // Anchor initial poses on entering relative mode
                    initialControllerPosition = model.anchor.transform.position;
                    initialControllerRotation = model.anchor.transform.rotation;
                    initialGripperPosition = spot.GetGripperPos().position;
                    initialGripperRotation = spot.GetGripperPos().rotation;
                    isRelativeModeActive = true;
                }

                // Compute controller delta from anchor
                Vector3 controllerDelta = model.anchor.transform.position - initialControllerPosition;
                Quaternion controllerDeltaRot = model.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);

                // Apply delta to initial gripper pose
                Vector3 newGripperPosition = initialGripperPosition + controllerDelta;
                Quaternion newGripperRotation = controllerDeltaRot * initialGripperRotation;

                spot.SetGripperWorldPose(newGripperPosition, newGripperRotation);
            }

            // Open/close gripper with grip button
            if (gripPressed)
                spot.SetGripperOpen(!spot.GetGripperOpen());

            bool isGripperOpen = spot.GetGripperOpen();

            thumbstickLabel = "Arm Mode";
            triggerLabel = "Release to Drive";
            gripLabel = isGripperOpen ? "Hold to Close Gripper" : "Hold to Open Gripper";
        }
        else
        {
            // === Drive / Rotate Mode ===
            isRelativeModeActive = false;

            if (isGripHeld && Mathf.Abs(joystick.x) > 0.1f)
            {
                spot.Rotate(joystick.x * 0.5f);
            }
            else if (!isGripHeld && joystick.magnitude > 0.1f)
            {
                spot.Drive(joystick * 0.5f);
            }

            thumbstickLabel = isGripHeld ? "Rotate" : "Drive";
            gripLabel = "Hold to Rotate";
            triggerLabel = isGripHeld ? "Hold to Control Arm (Release Grip First)" : "Control Arm";
        }

        // Apply label order: "", "", "", thumbstick, trigger, grip
        model.SetLabels(new[] {
            "",
            "",
            "",
            thumbstickLabel,
            triggerLabel,
            gripLabel
        });
    }

    public override string GetName()
    {
        return "Drive and Arm (Joystick)";
    }
}
