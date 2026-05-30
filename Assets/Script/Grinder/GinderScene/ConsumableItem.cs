using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// XR 컨트롤러로 잡는 순간 아이템을 소비합니다. (SetActive false)
/// 소비 시점을 다른 스크립트에서 감지하려면 OnItemConsumed 이벤트를 구독하세요.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class ConsumableItem : MonoBehaviour
{
    public event System.Action OnItemConsumed;

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
        OnItemConsumed?.Invoke();
        gameObject.SetActive(false);
    }
}
