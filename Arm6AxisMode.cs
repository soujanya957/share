using System;
using UnityEngine;

public class Arm6AxisMode : NewControlMode
{
    public enum ArmControlMode
    {
        Absolute,
        Relative
    }

    [Header("Arm Control Mode")]
    public ArmControlMode armControlMode = ArmControlMode.Absolute;

    // For relative mode anchoring (per controller)
    private Vector3 initialControllerPosition;
    private Quaternion initialControllerRotation;
    private Vector3 initialGripperPosition;
    private Quaternion initialGripperRotation;
    private bool wasInArmModeLastFrame = false;

    public override void ControlUpdate(SpotMode spot, ControllerModel model, ControllerModel _)
    {
        // Set UI labels
        model.SetLabels(new[] {
            "",
            "",
            "",
            "",
            spot.GetGripperOpen() ? "Close Gripper" : "Open Gripper",
            "",
            ""
        });

        // Are we in arm mode? (always true for Arm6AxisMode)
        bool inArmMode = true;

        if (armControlMode == ArmControlMode.Absolute)
        {
            // Snap gripper to controller
            spot.SetGripperPos(model.anchor.transform);
        }
        else // Relative mode
        {
            // Only anchor when entering arm mode from a non-arm state
            if (!wasInArmModeLastFrame)
            {
                initialControllerPosition = model.anchor.transform.position;
                initialControllerRotation = model.anchor.transform.rotation;
                initialGripperPosition = spot.GetGripperPos().position;
                initialGripperRotation = spot.GetGripperPos().rotation;
            }

            // Compute controller delta from anchor
            Vector3 controllerDelta = model.anchor.transform.position - initialControllerPosition;
            Quaternion controllerDeltaRot = model.anchor.transform.rotation * Quaternion.Inverse(initialControllerRotation);

            // Apply delta to initial gripper pose
            Vector3 newGripperPosition = initialGripperPosition + controllerDelta;
            Quaternion newGripperRotation = controllerDeltaRot * initialGripperRotation;

            spot.SetGripperWorldPose(newGripperPosition, newGripperRotation);
        }

        // Gripper open/close logic (index trigger press)
        bool indexTrigger;
        if (model.isLeft)
            indexTrigger = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);
        else
            indexTrigger = OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);

        if (indexTrigger)
            spot.SetGripperOpen(!spot.GetGripperOpen());

        // Update last frame state
        wasInArmModeLastFrame = inArmMode;
    }

    public override string GetName()
    {
        return "Arm (6 Axis)";
    }
}
