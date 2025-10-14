using UnityEngine;
using TMPro;
using System.Collections;

// ARCHIVO: MoneyDisplayUI.cs
// INSTRUCCIÓN D3: HUD de dinero con feedback visual simple
// Contador con transiciones suaves e indicador de pérdidas/ganancias

namespace ShootingRange
{
    public class MoneyDisplayUI : MonoBehaviour
    {
        [Header("Referencias UI")]
        [Tooltip("Texto principal del dinero actual")]
        public TextMeshProUGUI currentMoneyText;
        
        [Tooltip("Texto del indicador de cambio (+X / -X)")]
        public TextMeshProUGUI changeIndicatorText;
        
        [Header("Configuración de Formato")]
        [Tooltip("Prefijo del dinero (ej: $, €, Coins:)")]
        public string moneyPrefix = "$";
        
        [Tooltip("Usar separadores de miles")]
        public bool useThousandsSeparator = true;
        
        [Header("Configuración de Animación")]
        [Tooltip("Usar transición suave en los números")]
        public bool useSmoothTransition = true;
        
        [Tooltip("Duración de la transición (segundos)")]
        [Range(0.1f, 2f)]
        public float transitionDuration = 0.5f;
        
        [Header("Color Coding")]
        [Tooltip("Color para ganancias")]
        public Color gainColor = Color.green;
        
        [Tooltip("Color para pérdidas")]
        public Color lossColor = Color.red;
        
        [Tooltip("Color neutral")]
        public Color neutralColor = Color.white;
        
        [Header("Indicador de Cambio")]
        [Tooltip("Mostrar indicador de cambio debajo del contador")]
        public bool showChangeIndicator = true;
        
        [Tooltip("Duración del indicador visible (segundos)")]
        [Range(0.5f, 3f)]
        public float indicatorDuration = 1.5f;
        
        [Tooltip("Distancia que sube el indicador")]
        [Range(10f, 100f)]
        public float indicatorFloatDistance = 30f;
        
        [Header("Referencias de Sistema")]
        [Tooltip("Sistema de dinero (se busca automáticamente)")]
        public MoneySystem moneySystem;
        
        // Variables privadas
        private int currentDisplayedMoney = 0;
        private int targetMoney = 0;
        private Coroutine transitionCoroutine;
        private Coroutine indicatorCoroutine;
        private Vector3 changeIndicatorOriginalPosition;
        
        void Start()
        {
            InitializeMoneyDisplay();
        }
        
        void InitializeMoneyDisplay()
        {
            // Buscar MoneySystem si no está asignado
            if (moneySystem == null)
            {
                moneySystem = FindObjectOfType<MoneySystem>();
                if (moneySystem == null)
                {
                    Debug.LogWarning("MoneyDisplayUI: No se encontró MoneySystem en la escena");
                }
            }
            
            // Configurar indicador de cambio
            if (changeIndicatorText != null)
            {
                changeIndicatorOriginalPosition = changeIndicatorText.transform.localPosition;
                changeIndicatorText.gameObject.SetActive(false);
            }
            
            // Suscribirse a eventos del MoneySystem
            if (moneySystem != null)
            {
                moneySystem.OnMoneyChanged += OnMoneyChanged;
                
                // Inicializar con dinero actual
                currentDisplayedMoney = moneySystem.CurrentMoney;
                targetMoney = currentDisplayedMoney;
                UpdateMoneyText(currentDisplayedMoney, true);
            }
            else
            {
                // Valor por defecto
                UpdateMoneyText(0, true);
            }
            
            Debug.Log("MoneyDisplayUI inicializado");
        }
        
        // ========================================
        // CALLBACKS DE EVENTOS
        // ========================================
        
        void OnMoneyChanged(int newAmount)
        {
            int change = newAmount - currentDisplayedMoney;
            
            // Actualizar target
            targetMoney = newAmount;
            
            // Mostrar indicador de cambio
            if (showChangeIndicator && change != 0)
            {
                ShowChangeIndicator(change);
            }
            
            // Aplicar transición suave o instantánea
            if (useSmoothTransition)
            {
                if (transitionCoroutine != null)
                {
                    StopCoroutine(transitionCoroutine);
                }
                transitionCoroutine = StartCoroutine(SmoothMoneyTransition(targetMoney));
            }
            else
            {
                currentDisplayedMoney = targetMoney;
                UpdateMoneyText(currentDisplayedMoney, false);
            }
        }
        
        // ========================================
        // ACTUALIZACIÓN DE UI
        // ========================================
        
        void UpdateMoneyText(int amount, bool immediate)
        {
            if (currentMoneyText == null) return;
            
            // Formatear número
            string formattedMoney = FormatMoney(amount);
            currentMoneyText.text = $"{moneyPrefix}{formattedMoney}";
            
            // Aplicar color coding si no es inmediato
            if (!immediate)
            {
                int change = amount - currentDisplayedMoney;
                if (change > 0)
                {
                    StartCoroutine(FlashColor(currentMoneyText, gainColor));
                }
                else if (change < 0)
                {
                    StartCoroutine(FlashColor(currentMoneyText, lossColor));
                }
            }
        }
        
        string FormatMoney(int amount)
        {
            if (useThousandsSeparator)
            {
                return amount.ToString("N0"); // Formato con separadores
            }
            else
            {
                return amount.ToString();
            }
        }
        
        // ========================================
        // TRANSICIÓN SUAVE
        // ========================================
        
