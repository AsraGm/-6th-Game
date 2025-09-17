// Archivo: ProjectileVisual.cs
using UnityEngine;
using System.Collections;

// Componente para el efecto visual del proyectil
public class ProjectileVisual : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float projectileLifetime = 2f;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private GameObject impactEffect;
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isActive = false;
    
    private void Awake()
    {
        // Si no hay LineRenderer asignado, crear uno
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                SetupLineRenderer();
            }
        }
    }
    
    private void SetupLineRenderer()
    {
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.yellow;
        lineRenderer.widthMultiplier = 0.05f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
    }
    
    public void FireProjectile(Vector3 start, Vector3 end)
    {
        startPosition = start;
        targetPosition = end;
        transform.position = startPosition;
        isActive = true;
        
        // Configurar LineRenderer
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, startPosition); // Inicialmente en la misma posición
        
        StartCoroutine(ProjectileMovement());
    }
    
    private IEnumerator ProjectileMovement()
    {
        float elapsed = 0f;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / projectileSpeed;
        
        while (elapsed < duration && isActive)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, progress);
            transform.position = currentPos;
            
            // Actualizar LineRenderer para mostrar la estela
            lineRenderer.SetPosition(1, currentPos);
            
            yield return null;
        }
        
        // Impacto
        if (isActive)
        {
            OnImpact();
        }
        
        // Retornar al pool después de un breve delay
        yield return new WaitForSeconds(0.1f);
        ReturnToPool();
    }
    
    private void OnImpact()
    {
        // Crear efecto de impacto si está asignado
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, targetPosition, Quaternion.identity);
            Destroy(effect, 1f); // Limpiar efecto después de 1 segundo
        }
    }
    
    private void ReturnToPool()
    {
        isActive = false;
        lineRenderer.enabled = false;
        gameObject.SetActive(false);
    }
    
    private void OnDisable()
    {
        StopAllCoroutines();
        isActive = false;
    }
}