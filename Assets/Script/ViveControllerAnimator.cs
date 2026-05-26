using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

/// <summary>
/// Drives Vive 1.5 controller sub-component transforms from XRI input actions.
/// Attach to the ViveController prefab root (or XR Controller Left/Right root).
/// Assign each Transform field to the matching child mesh GameObject.
/// All pivot values are taken from vr_controller_vive_1_5.json (parent body local space).
/// </summary>
public class ViveControllerAnimator : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] Transform m_Trigger;
    [SerializeField] float m_TriggerMaxAngle = -17f;
    [SerializeField] XRInputValueReader<float> m_TriggerInput = new XRInputValueReader<float>("Trigger");

    [Header("Grip")]
    [SerializeField] Transform m_LGrip;
    [SerializeField] Transform m_RGrip;
    [SerializeField] float m_LGripMaxAngle = 2f;
    [SerializeField] float m_RGripMaxAngle = -2f;
    [SerializeField] XRInputValueReader<float> m_GripInput = new XRInputValueReader<float>("Grip");

    [Header("Trackpad")]
    [SerializeField] Transform m_Trackpad;
    [SerializeField] Transform m_TrackpadTouch;
    [SerializeField] float m_TrackpadTiltX = 7f;
    [SerializeField] float m_TrackpadTiltY = 4f;
    [SerializeField] XRInputValueReader<Vector2> m_TrackpadInput = new XRInputValueReader<Vector2>("Trackpad");
    [SerializeField] XRInputValueReader<bool> m_TrackpadTouchInput = new XRInputValueReader<bool>("Trackpad Touch");

    [Header("Scroll Wheel")]
    [SerializeField] Transform m_ScrollWheel;
    [SerializeField] float m_ScrollWheelMaxAngle = -40f;

    [Header("Buttons")]
    [SerializeField] Transform m_SysButton;
    [SerializeField] Transform m_AppMenuButton;
    [SerializeField] float m_ButtonPressOffset = -0.00075f;
    [SerializeField] XRInputValueReader<bool> m_SysButtonInput = new XRInputValueReader<bool>("Menu Button");
    [SerializeField] XRInputValueReader<bool> m_AppMenuButtonInput = new XRInputValueReader<bool>("Primary Button");

    // Pivot points in parent (ViveController body) local space — from vr_controller_vive_1_5.json
    static readonly Vector3 k_TriggerPivot    = new Vector3( 0f,     -0.016f, 0.039f);
    static readonly Vector3 k_LGripPivot      = new Vector3(-0.019f, -0.006f, 0.075f);
    static readonly Vector3 k_RGripPivot      = new Vector3( 0.019f, -0.006f, 0.075f);
    static readonly Vector3 k_ScrollWheelPivot = new Vector3(0f,     -0.006f, 0.048f);

    // Cached initial local positions (all sub-component OBJs are placed at (0,0,0) since vertices are baked)
    Vector3 m_TriggerInitPos;
    Vector3 m_LGripInitPos;
    Vector3 m_RGripInitPos;
    Vector3 m_ScrollWheelInitPos;

    void Start()
    {
        if (m_Trigger)     m_TriggerInitPos     = m_Trigger.localPosition;
        if (m_LGrip)       m_LGripInitPos       = m_LGrip.localPosition;
        if (m_RGrip)       m_RGripInitPos       = m_RGrip.localPosition;
        if (m_ScrollWheel) m_ScrollWheelInitPos = m_ScrollWheel.localPosition;

        if (m_TrackpadTouch != null)
            m_TrackpadTouch.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        m_TriggerInput?.EnableDirectActionIfModeUsed();
        m_GripInput?.EnableDirectActionIfModeUsed();
        m_TrackpadInput?.EnableDirectActionIfModeUsed();
        m_TrackpadTouchInput?.EnableDirectActionIfModeUsed();
        m_SysButtonInput?.EnableDirectActionIfModeUsed();
        m_AppMenuButtonInput?.EnableDirectActionIfModeUsed();
    }

    void OnDisable()
    {
        m_TriggerInput?.DisableDirectActionIfModeUsed();
        m_GripInput?.DisableDirectActionIfModeUsed();
        m_TrackpadInput?.DisableDirectActionIfModeUsed();
        m_TrackpadTouchInput?.DisableDirectActionIfModeUsed();
        m_SysButtonInput?.DisableDirectActionIfModeUsed();
        m_AppMenuButtonInput?.DisableDirectActionIfModeUsed();
    }

    void Update()
    {
        AnimateTrigger();
        AnimateGrips();
        AnimateTrackpad();
        AnimateScrollWheel();
        AnimateButtons();
    }

    void AnimateTrigger()
    {
        if (m_Trigger == null) return;
        float val = m_TriggerInput?.ReadValue() ?? 0f;
        RotateAroundPivot(m_Trigger, m_TriggerInitPos, k_TriggerPivot,
            Vector3.right, Mathf.Lerp(0f, m_TriggerMaxAngle, val));
    }

    void AnimateGrips()
    {
        float val = m_GripInput?.ReadValue() ?? 0f;
        if (m_LGrip != null)
            RotateAroundPivot(m_LGrip, m_LGripInitPos, k_LGripPivot,
                Vector3.up, Mathf.Lerp(0f, m_LGripMaxAngle, val));
        if (m_RGrip != null)
            RotateAroundPivot(m_RGrip, m_RGripInitPos, k_RGripPivot,
                Vector3.up, Mathf.Lerp(0f, m_RGripMaxAngle, val));
    }

    void AnimateTrackpad()
    {
        Vector2 val = m_TrackpadInput?.ReadValue() ?? Vector2.zero;
        bool touching = m_TrackpadTouchInput?.ReadValue() ?? false;

        if (m_Trackpad != null)
            m_Trackpad.localRotation = Quaternion.Euler(
                -val.y * m_TrackpadTiltX, 0f, -val.x * m_TrackpadTiltY);

        if (m_TrackpadTouch != null)
        {
            m_TrackpadTouch.gameObject.SetActive(touching);
            if (touching && m_Trackpad != null)
                m_TrackpadTouch.localRotation = m_Trackpad.localRotation;
        }
    }

    void AnimateScrollWheel()
    {
        if (m_ScrollWheel == null) return;
        Vector2 val = m_TrackpadInput?.ReadValue() ?? Vector2.zero;
        // Map trackpad Y (-1..1) → scroll wheel angle (0..max)
        float angle = Mathf.Lerp(0f, m_ScrollWheelMaxAngle, (val.y + 1f) * 0.5f);
        RotateAroundPivot(m_ScrollWheel, m_ScrollWheelInitPos, k_ScrollWheelPivot,
            Vector3.right, angle);
    }

    void AnimateButtons()
    {
        if (m_SysButton != null)
        {
            bool pressed = m_SysButtonInput?.ReadValue() ?? false;
            var p = m_SysButton.localPosition;
            m_SysButton.localPosition = new Vector3(p.x, pressed ? m_ButtonPressOffset : 0f, p.z);
        }
        if (m_AppMenuButton != null)
        {
            bool pressed = m_AppMenuButtonInput?.ReadValue() ?? false;
            var p = m_AppMenuButton.localPosition;
            m_AppMenuButton.localPosition = new Vector3(p.x, pressed ? m_ButtonPressOffset : 0f, p.z);
        }
    }

    // Rotates transform t around a pivot point (all in parent local space).
    // initLocalPos is t's resting localPosition (cached from Start).
    static void RotateAroundPivot(Transform t, Vector3 initLocalPos, Vector3 pivot, Vector3 axis, float angle)
    {
        Quaternion rot = Quaternion.AngleAxis(angle, axis);
        t.localPosition = pivot + rot * (initLocalPos - pivot);
        t.localRotation = rot;
    }
}
