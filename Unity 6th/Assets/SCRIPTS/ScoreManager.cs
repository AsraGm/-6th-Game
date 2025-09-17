// Archivo: ScoreManager.cs - REEMPLAZA el actual
using UnityEngine;
using System.Collections;

// A3: Sistema de puntuación y economía en tiempo real
[CreateAssetMenu(fileName = "EnemyMoneyValues", menuName = "ShootingRange/Enemy Money Values")]
public class EnemyMoneyValues : ScriptableObject
{
    [Header("Money Values by Enemy Type")]
    public int normalEnemyValue = 10;
    public int fastEnemyValue = 15;
    public int jumperEnemyValue = 20;
    public int valuableEnemyValue = 50;
    public int innocentPenalty = -25;

    public int GetMoneyValue(EnemyType enemyType, bool isInnocent = false)
    {
        if (isInnocent)
            return innocentPenalty;

        switch (enemyType)
        {
            case EnemyType.Normal: return normalEnemyValue;
            case EnemyType.Fast: return fastEnemyValue;
            case EnemyType.Jumper: return jumperEnemyValue;
            case EnemyType.Valuable: return valuableEnemyValue;
            default: return normalEnemyValue;
        }
    }
}

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Settings")]
    [SerializeField] private EnemyMoneyValues enemyMoneyValues;
    [SerializeField] private int startingMoney = 0;

    [Header("UI References")]
    [SerializeField] private TMPro.TextMeshProUGUI moneyText;
    [SerializeField] private TMPro.TextMeshProUGUI penaltyText;

    // Events para actualizar UI solo cuando cambian los valores
    public System.Action<int> OnMoneyChanged;
    public System.Action<int> OnPenaltyReceived;

    private int currentMoney;
    private int lastDisplayedMoney = -1;
    private bool uiNeedsUpdate = false;

    public int CurrentMoney => currentMoney;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            currentMoney = startingMoney;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Inicializar UI
        UpdateMoneyUI(true);
    }

    private void Update()
    {
        // Optimización: Actualizar UI solo cuando cambien los valores
        if (uiNeedsUpdate)
        {
            UpdateMoneyUI();
            uiNeedsUpdate = false;
        }
    }

    public void ProcessEnemyHit(EnemyType enemyType, int baseValue)
    {
        int moneyToAdd = enemyMoneyValues != null
            ? enemyMoneyValues.GetMoneyValue(enemyType, false)
            : baseValue;

        AddMoney(moneyToAdd);
    }

    public void ProcessInnocentHit(int baseValue)
    {
        int penalty = enemyMoneyValues != null
            ? enemyMoneyValues.GetMoneyValue(EnemyType.Normal, true)
            : -baseValue;

        AddMoney(penalty);
        ShowPenalty(Mathf.Abs(penalty));
    }

    private void AddMoney(int amount)
    {
        currentMoney += amount;
        if (currentMoney < 0) currentMoney = 0; // No permitir dinero negativo

        OnMoneyChanged?.Invoke(currentMoney);
        uiNeedsUpdate = true;
    }

    private void UpdateMoneyUI(bool forceUpdate = false)
    {
        if (moneyText == null) return;

        // Solo actualizar si el valor cambió o es forzado
        if (lastDisplayedMoney != currentMoney || forceUpdate)
        {
            moneyText.text = $"${currentMoney}";

            // Efecto de escala simple sin DOTween
            StartCoroutine(ScaleEffect());

            // Color verde para ganancias, rojo para pérdidas
            if (currentMoney > lastDisplayedMoney)
            {
                moneyText.color = Color.green;
                StartCoroutine(ColorFade(Color.white, 0.5f));
            }
            else if (currentMoney < lastDisplayedMoney)
            {
                moneyText.color = Color.red;
                StartCoroutine(ColorFade(Color.white, 0.5f));
            }

            lastDisplayedMoney = currentMoney;
        }
    }

    private void ShowPenalty(int penaltyAmount)
    {
        if (penaltyText == null) return;

        penaltyText.text = $"-${penaltyAmount}";
        penaltyText.color = Color.red;
        penaltyText.gameObject.SetActive(true);

        // Animación del penalty SIN DOTween
        StartCoroutine(PenaltyAnimation());

        OnPenaltyReceived?.Invoke(penaltyAmount);
    }

    public void ResetMoney()
    {
        currentMoney = startingMoney;
        OnMoneyChanged?.Invoke(currentMoney);
        uiNeedsUpdate = true;
    }

    public bool CanAfford(int cost)
    {
        return currentMoney >= cost;
    }

    public bool SpendMoney(int amount)
    {
        if (CanAfford(amount))
        {
            AddMoney(-amount);
            return true;
        }
        return false;
    }

    // Corrutinas para reemplazar DOTween
    private IEnumerator ScaleEffect()
    {
        Vector3 originalScale = moneyText.transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;

        float duration = 0.15f;
        float elapsed = 0f;

        // Escalar hacia arriba
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            moneyText.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        elapsed = 0f;
        // Escalar hacia abajo
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            moneyText.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        moneyText.transform.localScale = originalScale;
    }

    private IEnumerator ColorFade(Color targetColor, float duration)
    {
        Color startColor = moneyText.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            moneyText.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        moneyText.color = targetColor;
    }

    private IEnumerator PenaltyAnimation()
    {
        penaltyText.transform.localScale = Vector3.zero;

        // Aparecer
        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            penaltyText.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }

        // Esperar
        yield return new WaitForSeconds(1f);

        // Desaparecer
        elapsed = 0f;
        duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            penaltyText.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            yield return null;
        }

        penaltyText.gameObject.SetActive(false);
    }
}