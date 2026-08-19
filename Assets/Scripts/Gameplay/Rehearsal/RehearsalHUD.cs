using System.Collections.Generic;
using Magma.Data;
using UnityEngine;

namespace Magma.Gameplay.Rehearsal
{
    /// <summary>
    /// HUD temporaire dessiné en IMGUI (placeholder existant, hors périmètre de cette
    /// extraction). Affiche le score courant et fait apparaître une étiquette de
    /// jugement colorée et éphémère à la position de chaque note.
    /// </summary>
    [RequireComponent(typeof(RehearsalManager))]
    public class RehearsalHUD : MonoBehaviour
    {
        // A floating judgement label that rises and fades over its lifetime.
        private struct JudgementPopup
        {
            public string Text;
            public Color Color;
            public Vector3 WorldPosition;
            public float SpawnTime;
        }

        private RehearsalManager rehearsalManager;
        private Camera hudCamera;

        private readonly List<JudgementPopup> popups = new List<JudgementPopup>();

        private GUIStyle scoreStyle;
        private GUIStyle popupStyle;
        private bool stylesReady;

        private static readonly Color PerfectColor = new Color(1f, 0.85f, 0.2f);
        private static readonly Color GoodColor = new Color(0.4f, 0.85f, 1f);
        private static readonly Color MissColor = new Color(1f, 0.35f, 0.35f);

        private const float PopupLifetimeSeconds = 0.9f;
        private const float PopupRiseWorldUnits = 0.7f;

        private void Awake()
        {
            rehearsalManager = GetComponent<RehearsalManager>();
            hudCamera = Camera.main;
        }

        private void OnEnable()
        {
            rehearsalManager.Judged += OnNoteJudged;
        }

        private void OnDisable()
        {
            rehearsalManager.Judged -= OnNoteJudged;
        }

        private void Update()
        {
            // Prune expired popups outside of OnGUI to keep drawing stable.
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                if (Time.time - popups[i].SpawnTime >= PopupLifetimeSeconds)
                {
                    popups.RemoveAt(i);
                }
            }
        }

        private void OnNoteJudged(Judgement result, Vector3 worldPosition)
        {
            popups.Add(new JudgementPopup
            {
                Text = GetLabel(result),
                Color = GetColor(result),
                WorldPosition = worldPosition,
                SpawnTime = Time.time
            });
        }

        private void OnGUI()
        {
            EnsureStyles();

            DrawScore();
            DrawPopups();
        }

        private void DrawScore()
        {
            GUI.Label(
                new Rect(0f, 12f, Screen.width, 40f),
                "Score : " + rehearsalManager.CurrentScore,
                scoreStyle
            );
        }

        private void DrawPopups()
        {
            if (hudCamera == null)
            {
                return;
            }

            foreach (JudgementPopup popup in popups)
            {
                float age = Time.time - popup.SpawnTime;
                float t = Mathf.Clamp01(age / PopupLifetimeSeconds);

                Vector3 risenWorld = popup.WorldPosition + Vector3.up * (t * PopupRiseWorldUnits);
                Vector3 screenPoint = hudCamera.WorldToScreenPoint(risenWorld);

                if (screenPoint.z < 0f)
                {
                    continue;
                }

                // IMGUI origin is top-left, screen origin is bottom-left, so flip Y.
                float guiX = screenPoint.x - 60f;
                float guiY = Screen.height - screenPoint.y - 16f;

                Color color = popup.Color;
                color.a = 1f - t;

                Color previous = GUI.color;
                GUI.color = color;

                GUI.Label(new Rect(guiX, guiY, 120f, 32f), popup.Text, popupStyle);

                GUI.color = previous;
            }
        }

        private void EnsureStyles()
        {
            if (stylesReady)
            {
                return;
            }

            scoreStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
            scoreStyle.normal.textColor = Color.white;

            popupStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            stylesReady = true;
        }

        private static string GetLabel(Judgement result)
        {
            switch (result)
            {
                case Judgement.Perfect:
                    return "PERFECT";

                case Judgement.Good:
                    return "GOOD";

                default:
                    return "MISS";
            }
        }

        private static Color GetColor(Judgement result)
        {
            switch (result)
            {
                case Judgement.Perfect:
                    return PerfectColor;

                case Judgement.Good:
                    return GoodColor;

                default:
                    return MissColor;
            }
        }
    }
}
