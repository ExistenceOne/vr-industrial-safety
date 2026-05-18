using UnityEngine;

public class MainBGMManager : MonoBehaviour
{
    [Header("BGM Settings")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField][Range(0f, 1f)] private float volume = 0.3f;
    [SerializeField] private bool loop = true;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = bgmClip;
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.playOnAwake = false;

        audioSource.spatialBlend = 0f; // 2D 사운드
    }

    private void Start()
    {
        PlayBGM();
    }

    public void PlayBGM()
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("[MainBGMManager] BGM Clip이 비어 있습니다.");
            return;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void StopBGM()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        audioSource.volume = volume;
    }
}