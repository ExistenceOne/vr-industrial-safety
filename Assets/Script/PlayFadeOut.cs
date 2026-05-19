using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayFadeOut : MonoBehaviour
{
    public static PlayFadeOut Instance { get; private set; }

    [Tooltip("페이드아웃을 적용할 CanvasGroup이 달린 패널을 넣으세요")]
    public CanvasGroup FadePanel;
    private float fadeDuration = 2.0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /* 테스트
    private void Update()
    {
        // 스페이스바를 누르면 페이드아웃 연출 시작
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartFade();
        }
    }*/

    public void StartFade()
    {
        if (FadePanel == null)
        {
            UnityEngine.Debug.Log("FadePanel이 할당되지 않았습니다. 인스펙터를 확인해주세요.");
            return;
        }
        UnityEngine.Debug.Log("페이드아웃 연출을 시작합니다.");
        StartCoroutine(ProcessFadeCoroutine());
    }

    private IEnumerator ProcessFadeCoroutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Clamp01(elapsedTime / fadeDuration);

            FadePanel.alpha = currentAlpha;
            yield return null;
        }
        FadePanel.alpha = 1f;
        UnityEngine.Debug.Log("페이드아웃 연출이 완료되었습니다.");
    }
}