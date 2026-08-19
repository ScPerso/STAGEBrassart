using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConcertManager : MonoBehaviour, IQTECircleHit
{
    [Header("Références")]
    [SerializeField] private RectTransform spawnZone;
    [SerializeField] private Slider satisfactionBar;

    [Header("QTE")]
    [SerializeField] private QTEConfig[] qteConfigs;
    private List<CircleQTE> activeQTEs = new List<CircleQTE>();

    [Header("Satisfaction")]
    [SerializeField, Range(0f, 100f)]
    private float satisfaction = 50f;

    [SerializeField]
    private float satisfactionGain = 5f;

    [SerializeField]
    private float satisfactionLoss = 10f;

    [Header("Configuration")]
    [SerializeField]
    private int accuracyLevel = 80;

    [Header("Spawn")]
    [SerializeField]
    private int maxSpawnAttempts = 20;


    private void Start()
    {
        //UpdateSatisfactionBar();

        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            QTEConfig config = GetRandomQTEConfig();

            if (config != null)
            {
                SpawnQTE(config);

                yield return new WaitForSeconds(
                    config.spawnInterval
                );
            }
            else
            {
                yield return null;
            }
        }
    }

    public void Hit(float accuracy)
    {

    }

    private QTEConfig GetRandomQTEConfig()
    {
        if (qteConfigs == null || qteConfigs.Length == 0)
        {
            Debug.LogWarning("Aucun QTE configuré.");
            return null;
        }

        float randomValue = Random.Range(0f, 100f);

        float currentValue = 0f;

        foreach (QTEConfig config in qteConfigs)
        {
            currentValue += config.spawnChance;

            if (randomValue <= currentValue)
            {
                return config;
            }
        }

        return qteConfigs[0];
    }

    private void SpawnQTE(QTEConfig config)
    {
        Vector2 spawnPosition;

        if (!TryGetSpawnPosition(out spawnPosition, config.minDistance))
        {
            Debug.LogWarning(
                "Impossible de trouver une position libre."
            );

            return;
        }

        GameObject qte = Instantiate(
            config.prefab,
            spawnPosition,
            Quaternion.identity,
            spawnZone
        );

        CircleQTE circleQTE =
            qte.GetComponent<CircleQTE>();

        if (circleQTE != null)
        {
            circleQTE.Initialize(
                config.duration,
                this
            );
        }
    }

    private bool TryGetSpawnPosition(
    out Vector2 position,
    float minDistance
)
    {
        for (int attempt = 0;
             attempt < maxSpawnAttempts;
             attempt++)
        {
            float x = Random.Range(
                -spawnZone.rect.width / 2f,
                spawnZone.rect.width / 2f
            );

            float y = Random.Range(
                -spawnZone.rect.height / 2f,
                spawnZone.rect.height / 2f
            );

            Vector2 candidate =
                new Vector2(x, y);

            bool valid = true;

            foreach (CircleQTE qte in activeQTEs)
            {
                if (qte == null)
                    continue;

                RectTransform qteRect =
                    qte.GetComponent<RectTransform>();

                float distance =
                    Vector2.Distance(
                        candidate,
                        qteRect.anchoredPosition
                    );

                if (distance < minDistance)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                position = candidate;
                return true;
            }
        }

        position = Vector2.zero;

        return false;
    }
}