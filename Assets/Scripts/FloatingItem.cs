using UnityEngine;

public class FloatingItem : MonoBehaviour
{
    [SerializeField] float amplitud = 0.5f;
    [SerializeField] float velocidad = 1f;
    private Vector3 posicionInicial;

    [Range(0, 1)] public float volumen = 1f;
    public AudioClip keySound;

    void Start() => posicionInicial = transform.position;

    void Update()
    {
        float nuevaY = posicionInicial.y + Mathf.Sin(Time.time * velocidad) * amplitud;
        transform.position = new Vector3(transform.position.x, nuevaY, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AgarrarLlave();
            }

            if (keySound != null)
            {
                AudioSource.PlayClipAtPoint(keySound, transform.position, volumen);
            }

            Destroy(gameObject);
        }
    }
}
