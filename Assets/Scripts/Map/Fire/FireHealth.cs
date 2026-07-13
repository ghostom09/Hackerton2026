using System;
using UnityEngine;

public enum FireSize
{
    Small,
    Large
}

[DisallowMultipleComponent]
public class FireHealth : MonoBehaviour
{
    [SerializeField] private FireSize fireSize = FireSize.Small;
    [SerializeField] private float smallFireMaxHealth = 3f;
    [SerializeField] private float largeFireMaxHealth = 8f;
    [SerializeField] private bool resetHealthOnEnable = true;
    [SerializeField] private bool destroyOnExtinguished = true;
    [SerializeField] private GameObject extinguishedEffectPrefab;

    private float currentHealth;
    private bool isExtinguished;

    public event Action<FireHealth, float, float> HealthChanged;
    public event Action<FireHealth> Extinguished;

    public FireSize FireSize => fireSize;
    public float MaxHealth => fireSize == FireSize.Small ? smallFireMaxHealth : largeFireMaxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthRatio => MaxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / MaxHealth);
    public bool IsExtinguished => isExtinguished;

    private void Awake()
    {
        if (!resetHealthOnEnable)
        {
            ResetHealth();
        }
    }

    private void OnEnable()
    {
        if (resetHealthOnEnable)
        {
            ResetHealth();
        }
    }

    private void OnValidate()
    {
        smallFireMaxHealth = Mathf.Max(1f, smallFireMaxHealth);
        largeFireMaxHealth = Mathf.Max(smallFireMaxHealth, largeFireMaxHealth);
    }

    public void SetFireSize(FireSize newFireSize, bool resetHealth = true)
    {
        fireSize = newFireSize;

        if (resetHealth)
        {
            ResetHealth();
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
        NotifyHealthChanged();
    }

    public void ResetHealth()
    {
        isExtinguished = false;
        currentHealth = MaxHealth;
        NotifyHealthChanged();
    }

    public void TakeExtinguishDamage(float amount)
    {
        if (isExtinguished || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, MaxHealth);

        if (currentHealth <= 0f)
        {
            Extinguish();
            return;
        }

        NotifyHealthChanged();
    }

    public void ExtinguishImmediately()
    {
        if (isExtinguished)
        {
            return;
        }

        currentHealth = 0f;
        Extinguish();
    }

    private void Extinguish()
    {
        isExtinguished = true;
        currentHealth = 0f;

        NotifyHealthChanged();
        SpawnExtinguishedEffect();
        Extinguished?.Invoke(this);

        if (destroyOnExtinguished)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void SpawnExtinguishedEffect()
    {
        if (extinguishedEffectPrefab == null)
        {
            return;
        }

        Instantiate(extinguishedEffectPrefab, transform.position, transform.rotation);
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(this, currentHealth, MaxHealth);
    }
}
