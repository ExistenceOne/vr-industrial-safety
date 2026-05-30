using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Blends hand finger bones between an open pose and a fist pose
/// based on an analog controller input (trigger or grip).
/// If a GrabbedObject is assigned, the fist is held at 1 for as long
/// as that object is selected, regardless of the raw input value.
///
/// Setup:
/// 1. Attach this component to the hand model root (the SkinnedMeshRenderer parent).
/// 2. Expand the hand skeleton in the Hierarchy and drag every finger bone
///    (all phalanges) into the Finger Bones list.
/// 3. Make sure the hand is in its natural open state, then right-click this
///    component → "Capture Open Pose from Scene" to record the open rotations.
/// 4. Manually rotate the bones to form a fist in the Scene view, then
///    right-click → "Capture Fist Pose from Scene".
/// 5. Undo the manual rotations so the hand goes back to open.
/// 6. Set Grip Input action name to match your XRI action ("Grip" or "Trigger").
/// 7. Drag the hammer's XRGrabInteractable into the Grabbed Object field.
/// </summary>
public class HandGripAnimator : MonoBehaviour
{
    [System.Serializable]
    public class FingerBone
    {
        public Transform bone;
        public Vector3 openEuler;
        public Vector3 fistEuler;
        [Tooltip("Index finger bones: driven by the trigger (activate) input while the object is held.")]
        public bool isIndexFinger;
    }

    [SerializeField] FingerBone[] m_FingerBones = new FingerBone[0];

    [SerializeField]
    XRInputValueReader<float> m_GripInput = new XRInputValueReader<float>("Grip");

    [Tooltip("Drives index finger curl while the object is held (trigger / activate button).")]
    [SerializeField]
    XRInputValueReader<float> m_TriggerInput = new XRInputValueReader<float>("Activate");

    [Tooltip("While this interactable is selected the hand is locked in the fist pose.")]
    [SerializeField] XRGrabInteractable m_GrabbedObject;

    [Tooltip("Speed at which the hand transitions between poses (higher = snappier).")]
    [SerializeField] float m_TransitionSpeed = 12f;

    float m_CurrentValue;
    float m_IndexValue;
    bool m_IsHolding;

    void OnEnable()
    {
        m_GripInput?.EnableDirectActionIfModeUsed();
        m_TriggerInput?.EnableDirectActionIfModeUsed();
        if (m_GrabbedObject != null)
        {
            m_GrabbedObject.selectEntered.AddListener(OnSelectEntered);
            m_GrabbedObject.selectExited.AddListener(OnSelectExited);
        }
    }

    void OnDisable()
    {
        m_GripInput?.DisableDirectActionIfModeUsed();
        m_TriggerInput?.DisableDirectActionIfModeUsed();
        if (m_GrabbedObject != null)
        {
            m_GrabbedObject.selectEntered.RemoveListener(OnSelectEntered);
            m_GrabbedObject.selectExited.RemoveListener(OnSelectExited);
        }
    }

    void OnSelectEntered(SelectEnterEventArgs args) => m_IsHolding = true;
    void OnSelectExited(SelectExitEventArgs args)   => m_IsHolding = false;

    void Update()
    {
        float gripTarget    = m_IsHolding ? 1f : (m_GripInput?.ReadValue() ?? 0f);
        float indexTarget   = m_IsHolding ? (m_TriggerInput?.ReadValue() ?? 0f) : gripTarget;

        m_CurrentValue = Mathf.MoveTowards(m_CurrentValue, gripTarget,  Time.deltaTime * m_TransitionSpeed);
        m_IndexValue   = Mathf.MoveTowards(m_IndexValue,   indexTarget, Time.deltaTime * m_TransitionSpeed);

        for (int i = 0; i < m_FingerBones.Length; i++)
        {
            var fb = m_FingerBones[i];
            if (fb.bone == null) continue;
            float t = (m_IsHolding && fb.isIndexFinger) ? m_IndexValue : m_CurrentValue;
            fb.bone.localRotation = Quaternion.Slerp(
                Quaternion.Euler(fb.openEuler),
                Quaternion.Euler(fb.fistEuler),
                t);
        }
    }

    [ContextMenu("Capture Open Pose from Scene")]
    void CaptureOpenPose()
    {
        for (int i = 0; i < m_FingerBones.Length; i++)
        {
            if (m_FingerBones[i].bone != null)
                m_FingerBones[i].openEuler = m_FingerBones[i].bone.localEulerAngles;
        }
        Debug.Log($"[HandGripAnimator] Open pose captured for {m_FingerBones.Length} bones.");
    }

    [ContextMenu("Capture Fist Pose from Scene")]
    void CaptureFistPose()
    {
        for (int i = 0; i < m_FingerBones.Length; i++)
        {
            if (m_FingerBones[i].bone != null)
                m_FingerBones[i].fistEuler = m_FingerBones[i].bone.localEulerAngles;
        }
        Debug.Log($"[HandGripAnimator] Fist pose captured for {m_FingerBones.Length} bones.");
    }
}
