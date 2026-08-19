using UnityEngine;

[System.Serializable]
public class QTEConfig
{
    public string qteName;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Timing")]
    public float spawnInterval = 2f;
    public float duration = 2f;

    [Header("Spawn")]
    public float minDistance = 150f;

    [Header("Probabilité")]
    [Range(0f, 100f)]
    public float spawnChance = 100f;
}