using UnityEngine;

public class ButtonHandler : MonoBehaviour
{
    private float thumbstickThreshold = 0.7f;

    private bool leftTriggered;
    private bool rightTriggered;
    private bool upTriggered;
    private bool downTriggered;

    public Evaluation evaluation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void Update()
    {
        HandleButtons();
        HandleThumbstick();
    }

    private void HandleButtons()
    {
        // A button pressed
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            OnAPressed();
        }

        if (evaluation.State == ExperimentState.ES_SLIDES
            && OVRInput.Get(OVRInput.Button.One))
        {
            evaluation.StateChange();
        }

        // B button pressed
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            OnBPressed();
        }

        // Trigger button pressed
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            OnTriggerPressed();
        }
    }

    private void HandleThumbstick()
    {
        Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

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
        
        else if (evaluation.State == ExperimentState.ES_QUESTION)
        {
            evaluation.SubmitResponse();
        }

        else if (evaluation.State == ExperimentState.ES_READYQUESTION
            || evaluation.State == ExperimentState.ES_THANKYOU)
        {
            evaluation.StateChange();
        }
    }

    private void OnBPressed()
    {
        Debug.Log("B button pressed");
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

        else if (evaluation.State == ExperimentState.ES_PID)
        {
            evaluation.MovePidSelection(-1, 0);
        }   
    }

    private void OnThumbstickRight()
    {
        Debug.Log("Thumbstick RIGHT");

        if (evaluation.State == ExperimentState.ES_QUESTION)
        {
            evaluation.AdjustResponse(1);
        }
        else if (evaluation.State == ExperimentState.ES_PID)
        {
            evaluation.MovePidSelection(1, 0);
        }
    }

    private void OnThumbstickUp()
    {
        Debug.Log("Thumbstick UP");
         
        if (evaluation.State == ExperimentState.ES_PID)
        {
            evaluation.MovePidSelection(0, -1);
        }
    }

    private void OnThumbstickDown()
    {
        Debug.Log("Thumbstick DOWN");
        
        if (evaluation.State == ExperimentState.ES_PID)
        {
            evaluation.MovePidSelection(0, 1);
        }

    }
}
