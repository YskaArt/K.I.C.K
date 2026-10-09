using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Recuerda que skins compro el jugador en la tienda. Se guarda como una
/// lista de indices (la posicion de la skin dentro de RobotSkinLibrary),
/// el mismo criterio que ya usa PlayerSkinPrefs para la seleccion.
///
/// Una skin con 'price' en 0 (ver RobotSkin) esta disponible siempre, sin
/// necesidad de desbloquearla -- esto solo hace falta consultarlo para las
/// que tienen costo.
/// </summary>
public static class SkinUnlocks
{
    private const string Key = "Robot_UnlockedSkins";

    /// <summary>Se dispara cuando se desbloquea una skin nueva.</summary>
    public static event Action Changed;

    private static HashSet<int> cache;

    private static HashSet<int> Data
    {
        get
        {
            if (cache != null) return cache;

            cache = new HashSet<int>();
            string raw = PlayerPrefs.GetString(Key, string.Empty);
            if (!string.IsNullOrEmpty(raw))
            {
                foreach (string part in raw.Split(','))
                {
                    if (int.TryParse(part, out int index)) cache.Add(index);
                }
            }

            return cache;
        }
    }

    /// <summary>True si esa skin ya fue comprada.</summary>
    public static bool IsUnlocked(int index) => Data.Contains(index);

    /// <summary>Marca esa skin como comprada.</summary>
    public static void Unlock(int index)
    {
        if (!Data.Add(index)) return;
        Save();
    }

    private static void Save()
    {
        PlayerPrefs.SetString(Key, string.Join(",", Data));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
