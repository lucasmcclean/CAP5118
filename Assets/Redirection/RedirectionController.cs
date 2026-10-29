using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class RedirectionController : MonoBehaviour
{
    public Evaluation evaluation;
    public GameObject cameraRig;

    int e_lastTrial = -100;
    float c_yawOffset = 0;
    float e_maxAngle = 0;
    float e_maxDistance = 0;
    float e_virtualYaw = 0;

    const double DTOR = Mathf.PI / 180.0;

    public GameObject quad;
    public GameObject quad_user;
    public GameObject path;

    float c_pathYaw = 0;
    Vector3 c_pathPivot = new Vector3(0, 0, 0);
    Vector3 c_pathHomePos;
    Quaternion c_pathHomeRot;

    Texture2D pReadyQuestion_;
    Texture2D pQuestionR_;
    Texture2D pQuestionT_;
    Texture2D pQuestionC_;
    Texture2D pAdjust_;
    Texture2D pThankYou_;
    Texture2D pUser_;
    Texture2D pUserNoDir_;
    Texture2D pPleaseWait_;
    Texture2D p0_;
    Texture2D p1_;
    Texture2D p2_;
    Texture2D p3R_;
    Texture2D p3TC_;
    Texture2D p4_;
    Texture2D p4R_;
    Texture2D p5R_;
    Texture2D p5T_;
    Texture2D p5C_;
    Texture2D p6_;
    const int SLIDES_R = 5;
    Texture2D[] pSlideR_;
    const int SLIDES_T = 7;
    Texture2D[] pSlideT_;
    const int SLIDES_C = 7;
    Texture2D[] pSlideC_;

    GameObject questionUI_;
    Image[] questionBoxes_;
    Text[] questionLabels_;
    int[] questionValues_;
    readonly Color32 questionBoxColor_ = new Color32(229, 231, 235, 255);
    readonly Color32 questionBoxSelectedColor_ = new Color32(47, 111, 237, 255);

    // PID numpad UI
    GameObject pidUI_;
    Image[] pidKeys_;
    Text[] pidKeyLabels_;
    Text pidDisplay_;

    //NOTE: Not very efficient, but numerically stable
    float reducePeriodic(float p)
    {
        // Reduces periodic p from real number space to [0, 2PI[
        const float PI2 = 2 * (float)Mathf.PI;
        while (p >= PI2)
            p -= PI2;
        while (p < 0)
            p += PI2;
        return p;
    }

    //NOTE: Not very efficient, but numerically stable
    float periodicMinusPeriodic(float p1, float p2)
    {
        p1 = reducePeriodic(p1);
        p2 = reducePeriodic(p2);

        // p1 in [0, 2PI[
        // p2 in [0, 2PI[
        // result in [-PI, PI[

        // We make the assumption that there is only a slight difference.
        // This means that we can be in one of the following four cases:
        // 
        //        [0 . . . . . . . .2PI][0 . . . . . . . .2PI][0 . . . . . . . .2PI]
        // Case A:  p2            p1      p2            p1      p2            p1
        // Case B:  p1            p2      p1            p2      p1            p2
        // Case C:      p1    p2              p1    p2              p1    p2
        // Case C':     p2    p1              p2    p1              p2    p1

        const float PI2 = 2 * (float)Mathf.PI;
        float caseA = p1 - (p2 + PI2);
        float caseB = p1 - (p2 - PI2);
        float caseC = p1 - p2;
        float absA = Mathf.Abs(caseA);
        float absB = Mathf.Abs(caseB);
        float absC = Mathf.Abs(caseC);

        float tmp = 0;
        if (absA < absB)
        {
            if (absA < absC)
            {
                // a < b, a < c
                tmp = caseA;
            }
            else
            {
                // c <= a < b
                tmp = caseC;
            }
        }
        else
        {
            if (absC < absB)
            {
                // c < b <= a
                tmp = caseC;
            }
            else
            {
                // b <= a, b <= c
                tmp = caseB;
            }
        }
        return tmp;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        pReadyQuestion_ = Resources.Load<Texture2D>("Images/_exp_readyquestion");
        pQuestionR_ = Resources.Load<Texture2D>("Images/_exp_questionR");
        pQuestionT_ = Resources.Load<Texture2D>("Images/_exp_questionT");
        pQuestionC_ = Resources.Load<Texture2D>("Images/_exp_questionC");
        pAdjust_ = Resources.Load<Texture2D>("Images/_exp_adjust");
        pThankYou_ = Resources.Load<Texture2D>("Images/_exp_thankyou");
        pUser_ = Resources.Load<Texture2D>("Images/_exp_user");
        pUserNoDir_ = Resources.Load<Texture2D>("Images/_exp_user_no_dir");
        pPleaseWait_ = Resources.Load<Texture2D>("Images/_exp_pleasewait");
        p0_ = Resources.Load<Texture2D>("Images/_exp_slide_0");
        p1_ = Resources.Load<Texture2D>("Images/_exp_slide_1");
        p2_ = Resources.Load<Texture2D>("Images/_exp_slide_2");
        p3R_ = Resources.Load<Texture2D>("Images/_exp_slide_3R");
        p3TC_ = Resources.Load<Texture2D>("Images/_exp_slide_3T");
        p4_ = Resources.Load<Texture2D>("Images/_exp_slide_4");
        p4R_ = Resources.Load<Texture2D>("Images/_exp_slide_4R");
        p5R_ = Resources.Load<Texture2D>("Images/_exp_slide_5R");
        p5T_ = Resources.Load<Texture2D>("Images/_exp_slide_5T");
        p5C_ = Resources.Load<Texture2D>("Images/_exp_slide_5C");
        p6_ = Resources.Load<Texture2D>("Images/_exp_slide_6");
        pSlideR_ = new Texture2D[SLIDES_R];
        pSlideR_[0] = p0_;
        pSlideR_[1] = p3R_;
        pSlideR_[2] = p4R_;
        pSlideR_[3] = p5R_;
        pSlideR_[4] = p6_;
        pSlideT_ = new Texture2D[SLIDES_T];
        pSlideT_[0] = p0_;
        pSlideT_[1] = p1_;
        pSlideT_[2] = p2_;
        pSlideT_[3] = p3TC_;
        pSlideT_[4] = p4_;
        pSlideT_[5] = p5T_;
        pSlideT_[6] = p6_;
        pSlideC_ = new Texture2D[SLIDES_C];
        pSlideC_[0] = p0_;
        pSlideC_[1] = p1_;
        pSlideC_[2] = p2_;
        pSlideC_[3] = p3TC_;
        pSlideC_[4] = p4_;
        pSlideC_[5] = p5C_;
        pSlideC_[6] = p6_;

        c_pathHomePos = path.transform.localPosition;
        c_pathHomeRot = path.transform.localRotation;
        ApplyPathYaw(c_pathYaw);

        BuildQuestionScale();
        BuildPidPad();
    }

    private void BuildQuestionScale()
    {
        questionUI_ = new GameObject("QuestionScale");
        questionUI_.transform.SetParent(quad.transform, false);

        Canvas canvas = questionUI_.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = questionUI_.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(800f, 600f);
        canvasRect.localPosition = new Vector3(0f, 0f, -0.01f);
        canvasRect.localRotation = Quaternion.identity;
        canvasRect.localScale = Vector3.one * (1f / 800f);

        OVRCameraRig rig = cameraRig.GetComponent<OVRCameraRig>();
        if (rig != null)
        {
            Camera eyeCamera = rig.centerEyeAnchor.GetComponent<Camera>();
            if (eyeCamera != null)
                canvas.worldCamera = eyeCamera;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        bool showLabels = evaluation.EXP_TYPE == ExperimentType.ET_CURVATURE;
        int optionCount = Evaluation.RESPONSE_MAX - Evaluation.RESPONSE_MIN + (showLabels ? 0 : 1);
        questionBoxes_ = new Image[optionCount];
        questionLabels_ = new Text[optionCount];
        questionValues_ = new int[optionCount];

        float boxSize = showLabels ? 96f : 76f;
        float boxGap = showLabels ? 32f : 12f;
        float boxY = showLabels ? -120f : -145f;

        float totalWidth = optionCount * boxSize + (optionCount - 1) * boxGap;
        float firstX = -totalWidth * 0.5f + boxSize * 0.5f;

        int optionIndex = 0;
        for (int value = Evaluation.RESPONSE_MIN; value <= Evaluation.RESPONSE_MAX; value++)
        {
            if (showLabels && value == 0)
                continue;

            float boxX = firstX + optionIndex * (boxSize + boxGap);

            GameObject boxGO = new GameObject("Answer " + value, typeof(RectTransform), typeof(Image));
            boxGO.transform.SetParent(questionUI_.transform, false);

            RectTransform boxRect = boxGO.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.anchoredPosition = new Vector2(boxX, boxY);
            boxRect.sizeDelta = new Vector2(boxSize, boxSize);

            Image boxImage = boxGO.GetComponent<Image>();
            boxImage.raycastTarget = false;
            boxImage.color = questionBoxColor_;
            questionBoxes_[optionIndex] = boxImage;

            Outline outline = boxGO.AddComponent<Outline>();
            outline.effectColor = new Color32(40, 40, 40, 255);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(boxGO.transform, false);

            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelGO.GetComponent<Text>();
            label.font = font;
            label.fontSize = 46;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.color = Color.black;
            label.text = value.ToString();
            questionLabels_[optionIndex] = label;
            questionValues_[optionIndex] = value;

            if (showLabels)
            {
                GameObject dirGO = new GameObject("Direction " + value, typeof(RectTransform), typeof(Text));
                dirGO.transform.SetParent(questionUI_.transform, false);

                RectTransform dirRect = dirGO.GetComponent<RectTransform>();
                dirRect.anchorMin = new Vector2(0.5f, 0.5f);
                dirRect.anchorMax = new Vector2(0.5f, 0.5f);
                dirRect.pivot = new Vector2(0.5f, 0.5f);
                dirRect.anchoredPosition = new Vector2(boxX, boxY - boxSize * 0.5f - 20f);
                dirRect.sizeDelta = new Vector2(140f, 34f);

                Text dirLabel = dirGO.GetComponent<Text>();
                dirLabel.font = font;
                dirLabel.fontSize = 18;
                dirLabel.fontStyle = FontStyle.Bold;
                dirLabel.alignment = TextAnchor.MiddleCenter;
                dirLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                dirLabel.verticalOverflow = VerticalWrapMode.Overflow;
                dirLabel.raycastTarget = false;
                dirLabel.color = new Color32(40, 40, 40, 255);
                dirLabel.text = GetResponseLabel(value);
            }

            optionIndex++;
        }

        CreateQuestionText(
            "Hint",
            "Flick stick left / right to select, then press A",
            new Vector2(0f, -250f),
            24,
            new Color32(70, 70, 70, 255),
            font);

        questionUI_.SetActive(false);
    }

    private void CreateQuestionText(
        string name,
        string text,
        Vector2 position,
        int fontSize,
        Color color,
        Font font)
    {
        GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(questionUI_.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = position;
        textRect.sizeDelta = new Vector2(760f, 40f);

        Text label = textGO.GetComponent<Text>();
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        label.color = color;
        label.text = text;
    }

    private void RefreshQuestionScale()
    {
        if (questionBoxes_ == null)
            return;

        for (int i = 0; i < questionBoxes_.Length; i++)
        {
            int value = questionValues_[i];
            bool selected = value == evaluation.ResponseSelection;

            questionBoxes_[i].color =
                selected ? questionBoxSelectedColor_ : questionBoxColor_;
            questionLabels_[i].color =
                selected ? Color.white : Color.black;
        }
    }

    private static string GetResponseLabel(int value)
    {
        switch (value)
        {
            case -3: return "very left";
            case -2: return "left";
            case -1: return "slightly left";
            case 1: return "slightly right";
            case 2: return "right";
            case 3: return "very right";
            default: return "";
        }
    }

    // ---- PID numpad ----

    private void BuildPidPad()
    {
        pidUI_ = new GameObject("PidPad");
        pidUI_.transform.SetParent(quad.transform, false);

        Canvas canvas = pidUI_.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = pidUI_.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(800f, 600f);
        canvasRect.localPosition = new Vector3(0f, 0f, -0.01f);
        canvasRect.localRotation = Quaternion.identity;
        canvasRect.localScale = Vector3.one * (1f / 800f);

        OVRCameraRig rig = cameraRig.GetComponent<OVRCameraRig>();
        if (rig != null)
        {
            Camera eyeCamera = rig.centerEyeAnchor.GetComponent<Camera>();
            if (eyeCamera != null)
                canvas.worldCamera = eyeCamera;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(pidUI_.transform, false);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgGO.GetComponent<Image>();
        bgImage.raycastTarget = false;
        bgImage.color = new Color32(245, 246, 248, 255);

        CreatePidText("Title", "Enter Participant ID", new Vector2(0f, 240f), 40, FontStyle.Bold,
            new Color32(30, 30, 30, 255), font, new Vector2(760f, 60f));
        pidDisplay_ = CreatePidText("Display", "_", new Vector2(0f, 160f), 56, FontStyle.Bold,
            new Color32(47, 111, 237, 255), font, new Vector2(760f, 70f));
        CreatePidText("Hint", "Flick stick to move, press A to select", new Vector2(0f, -275f), 24,
            FontStyle.Normal, new Color32(70, 70, 70, 255), font, new Vector2(760f, 40f));

        string[] labels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "Del", "0", "OK" };
        pidKeys_ = new Image[12];
        pidKeyLabels_ = new Text[12];

        const float keySize = 80f;
        const float keyGap = 10f;
        const float topY = 70f;

        for (int i = 0; i < 12; i++)
        {
            int col = i % 3;
            int row = i / 3;

            GameObject keyGO = new GameObject("Key " + labels[i], typeof(RectTransform), typeof(Image));
            keyGO.transform.SetParent(pidUI_.transform, false);

            RectTransform keyRect = keyGO.GetComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0.5f, 0.5f);
            keyRect.anchorMax = new Vector2(0.5f, 0.5f);
            keyRect.pivot = new Vector2(0.5f, 0.5f);
            keyRect.anchoredPosition = new Vector2((col - 1) * (keySize + keyGap), topY - row * (keySize + keyGap));
            keyRect.sizeDelta = new Vector2(keySize, keySize);

            Image keyImage = keyGO.GetComponent<Image>();
            keyImage.raycastTarget = false;
            keyImage.color = questionBoxColor_;
            pidKeys_[i] = keyImage;

            Outline outline = keyGO.AddComponent<Outline>();
            outline.effectColor = new Color32(40, 40, 40, 255);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(keyGO.transform, false);
            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelGO.GetComponent<Text>();
            label.font = font;
            label.fontSize = labels[i].Length > 1 ? 30 : 46;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.color = Color.black;
            label.text = labels[i];
            pidKeyLabels_[i] = label;
        }

        pidUI_.SetActive(false);
    }

    private Text CreatePidText(string name, string text, Vector2 position, int fontSize,
        FontStyle style, Color color, Font font, Vector2 size)
    {
        GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(pidUI_.transform, false);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text label = textGO.GetComponent<Text>();
        label.font = font;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        label.color = color;
        label.text = text;
        return label;
    }

    private void RefreshPidPad()
    {
        if (pidKeys_ == null)
            return;

        pidDisplay_.text = evaluation.ParticipantID.Length == 0 ? "_" : evaluation.ParticipantID;

        for (int i = 0; i < pidKeys_.Length; i++)
        {
            bool selected = i == evaluation.PidSelectedKey;
            pidKeys_[i].color = selected ? questionBoxSelectedColor_ : questionBoxColor_;
            pidKeyLabels_[i].color = selected ? Color.white : Color.black;
        }
    }



    private void OnEnable() => cameraRig.GetComponent<OVRCameraRig>().UpdatedAnchors += OnUpdatedAnchors;
    private void OnDisable() => cameraRig.GetComponent<OVRCameraRig>().UpdatedAnchors -= OnUpdatedAnchors;

    private void OnUpdatedAnchors(OVRCameraRig rig)
    {
        Transform centerEye = rig.centerEyeAnchor;
        Transform leftEye = rig.leftEyeAnchor;
        Transform rightEye = rig.rightEyeAnchor;
        Vector3 leftOffset = leftEye.localPosition - centerEye.localPosition;
        Vector3 rightOffset = rightEye.localPosition - centerEye.localPosition;

        evaluation.outsideYaw = centerEye.transform.eulerAngles.y * Mathf.Deg2Rad;
        evaluation.outsidePos = new Vector2(centerEye.localPosition.x, centerEye.localPosition.z);
        evaluation.insideUpdate = true;

        UpdateInternal();

        Quaternion correctionRot = Quaternion.Euler(new Vector3(0, (evaluation.offsetYaw + evaluation.resetYaw) * Mathf.Rad2Deg, 0));
        centerEye.localRotation = correctionRot * centerEye.localRotation;

        correctionRot = Quaternion.Euler(new Vector3(0, evaluation.resetYaw * Mathf.Rad2Deg, 0));
        Vector3 centerPosOld = centerEye.localPosition;
        Vector3 centerPosNew = centerPosOld + new Vector3(evaluation.offsetPos.x, 0, evaluation.offsetPos.y) + new Vector3(evaluation.resetPos.x, 0, evaluation.resetPos.y);
        centerEye.localPosition = correctionRot * centerPosNew;

        if (evaluation.State == ExperimentState.ES_INSTRUCTION)
        {
            centerEye.localPosition = new Vector3(0, 1000.0f, 0);
        }

        //if (leftEye != null)
        //{
        //    leftEye.localPosition = centerPosNew + (correctionRot * leftOffset);
        //    leftEye.localRotation = correctionRot * leftEye.localRotation;
        //}
        //if (rightEye != null)
        //{
        //    rightEye.localPosition = centerPosNew + (correctionRot * rightOffset);
        //    rightEye.localRotation = correctionRot * rightEye.localRotation;
        //}

        evaluation.insideUpdate = false;
        Debug.Log("pos(" + evaluation.getPos().x + ", " + evaluation.getPos().y + ") -- offsetPos(" + evaluation.offsetPos.x + ", " + evaluation.offsetPos.y + ")");

        //        Debug.Log("yaw(" + evaluation.getYaw() / DTOR + ") -- yaw(" + evaluation.start_yaw / DTOR + ") -- offset(" + evaluation.offsetYaw / DTOR + ") -- reset(" + evaluation.resetYaw / DTOR + ")");
    }
    
    private void Update()
    {
    }

    private void ApplyPathYaw(float yaw)
    {
        c_pathYaw = reducePeriodic(yaw);

        Quaternion rot = Quaternion.Euler(new Vector3(0, c_pathYaw * Mathf.Rad2Deg, 0));
        path.transform.localPosition = rot * (c_pathHomePos - c_pathPivot);
        path.transform.localRotation = rot * c_pathHomeRot;
    }

    public void RotateWorld(float deltaYaw)
    {
        ApplyPathYaw(c_pathYaw + deltaYaw);

        Quaternion rot = Quaternion.Euler(new Vector3(0, deltaYaw * Mathf.Rad2Deg, 0));

        if (quad != null)
            quad.transform.localRotation = rot * quad.transform.localRotation;
        if (quad_user != null)
            quad_user.transform.localRotation = rot * quad_user.transform.localRotation;
    }

    private void UpdateInternal()
    {
        if (questionUI_ != null)
            questionUI_.SetActive(evaluation.State == ExperimentState.ES_QUESTION);

         if (pidUI_ != null)
            pidUI_.SetActive(evaluation.State == ExperimentState.ES_PID);


        switch (evaluation.State)
        {
            case ExperimentState.ES_UNDEFINED:
                quad_user.GetComponent<Renderer>().enabled = false;
                quad.GetComponent<Renderer>().enabled = true;
                quad.GetComponent<Renderer>().material.mainTexture = pPleaseWait_;
                break;

            case ExperimentState.ES_READYQUESTION:
                quad_user.GetComponent<Renderer>().enabled = false;
                quad.GetComponent<Renderer>().enabled = true;
                quad.GetComponent<Renderer>().material.mainTexture = pReadyQuestion_;

                if (evaluation.EXP_TYPE == ExperimentType.ET_ROTATION)
                    path.GetComponent<Renderer>().enabled = false;
                else
                    path.GetComponent<Renderer>().enabled = true;
                break;
            
            case ExperimentState.ES_PID:
                quad_user.GetComponent<Renderer>().enabled = false;
                quad.GetComponent<Renderer>().enabled = false;
                RefreshPidPad();
                break;

            case ExperimentState.ES_SLIDES:
                {
                    quad_user.GetComponent<Renderer>().enabled = false;
                    quad.GetComponent<Renderer>().enabled = true;

                    if (evaluation.SKIP_SLIDES)
                    {
                        evaluation.StateChange();
                        break;
                    }

                    int iNow = (int)(Time.realtimeSinceStartup * 1000f); // in ms
                    int elapsed = iNow - (int)evaluation.StartTime; // in ms
                    int slide = elapsed / 14000; //NITE: value seems okay
                    if (evaluation.EXP_TYPE == ExperimentType.ET_ROTATION)
                    {
                        if (slide > SLIDES_R - 1)
                            evaluation.StateChange();
                        else
                            quad.GetComponent<Renderer>().material.mainTexture = pSlideR_[slide];
                    }
                    else if (evaluation.EXP_TYPE == ExperimentType.ET_TRANSLATION)
                    {
                        if (slide > SLIDES_T - 1)
                            evaluation.StateChange();
                        else
                            quad.GetComponent<Renderer>().material.mainTexture = pSlideT_[slide];
                    }
                    else // ET_CURVATURE
                    {
                        if (slide > SLIDES_C - 1)
                            evaluation.StateChange();
                        else
                            quad.GetComponent<Renderer>().material.mainTexture = pSlideC_[slide];
                    }
                }
                break;

            case ExperimentState.ES_ACTIVE:
                {
                    quad_user.GetComponent<Renderer>().enabled = false;
                    quad.GetComponent<Renderer>().enabled = false;

                    // Get current gain value
                    float gain = evaluation.ActiveTrial.Value;

                    if (evaluation.EXP_TYPE == ExperimentType.ET_ROTATION)
                    {
                        // Random parts
                        if (evaluation.ActiveTrialNumber != e_lastTrial)
                        {
                            e_lastTrial = evaluation.ActiveTrialNumber;

                            // Orientation (randomized in 360 degrees)
                            float r1 = UnityEngine.Random.value; // in [0,1]
                            evaluation.recenterPose(r1 * (float)Mathf.PI * 2);
                            RotateWorld(r1 * (float)Mathf.PI * 2);

                            evaluation.start_p = evaluation.getPos();
                            evaluation.start_yaw = evaluation.getYaw();

                            // Turn angle
                            float r2 = UnityEngine.Random.value; // in [0,1]
                            e_maxAngle = evaluation.ANGLE - (float)Mathf.PI / 8 + (float)Mathf.PI / 8 * r2;
                            //e_maxAngle = evaluation.ANGLE;

                            //                            Debug.Log("TRIAL(" + evaluation.ActiveTrialNumber + ")_START: yaw(" + evaluation.getYaw() / DTOR + ") -- yaw(" + periodicMinusPeriodic(evaluation.getYaw(), evaluation.start_yaw) / DTOR + ") * gain(" + gain + ") < angle(" + e_maxAngle / DTOR + ")");
                        }

                        // Compute yaw angle
                        evaluation.reset();
                        float diff = periodicMinusPeriodic(evaluation.getYaw(), evaluation.start_yaw);
                        float yaw = evaluation.start_yaw + diff * gain;

                        // Update orientation
                        evaluation.setYawOffset(yaw);

                        //                        Debug.Log("TRIAL(" + evaluation.ActiveTrialNumber + ")_MID: yaw(" + diff / DTOR + ") * gain(" + gain + ") ?? angle(" + e_maxAngle / DTOR + ")");

                        // Check angle
                        if (Mathf.Abs(diff) * gain >= e_maxAngle)
                        {
                            Debug.Log("TRIAL(" + evaluation.ActiveTrialNumber + ")_END: yaw(" + diff / DTOR + ") * gain(" + gain + ") > angle(" + e_maxAngle / DTOR + ")");
                            //                            Debug.Log("--- statechange: start_yaw=" + evaluation.getYaw() / DTOR + " ; yaw=" + evaluation.start_yaw / DTOR + " ; diff=" + diff / DTOR);

                            evaluation.StateChange();
                        }

                        evaluation.writeLog(0, 0, 0, 0, diff / (float)DTOR, diff * gain / (float)DTOR);
                    }
                    else if (evaluation.EXP_TYPE == ExperimentType.ET_TRANSLATION)
                    {
                        // Random parts
                        if (evaluation.ActiveTrialNumber != e_lastTrial)
                        {
                            e_lastTrial = evaluation.ActiveTrialNumber;

                            //evaluation.recenterPose();

                            // Orientation (randomized in 360 degrees)
                            //float r1 = UnityEngine.Random.value; // in [0,1]
                            //evaluation.recenterPose(r1 * (float)Mathf.PI * 2);
                            //RotateWorld(r1 * (float)Mathf.PI * 2);

                            //evaluation.start_p = evaluation.getPos();
                            //evaluation.start_yaw = evaluation.getYaw();

                            // Turn angle
                            float r2 = UnityEngine.Random.value; // in [0,1]
                            e_maxDistance = evaluation.DISTANCE - 0.5f + 0.5f * r2;
                        }

                        // Compute translation distance
                        //NOTE: Assumes participants walk (0,0) -> (0,e_maxDistance)
                        evaluation.reset();

                        Quaternion correctionRot = Quaternion.Euler(new Vector3(0, evaluation.resetYaw * Mathf.Rad2Deg, 0));
                        Vector3 start = correctionRot * new Vector3(evaluation.start_p.x, 0, evaluation.start_p.y);
                        Vector3 pos = correctionRot * new Vector3(evaluation.getPos().x, 0, evaluation.getPos().y);

                        Vector3 diff = new Vector3(pos.x - start.x, 0, pos.z - start.z);

                        Quaternion correctionRotInv = Quaternion.Euler(new Vector3(0, -evaluation.resetYaw * Mathf.Rad2Deg, 0));

                        Vector3 pos_new = new Vector3(evaluation.start_p.x, 0, evaluation.start_p.y) + (correctionRotInv * new Vector3(diff.x, 0, diff.z * gain));

                        // Update position
                        evaluation.setPosX(pos_new.x);
                        evaluation.setPosY(pos_new.z);

                        // Check angle
                        if (Mathf.Abs(diff.z) * gain >= e_maxDistance)
                        {
                            evaluation.StateChange();
                        }

//                        evaluation.writeLog(evaluation.getPos().x, evaluation.start_p.y + diff, evaluation.getPos().x, evaluation.start_p.y + diff * gain, evaluation.getYaw() / (float)DTOR, evaluation.getYaw() / (float)DTOR);
                    }
                    else // Curvature
                    {
                        // Random parts
                        if (evaluation.ActiveTrialNumber != e_lastTrial)
                        {
                            e_lastTrial = evaluation.ActiveTrialNumber;

                            // Orientation (randomized in 360 degrees)
                            float r1 = UnityEngine.Random.value;
                            e_virtualYaw = r1 * (float)Mathf.PI * 2;
                            evaluation.ActiveTrial.VirtualYaw = e_virtualYaw;
                            evaluation.recenterPose(e_virtualYaw);
                            ApplyPathYaw(e_virtualYaw);
                        }

                        // Get curvature gain radius and direction
                        float r = Mathf.Abs(evaluation.ActiveTrial.Value);
                        bool leftTurn = (evaluation.ActiveTrial.Value < 0);

                        evaluation.reset();

                        float canonicalYaw = evaluation.resetYaw - e_virtualYaw;

                        Quaternion correctionRot = Quaternion.Euler(new Vector3(0, canonicalYaw * Mathf.Rad2Deg, 0));
                        Vector3 start = correctionRot * new Vector3(evaluation.start_p.x, 0, evaluation.start_p.y);
                        Vector3 pos = correctionRot * new Vector3(evaluation.getPos().x, 0, evaluation.getPos().y);

                        Vector3 diff = new Vector3(pos.x - start.x, 0, pos.z - start.z);

                        // Get current 2D position
                        Vector2 p = new Vector2(diff.x, diff.z);

                        // Compute redirection
                        Vector2 pt = new Vector2(0, 0);
                        float theta = 0;
                        Vector2 c;
                        if (leftTurn)
                        {
                            c = new Vector2(-r, 0);
                            Vector2 tmp = (p - c) / (p - c).magnitude;
                            theta = Mathf.Acos(Mathf.Abs(tmp.x));
//                            Debug.Log("acos(" + Mathf.Abs(tmp.x) + ")= " + Mathf.Acos(Mathf.Abs(tmp.x)));
                            float sy = theta * r;
                            float sx = (p - c).magnitude - r;
                            pt = new Vector2(sx, sy);
                        }
                        else
                        {
                            c = new Vector2(r, 0);
                            Vector2 tmp = (p - c) / (p - c).magnitude;
                            theta = Mathf.Acos(Mathf.Abs(tmp.x));
                            float sy = theta * r;
                            float sx = r - (p - c).magnitude;
                            pt = new Vector2(sx, sy);
                        }

                        float yaw = 0;
                        if (leftTurn)
                            yaw = evaluation.getYaw() + theta;
                        else
                            yaw = evaluation.getYaw() - theta;

                        // Make sure we do not change anything if p.y<0
                        if (p.y > 0)
                        {
                            Quaternion correctionRotInv = Quaternion.Euler(new Vector3(0, -canonicalYaw * Mathf.Rad2Deg, 0));
                            Vector3 pos_new = new Vector3(evaluation.start_p.x, 0, evaluation.start_p.y) + (correctionRotInv * new Vector3(pt.x, 0, pt.y));

                            // Update position
                            evaluation.setPosX(pos_new.x);
                            evaluation.setPosY(pos_new.z);

                            // Update orientation
                            evaluation.setYawOffset(yaw);

                            if (theta > 3.1415f / 2)
                                Debug.Log("ERROR -- EXCEEDED");
                            Debug.Log("left(" + leftTurn + ") -- center(" +c.x+ ", " +c.y+ ")  -- diff.x(" + diff.x + ") -- diff.z(" + diff.z + ") -- pt.x(" + pt.x + ") -- pt.z(" + pt.y + ") -- theta(" + theta / (float)DTOR + ") -- offsetYaw(" + evaluation.offsetYaw / (float)DTOR + ")");

                            float realYaw = periodicMinusPeriodic(evaluation.getYaw(), evaluation.start_yaw);
                            evaluation.writeLog(
                                p.x,
                                p.y,
                                pt.x,
                                pt.y,
                                realYaw / (float)DTOR,
                                theta / (float)DTOR,
                                false);

                            // Check walk distance
                            if (Mathf.Abs(pt.y) >= evaluation.DISTANCE)
                            {
                                evaluation.StateChange();
                            }
                        }

                        //// Get current 2D position
                        //Vector2 p = evaluation.getPos();

                        //// Compute redirection
                        //Vector2 pt = new Vector2(0, 0);
                        //float theta = 0;
                        //if (leftTurn)
                        //{
                        //    Vector2 c = new Vector2(-r, 0);
                        //    Vector2 tmp = (p - c) / (p - c).magnitude;
                        //    theta = Mathf.Acos(Mathf.Abs(tmp.x));
                        //    float sy = theta * r;
                        //    float sx = (p - c).magnitude - r;
                        //    pt = new Vector2(sx, sy);
                        //}
                        //else
                        //{
                        //    Vector2 c = new Vector2(r, 0);
                        //    Vector2 tmp = (p - c) / (p - c).magnitude;
                        //    theta = Mathf.Acos(Mathf.Abs(tmp.x));
                        //    float sy = theta * r;
                        //    float sx = r - (p - c).magnitude;
                        //    pt = new Vector2(sx, sy);
                        //}

                        //float yaw = 0;
                        //if (leftTurn)
                        //    yaw = evaluation.getYaw() - theta;
                        //else
                        //    yaw = evaluation.getYaw() + theta;

                        //// Make sure we do not change anything if p.y<0
                        //if (p.y < 0)
                        //{
                        //    pt = p;
                        //    yaw = evaluation.getYaw();
                        //}

                        //// Update position
                        //evaluation.setPosX(pt.x);
                        //evaluation.setPosY(pt.y);

                        //// Update orientation
                        //evaluation.setYawOffset(yaw);

                        //// Check distance
                        //float distance = evaluation.getPos().y;
                        //if (distance >= evaluation.DISTANCE)
                        //{
                        //    evaluation.StateChange();
                        //}

                        //// Vary virtual start orientation
                        //int currentTrial = evaluation.ActiveTrialNumber;
                        //if (currentTrial != e_lastTrial)
                        //{
                        //    e_lastTrial = currentTrial;
                        //    float rr = UnityEngine.Random.value; // in [0,1]
                        //    if (rr < 0.25f)
                        //        c_yawOffset = 0;
                        //    else if (rr < 0.5f)
                        //        c_yawOffset = (float)Mathf.PI / 2;
                        //    else if (rr < 0.75f)
                        //        c_yawOffset = (float)Mathf.PI;
                        //    else
                        //        c_yawOffset = (float)Mathf.PI * 3 / 2;
                        //}
                        //evaluation.setYawOffset(evaluation.getYaw() + c_yawOffset);
                        //Vector2 ppp = evaluation.getPos();
                        //evaluation.setPosX(ppp.x * Mathf.Cos(c_yawOffset) + ppp.y * -Mathf.Sin(c_yawOffset));
                        //evaluation.setPosY(ppp.x * Mathf.Sin(c_yawOffset) + ppp.y * Mathf.Cos(c_yawOffset));

                        //TODO
                        //evaluation.writeLog(orig_pos.x, evaluation.getPos(), orig_yaw, evaluation.getYaw());
                    }
                }
                break;

            case ExperimentState.ES_QUESTION:
                {
                    quad_user.GetComponent<Renderer>().enabled = false;
                    quad.GetComponent<Renderer>().enabled = true;
                    if (evaluation.EXP_TYPE == ExperimentType.ET_ROTATION)
                        quad.GetComponent<Renderer>().material.mainTexture = pQuestionR_;
                    else if (evaluation.EXP_TYPE == ExperimentType.ET_TRANSLATION)
                        quad.GetComponent<Renderer>().material.mainTexture = pQuestionT_;
                    else // Curvature
                        quad.GetComponent<Renderer>().material.mainTexture = pQuestionC_;

                    RefreshQuestionScale();
                }
                break;

            case ExperimentState.ES_INSTRUCTION:
                {
                    quad_user.GetComponent<Renderer>().enabled = true;
                    quad.GetComponent<Renderer>().enabled = true;

                    if (evaluation.EXP_TYPE == ExperimentType.ET_ROTATION)
                    {
                        evaluation.StateChange();
                        break;
                    }

                    // Relative coordinates for reset
                    Vector2 startP_rel = evaluation.start_p + evaluation.resetPos;
                    Vector2 pos_rel = evaluation.getPos() + evaluation.resetPos;
                    float yaw_rel = evaluation.getYaw() + evaluation.resetYaw - e_virtualYaw;

                    // Compute coordinate system
                    float distance = startP_rel.magnitude;
                    Vector2 dir = startP_rel / distance;
                    Vector2 strafe = new Vector2(dir.y, -dir.x);

                    // Compute current 2D position
                    Vector2 p = pos_rel;
                    p = p / distance;
                    float dd = dir.x * p.x + dir.y * p.y;
                    float ds = strafe.x * p.x + strafe.y * p.y;

                    // Compute screen position
                    float x = ds / 10.0f;
                    float y = dd / 10.0f;
                    float markX = x;
                    float markY = y;

                    // Draw background
                    quad.GetComponent<Renderer>().material.mainTexture = pAdjust_;

                    // Check if user is close to center
                    const float POS_THRESHOLD_BROAD = 0.5f;
                    const float POS_THRESHOLD_FINE = 0.15f;
                    if (pos_rel.magnitude < POS_THRESHOLD_BROAD)
                    {
                        // Draw user marker
                        float theta = yaw_rel;
                        quad_user.GetComponent<Renderer>().material.mainTexture = pUser_;
                        quad_user.transform.localPosition = new Vector3(pos_rel.x * 0.25f, pos_rel.y * 0.25f, 0.5f);
                        quad_user.transform.localEulerAngles = new Vector3(0, 0, -theta / (float)DTOR);

                        // Check if user is close to start orientation
                        const float ORI_THRESHOLD = 5.0f * (float)DTOR;
                        if (Mathf.Abs(periodicMinusPeriodic(yaw_rel, 0)) < ORI_THRESHOLD && pos_rel.magnitude < POS_THRESHOLD_FINE)
                            evaluation.StateChange();
                    }
                    else
                    {
                        // Compute rotation angle
                        float angle = Mathf.Acos(Mathf.Abs(dir.y));
                        if (dir.x < 0)
                            angle *= -1;

                        float theta = yaw_rel - angle;

                        // Draw user marker
                        /*if (evaluation.EXP_TYPE == ExperimentType.ET_CURVATURE)
                        {
                            Vector2 ppp = pos_rel - startP_rel;
                            float ddd = ppp.magnitude;
                            if (ddd > 1.0f)
                            {
                                quad_user.GetComponent<Renderer>().material.mainTexture = pUser_;
                                quad_user.transform.localPosition = new Vector3(markX, markY, 0.5f);
                                quad_user.transform.localEulerAngles = new Vector3(0, 0, -theta / (float)DTOR);
                            }
                            else
                            {
                                quad_user.GetComponent<Renderer>().material.mainTexture = pUserNoDir_;
                                quad_user.transform.localPosition = new Vector3(markX, markY, 0.5f);
                                quad_user.transform.localEulerAngles = new Vector3(0, 0, -theta / (float)DTOR);
                            }
                        }
                        else*/
                        {
                            quad_user.GetComponent<Renderer>().material.mainTexture = pUserNoDir_;
                            quad_user.transform.localPosition = new Vector3(markX, markY, 0.5f);
                            quad_user.transform.localEulerAngles = new Vector3(0, 0, -theta / (float)DTOR);
                        }
                    }
                }
                break;

            case ExperimentState.ES_THANKYOU:
                {
                    quad_user.GetComponent<Renderer>().enabled = false;
                    quad.GetComponent<Renderer>().enabled = true;
                    quad.GetComponent<Renderer>().material.mainTexture = pThankYou_;
                }
                break;
        }
    }
}
