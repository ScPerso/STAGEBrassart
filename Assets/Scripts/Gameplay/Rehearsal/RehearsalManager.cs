using System;
using Magma.Data;
using Magma.Rhythm;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Magma.Gameplay.Rehearsal
{
    /// <summary>
    /// Orchestrateur du mini-jeu de répétition. Compose l'horloge (Conductor), le
    /// spawner de notes, le juge, le combo et le score ; route les entrées joueur
    /// (souris, clavier, tactile) vers le jugement et décide de la fin de partie.
    /// Toute la mécanique de rythme vit dans Magma.Rhythm.
    /// </summary>
    public class RehearsalManager : MonoBehaviour
    {
        [Header("Composition")]
        [Tooltip("Horloge audio du morceau.")]
        [SerializeField] private Conductor conductor;

        [Tooltip("Spawner qui gère l'apparition et le recyclage des notes.")]
        [SerializeField] private NoteSpawner noteSpawner;

        [Tooltip("Morceau (chart + musique) joué durant cette répétition.")]
        [SerializeField] private SongTrack songTrack;

        [Header("Entrées colonnes")]
        [Tooltip("Touche clavier de chaque colonne, dans l'ordre des colonnes.")]
        [SerializeField] private Key[] laneKeys;

        [Header("Fenêtres de jugement (secondes)")]
        [Tooltip("Fenêtre Perfect (valeur historique du projet : 0.6).")]
        [SerializeField] private float perfectWindow = DefaultPerfectWindow;

        [Tooltip("Fenêtre Good (valeur historique du projet : 1.0).")]
        [SerializeField] private float goodWindow = DefaultGoodWindow;

        [Tooltip("Fenêtre d'expiration en retard (valeur historique du projet : 1.0).")]
        [SerializeField] private float missWindow = DefaultMissWindow;

        private const float DefaultPerfectWindow = 0.6f;
        private const float DefaultGoodWindow = 1.0f;
        private const float DefaultMissWindow = 1.0f;

        // The piano rehearsal keeps a flat multiplier to preserve historical scoring;
        // the combo is tracked and available for other mini-games (e.g. Concert).
        private const float PianoScoreMultiplier = 1f;

        private NoteJudge judge;
        private readonly RhythmScorer scorer = new RhythmScorer();
        private readonly ComboSystem combo = new ComboSystem();

        private Camera mainCamera;
        private bool isRunning;

        /// <summary>Déclenché une fois la répétition terminée, avec le résultat.</summary>
        public event Action<MiniGameResult> Completed;

        /// <summary>Déclenché à chaque note jugée, avec le jugement et la position monde.</summary>
        public event Action<Judgement, Vector3> Judged;

        /// <summary>Score courant accumulé cette répétition.</summary>
        public int CurrentScore => scorer.TotalScore;

        /// <summary>Nombre de Perfect de la répétition courante.</summary>
        public int PerfectCount => scorer.PerfectCount;

        /// <summary>Nombre de Good de la répétition courante.</summary>
        public int GoodCount => scorer.GoodCount;

        /// <summary>Nombre de Miss de la répétition courante.</summary>
        public int MissCount => scorer.MissCount;

        /// <summary>Score maximal théorique (tout Perfect, sans multiplicateur).</summary>
        public int MaxScore => scorer.TotalRegistered * RhythmScorer.PerfectScore;

        private void Awake()
        {
            mainCamera = Camera.main;
            judge = new NoteJudge(perfectWindow, goodWindow, missWindow);
        }

        private void OnEnable()
        {
            if (noteSpawner != null)
            {
                noteSpawner.OnNoteExpired += HandleNoteExpired;
            }
        }

        private void OnDisable()
        {
            if (noteSpawner != null)
            {
                noteSpawner.OnNoteExpired -= HandleNoteExpired;
            }
        }

        private void Start()
        {
            StartRehearsal();
        }

        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            float songTime = conductor.SongPositionSeconds;

            HandleMouseInput(songTime);
            HandleKeyboardInput(songTime);
            HandleTouchInput(songTime);

            if (noteSpawner.IsFinished)
            {
                EndRehearsal();
            }
        }

        /// <summary>Démarre (ou relance) la répétition depuis le début.</summary>
        public void StartRehearsal()
        {
            if (conductor == null || noteSpawner == null || songTrack == null)
            {
                Debug.LogWarning("RehearsalManager is missing a Conductor, NoteSpawner or SongTrack reference.");
                return;
            }

            scorer.Reset();
            combo.Reset();

            noteSpawner.Setup(songTrack, conductor, songTrack.travelTime);
            conductor.Play(songTrack);

            isRunning = true;
        }

        /// <summary>Relance la répétition en repartant de zéro.</summary>
        public void Restart()
        {
            conductor.Stop();
            noteSpawner.Clear();
            StartRehearsal();
        }

        private void HandleMouseInput(float songTime)
        {
            Mouse mouse = Mouse.current;

            if (mouse == null || mainCamera == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                FallingNote note = hit.collider.GetComponentInParent<FallingNote>();

                // A click on a note still too far from its target time is ignored rather than
                // scored as a Miss, mirroring the keyboard/touch lane guard. Only a note that
                // is close enough (within the Good window) gets judged.
                if (note != null && Mathf.Abs(songTime - note.TargetTime) <= goodWindow)
                {
                    JudgeNote(note, songTime);
                }
            }
        }

        private void HandleKeyboardInput(float songTime)
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null || laneKeys == null)
            {
                return;
            }

            for (int lane = 0; lane < laneKeys.Length; lane++)
            {
                Key key = laneKeys[lane];

                if (key != Key.None && keyboard[key].wasPressedThisFrame)
                {
                    TryHitLane(lane, songTime);
                }
            }
        }

        private void HandleTouchInput(float songTime)
        {
            Touchscreen touchscreen = Touchscreen.current;

            if (touchscreen == null)
            {
                return;
            }

            foreach (var touch in touchscreen.touches)
            {
                if (touch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Began)
                {
                    continue;
                }

                int lane = GetLaneFromScreenX(touch.position.ReadValue().x);
                TryHitLane(lane, songTime);
            }
        }

        private void TryHitLane(int lane, float songTime)
        {
            FallingNote note = noteSpawner.GetActiveNoteInLane(lane);

            if (note == null)
            {
                return;
            }

            // A tap nowhere near a note (empty-lane tap) scores nothing.
            if (Mathf.Abs(songTime - note.TargetTime) > goodWindow)
            {
                return;
            }

            JudgeNote(note, songTime);
        }

        private void JudgeNote(FallingNote note, float songTime)
        {
            if (note.IsResolved)
            {
                return;
            }

            Judgement judgement = judge.Evaluate(note.TargetTime, songTime, out _);

            note.Resolve();
            combo.RegisterHit(judgement);
            scorer.Register(judgement, PianoScoreMultiplier);

            Judged?.Invoke(judgement, note.transform.position);
        }

        private void HandleNoteExpired(NoteData note)
        {
            combo.RegisterHit(Judgement.Miss);
            scorer.Register(Judgement.Miss, PianoScoreMultiplier);

            Judged?.Invoke(Judgement.Miss, noteSpawner.GetLaneBottomPosition(note.laneIndex));
        }

        private int GetLaneFromScreenX(float screenX)
        {
            int laneCount = Mathf.Max(1, noteSpawner.LaneCount);
            float normalized = Mathf.Clamp01(screenX / Screen.width);
            int lane = (int)(normalized * laneCount);

            return Mathf.Clamp(lane, 0, laneCount - 1);
        }

        private void EndRehearsal()
        {
            isRunning = false;
            conductor.Stop();

            Completed?.Invoke(BuildResult());
        }

        private MiniGameResult BuildResult()
        {
            return new MiniGameResult
            {
                Id = MiniGameId.Music,
                RawScore = scorer.TotalScore,
                Accuracy = scorer.Accuracy,
                TargetStat = StatType.Music,
                StatGain = 0,
                Completed = true
            };
        }
    }
}
