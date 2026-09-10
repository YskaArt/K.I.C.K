using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Boton de "mutear el juego". Poné este componente sobre un Button de la UI
/// y funciona solo: al hacer click alterna el mute global (ver
/// <see cref="AudioManager"/>) y actualiza su apariencia.
///
/// Todos los campos de apariencia son opcionales: usá el que te sirva
/// (cambiar el texto, prender/apagar un icono, o intercambiar el sprite).
/// </summary>
[RequireComponent(typeof(Button))]
public class MuteButton : MonoBehaviour
{
    [Header("Texto (opcional)")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private string mutedText = "Sonido: OFF";
    [SerializeField] private string unmutedText = "Sonido: ON";

    [Header("Iconos por estado (opcional)")]
    [Tooltip("Se muestra cuando el juego esta silenciado")]
    [SerializeField] private GameObject mutedIcon;
    [Tooltip("Se muestra cuando el juego tiene sonido")]
    [SerializeField] private GameObject unmutedIcon;

    [Header("Intercambio de sprite (opcional)")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite mutedSprite;
    [SerializeField] private Sprite unmutedSprite;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(OnClick);
        AudioManager.OnMuteChanged += Refresh;

        // Asegura que exista el manager y sincroniza el estado inicial.
        Refresh(AudioManager.Ensure().IsMuted);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(OnClick);
        AudioManager.OnMuteChanged -= Refresh;
    }

    private void OnClick()
    {
        AudioManager.Ensure().ToggleMute();
    }

    private void Refresh(bool muted)
    {
        if (label != null)
        {
            label.text = muted ? mutedText : unmutedText;
        }

        if (mutedIcon != null) mutedIcon.SetActive(muted);
        if (unmutedIcon != null) unmutedIcon.SetActive(!muted);

        if (targetImage != null)
        {
            Sprite next = muted ? mutedSprite : unmutedSprite;
            if (next != null) targetImage.sprite = next;
        }
    }
}
