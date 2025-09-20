using UnityEngine;
using UnityEngine.UI;
using TMPro;
// ELIMINADO: using DG.Tweening;

// ARCHIVO: MoneyDisplay.cs - VERSIÓN SIN DOTWEEN
// UI del dinero con animaciones básicas usando Corrutinas

namespace ShootingRange
{
    public class MoneyDisplay : MonoBehaviour
    {
        [Header("Referencias UI")]
        [Tooltip("ARRASTRA AQUÍ tu Text/TextMeshPro del contador principal de dinero")]
        public TextMeshProUGUI mainMoneyText;

        [Tooltip("ARRASTRA AQUÍ tu Text/TextMeshPro para mostrar cambios (+X / -X)")]
        public TextMeshProUGUI changeIndicatorText;

        [Tooltip("ARRASTRA AQUÍ un Image para el fondo del contador (opcional)")]
        public Image backgroundImage;

        [Header("Configuración Visual")]
        [Tooltip("Color para ganancias de dinero")]
        public Color gainColor = Color.green;

        [Tooltip("Color para pérdidas de dinero")]
        public Color lossColor = Color.red;

        [Tooltip("Color normal del texto principal")]
        public Color normalColor = Color.white;

        [Header("Configuración de Animaciones")]
        [Tooltip("Duración de la animación del contador principal")]
        [Range(0.1f, 2.0f)]
        public float counterAnimationDuration = 0.8f;

        [Tooltip("Duración de la animación del indicador de cambio")]
        [Range(0.1f, 3.0f)]
        public float changeIndicatorDuration = 1.5f;

        [Header("Efectos Visuales")]
        [Tooltip("Escalar el texto principal al cambiar")]
        public bool useScaleEffect = true;

        [Tooltip("Escala máxima del efecto (1.0 = sin efecto)")]
        [Range(1.0f, 1.5f)]
        public float maxScale = 1.2f;

        [Header("Formato de Texto")]
        [Tooltip("Prefijo para el dinero (ej: '$', 'G', '₽')")]
        public string moneyPrefix = "$";

        [Tooltip("Usar separadores de miles (1,000 en lugar de 1000)")]
        public bool useThousandSeparators = true;

        // Variables privadas
        private int currentDisplayedMoney = 0;
        private Vector3 originalScale;
        private Coroutine currentAnimation;

        // Cache para optimización
        private bool isAnimating = false;

        void Start()
        {
            InitializeDisplay();
        }

        void InitializeDisplay()
        {
            // Guardar escala original
            if (mainMoneyText != null)
            {
                originalScale = mainMoneyText.transform.localScale;
            }

            // Configurar colores iniciales
            if (mainMoneyText != null)
            {
                mainMoneyText.color = normalColor;
            }

            // Ocultar indicador de cambio inicialmente
            if (changeIndicatorText != null)
            {
                changeIndicatorText.gameObject.SetActive(false);
            }

            Debug.Log("MoneyDisplay inicializado");
        }

        // MÉTODO PRINCIPAL: Actualizar dinero con animación
        public void SetMoney(int newAmount, bool animate = true, int changeAmount = 0, bool isPositive = true)
        {
            // Detener animaciones previas
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            if (animate)
            {
                currentAnimation = StartCoroutine(AnimateMoneyCoroutine(newAmount, changeAmount, isPositive));
            }
            else
            {
                // Sin animación, actualizar directamente
                UpdateMoneyText(newAmount);
                currentDisplayedMoney = newAmount;
            }
        }

        // Animación con Corrutina (sin DOTween)
        System.Collections.IEnumerator AnimateMoneyCoroutine(int targetAmount, int changeAmount, bool isPositive)
        {
            if (mainMoneyText == null) yield break;

            isAnimating = true;
            int startAmount = currentDisplayedMoney;
            float elapsed = 0f;

            // Mostrar indicador de cambio
            if (changeAmount != 0 && changeIndicatorText != null)
            {
                ShowChangeIndicator(changeAmount, isPositive);
            }

            // Efecto de escala inicial
            if (useScaleEffect && mainMoneyText != null)
            {
                StartCoroutine(ScaleEffect());
            }

            // Animar contador
            while (elapsed < counterAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / counterAnimationDuration;

                // Curva de suavizado
                progress = 1f - (1f - progress) * (1f - progress); // Ease out

                currentDisplayedMoney = Mathf.RoundToInt(Mathf.Lerp(startAmount, targetAmount, progress));
                UpdateMoneyText(currentDisplayedMoney);

                yield return null;
            }

            currentDisplayedMoney = targetAmount;
            UpdateMoneyText(currentDisplayedMoney);
            isAnimating = false;
        }

