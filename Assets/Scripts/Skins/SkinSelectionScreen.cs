using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pantalla de seleccion / tienda de skins. Arma sola una fila/grilla de
/// botones a partir de <see cref="RobotSkinLibrary"/>: un boton por skin.
///
/// Las skins con 'price' en 0 estan disponibles para elegir siempre. Las
/// que tienen costo (price > 0) aparecen bloqueadas hasta comprarlas: al
/// tocarlas, si hay saldo suficiente en <see cref="PlayerWallet"/> se
/// descuenta, se desbloquean para siempre (<see cref="SkinUnlocks"/>) y
/// quedan elegidas; si no hay saldo, no pasa nada (sigue bloqueada).
///
/// Setup en el editor (una sola vez):
///   1. Un panel con un contenedor que tenga un Horizontal/Grid Layout Group
///      -> asignalo en 'Button Container'.
///   2. Un boton que sirva de plantilla dentro (o fuera) del contenedor, con
///      un hijo Image para el icono y, opcional, un TMP Text para el nombre,
///      otro TMP Text llamado 'Price' para el costo, y un GameObject
///      llamado 'Lock' (candadito/overlay) que se prende solo cuando esta
///      bloqueada -> asignalo en 'Button Template'. Se clona una vez por skin.
///   3. (Opcional) un TMP Text en el panel para mostrar el saldo -> 'Balance Text'.
///   4. (Opcional) el RobotSkinController del robot visible en esta escena
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

    [Tooltip("Nombre del hijo TMP Text donde va el precio (opcional). Si no existe, simplemente no se muestra precio.")]
    [SerializeField] private string priceChildName = "Price";

    [Tooltip("Nombre del hijo (candado/overlay) que se prende cuando la skin esta bloqueada (opcional).")]
    [SerializeField] private string lockChildName = "Lock";

    [Header("Saldo (opcional)")]
    [Tooltip("Texto donde se muestra PlayerWallet.Balance. Si queda vacio, no se muestra en ningun lado.")]
    [SerializeField] private TMP_Text balanceText;

    [SerializeField] private string balanceFormat = "{0} pts";

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
        UpdateBalanceText();

        PlayerWallet.Changed += UpdateBalanceText;
        SkinUnlocks.Changed += RefreshLocks;
    }

    private void OnDisable()
    {
        PlayerWallet.Changed -= UpdateBalanceText;
        SkinUnlocks.Changed -= RefreshLocks;
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

            Image icon = FindChildImage(btn.transform, iconChildName);
            if (icon == null) icon = btn.image;
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
            btn.onClick.AddListener(() => OnButtonClicked(index));

            spawnedButtons.Add(btn);
        }

        RefreshLocks();
    }

    /// <summary>
    /// Al tocar una skin: si ya esta disponible (gratis o ya comprada), la
    /// elige. Si tiene costo y todavia esta bloqueada, intenta comprarla.
    /// </summary>
    private void OnButtonClicked(int index)
    {
        RobotSkin skin = library.Get(index);
        if (skin == null) return;

        if (IsAvailable(skin, index))
        {
            Select(index);
            return;
        }

        if (PlayerWallet.TrySpend(skin.price))
        {
            SkinUnlocks.Unlock(index);
            Select(index);
        }
        // Si no hay saldo suficiente, no pasa nada: sigue bloqueada.
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

    private static bool IsAvailable(RobotSkin skin, int index)
    {
        return skin.price <= 0 || SkinUnlocks.IsUnlocked(index);
    }

    /// <summary>Repinta precio/candado de cada boton segun el saldo y lo ya desbloqueado.</summary>
    private void RefreshLocks()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            Button btn = spawnedButtons[i];
            RobotSkin skin = library.Get(i);
            if (btn == null || skin == null) continue;

            bool available = IsAvailable(skin, i);

            GameObject lockIcon = FindChildByName(btn.transform, lockChildName);
            if (lockIcon != null) lockIcon.SetActive(!available);

            TMP_Text priceLabel = FindChildTMP(btn.transform, priceChildName);
            if (priceLabel != null)
            {
                priceLabel.text = available ? string.Empty : skin.price.ToString();
            }
        }
    }

    private void UpdateBalanceText()
    {
        if (balanceText != null)
        {
            balanceText.text = string.Format(balanceFormat, PlayerWallet.Balance);
        }
    }

    private Image FindChildImage(Transform root, string childName)
    {
        if (string.IsNullOrEmpty(childName)) return null;

        foreach (Image img in root.GetComponentsInChildren<Image>(includeInactive: true))
        {
            if (img.gameObject.name == childName) return img;
        }
        return null;
    }

    private TMP_Text FindChildTMP(Transform root, string childName)
    {
        if (string.IsNullOrEmpty(childName)) return null;

        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(includeInactive: true))
        {
            if (text.gameObject.name == childName) return text;
        }
        return null;
    }

    private GameObject FindChildByName(Transform root, string childName)
    {
        if (string.IsNullOrEmpty(childName)) return null;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            if (t.gameObject.name == childName) return t.gameObject;
        }
        return null;
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
