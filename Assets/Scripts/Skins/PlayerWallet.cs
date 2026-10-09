using System;
using UnityEngine;

/// <summary>
/// Moneda persistente del jugador: se va acumulando con el puntaje de CADA
/// partida jugada (no es el high score ni la tabla de posiciones, que
/// miden la mejor partida -- esto es la suma de todas). Se usa para comprar
/// skins en la tienda.
/// </summary>
public static class PlayerWallet
{
    private const string Key = "Player_Points";

    /// <summary>Se dispara cada vez que cambia el saldo (compra, o puntos ganados).</summary>
    public static event Action Changed;

    /// <summary>Saldo actual de puntos acumulados.</summary>
    public static int Balance
    {
        get => PlayerPrefs.GetInt(Key, 0);
        private set
        {
            int clamped = Mathf.Max(0, value);
            if (clamped == PlayerPrefs.GetInt(Key, 0)) return;

            PlayerPrefs.SetInt(Key, clamped);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    /// <summary>Suma puntos al saldo (llamar al terminar cada partida).</summary>
    public static void Add(int amount)
    {
        if (amount <= 0) return;
        Balance += amount;
    }

    /// <summary>Intenta gastar puntos. Devuelve false (y no gasta nada) si no hay saldo suficiente.</summary>
    public static bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (Balance < amount) return false;

        Balance -= amount;
        return true;
    }
}
