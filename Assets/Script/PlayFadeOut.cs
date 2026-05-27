using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayFadeOut : MonoBehaviour
{
    public static PlayFadeOut Instance { get; private set; }

    [Tooltip("���̵�ƿ��� ������ CanvasGroup�� �޸� �г��� ��������")]
    public CanvasGroup FadePanel;
    private float fadeDuration = 2.0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /* �׽�Ʈ
    private void Update()
    {
        // �����̽��ٸ� ������ ���̵�ƿ� ���� ����
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartFade();
        }
    }*/

    public void StartFade()
    {
        if (FadePanel == null)
        {
            UnityEngine.Debug.Log("FadePanel�� �Ҵ���� �ʾҽ��ϴ�. �ν����͸� Ȯ�����ּ���.");
            return;
        }
        UnityEngine.Debug.Log("���̵�ƿ� ������ �����մϴ�.");
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
        UnityEngine.Debug.Log("���̵�ƿ� ������ �Ϸ�Ǿ����ϴ�.");
    }
}