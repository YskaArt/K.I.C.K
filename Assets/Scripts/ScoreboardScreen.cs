using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pantalla de mejores puntajes. Se arma sola con lo que haya en
/// <see cref="Scoreboard"/>: clona una fila plantilla, una por puntaje.
///
/// Setup en el editor (una vez):
///   1. Un panel con un contenedor con Vertical Layout Group -> 'Row Container'.
///   2. Una fila plantilla dentro con TMP Text hijos llamados "Rank", "Score"
///      y "Date" (los que no encuentre los ignora; si no hay ninguno con esos
///      nombres usa el primer TMP Text y escribe todo en una linea) -> 'Row Template'.
///   3. (Opcional) un TMP Text "Sin puntajes todavia" -> 'Empty Message'.
///   4. (Opcional) un boton para borrar la tabla -> 'Clear Button'.
///
/// Conectá un boton "Puntajes" del menu a <see cref="Open"/> y uno "Volver" a
/// <see cref="Close"/>.
/// </summary>
public class ScoreboardScreen : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Contenedor con Layout Group donde se instancian las filas.")]
    [SerializeField] private RectTransform rowContainer;

    [Tooltip("Fila plantilla. Se clona una por puntaje. Puede estar desactivada.")]
    [SerializeField] private GameObject rowTemplate;

    [Tooltip("Texto a mostrar cuando la tabla esta vacia (opcional).")]
    [SerializeField] private TMP_Text emptyMessage;

    [Tooltip("Boton opcional para borrar toda la tabla.")]
    [SerializeField] private Button clearButton;

    [SerializeField] private string dateFormat = "dd/MM/yyyy";

    private readonly List<GameObject> spawnedRows = new List<GameObject>();

    private void OnEnable()
    {
        if (clearButton != null) clearButton.onClick.AddListener(OnClearClicked);
        Scoreboard.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (clearButton != null) clearButton.onClick.RemoveListener(OnClearClicked);
        Scoreboard.Changed -= Refresh;
    }

    /// <summary>Muestra la pantalla (conectar al OnClick de un boton "Puntajes").</summary>
    public void Open() => gameObject.SetActive(true);

    /// <summary>Oculta la pantalla (conectar al OnClick de un boton "Volver").</summary>
    public void Close() => gameObject.SetActive(false);

    /// <summary>Reconstruye las filas desde la tabla.</summary>
    public void Refresh()
    {
        if (rowContainer == null || rowTemplate == null)
        {
            Debug.LogWarning("ScoreboardScreen: falta asignar Row Container o Row Template.");
            return;
        }

        foreach (GameObject go in spawnedRows)
        {
            if (go != null) Destroy(go);
        }
        spawnedRows.Clear();

        rowTemplate.SetActive(false);

        IReadOnlyList<Scoreboard.Entry> entries = Scoreboard.Entries;

        if (emptyMessage != null)
        {
            emptyMessage.gameObject.SetActive(entries.Count == 0);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            GameObject row = Instantiate(rowTemplate, rowContainer);
            row.SetActive(true);
            row.name = "ScoreRow_" + (i + 1);
            FillRow(row, i + 1, entries[i]);
            spawnedRows.Add(row);
        }
    }

    private void FillRow(GameObject row, int rank, Scoreboard.Entry entry)
    {
        string dateStr = entry.Date == DateTime.MinValue ? string.Empty : entry.Date.ToString(dateFormat);

        TMP_Text rankText = FindText(row, "Rank");
        TMP_Text scoreText = FindText(row, "Score");
        TMP_Text dateText = FindText(row, "Date");

        if (rankText != null || scoreText != null || dateText != null)
        {
            if (rankText != null) rankText.text = rank + ".";
            if (scoreText != null) scoreText.text = entry.score.ToString();
            if (dateText != null) dateText.text = dateStr;
        }
        else
        {
            TMP_Text single = row.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (single != null)
            {
                single.text = $"{rank,2}.   {entry.score,6}    {dateStr}";
            }
        }
    }

    private static TMP_Text FindText(GameObject root, string childName)
    {
        foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(includeInactive: true))
        {
            if (t.gameObject.name == childName) return t;
        }
        return null;
    }

    private void OnClearClicked()
    {
        Scoreboard.Clear();
    }
}
