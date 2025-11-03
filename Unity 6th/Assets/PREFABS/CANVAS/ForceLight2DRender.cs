using UnityEngine;
using UnityEngine.Rendering.Universal;

// Script para forzar que Light2D se rendericen correctamente en Android build
// Pégalo en cada GameObject que tenga una Light2D que no se vea
public class ForceLight2DRender : MonoBehaviour
{
    private Light2D light2D;
    private float originalIntensity;
    private bool hasInitialized = false;

    void Start()
    {
        light2D = GetComponent<Light2D>();
        if (light2D != null)
        {
            originalIntensity = light2D.intensity;
            
            // Forzar recarga completa
            StartCoroutine(ForceRefresh());
        }
    }

    System.Collections.IEnumerator ForceRefresh()
    {
        yield return new WaitForEndOfFrame();
        
        if (light2D != null)
        {
            // Trick: Cambiar propiedades para forzar re-render
            float tempIntensity = light2D.intensity;
            light2D.intensity = 0f;
            yield return null;
            light2D.intensity = tempIntensity;
            
            // Forzar dirty flag
            light2D.enabled = false;
            yield return null;
            light2D.enabled = true;
            
            hasInitialized = true;
            Debug.Log($"✅ Light2D forzado: {gameObject.name}");
        }
    }

    // Cada vez que se activa, forzar refresh
    void OnEnable()
    {
        if (hasInitialized && light2D != null)
        {
            StartCoroutine(RefreshOnEnable());
        }
    }

    System.Collections.IEnumerator RefreshOnEnable()
    {
        yield return null;
        
        if (light2D != null && light2D.enabled)
        {
            float temp = light2D.intensity;
            light2D.intensity = 0f;
            yield return null;
            light2D.intensity = temp;
        }
    }
}
