using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// XR 컨트롤러로 잡는 순간 아이템을 소비합니다. (SetActive false)
/// 보호구 아이템인 경우 SafetyPracticeManager에 착용 상태를 전달합니다.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class ConsumableItem : MonoBehaviour
{
    public event System.Action OnItemConsumed;

    [Header("Safety Gear Settings")]
    [SerializeField] private bool isSafetyGear = true;

    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log($"[ConsumableItem] {gameObject.name} 소비됨");

        if (isSafetyGear)
        {
            if (SafetyPracticeManager.Instance != null)
            {
                SafetyPracticeManager.Instance.EquipGlove();
                Debug.Log($"[ConsumableItem] {gameObject.name} 보호구 착용 처리 완료");
            }
            else
            {
                Debug.LogWarning("[ConsumableItem] SafetyPracticeManager.Instance가 없습니다.");
            }
        }

        OnItemConsumed?.Invoke();
        gameObject.SetActive(false);
    }
}