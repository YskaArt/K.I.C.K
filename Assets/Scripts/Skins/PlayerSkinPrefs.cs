using System;
using UnityEngine;

/// <summary>
/// Guarda y comparte la skin elegida entre escenas (menu y juego) usando
/// PlayerPrefs. Cualquier script puede leer <see cref="SelectedIndex"/> o
/// suscribirse a <see cref="Changed"/> para reaccionar al instante.
/// </summary>
public static class PlayerSkinPrefs
{
    private const string Key = "Robot_SkinIndex";

    /// <summary>Se dispara cada vez que cambia la skin elegida. Pasa el nuevo indice.</summary>
    public static event Action<int> Changed;

    /// <summary>Indice de la skin elegida (0 si nunca se eligio nada).</summary>
    public static int SelectedIndex
    {
        get => PlayerPrefs.GetInt(Key, 0);
        set
        {
            int clamped = Mathf.Max(0, value);
            if (clamped == PlayerPrefs.GetInt(Key, 0)) return;

            PlayerPrefs.SetInt(Key, clamped);
            PlayerPrefs.Save();
            Changed?.Invoke(clamped);
        }
    }
}
