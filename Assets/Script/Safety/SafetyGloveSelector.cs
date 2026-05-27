using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class SafetyGloveSelector : MonoBehaviour
{
    [Header("Glove Visual")]
    [SerializeField] private GameObject gloveObject;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.2f;
    [SerializeField] private bool deactivateAfterFade = true;

    [Header("Success Sound")]
    [SerializeField] private AudioClip successSoundClip;

    [Range(0f, 1f)]
    [SerializeField] private float successSoundVolume = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;

    private Renderer[] gloveRenderers;
    private Collider[] gloveColliders;

    private Material[][] runtimeMaterials;
    private Color[][] originalColors;

    private bool isSelected = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = successSoundVolume;

        if (gloveObject == null)
        {
            gloveObject = gameObject;
        }

        gloveRenderers = gloveObject.GetComponentsInChildren<Renderer>(true);
        gloveColliders = gloveObject.GetComponentsInChildren<Collider>(true);

        CacheMaterials();
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGloveGrabbed);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGloveGrabbed);
    }

    private void OnGloveGrabbed(SelectEnterEventArgs args)
    {
        if (isSelected)
            return;

        isSelected = true;

        if (SafetyPracticeManager.Instance != null)
        {
            SafetyPracticeManager.Instance.EquipGlove();
        }

        PlaySuccessSound();

        DisableInteraction();

        StartCoroutine(FadeOutGloveRoutine());

        if (showDebugLog)
        {
            Debug.Log("[SafetyGloveSelector] 보호장갑 선택됨. 투명화 시작.");
        }
    }

    private void CacheMaterials()
    {
        if (gloveRenderers == null || gloveRenderers.Length == 0)
            return;

        runtimeMaterials = new Material[gloveRenderers.Length][];
        originalColors = new Color[gloveRenderers.Length][];

        for (int i = 0; i < gloveRenderers.Length; i++)
        {
            runtimeMaterials[i] = gloveRenderers[i].materials;
            originalColors[i] = new Color[runtimeMaterials[i].Length];

            for (int j = 0; j < runtimeMaterials[i].Length; j++)
            {
                Material mat = runtimeMaterials[i][j];

                SetupMaterialForFade(mat);

                if (mat.HasProperty("_Color"))
                {
                    originalColors[i][j] = mat.color;
                }
                else
                {
                    originalColors[i][j] = Color.white;
                }
            }
        }
    }

    private void SetupMaterialForFade(Material mat)
    {
        if (mat == null)
            return;

        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3f);
        }

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);

        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        mat.renderQueue = 3000;
    }

    private void PlaySuccessSound()
    {
        if (successSoundClip == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("[SafetyGloveSelector] Success Sound Clip이 연결되지 않았습니다.");
            }

            return;
        }

        if (audioSource == null)
            return;

        audioSource.volume = successSoundVolume;
        audioSource.PlayOneShot(successSoundClip, successSoundVolume);

        if (showDebugLog)
        {
            Debug.Log("[SafetyGloveSelector] 보호장갑 착용 성공 효과음 재생 / 볼륨: " + successSoundVolume);
        }
    }

    private void DisableInteraction()
    {
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false;
        }

        if (gloveColliders == null)
            return;

        for (int i = 0; i < gloveColliders.Length; i++)
        {
            gloveColliders[i].enabled = false;
        }
    }

    private IEnumerator FadeOutGloveRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            SetGloveAlpha(alpha);

            yield return null;
        }

        SetGloveAlpha(0f);

        if (deactivateAfterFade && gloveObject != null)
        {
            gloveObject.SetActive(false);
        }

        if (showDebugLog)
        {
            Debug.Log("[SafetyGloveSelector] 보호장갑 투명화 완료");
        }
    }

    private void SetGloveAlpha(float alpha)
    {
        if (runtimeMaterials == null)
            return;

        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            for (int j = 0; j < runtimeMaterials[i].Length; j++)
            {
                Material mat = runtimeMaterials[i][j];

                if (mat == null || !mat.HasProperty("_Color"))
                    continue;

                Color color = originalColors[i][j];
                color.a = alpha;
                mat.color = color;
            }
        }
    }
}