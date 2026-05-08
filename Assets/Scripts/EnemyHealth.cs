using UnityEngine;

/// <summary>
/// Sistema simple de salud para enemigos
/// Implementa la interfaz IDamageable para recibir daño del CombatController
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Configuración de Salud")]
    [Tooltip("Vida máxima del enemigo")]
    public float maxHealth = 100f;

    [Tooltip("Vida actual del enemigo")]
    public float currentHealth;

    [Header("Opciones Visuales")]
    [Tooltip("Mostrar barra de vida sobre el enemigo")]
    public bool showHealthBar = true;

    [Tooltip("Color de la barra de vida")]
    public Color healthBarColor = Color.green;

    [Header("Efectos al Recibir Daño")]
    [Tooltip("Material que se pondrá rojo al recibir daño")]
    public Renderer enemyRenderer;

    [Tooltip("Duración del efecto de daño (en segundos)")]
    public float hitEffectDuration = 0.2f;

    // Estados internos
    private bool isDead = false;
    private Material originalMaterial;
    private Color originalColor;

    void Start()
    {
        // Inicializar salud
        currentHealth = maxHealth;

        // Guardar material original
        if (enemyRenderer != null)
        {
            originalMaterial = enemyRenderer.material;
            originalColor = originalMaterial.color;
        }
        else
        {
            // Intentar obtener el renderer del mismo objeto
            enemyRenderer = GetComponent<Renderer>();
            if (enemyRenderer != null)
            {
                originalMaterial = enemyRenderer.material;
                originalColor = originalMaterial.color;
            }
        }

        Debug.Log($"{gameObject.name} iniciado con {currentHealth} de vida");
    }

    /// <summary>
    /// Método de la interfaz IDamageable
    /// Se llama cuando el enemigo recibe daño
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} recibió {damage} de daño. Vida restante: {currentHealth}");

        // Activar efecto visual de daño
        StartCoroutine(HitEffect());

        // Verificar si murió
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Efecto visual cuando recibe daño
    /// </summary>
    System.Collections.IEnumerator HitEffect()
    {
        if (enemyRenderer != null && originalMaterial != null)
        {
            // Cambiar a color rojo
            enemyRenderer.material.color = Color.red;

            // Esperar un momento
            yield return new WaitForSeconds(hitEffectDuration);

            // Volver al color original
            if (!isDead) // Solo si no está muerto
            {
                enemyRenderer.material.color = originalColor;
            }
        }
    }

    /// <summary>
    /// Maneja la muerte del enemigo
    /// </summary>
    void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"{gameObject.name} ha muerto!");

        // Desactivar collider para que no pueda ser golpeado de nuevo
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Cambiar a color gris oscuro
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = new Color(0.3f, 0.3f, 0.3f);
        }

        // Aquí puedes agregar más efectos:
        // - Animación de muerte
        // - Efectos de partículas
        // - Sonidos
        // - Drop de items
        // - Dar experiencia al jugador

        // Opcionalmente destruir el objeto después de un tiempo
        Destroy(gameObject, 3f);
    }

    /// <summary>
    /// Dibuja una barra de vida simple sobre el enemigo
    /// </summary>
    void OnGUI()
    {
        if (!showHealthBar || isDead) return;

        // Convertir posición 3D a posición de pantalla
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);

        // Solo mostrar si está frente a la cámara
        if (screenPos.z > 0)
        {
            // Invertir Y porque GUI usa coordenadas diferentes
            screenPos.y = Screen.height - screenPos.y;

            // Configurar tamaño de la barra
            float barWidth = 100f;
            float barHeight = 10f;
            float healthPercent = currentHealth / maxHealth;

            // Dibujar fondo negro
            GUI.color = Color.black;
            GUI.DrawTexture(
                new Rect(screenPos.x - barWidth / 2, screenPos.y, barWidth, barHeight),
                Texture2D.whiteTexture
            );

            // Dibujar barra de vida
            GUI.color = healthBarColor;
            GUI.DrawTexture(
                new Rect(screenPos.x - barWidth / 2, screenPos.y, barWidth * healthPercent, barHeight),
                Texture2D.whiteTexture
            );

            // Restaurar color
            GUI.color = Color.white;
        }
    }

    /// <summary>
    /// Dibuja el rango de detección en la vista de escena
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isDead ? Color.gray : Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }

    // Métodos públicos útiles
    public bool IsDead() => isDead;
    public float GetHealthPercent() => currentHealth / maxHealth;
    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"{gameObject.name} curado por {amount}. Vida actual: {currentHealth}");
    }
}
