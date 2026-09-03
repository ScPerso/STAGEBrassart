using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// QTE de séquence : le joueur doit reproduire trois ou quatre touches avant la fin du compte à rebours.
/// Son affichage IMGUI le rend directement utilisable sans prefab supplémentaire.
/// </summary>
public class ConcertKeySequenceQTE : MonoBehaviour
{
    private const int MinimumSequenceLength = 3;
    private const int MaximumSequenceLength = 4;
    private const float DefaultDuration = 2.5f;
    private const float PanelWidth = 620f;
    private const float PanelHeight = 170f;
    private const float PanelBottomMargin = 80f;

    private static readonly Key[] AvailableKeys = { Key.A, Key.S, Key.D, Key.F, Key.J, Key.K, Key.L };

    private readonly StringBuilder sequenceLabelBuilder = new StringBuilder();
    private Key[] requiredKeys;
    private int currentKeyIndex;
    private float initialDuration;
    private float remainingTime;
    private bool isActive;
    private Action<float> onSucceeded;
    private Action onFailed;

    /// <summary>Nombre de touches constituant la séquence actuelle.</summary>
    public int SequenceLength => requiredKeys != null ? requiredKeys.Length : 0;

    /// <summary>Numéro de la prochaine touche à saisir, à partir de 1.</summary>
    public int CurrentStep => Mathf.Min(currentKeyIndex + 1, SequenceLength);

    /// <summary>Temps restant du QTE, normalisé entre 0 et 1.</summary>
    public float RemainingTimeNormalized => initialDuration > 0f ? Mathf.Clamp01(remainingTime / initialDuration) : 0f;

    /// <summary>Représentation visuelle des touches et de la progression de la séquence.</summary>
    public string SequenceDisplay => BuildSequenceLabel();

    /// <summary>Génère et démarre une séquence de touches avec les rappels de résolution.</summary>
    public void Initialize(float duration, Action<float> succeededCallback, Action failedCallback)
    {
        initialDuration = Mathf.Max(0.1f, duration > 0f ? duration : DefaultDuration);
        remainingTime = initialDuration;
        onSucceeded = succeededCallback;
        onFailed = failedCallback;
        currentKeyIndex = 0;
        requiredKeys = BuildSequence();
        isActive = true;
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            ResolveFailure();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        for (int index = 0; index < AvailableKeys.Length; index++)
        {
            Key pressedKey = AvailableKeys[index];
            if (!keyboard[pressedKey].wasPressedThisFrame)
            {
                continue;
            }

            if (pressedKey == requiredKeys[currentKeyIndex])
            {
                currentKeyIndex++;
                if (currentKeyIndex >= requiredKeys.Length)
                {
                    float accuracy = Mathf.Clamp01(remainingTime / initialDuration) * 100f;
                    ResolveSuccess(accuracy);
                }
            }
            else
            {
                ResolveFailure();
            }

            return;
        }
    }

    private void OnGUI()
    {
        // L'affichage est géré par ConcertUIController dans la scène pour rester éditable en UGUI.
    }

    private Key[] BuildSequence()
    {
        int sequenceLength = UnityEngine.Random.Range(MinimumSequenceLength, MaximumSequenceLength + 1);
        Key[] sequence = new Key[sequenceLength];

        for (int index = 0; index < sequence.Length; index++)
        {
            sequence[index] = AvailableKeys[UnityEngine.Random.Range(0, AvailableKeys.Length)];
        }

        return sequence;
    }

    private string BuildSequenceLabel()
    {
        sequenceLabelBuilder.Clear();
        for (int index = 0; index < requiredKeys.Length; index++)
        {
            if (index > 0)
            {
                sequenceLabelBuilder.Append("   ");
            }

            sequenceLabelBuilder.Append(index < currentKeyIndex ? "✓" : requiredKeys[index].ToString());
        }

        return sequenceLabelBuilder.ToString();
    }

    private void ResolveSuccess(float accuracy)
    {
        if (!isActive)
        {
            return;
        }

        isActive = false;
        onSucceeded?.Invoke(accuracy);
        Destroy(gameObject);
    }

    private void ResolveFailure()
    {
        if (!isActive)
        {
            return;
        }

        isActive = false;
        onFailed?.Invoke();
        Destroy(gameObject);
    }
}
