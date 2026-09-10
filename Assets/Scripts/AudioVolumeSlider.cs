using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Conecta un <see cref="Slider"/> de la UI a uno de los volumenes del
/// <see cref="AudioManager"/> (Master, Music o Sfx). Poné este componente
/// sobre el GameObject del Slider y elegí que canal controla.
///
/// Funciona solo: al mover el slider baja/sube ese canal para TODO el juego,
/// y al abrir la pantalla el slider aparece en el valor guardado.
/// </summary>
[RequireComponent(typeof(Slider))]
public class AudioVolumeSlider : MonoBehaviour
{
    public enum Channel { Master, Music, Sfx }

    [SerializeField] private Channel channel = Channel.Master;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
    }

    private void OnEnable()
    {
        slider.SetValueWithoutNotify(Read());
        slider.onValueChanged.AddListener(Write);
        AudioManager.OnChanged += SyncFromManager;
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(Write);
        AudioManager.OnChanged -= SyncFromManager;
    }

    private float Read()
    {
        var am = AudioManager.Ensure();
        return channel switch
        {
            Channel.Master => am.MasterVolume,
            Channel.Music => am.MusicVolume,
            _ => am.SfxVolume,
        };
    }

    private void Write(float value)
    {
        var am = AudioManager.Ensure();
        switch (channel)
        {
            case Channel.Master: am.MasterVolume = value; break;
            case Channel.Music: am.MusicVolume = value; break;
            case Channel.Sfx: am.SfxVolume = value; break;
        }
    }

    private void SyncFromManager()
    {
        if (slider != null) slider.SetValueWithoutNotify(Read());
    }
}
