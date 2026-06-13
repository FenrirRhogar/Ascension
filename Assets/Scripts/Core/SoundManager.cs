using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SoundManager (Auto)");
                _instance = go.AddComponent<SoundManager>();
            }
            return _instance;
        }
    }
    private static SoundManager _instance;

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 15;
    
    private AudioSource[] sourcePool;
    private int nextPoolIndex;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool()
    {
        if (sourcePool != null) return;

        sourcePool = new AudioSource[initialPoolSize];
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject go = new GameObject("AudioSource_" + i);
            go.transform.SetParent(transform);
            sourcePool[i] = go.AddComponent<AudioSource>();
            sourcePool[i].playOnAwake = false;
            sourcePool[i].spatialBlend = 0f; // 2D by default for global UI/combat
        }
    }

    public void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        if (sourcePool == null) InitializePool();

        AudioSource source = sourcePool[nextPoolIndex];
        source.Stop();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();

        Debug.Log($"[SoundManager] Playing {clip.name} at volume {volume}");

        nextPoolIndex = (nextPoolIndex + 1) % sourcePool.Length;
    }

    /// <summary>
    /// Plays a sound at a specific position in 3D space.
    /// </summary>
    public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    [Header("Music Settings")]
    private AudioSource musicSource;

    /// <summary>
    /// Plays background music. Replaces any currently playing music.
    /// </summary>
    public void PlayMusic(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (clip == null) return;

        if (musicSource == null)
        {
            GameObject go = new GameObject("MusicSource");
            go.transform.SetParent(transform);
            musicSource = go.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f; // Always 2D for music
        }

        if (musicSource.clip == clip && musicSource.isPlaying) return; // Already playing

        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.loop = loop;
        musicSource.Play();

        Debug.Log($"[SoundManager] Playing Music: {clip.name}");
    }

    /// <summary>
    /// Stops the currently playing background music.
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }
}
