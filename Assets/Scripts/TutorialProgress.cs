using UnityEngine;

/// <summary>
/// Recuerda si el jugador ya vio el tutorial "Como Jugar" completo
/// (llego a la ultima captura). Se guarda en PlayerPrefs.
/// </summary>
public static class TutorialProgress
{
    private const string Key = "Tutorial_Completed";

    public static bool Completed
    {
        get => PlayerPrefs.GetInt(Key, 0) == 1;
        set
        {
            if (value == Completed) return;
            PlayerPrefs.SetInt(Key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
