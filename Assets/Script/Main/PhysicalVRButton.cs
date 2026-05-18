using UnityEngine;

public class RayVRButton : MonoBehaviour
{
    [Header("Button Color")]
    public Color normalColor = Color.red;
    public Color hoverColor = Color.yellow;
    public Color pressedColor = Color.green;

    [Header("Button Movement")]
    public Transform buttonVisual;
    public Vector3 pressedLocalOffset = new Vector3(0f, 0f, -0.02f);
    public float moveSpeed = 10f;

    [Header("Target Objects")]
    public GameObject screenObject;
    public GameObject modeSelectPanel;
    public GameObject attentionEffect;

    [Header("Button Sound")]
    public AudioSource audioSource;
    public AudioClip turnOnSound;
    public AudioClip turnOffSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("Option")]
    public bool toggleMode = true;

    [Header("Debug")]
    public bool showDebugLog = true;

    private Renderer buttonRenderer;
    private Vector3 initialLocalPosition;
    private Vector3 targetLocalPosition;

    private bool isPressed = false;

    private void Awake()
    {
        if (buttonVisual == null)
        {
            buttonVisual = transform;
            DebugLog("buttonVisual이 비어 있어서 자기 자신으로 설정됨");
        }

        buttonRenderer = buttonVisual.GetComponent<Renderer>();

        if (buttonRenderer == null)
        {
            Debug.LogWarning("[RayVRButton] buttonVisual에 Renderer가 없습니다.", this);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D 사운드

        initialLocalPosition = buttonVisual.localPosition;
        targetLocalPosition = initialLocalPosition;

        SetColor(normalColor);

        if (screenObject != null)
        {
            screenObject.SetActive(false);
            DebugLog("screenObject 시작 시 비활성화: " + screenObject.name);
        }
        else
        {
            Debug.LogWarning("[RayVRButton] screenObject가 연결되지 않았습니다.", this);
        }

        if (modeSelectPanel != null)
        {
            modeSelectPanel.SetActive(false);
            DebugLog("modeSelectPanel 시작 시 비활성화: " + modeSelectPanel.name);
        }
        else
        {
            Debug.LogWarning("[RayVRButton] modeSelectPanel이 연결되지 않았습니다.", this);
        }

        if (attentionEffect != null)
        {
            attentionEffect.SetActive(true);
            DebugLog("attentionEffect 연결됨: " + attentionEffect.name);
        }
    }

    private void Update()
    {
        if (buttonVisual == null) return;

        buttonVisual.localPosition = Vector3.Lerp(
            buttonVisual.localPosition,
            targetLocalPosition,
            Time.deltaTime * moveSpeed
        );
    }

    public void OnRayHoverEnter()
    {
        DebugLog("Ray Hover Enter");

        if (isPressed) return;

        SetColor(hoverColor);
    }

    public void OnRayHoverExit()
    {
        DebugLog("Ray Hover Exit");

        if (isPressed) return;

        SetColor(normalColor);
    }

    public void OnRaySelect()
    {
        DebugLog("Ray Select 입력 감지됨");

        if (toggleMode)
        {
            ToggleButton();
        }
        else
        {
            PressButton();
        }
    }

    private void ToggleButton()
    {
        if (isPressed)
        {
            ReleaseButton();
        }
        else
        {
            PressButton();
        }
    }

    private void PressButton()
    {
        isPressed = true;

        SetColor(pressedColor);

        targetLocalPosition = initialLocalPosition + pressedLocalOffset;

        PlaySound(turnOnSound);

        DebugLog("버튼 눌림 처리");

        if (screenObject != null)
        {
            screenObject.SetActive(true);
            DebugLog("screenObject 활성화됨: " + screenObject.name);
        }

        if (modeSelectPanel != null)
        {
            modeSelectPanel.SetActive(true);
            DebugLog("modeSelectPanel 활성화됨: " + modeSelectPanel.name);
        }

        if (attentionEffect != null)
        {
            attentionEffect.SetActive(false);
            DebugLog("attentionEffect 비활성화됨: " + attentionEffect.name);
        }
    }

    private void ReleaseButton()
    {
        isPressed = false;

        SetColor(normalColor);

        targetLocalPosition = initialLocalPosition;

        PlaySound(turnOffSound);

        DebugLog("버튼 해제 처리");

        if (screenObject != null)
        {
            screenObject.SetActive(false);
            DebugLog("screenObject 비활성화됨: " + screenObject.name);
        }

        if (modeSelectPanel != null)
        {
            modeSelectPanel.SetActive(false);
            DebugLog("modeSelectPanel 비활성화됨: " + modeSelectPanel.name);
        }

        if (attentionEffect != null)
        {
            attentionEffect.SetActive(true);
            DebugLog("attentionEffect 활성화됨: " + attentionEffect.name);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        audioSource.PlayOneShot(clip, soundVolume);
    }

    private void SetColor(Color color)
    {
        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = color;
            DebugLog("색상 변경됨: " + color);
        }
    }

    private void DebugLog(string message)
    {
        if (!showDebugLog) return;

        Debug.Log("[RayVRButton] " + message, this);
    }
}