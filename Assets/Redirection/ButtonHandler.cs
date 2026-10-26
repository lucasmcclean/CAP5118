using UnityEngine;
using UnityEngine.XR;

public class ButtonHandler : MonoBehaviour
{
    private float thumbstickThreshold = 0.7f;

    private bool leftTriggered;
    private bool rightTriggered;
    private bool upTriggered;
    private bool downTriggered;
    private bool primaryHeld, secondaryHeld, triggerHeld;

    public Evaluation evaluation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void Update()
    {
        HandleThumbstick();
        // Apply a final stick selection before confirming when both arrive in one frame.
        HandleButtons();
    }

    private void HandleButtons()
    {
        // The standard OpenXR path also supports runtimes where OVRPlugin's
        // legacy action bindings are unavailable (including XR Simulator).
        InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        ProcessButtonState(
            OVRInput.Get(OVRInput.Button.One) || ReadButton(left, CommonUsages.primaryButton) || ReadButton(right, CommonUsages.primaryButton),
            OVRInput.Get(OVRInput.Button.Two) || ReadButton(left, CommonUsages.secondaryButton) || ReadButton(right, CommonUsages.secondaryButton),
            OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger) || ReadButton(left, CommonUsages.triggerButton) || ReadButton(right, CommonUsages.triggerButton));
    }

    private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> feature)
    {
        bool value;
        return device.isValid && device.TryGetFeatureValue(feature, out value) && value;
    }

    public void ProcessButtonState(bool primary, bool secondary, bool trigger)
    {
        if (primary && !primaryHeld)
        {
            OnAPressed();
        }
        if (secondary && !secondaryHeld)
        {
            OnBPressed();
        }
        if (trigger && !triggerHeld)
        {
            OnTriggerPressed();
        }
        primaryHeld = primary;
        secondaryHeld = secondary;
        triggerHeld = trigger;
    }

    private void HandleThumbstick()
    {
        Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        Vector2 secondaryStick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (secondaryStick.sqrMagnitude > stick.sqrMagnitude) stick = secondaryStick;
        InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        Vector2 xrStick;
        if (left.isValid && left.TryGetFeatureValue(CommonUsages.primary2DAxis, out xrStick) && xrStick.sqrMagnitude > stick.sqrMagnitude) stick = xrStick;
        if (right.isValid && right.TryGetFeatureValue(CommonUsages.primary2DAxis, out xrStick) && xrStick.sqrMagnitude > stick.sqrMagnitude) stick = xrStick;
        ProcessThumbstick(stick);
    }

    public void ProcessThumbstick(Vector2 stick)
    {

        // LEFT
        if (stick.x < -thumbstickThreshold)
        {
            if (!leftTriggered)
            {
                leftTriggered = true;
                OnThumbstickLeft();
            }
        }
        else
        {
            leftTriggered = false;
        }

        // RIGHT
        if (stick.x > thumbstickThreshold)
        {
            if (!rightTriggered)
            {
                rightTriggered = true;
                OnThumbstickRight();
            }
        }
        else
        {
            rightTriggered = false;
        }

        // UP
        if (stick.y > thumbstickThreshold)
        {
            if (!upTriggered)
            {
                upTriggered = true;
                OnThumbstickUp();
            }
        }
        else
        {
            upTriggered = false;
        }

        // DOWN
        if (stick.y < -thumbstickThreshold)
        {
            if (!downTriggered)
            {
                downTriggered = true;
                OnThumbstickDown();
            }
        }
        else
        {
            downTriggered = false;
        }
    }

    private void OnAPressed()
    {
        Debug.Log("A button pressed");

        if (evaluation.State == ExperimentState.ES_PID)
        {
            evaluation.PressPidKey();
        }
        else if (evaluation.State == ExperimentState.ES_READYQUESTION || evaluation.State == ExperimentState.ES_THANKYOU)
        {
            evaluation.StateChange();
        }
        else if (evaluation.State == ExperimentState.ES_SLIDES && evaluation.EXP_TYPE == ExperimentType.ET_ROTATION)
        {
            evaluation.StateChange();
        }
        else if (evaluation.State == ExperimentState.ES_QUESTION)
        {
            evaluation.SubmitResponse();
        }
    }

    private void OnBPressed()
    {
        Debug.Log("B button pressed");

        if (evaluation.State == ExperimentState.ES_READYQUESTION
            || evaluation.State == ExperimentState.ES_THANKYOU)
        {
            evaluation.StateChange();
        }
    }

    private void OnTriggerPressed()
    {
        Debug.Log("Trigger button pressed");

        //if (evaluation.EXP_TYPE == ExperimentType.ET_ROTATION || evaluation.EXP_TYPE == ExperimentType.ET_TRANSLATION || evaluation.EXP_TYPE == ExperimentType.ET_CURVATURE)
        //{
        //    evaluation.recenterPose();
        //}
    }

    private void OnThumbstickLeft()
    {
        Debug.Log("Thumbstick LEFT");

        if (evaluation.State == ExperimentState.ES_QUESTION)
        {
            evaluation.AdjustResponse(-1);
        }
        else if (evaluation.State == ExperimentState.ES_PID) evaluation.MovePidSelection(-1, 0);
    }

    private void OnThumbstickRight()
    {
        Debug.Log("Thumbstick RIGHT");

        if (evaluation.State == ExperimentState.ES_QUESTION)
        {
            evaluation.AdjustResponse(1);
        }
        else if (evaluation.State == ExperimentState.ES_PID) evaluation.MovePidSelection(1, 0);
    }

    private void OnThumbstickUp()
    {
        Debug.Log("Thumbstick UP");
        if (evaluation.State == ExperimentState.ES_PID) evaluation.MovePidSelection(0, -1);
    }

    private void OnThumbstickDown()
    {
        Debug.Log("Thumbstick DOWN");
        if (evaluation.State == ExperimentState.ES_PID) evaluation.MovePidSelection(0, 1);
    }
}
