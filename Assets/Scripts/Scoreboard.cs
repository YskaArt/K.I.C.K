using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Tabla de mejores puntajes (top 10) guardada en PlayerPrefs como JSON.
/// No necesita ninguna escena ni GameObject: se usa de forma estatica.
///
///   Scoreboard.Submit(puntaje);   // al terminar la partida
///   Scoreboard.Entries            // lista ordenada de mayor a menor
///
/// La pantalla <see cref="ScoreboardScreen"/> la dibuja en el menu.
/// </summary>
public static class Scoreboard
{
    private const string Key = "Scoreboard_v1";

    /// <summary>Cantidad maxima de puestos que se guardan.</summary>
    public const int Capacity = 10;

    /// <summary>Se dispara cuando cambia la tabla (nuevo puntaje o reset).</summary>
    public static event Action Changed;

    [Serializable]
    public class Entry
    {
        public int score;
        public string dateIso;

        public DateTime Date =>
            DateTime.TryParse(dateIso, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out DateTime d)
                ? d
                : DateTime.MinValue;
    }

    [Serializable]
    private class Wrapper
    {
        public List<Entry> entries = new List<Entry>();
    }

    private static Wrapper cache;

    private static Wrapper Data
    {
        get
        {
            if (cache != null) return cache;

            string json = PlayerPrefs.GetString(Key, string.Empty);
            cache = string.IsNullOrEmpty(json) ? new Wrapper() : JsonUtility.FromJson<Wrapper>(json);
            if (cache.entries == null) cache.entries = new List<Entry>();
            Sort(cache.entries);
            return cache;
        }
    }

    /// <summary>Puntajes de mayor a menor (solo lectura).</summary>
    public static IReadOnlyList<Entry> Entries => Data.entries;

    /// <summary>Mejor puntaje registrado, o 0 si la tabla esta vacia.</summary>
    public static int BestScore => Data.entries.Count > 0 ? Data.entries[0].score : 0;

    /// <summary>
    /// Registra un puntaje. Devuelve el puesto conseguido (1 = mejor) o -1 si
    /// no llego a entrar en la tabla.
    /// </summary>
    public static int Submit(int score)
    {
        if (score <= 0) return -1;

        List<Entry> list = Data.entries;

        var entry = new Entry
        {
            score = score,
            dateIso = DateTime.Now.ToString("o", CultureInfo.InvariantCulture),
        };

        list.Add(entry);
        Sort(list);

        if (list.Count > Capacity)
        {
            list.RemoveRange(Capacity, list.Count - Capacity);
        }

        Save();

        int rank = list.IndexOf(entry) + 1;
        return (rank >= 1 && rank <= Capacity) ? rank : -1;
    }

    /// <summary>Borra toda la tabla.</summary>
    public static void Clear()
    {
        Data.entries.Clear();
        Save();
    }

    private static void Sort(List<Entry> list)
    {
        list.Sort((a, b) => b.score.CompareTo(a.score));
    }

    private static void Save()
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(cache));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
