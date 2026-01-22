using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour
{
    public enum TipoPuerta { SalidaNivel, EntradaNivel }

    [Header("Configuración")]
    public TipoPuerta tipo;
    public string nombreSiguienteNivel;

    [Header("Requisitos (Solo Salida)")]
    public bool necesitaLlave = false;
    public GameObject enemigoJefe;

    [Header("Configuración Spawn")]
    public Transform puntoDeAparicion;

    private Animator animPuerta;
    private PlayerController playerScript;

    void Start()
    {
        animPuerta = GetComponent<Animator>();

        if (tipo == TipoPuerta.EntradaNivel)
        {
            StartCoroutine(SecuenciaEntradaSpawn());
        }
    }

    // --- LÓGICA DE SPAWN (NIVEL 2) ---
    IEnumerator SecuenciaEntradaSpawn()
    {
        yield return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (puntoDeAparicion != null)
            {
                player.transform.position = puntoDeAparicion.position;
            }
            else
            {
                player.transform.position = transform.position + new Vector3(0, 0.5f, 0);
            }

            playerScript = player.GetComponent<PlayerController>();
            if (playerScript != null)
            {
                playerScript.AnimacionSalirPuerta();
            }
        }

        animPuerta.Play("Opening");

        yield return new WaitForSeconds(0.5f);

        animPuerta.SetTrigger("Close");
    }

    // --- LÓGICA DE DETECCIÓN ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerScript = other.GetComponent<PlayerController>();
            if (playerScript != null) playerScript.SetPuertaCercana(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerScript != null) playerScript.SetPuertaCercana(null);
            playerScript = null;
        }
    }

    // --- LÓGICA DE SALIDA (CAMBIO DE NIVEL) ---
    public void IntentarAbrir()
    {
        if (tipo == TipoPuerta.EntradaNivel) return;

        if (necesitaLlave)
        {
            if (GameManager.Instance == null || !GameManager.Instance.tieneLlave)
            {
                Debug.Log("¡Necesitas la llave!");
                return;
            }
        }

        if (enemigoJefe != null)
        {
            Debug.Log("¡Derrota al jefe primero!");
            return;
        }

        // SI TODO OK:
        StartCoroutine(SecuenciaCambioNivel());
    }

    IEnumerator SecuenciaCambioNivel()
    {
        // 1. Abrir puerta
        animPuerta.SetTrigger("Open");
        yield return new WaitForSeconds(0.5f);

        // 2. Jugador entra
        if (playerScript != null)
        {
            playerScript.AnimacionEntrarPuerta();
        }

        // 3. Esperamos a que termine la animación del jugador entrando
        yield return new WaitForSeconds(1f);

        // 4. CARGAMOS LA ESCENA DIRECTAMENTE
        if (nombreSiguienteNivel == "Victoria")
        {
            Debug.Log("¡Juego Completado!");
            if (GameManager.Instance != null) GameManager.Instance.VictoriaFinal();
        }
        else
        {
            SceneManager.LoadScene(nombreSiguienteNivel);
        }
    }
}
