using UnityEngine;

public class CircleQTE : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private RectTransform approachCircle;
    [SerializeField] private RectTransform hitCircle;

    [Header("Configuration")]
    [SerializeField] private float startScale = 2.5f;
    [SerializeField] private float endScale = 1f;
    [SerializeField] private float duration = 2f;

    private float timer;
    private bool isActive;

    private IQTECircleHit qteHit;

    public void Initialize(
        float qteDuration,
        IQTECircleHit hitReceiver
    )
    {
        duration = qteDuration;

        qteHit = hitReceiver;

        StartQTE();
    }

    private void Update()
    {
        if (!isActive)
            return;

        timer += Time.deltaTime;

        float progress =
            timer / duration;

        progress =
            Mathf.Clamp01(progress);

        float currentScale =
            Mathf.Lerp(
                startScale,
                endScale,
                progress
            );

        approachCircle.localScale =
            Vector3.one * currentScale;

        if (progress >= 1f)
        {
            FailQTE();
        }
    }

    public void StartQTE()
    {
        timer = 0f;

        isActive = true;

        approachCircle.localScale =
            Vector3.one * startScale;
    }

    public void OnPlayerClick()
    {
        if (!isActive)
            return;

        float currentScale =
            approachCircle.localScale.x;

        float error =
            Mathf.Abs(
                currentScale - endScale
            );

        float accuracy =
            CalculateAccuracy(error);

        if (qteHit != null)
        {
            qteHit.Hit(accuracy);
        }
        else
        {
            Debug.LogError(
                "Aucun IQTECircleHit trouvé !"
            );
        }

        Debug.Log(
            "Précision : "
            + accuracy.ToString("F1")
            + "%"
        );

        isActive = false;

        Destroy(gameObject);
    }

    private float CalculateAccuracy(
        float error
    )
    {
        float maxError = 1.5f;

        float accuracy =
            1f - (error / maxError);

        accuracy =
            Mathf.Clamp01(accuracy);

        return accuracy * 100f;
    }

    private void FailQTE()
    {
        isActive = false;

        Debug.Log("QTE raté !");

        Destroy(gameObject);
    }
}