        IEnumerator SmoothMoneyTransition(int target)
        {
            int startAmount = currentDisplayedMoney;
            float elapsed = 0f;
            
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / transitionDuration;
                
                // Interpolación suave
                int displayAmount = Mathf.RoundToInt(Mathf.Lerp(startAmount, target, progress));
                
                // Actualizar solo el texto sin color coding
                if (currentMoneyText != null)
                {
                    string formattedMoney = FormatMoney(displayAmount);
                    currentMoneyText.text = $"{moneyPrefix}{formattedMoney}";
                }
                
                yield return null;
            }
            
            // Asegurar valor final exacto
            currentDisplayedMoney = target;
            UpdateMoneyText(currentDisplayedMoney, true);
        }
        
        // ========================================
        // INDICADOR DE CAMBIO
        // ========================================
        
        void ShowChangeIndicator(int change)
        {
            if (changeIndicatorText == null) return;
            
            // Cancelar indicador anterior
            if (indicatorCoroutine != null)
            {
                StopCoroutine(indicatorCoroutine);
            }
            
            indicatorCoroutine = StartCoroutine(ChangeIndicatorCoroutine(change));
        }
        
        IEnumerator ChangeIndicatorCoroutine(int change)
        {
            // Configurar texto
            string prefix = change > 0 ? "+" : "";
            changeIndicatorText.text = $"{prefix}{moneyPrefix}{Mathf.Abs(change)}";
            
            // Configurar color
            changeIndicatorText.color = change > 0 ? gainColor : lossColor;
            
            // Resetear posición
            changeIndicatorText.transform.localPosition = changeIndicatorOriginalPosition;
            
            // Mostrar
            changeIndicatorText.gameObject.SetActive(true);
            
            // Animación de flotación y fade
            float elapsed = 0f;
            Vector3 startPos = changeIndicatorOriginalPosition;
            Vector3 endPos = startPos + new Vector3(0, indicatorFloatDistance, 0);
            Color startColor = changeIndicatorText.color;
            
            while (elapsed < indicatorDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / indicatorDuration;
                
                // Mover hacia arriba
                changeIndicatorText.transform.localPosition = Vector3.Lerp(startPos, endPos, progress);
                
                // Fade out
                Color currentColor = startColor;
                currentColor.a = Mathf.Lerp(1f, 0f, progress);
                changeIndicatorText.color = currentColor;
                
                yield return null;
            }
            
            // Ocultar
            changeIndicatorText.gameObject.SetActive(false);
        }
        
        // ========================================
        // EFECTOS VISUALES
        // ========================================
        
        IEnumerator FlashColor(TextMeshProUGUI text, Color flashColor)
        {
            if (text == null) yield break;
            
            Color originalColor = text.color;
            
            // Flash rápido
            text.color = flashColor;
            yield return new WaitForSecondsRealtime(0.1f);
            
            // Fade back a color neutral
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                
                text.color = Color.Lerp(flashColor, neutralColor, progress);
                
                yield return null;
            }
            
            text.color = neutralColor;
        }
        
        // ========================================
        // MÉTODOS PÚBLICOS
        // ========================================
        
        /// <summary>
        /// Fuerza actualización del display
        /// </summary>
        public void ForceUpdate()
        {
            if (moneySystem != null)
            {
                currentDisplayedMoney = moneySystem.CurrentMoney;
                targetMoney = currentDisplayedMoney;
                UpdateMoneyText(currentDisplayedMoney, true);
            }
        }
        
        /// <summary>
        /// Establece el dinero sin animación
        /// </summary>
        public void SetMoneyInstant(int amount)
        {
            currentDisplayedMoney = amount;
            targetMoney = amount;
            UpdateMoneyText(amount, true);
        }
        
        /// <summary>
        /// Muestra un cambio manual (útil para testing)
        /// </summary>
        public void ShowManualChange(int change)
        {
            if (showChangeIndicator)
            {
                ShowChangeIndicator(change);
            }
        }
        
        // ========================================
        // MÉTODOS DE DEBUG
        // ========================================
        
        [ContextMenu("Test Gain +50")]
        public void TestGain()
        {
            if (moneySystem != null)
            {
                moneySystem.AddMoney(50);
            }
            else
            {
                OnMoneyChanged(currentDisplayedMoney + 50);
            }
        }
        
        [ContextMenu("Test Loss -25")]
        public void TestLoss()
        {
            if (moneySystem != null)
            {
                moneySystem.SpendMoney(25);
            }
            else
            {
                OnMoneyChanged(currentDisplayedMoney - 25);
            }
        }
        
        [ContextMenu("Test Big Gain +500")]
        public void TestBigGain()
        {
            if (moneySystem != null)
            {
                moneySystem.AddMoney(500);
            }
            else
            {
                OnMoneyChanged(currentDisplayedMoney + 500);
            }
        }
        
        [ContextMenu("Force Update Display")]
        public void DebugForceUpdate()
        {
            ForceUpdate();
        }
        
        void OnDestroy()
        {
            // Desuscribirse de eventos
            if (moneySystem != null)
            {
                moneySystem.OnMoneyChanged -= OnMoneyChanged;
            }
        }
        
        void OnValidate()
        {
            transitionDuration = Mathf.Max(0.1f, transitionDuration);
            indicatorDuration = Mathf.Max(0.5f, indicatorDuration);
            indicatorFloatDistance = Mathf.Max(10f, indicatorFloatDistance);
        }
    }
}