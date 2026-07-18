using UnityEngine;

public class GameStartManager : MonoBehaviour
{
    [Header("UI Opcional")]
    [SerializeField] private GameObject startMessage;

    private bool gameStarted = false;

    private void Start()
    {
        Time.timeScale = 0f; // Pausar el juego

        if (startMessage != null)
            startMessage.SetActive(true);
    }

    private void Update()
    {
        if (gameStarted)
            return;

        // Mouse (Editor/PC)
        if (Input.GetMouseButtonDown(0))
        {
            StartGame();
            return;
        }

        // Pantalla táctil (Android)
        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == TouchPhase.Began)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        gameStarted = true;
        Time.timeScale = 1f;

        if (startMessage != null)
            startMessage.SetActive(false);
    }
}