using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// ARCHIVO: FloatingMoneyFeedback.cs
// CONEXIÓN: Lista D3 (Money feedback) + Lista A2 (Hit detection) + Lista I1 (Pooling)
// Sistema de feedback visual que muestra "+X" donde murió el enemigo

namespace ShootingRange
{
    public class FloatingMoneyFeedback : MonoBehaviour
    {
        [Header("Prefab Setup - IMPORTANTE")]
        [Tooltip("ARRASTRA AQUÍ el prefab del floating text (Canvas con TextMeshProUGUI)")]
        public GameObject floatingTextPrefab;
        
        [Header("Pool Configuration - Optimización Móvil")]
        [Tooltip("Cantidad de textos pre-instanciados en el pool")]
        [Range(5, 30)]
        public int poolSize = 15;
        
        [Header("Visual Configuration")]
        [Tooltip("Color para ganancias de dinero (enemigos)")]
        public Color gainColor = Color.green;
        
        [Tooltip("Color para pérdidas de dinero (inocentes)")]
        public Color lossColor = Color.red;
        
        [Tooltip("Tamaño de fuente del texto")]
        [Range(20, 80)]
        public float fontSize = 36f;
        
        [Header("Animation Settings")]
        [Tooltip("Duración de la animación en segundos")]
        [Range(0.5f, 3f)]
        public float animationDuration = 1.5f;
        
        [Tooltip("Distancia que sube el texto (en pixels)")]
        [Range(30f, 150f)]
        public float moveDistance = 80f;
        
        [Tooltip("Usar fade out al final")]
        public bool useFadeOut = true;
        
        [Header("World to Screen")]
        [Tooltip("Cámara principal (se busca automáticamente si está vacío)")]
        public Camera mainCamera;
        
        [Tooltip("Canvas donde se mostrarán los textos")]
        public Canvas targetCanvas;
        
        [Header("Optimization")]
        [Tooltip("Offset aleatorio para evitar textos apilados")]
        [Range(0f, 30f)]
        public float randomOffset = 15f;
        
        // Pool de objetos
        private Queue<GameObject> textPool;
        private List<GameObject> activeTexts;
        
        // Referencias
        private RectTransform canvasRect;
        
        void Start()
        {
            InitializePool();
        }
        
        void InitializePool()
        {
            // Buscar cámara si no está asignada
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("FloatingMoneyFeedback: No se encontró cámara principal");
                    return;
                }
            }
            
