using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Puntuación")]
    public TMP_Text textoPuntuacion;

    [Header("Vidas (Corazones)")]
    public Image[] iconosVidas;

    [Header("Paneles (Arrastra los objetos aquí)")]
    public GameObject panelGameOver;
    public GameObject panelVictoria;
    public TMP_Text textoPuntuacionFinalGO;
    public TMP_Text textoPuntuacionFinalVI;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetUI(this);

            ActualizarPuntuacion(GameManager.Instance.puntos);
            ActualizarVidas(GameManager.Instance.vidasActuales);
        }
    }

    public void ActualizarPuntuacion(int puntuacion)
    {
        if (textoPuntuacion != null) textoPuntuacion.text = puntuacion.ToString("D4");
    }

    public void ActualizarVidas(int vidas)
    {
        for (int i = 0; i < iconosVidas.Length; i++)
        {
            iconosVidas[i].enabled = (i < vidas);
        }
    }

    public void BotonReiniciar()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReiniciarJuego();
        }
    }

    public void BotonMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SalirJuego();
        }
    }

    public void MostrarGameOver()
    {
        if (panelGameOver != null) panelGameOver.SetActive(true);
        if (textoPuntuacionFinalGO != null)
            textoPuntuacionFinalGO.text = " " + GameManager.Instance.puntos;
    }

    public void MostrarVictoria()
    {
        if (textoPuntuacionFinalVI != null)
            textoPuntuacionFinalVI.text = " " + GameManager.Instance.puntos;
        if (panelVictoria != null) panelVictoria.SetActive(true);
    }
}