using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pantalla de seleccion de skins. Arma sola una fila/grilla de botones a
/// partir de <see cref="RobotSkinLibrary"/>: un boton por skin.
///
/// Setup en el editor (una sola vez):
///   1. Un panel con un contenedor que tenga un Horizontal/Grid Layout Group
///      -> asignalo en 'Button Container'.
///   2. Un boton que sirva de plantilla dentro (o fuera) del contenedor, con
///      un hijo Image para el icono y, opcional, un TMP Text para el nombre
///      -> asignalo en 'Button Template'. Se clona una vez por skin.
///   3. (Opcional) el RobotSkinController del robot visible en esta escena
///      -> 'Live Preview', para ver el cambio al instante.
///
/// Agregar skins nuevas despues no requiere tocar nada de esto: alcanza con
/// sumarlas a la libreria.
/// </summary>
public class SkinSelectionScreen : MonoBehaviour
{
    [Header("Datos")]
    [SerializeField] private RobotSkinLibrary library;

    [Header("UI")]
    [Tooltip("Contenedor con Layout Group donde se instancian los botones.")]
    [SerializeField] private RectTransform buttonContainer;

    [Tooltip("Boton plantilla. Se clona uno por skin. Puede estar desactivado.")]
    [SerializeField] private Button buttonTemplate;

    [Tooltip("Nombre del hijo Image donde va el icono de la skin. Si queda vacio se usa la Image del propio boton.")]
    [SerializeField] private string iconChildName = "Icon";

    [Header("Resaltado del seleccionado")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color normalColor = Color.white;

    [Header("Preview en vivo (opcional)")]
    [SerializeField] private RobotSkinController livePreview;

    private readonly List<Button> spawnedButtons = new List<Button>();

    private void OnEnable()
    {
        Build();
        Highlight(PlayerSkinPrefs.SelectedIndex);
    }

    /// <summary>Reconstruye los botones desde la libreria.</summary>
    public void Build()
    {
        if (library == null || buttonTemplate == null || buttonContainer == null)
        {
            Debug.LogWarning("SkinSelectionScreen: falta asignar Library, Button Template o Button Container.");
            return;
        }

        foreach (Button b in spawnedButtons)
        {
            if (b != null) Destroy(b.gameObject);
        }
        spawnedButtons.Clear();

        buttonTemplate.gameObject.SetActive(false);

        for (int i = 0; i < library.Count; i++)
        {
            RobotSkin skin = library.Get(i);
            if (skin == null) continue;

            Button btn = Instantiate(buttonTemplate, buttonContainer);
            btn.gameObject.SetActive(true);
            btn.name = "SkinButton_" + i;

            Image icon = FindIconOn(btn);
            if (icon != null && skin.previewIcon != null)
            {
                icon.sprite = skin.previewIcon;
            }

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (label != null)
            {
                label.text = string.IsNullOrEmpty(skin.displayName) ? "Skin" : skin.displayName;
            }

            int index = i; // capturar por valor para el listener
            btn.onClick.AddListener(() => Select(index));

            spawnedButtons.Add(btn);
        }
    }

    /// <summary>Elige la skin de ese indice: la guarda y actualiza el preview.</summary>
    public void Select(int index)
    {
        PlayerSkinPrefs.SelectedIndex = index;

        // Explicito por si se re-elige la misma (PlayerSkinPrefs no dispara evento en ese caso).
        if (livePreview != null) livePreview.ApplySkin(index);

        Highlight(index);
    }

    /// <summary>Muestra la pantalla (para conectar al OnClick de un boton "Skins").</summary>
    public void Open() => gameObject.SetActive(true);

    /// <summary>Oculta la pantalla (para conectar al OnClick de un boton "Volver").</summary>
    public void Close() => gameObject.SetActive(false);

    private Image FindIconOn(Button btn)
    {
        if (!string.IsNullOrEmpty(iconChildName))
        {
            foreach (Image img in btn.GetComponentsInChildren<Image>(includeInactive: true))
            {
                if (img.gameObject.name == iconChildName) return img;
            }
        }
        return btn.image;
    }

    private void Highlight(int selectedIndex)
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            Button b = spawnedButtons[i];
            if (b == null || b.targetGraphic == null) continue;
            b.targetGraphic.color = (i == selectedIndex) ? selectedColor : normalColor;
        }
    }
}
