using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayFadeIn : MonoBehaviour
{
    public static PlayFadeIn instance { get; private set; }

    [Tooltip("페이드인을 적용할 CanvasGroup이 달린 패널을 넣으세요")]
    public CanvasGroup FadePanel;
    private float fadeDuration = 2.0f;
    private bool isFading = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        // 씬이 로딩되자마자 자동 실행
        StartFade();
    }

    private void StartFade()
    {
        if (FadePanel == null)
        {
            UnityEngine.Debug.LogWarning("FadePanel이 할당되지 않았습니다. 인스펙터를 확인해주세요.");
            return;
        }

        if (isFading) return;

        UnityEngine.Debug.Log("페이드인 연출을 시작합니다.");
        StartCoroutine(ProcessFadeCoroutine());
    }

    private IEnumerator ProcessFadeCoroutine()
    {
        isFading = true;
        FadePanel.alpha = 1f; // 안전장치: 시작할 때 완전히 검은 화면으로 강제 설정
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            // 현재 알파값을 1에서 0으로 서서히 줄어들도록 계산 (처음 1f - 현재 비율)
            float currentAlpha = 1f - Mathf.Clamp01(elapsedTime / fadeDuration);

            FadePanel.alpha = currentAlpha;
            yield return null;
        }

        FadePanel.alpha = 0f;
        isFading = false;
        UnityEngine.Debug.Log("페이드인 연출이 완료되었습니다.");
    }
}