        // Efecto de escala sin DOTween
        System.Collections.IEnumerator ScaleEffect()
        {
            if (mainMoneyText == null) yield break;

            float duration = counterAnimationDuration * 0.3f;
            float elapsed = 0f;

            // Escalar hacia arriba
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;

                Vector3 scale = Vector3.Lerp(originalScale, originalScale * maxScale, progress);
                mainMoneyText.transform.localScale = scale;

                yield return null;
            }

            // Escalar hacia abajo
            elapsed = 0f;
            duration = counterAnimationDuration * 0.7f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;

                Vector3 scale = Vector3.Lerp(originalScale * maxScale, originalScale, progress);
                mainMoneyText.transform.localScale = scale;

                yield return null;
            }

            mainMoneyText.transform.localScale = originalScale;
        }

        // Mostrar indicador de cambio (+X / -X)
        void ShowChangeIndicator(int amount, bool isPositive)
        {
            if (changeIndicatorText == null) return;

            // Configurar texto
            string prefix = isPositive ? "+" : "-";
            changeIndicatorText.text = $"{prefix}{moneyPrefix}{FormatMoney(Mathf.Abs(amount))}";
            changeIndicatorText.color = isPositive ? gainColor : lossColor;

            // Posicionar debajo del contador principal
            if (mainMoneyText != null)
            {
                RectTransform changeRect = changeIndicatorText.GetComponent<RectTransform>();
                RectTransform mainRect = mainMoneyText.GetComponent<RectTransform>();

                if (changeRect != null && mainRect != null)
                {
                    Vector3 pos = changeRect.position;
                    pos.y = mainRect.position.y - 50f; // 50 pixels abajo
                    changeRect.position = pos;
                }
            }

            // Mostrar y animar
            changeIndicatorText.gameObject.SetActive(true);
            StartCoroutine(HideChangeIndicatorCoroutine());
        }

        // Ocultar indicador de cambio con fade
        System.Collections.IEnumerator HideChangeIndicatorCoroutine()
        {
            if (changeIndicatorText == null) yield break;

            float elapsed = 0f;
            Color startColor = changeIndicatorText.color;
            Vector3 startPos = changeIndicatorText.transform.position;

            while (elapsed < changeIndicatorDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / changeIndicatorDuration;

                // Fade out
                Color color = startColor;
                color.a = Mathf.Lerp(1f, 0f, progress);
                changeIndicatorText.color = color;

                // Move up
                Vector3 pos = startPos;
                pos.y += progress * 30f;
                changeIndicatorText.transform.position = pos;

                yield return null;
            }

            changeIndicatorText.gameObject.SetActive(false);

            // Restore original values
            changeIndicatorText.color = startColor;
            changeIndicatorText.transform.position = startPos;
        }

        // Actualizar el texto del dinero
        void UpdateMoneyText(int amount)
        {
            if (mainMoneyText != null)
            {
                mainMoneyText.text = $"{moneyPrefix}{FormatMoney(amount)}";
            }
        }

        // Formatear números con separadores de miles
        string FormatMoney(int amount)
        {
            if (useThousandSeparators)
            {
                return amount.ToString("N0"); // Formato con separadores
            }
            else
            {
                return amount.ToString();
            }
        }

        // Métodos públicos para otros sistemas
        public void SetColors(Color gain, Color loss, Color normal)
        {
            gainColor = gain;
            lossColor = loss;
            normalColor = normal;

            if (mainMoneyText != null)
            {
                mainMoneyText.color = normalColor;
            }
        }

        public bool IsAnimating()
        {
            return isAnimating;
        }

        public void ForceUpdate(int amount)
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            currentDisplayedMoney = amount;
            UpdateMoneyText(amount);
            isAnimating = false;
        }

        void OnDestroy()
        {
            // Detener corrutinas
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
        }
    }
}