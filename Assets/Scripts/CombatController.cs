using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de combate simple para personaje Third Person
/// Compatible con StarterAssets
/// </summary>
public class CombatController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El Animator del personaje")]
    public Animator animator;

    [Header("Configuración de Combate")]
    [Tooltip("Daño que hace cada golpe")]
    public float punchDamage = 10f;
    
    [Tooltip("Distancia máxima del golpe")]
    public float punchRange = 2f;
    
    [Tooltip("Tiempo mínimo entre golpes (en segundos)")]
    public float punchCooldown = 0.5f;
    
    [Tooltip("Capa de los objetos que pueden recibir daño")]
    public LayerMask damageableLayer;

    [Header("Opciones Visuales")]
    [Tooltip("Mostrar el rango del golpe en la escena")]
    public bool showPunchRange = true;

    // Estados internos
    private bool isInCombatMode = false;
    private bool canPunch = true;
    private float lastPunchTime = 0f;
    
    // Referencias a Input Actions
    private PlayerInput playerInput;
    private InputAction combatModeAction;
    private InputAction punchAction;

    // Nombres de los parámetros del Animator
    private readonly string combatModeParam = "CombatMode";
    private readonly string punchParam = "Punch";

    void Start()
    {
        // Si no se asignó el animator, intentar obtenerlo del mismo GameObject
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("CombatController: No se encontró Animator! Asígnalo en el Inspector.");
            }
        }

        // Configurar el Input System
        SetupInputActions();

        Debug.Log("CombatController iniciado correctamente");
    }

    void SetupInputActions()
    {
        // Obtener el PlayerInput component
        playerInput = GetComponent<PlayerInput>();
        
        if (playerInput != null)
        {
            // Usar las acciones ya configuradas en StarterAssets
            combatModeAction = playerInput.actions["Look"]; // Temporal, cambiaremos esto
            punchAction = playerInput.actions["Fire"]; // Temporal
        }
        
        // Nota: Como StarterAssets no tiene acciones de combate predefinidas,
        // vamos a usar el Input System nuevo directamente
    }

    void Update()
    {
        // Detectar click derecho (mantener presionado)
        // Mouse 1 = Click derecho
        if (Mouse.current != null)
        {
            bool rightMousePressed = Mouse.current.rightButton.isPressed;
            
            if (rightMousePressed != isInCombatMode)
            {
                SetCombatMode(rightMousePressed);
            }

            // Detectar click izquierdo para golpear (solo en modo combate)
            if (isInCombatMode && Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryPunch();
            }
        }

        // También podemos usar teclas del teclado como alternativa
        // Shift = Modo combate
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftShiftKey.wasPressedThisFrame || 
                Keyboard.current.rightShiftKey.wasPressedThisFrame)
            {
                SetCombatMode(!isInCombatMode);
            }

            // Espacio = Golpe alternativo
            if (isInCombatMode && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TryPunch();
            }
        }
    }

    /// <summary>
    /// Activa o desactiva el modo de combate
    /// </summary>
    void SetCombatMode(bool active)
    {
        isInCombatMode = active;
        
        if (animator != null)
        {
            animator.SetBool(combatModeParam, isInCombatMode);
        }

        Debug.Log($"Modo combate: {(isInCombatMode ? "ACTIVADO" : "DESACTIVADO")}");
    }

    /// <summary>
    /// Intenta ejecutar un golpe
    /// </summary>
    void TryPunch()
    {
        // Verificar cooldown
        if (!canPunch)
        {
            Debug.Log("Todavía en cooldown, espera un momento");
            return;
        }

        if (Time.time - lastPunchTime < punchCooldown)
        {
            return;
        }

        // Ejecutar golpe
        ExecutePunch();
    }

    /// <summary>
    /// Ejecuta el golpe y el trigger de animación
    /// </summary>
    void ExecutePunch()
    {
        Debug.Log("¡GOLPE!");

        // Activar la animación de golpe
        if (animator != null)
        {
            animator.SetTrigger(punchParam);
        }

        // Detectar enemigos en rango
        DetectHit();

        // Actualizar cooldown
        lastPunchTime = Time.time;
        StartCoroutine(PunchCooldownCoroutine());
    }

    /// <summary>
    /// Detecta si el golpe impactó algo
    /// </summary>
    void DetectHit()
    {
        // Crear un raycast desde la posición del personaje hacia adelante
        Vector3 rayOrigin = transform.position + Vector3.up * 1f; // Altura del pecho
        Vector3 rayDirection = transform.forward;

        // Hacer el raycast
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, punchRange, damageableLayer))
        {
            Debug.Log($"¡Golpe impactó a: {hit.collider.name}!");

            // Intentar hacer daño al objeto
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(punchDamage);
            }

            // También podemos buscar un componente de salud
            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(punchDamage);
            }
        }
        else
        {
            Debug.Log("Golpe no impactó nada");
        }
    }

    /// <summary>
    /// Coroutine para el cooldown del golpe
    /// </summary>
    System.Collections.IEnumerator PunchCooldownCoroutine()
    {
        canPunch = false;
        yield return new WaitForSeconds(punchCooldown);
        canPunch = true;
    }

    /// <summary>
    /// Dibuja el rango del golpe en la vista de escena (solo para debugging)
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showPunchRange) return;

        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 direction = transform.forward;

        // Dibujar línea del rango
        Gizmos.color = isInCombatMode ? Color.red : Color.yellow;
        Gizmos.DrawLine(origin, origin + direction * punchRange);
        
        // Dibujar esfera al final
        Gizmos.DrawWireSphere(origin + direction * punchRange, 0.3f);
    }

    // Métodos públicos para acceder desde otros scripts
    public bool IsInCombatMode() => isInCombatMode;
    public void ForceExitCombatMode() => SetCombatMode(false);
}

/// <summary>
/// Interfaz para objetos que pueden recibir daño
/// Implementa esta en tus enemigos
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
}
