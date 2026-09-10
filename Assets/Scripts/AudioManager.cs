using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bus de audio central del juego. Resuelve el problema de que bajar el
/// volumen de UN AudioSource en el Inspector no baja el resto: aca hay
/// controles reales de Master / Musica / SFX que escalan a TODOS los
/// AudioSource de la escena a la vez, ademas del mute.
///
/// Setup por escena:
///   1. Un GameObject con este componente.
///   2. Arrastra los AudioSource de musica/ambiente a "Music Sources" y los
///      de efectos a "Sfx Sources". Si dejas las dos listas vacias, al
///      arrancar los busca solos (loop/PlayOnAwake -> musica, el resto -> SFX).
///
/// Los sliders Master/Music/Sfx SI funcionan en vivo (incluso arrastrandolos
/// en el Inspector durante Play). Se guardan en PlayerPrefs y persisten entre
/// escenas y sesiones.
/// </summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    private const string MasterKey = "Audio_Master";
    private const string MusicKey = "Audio_Music";
    private const string SfxKey = "Audio_Sfx";
    private const string MutedKey = "Audio_Muted";

    public static AudioManager Instance { get; private set; }

    /// <summary>Se dispara al cambiar el mute (true = silenciado).</summary>
    public static event Action<bool> OnMuteChanged;

    /// <summary>Se dispara ante cualquier cambio de audio (volumenes o mute).</summary>
    public static event Action OnChanged;

    [Header("Volumenes (0 a 1)")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private bool muted;

    [Header("Fuentes de la escena")]
    [Tooltip("AudioSources de musica/ambiente. Se respeta su balance relativo y se escalan con Music x Master.")]
    [SerializeField] private List<AudioSource> musicSources = new List<AudioSource>();

    [Tooltip("AudioSources de efectos. Se escalan con SFX x Master.")]
    [SerializeField] private List<AudioSource> sfxSources = new List<AudioSource>();

    [Tooltip("Si esta activo, ignora las listas de arriba y agarra todos los AudioSource de la escena al arrancar.")]
    [SerializeField] private bool autoCollectSceneSources = true;

    // Volumen original de cada fuente (su "mezcla" base, antes de escalar).
    private readonly Dictionary<AudioSource, float> baseVolume = new Dictionary<AudioSource, float>();
    private readonly HashSet<AudioSource> musicSet = new HashSet<AudioSource>();

    private AudioSource sfxPlayer;
    private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

    public bool IsMuted => muted;
    public float MasterVolume { get => masterVolume; set => SetVolume(ref masterVolume, value); }
    public float MusicVolume { get => musicVolume; set => SetVolume(ref musicVolume, value); }
    public float SfxVolume { get => sfxVolume; set => SetVolume(ref sfxVolume, value); }

    /// <summary>Devuelve el AudioManager de la escena o crea uno al vuelo.</summary>
    public static AudioManager Ensure()
    {
        if (Instance != null) return Instance;

        var existing = FindAnyObjectByType<AudioManager>();
        if (existing != null) return existing;

        return new GameObject("AudioManager").AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        Load();
        BuildSourceTable();
        EnsureSfxPlayer();
        Apply();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Reaplicar despues de que los PlayOnAwake ya arrancaron.
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);

        if (Application.isPlaying && Instance == this)
        {
            Apply();
        }
    }
#endif

    private void Load()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterKey, masterVolume);
        musicVolume = PlayerPrefs.GetFloat(MusicKey, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxKey, sfxVolume);
        muted = PlayerPrefs.GetInt(MutedKey, muted ? 1 : 0) == 1;
    }

    private void Persist()
    {
        PlayerPrefs.SetFloat(MasterKey, masterVolume);
        PlayerPrefs.SetFloat(MusicKey, musicVolume);
        PlayerPrefs.SetFloat(SfxKey, sfxVolume);
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void BuildSourceTable()
    {
        baseVolume.Clear();
        musicSet.Clear();

        bool listsEmpty = musicSources.Count == 0 && sfxSources.Count == 0;

        if (autoCollectSceneSources && listsEmpty)
        {
            foreach (var src in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool isMusic = src.loop || src.playOnAwake;
                Register(src, isMusic);
            }
        }
        else
        {
            foreach (var src in musicSources) Register(src, isMusic: true);
            foreach (var src in sfxSources) Register(src, isMusic: false);
        }
    }

    private void Register(AudioSource src, bool isMusic)
    {
        if (src == null || baseVolume.ContainsKey(src)) return;

        baseVolume[src] = src.volume;
        if (isMusic) musicSet.Add(src);
    }

    private void EnsureSfxPlayer()
    {
        sfxPlayer = gameObject.AddComponent<AudioSource>();
        sfxPlayer.playOnAwake = false;
        sfxPlayer.loop = false;
        sfxPlayer.spatialBlend = 0f;
    }

    /// <summary>Alterna silenciado / con sonido.</summary>
    public void ToggleMute() => SetMuted(!muted);

    /// <summary>Fija el estado de mute.</summary>
    public void SetMuted(bool value)
    {
        if (value == muted) return;
        muted = value;
        Persist();
        Apply();
        OnMuteChanged?.Invoke(muted);
    }

    private void SetVolume(ref float field, float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (Mathf.Approximately(clamped, field)) return;
        field = clamped;
        Persist();
        Apply();
    }

    /// <summary>Recalcula y aplica el volumen a todas las fuentes registradas.</summary>
    public void Apply()
    {
        float master = muted ? 0f : masterVolume;

        // Limpiar fuentes destruidas al cambiar de escena.
        var dead = new List<AudioSource>();
        foreach (var kv in baseVolume)
        {
            if (kv.Key == null) { dead.Add(kv.Key); continue; }

            float category = musicSet.Contains(kv.Key) ? musicVolume : sfxVolume;
            kv.Key.volume = kv.Value * category * master;
        }
        foreach (var d in dead) baseVolume.Remove(d);

        if (sfxPlayer != null) sfxPlayer.volume = sfxVolume * master;

        OnChanged?.Invoke();
    }

    /// <summary>
    /// Registra un AudioSource que aparecio despues (ej: instanciado en runtime)
    /// para que tambien lo controle el bus.
    /// </summary>
    public void RegisterSource(AudioSource src, bool isMusic)
    {
        Register(src, isMusic);
        Apply();
    }

    /// <summary>Reproduce un SFX ya escalado por SFX x Master (respeta el mute).</summary>
    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxPlayer == null) return;
        sfxPlayer.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    /// <summary>Igual que PlaySfx pero cargando el clip desde Resources (con cache).</summary>
    public void PlaySfx(string resourcePath, float volumeScale = 1f)
    {
        if (string.IsNullOrEmpty(resourcePath)) return;

        if (!clipCache.TryGetValue(resourcePath, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>(resourcePath);
            clipCache[resourcePath] = clip;
        }

        if (clip == null)
        {
            Debug.LogWarning($"AudioManager: no se encontro el clip 'Resources/{resourcePath}'.");
            return;
        }

        PlaySfx(clip, volumeScale);
    }
}