            // Buscar o crear canvas
            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
                if (targetCanvas == null)
                {
                    Debug.LogError("FloatingMoneyFeedback: No se encontró Canvas en la escena");
                    return;
                }
            }
            
            canvasRect = targetCanvas.GetComponent<RectTransform>();
            
            // Crear prefab por defecto si no existe
            if (floatingTextPrefab == null)
            {
                CreateDefaultPrefab();
            }
            
            // Inicializar pool
            textPool = new Queue<GameObject>();
            activeTexts = new List<GameObject>();
            
            // Pre-instanciar objetos del pool
            for (int i = 0; i < poolSize; i++)
            {
                GameObject textObj = Instantiate(floatingTextPrefab, targetCanvas.transform);
                textObj.SetActive(false);
                textPool.Enqueue(textObj);
            }
            
            Debug.Log($"FloatingMoneyFeedback pool inicializado con {poolSize} objetos");
        }
        
        void CreateDefaultPrefab()
        {
            // Crear prefab básico en runtime
            GameObject prefab = new GameObject("FloatingText");
            
            TextMeshProUGUI text = prefab.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
            
            RectTransform rect = prefab.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);
            
            floatingTextPrefab = prefab;
            Debug.LogWarning("FloatingMoneyFeedback: Prefab creado automáticamente. Recomendado asignar uno custom.");
        }
        
        // MÉTODO PRINCIPAL: Mostrar feedback en posición 3D
        public void ShowMoneyGain(Vector3 worldPosition, int amount, bool isPositive = true)
        {
            if (textPool == null || textPool.Count == 0)
            {
                Debug.LogWarning("Pool vacío, no se puede mostrar feedback");
                return;
            }
            
            // Obtener objeto del pool
            GameObject textObj = textPool.Dequeue();
            textObj.SetActive(true);
            activeTexts.Add(textObj);
            
            // Configurar texto
            TextMeshProUGUI textComponent = textObj.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                string prefix = isPositive ? "+" : "-";
                textComponent.text = $"{prefix}${Mathf.Abs(amount)}";
                textComponent.color = isPositive ? gainColor : lossColor;
                textComponent.fontSize = fontSize;
            }
            
            // Convertir posición 3D a screen space
            Vector2 screenPos = WorldToCanvasPosition(worldPosition);
            
            // Aplicar offset aleatorio para evitar superposición
            screenPos += new Vector2(
                Random.Range(-randomOffset, randomOffset),
                Random.Range(-randomOffset/2, randomOffset/2)
            );
            
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = screenPos;
            }
            
            // Iniciar animación
            StartCoroutine(AnimateFloatingText(textObj, screenPos, textComponent));
            
            Debug.Log($"Feedback mostrado: {(isPositive ? "+" : "-")}${amount} en {worldPosition}");
        }
        
        // Convertir posición mundo a posición en canvas
        Vector2 WorldToCanvasPosition(Vector3 worldPosition)
        {
            // Convertir de world space a screen space
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
            
            // Convertir de screen space a canvas space
            Vector2 canvasPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
                out canvasPos
            );
            
            return canvasPos;
        }
        
        // Animación del texto flotante
        System.Collections.IEnumerator AnimateFloatingText(GameObject textObj, Vector2 startPos, TextMeshProUGUI textComponent)
        {
            float elapsed = 0f;
            Vector2 endPos = startPos + new Vector2(0, moveDistance);
            Color startColor = textComponent.color;
            
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / animationDuration;
                
                // Curva de ease out
                float easedProgress = 1f - (1f - progress) * (1f - progress);
                
                // Mover hacia arriba
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, easedProgress);
                }
                
                // Fade out en la segunda mitad
                if (useFadeOut && progress > 0.5f)
                {
                    float fadeProgress = (progress - 0.5f) / 0.5f;
                    Color currentColor = startColor;
                    currentColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
                    textComponent.color = currentColor;
                }
                
                yield return null;
            }
            
            // Devolver al pool
            ReturnToPool(textObj);
        }
        
        // Devolver objeto al pool
        void ReturnToPool(GameObject textObj)
        {
            textObj.SetActive(false);
            activeTexts.Remove(textObj);
            textPool.Enqueue(textObj);
        }
        
        // MÉTODOS PÚBLICOS DE UTILIDAD
        
        // Sobrecarga para llamar con RaycastHit
        public void ShowMoneyGainAtHit(RaycastHit hit, int amount, bool isPositive = true)
        {
            ShowMoneyGain(hit.point, amount, isPositive);
        }
        
        // Sobrecarga para llamar con RaycastHit2D
        public void ShowMoneyGainAtHit2D(RaycastHit2D hit, int amount, bool isPositive = true)
        {
            ShowMoneyGain(hit.point, amount, isPositive);
        }
        
        // Limpiar todos los textos activos
        public void ClearAllActiveTexts()
        {
            foreach (GameObject textObj in activeTexts.ToArray())
            {
                StopAllCoroutines();
                ReturnToPool(textObj);
            }
            activeTexts.Clear();
        }
        
        // MÉTODOS DE CONFIGURACIÓN
        
        public void SetGainColor(Color color)
        {
            gainColor = color;
        }
        
        public void SetLossColor(Color color)
        {
            lossColor = color;
        }
        
        public void SetAnimationDuration(float duration)
        {
            animationDuration = Mathf.Max(0.5f, duration);
        }
        
        // TESTING
        
        [ContextMenu("Test Floating Money +100")]
        void TestPositiveFeedback()
        {
            Vector3 testPos = mainCamera.transform.position + mainCamera.transform.forward * 5f;
            ShowMoneyGain(testPos, 100, true);
        }
        
        [ContextMenu("Test Floating Money -50")]
        void TestNegativeFeedback()
        {
            Vector3 testPos = mainCamera.transform.position + mainCamera.transform.forward * 5f;
            ShowMoneyGain(testPos, 50, false);
        }
        
        void OnDestroy()
        {
            // Limpiar pool
            if (textPool != null)
            {
                foreach (GameObject obj in textPool)
                {
                    if (obj != null) Destroy(obj);
                }
                textPool.Clear();
            }
            
            if (activeTexts != null)
            {
                foreach (GameObject obj in activeTexts)
                {
                    if (obj != null) Destroy(obj);
                }
                activeTexts.Clear();
            }
        }
    }
}