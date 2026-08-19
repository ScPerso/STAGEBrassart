using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magma.Rhythm
{
    /// <summary>
    /// Fait apparaître les notes d'un SongTrack en avance sur leur temps cible via un
    /// object pool (aucun Instantiate/Destroy par frame), les fait avancer et les
    /// recycle une fois résolues ou expirées. Le jugement des appuis reste extérieur :
    /// le spawner expose seulement la note candidate d'une colonne.
    /// </summary>
    public class NoteSpawner : MonoBehaviour
    {
        [Tooltip("Prefab instancié pour chaque note. Doit porter un composant FallingNote.")]
        [SerializeField] private GameObject notePrefab;

        [Tooltip("Définition des colonnes. Le laneIndex d'une note pointe vers une entrée.")]
        [SerializeField] private Lane[] lanes;

        [Tooltip("Position Y monde où les notes apparaissent (haut du champ).")]
        [SerializeField] private float spawnHeight = DefaultSpawnHeight;

        [Tooltip("Position Y monde où les notes disparaissent (bas du champ).")]
        [SerializeField] private float despawnHeight = DefaultDespawnHeight;

        [Tooltip("Temps de chute d'une note après son temps cible avant recyclage, en secondes.")]
        [SerializeField] private float noteTrailSeconds = DefaultNoteTrailSeconds;

        [Tooltip("Nombre de notes préchargées dans le pool au démarrage.")]
        [SerializeField] private int poolPrewarm = DefaultPoolPrewarm;

        private const float DefaultSpawnHeight = 5f;
        private const float DefaultDespawnHeight = -5f;
        private const float DefaultNoteTrailSeconds = 1.2f;
        private const int DefaultPoolPrewarm = 32;

        private readonly Queue<FallingNote> pool = new Queue<FallingNote>();
        private readonly List<FallingNote> activeNotes = new List<FallingNote>();
        private readonly List<NoteData> activeData = new List<NoteData>();
        private readonly List<NoteData> orderedNotes = new List<NoteData>();

        private Conductor conductor;
        private float lookahead;
        private int spawnCursor;
        private bool isReady;
        private bool prewarmed;

        /// <summary>Déclenché à chaque note qui apparaît, avec sa donnée et son instance.</summary>
        public event Action<NoteData, FallingNote> OnNoteSpawned;

        /// <summary>Déclenché quand une note dépasse sa fenêtre sans être résolue (raté).</summary>
        public event Action<NoteData> OnNoteExpired;

        /// <summary>Nombre de colonnes configurées.</summary>
        public int LaneCount => lanes != null ? lanes.Length : 0;

        /// <summary>Vrai quand toutes les notes ont été jouées et le champ est vide.</summary>
        public bool IsFinished => isReady && spawnCursor >= orderedNotes.Count && activeNotes.Count == 0;

        /// <summary>
        /// Prépare le spawner pour un morceau donné. Les notes sont triées par temps croissant.
        /// </summary>
        /// <param name="track">Morceau fournissant la liste de notes.</param>
        /// <param name="conductor">Horloge audio pilotant le temps du morceau.</param>
        /// <param name="lookahead">Avance, en secondes, entre l'apparition et le temps cible.</param>
        public void Setup(SongTrack track, Conductor conductor, float lookahead)
        {
            this.conductor = conductor;
            this.lookahead = lookahead;

            EnsurePrewarmed();
            Clear();

            orderedNotes.Clear();

            if (track != null && track.notes != null)
            {
                orderedNotes.AddRange(track.notes);
                orderedNotes.Sort((left, right) => left.beatTime.CompareTo(right.beatTime));
            }

            spawnCursor = 0;
            isReady = true;
        }

        /// <summary>
        /// Renvoie la note active non résolue la plus proche du temps courant dans une colonne.
        /// </summary>
        /// <param name="lane">Index de la colonne.</param>
        /// <returns>La note candidate, ou null si aucune.</returns>
        public FallingNote GetActiveNoteInLane(int lane)
        {
            if (conductor == null)
            {
                return null;
            }

            float songTime = conductor.SongPositionSeconds;
            FallingNote best = null;
            float bestError = float.MaxValue;

            for (int i = 0; i < activeNotes.Count; i++)
            {
                FallingNote note = activeNotes[i];

                if (note.IsResolved || note.LaneIndex != lane)
                {
                    continue;
                }

                float error = Mathf.Abs(songTime - note.TargetTime);

                if (error < bestError)
                {
                    bestError = error;
                    best = note;
                }
            }

            return best;
        }

        /// <summary>
        /// Position monde du bas d'une colonne, utile pour placer un retour de raté.
        /// </summary>
        /// <param name="lane">Index de la colonne.</param>
        /// <returns>La position monde en bas de la colonne.</returns>
        public Vector3 GetLaneBottomPosition(int lane)
        {
            if (lanes == null || lanes.Length == 0)
            {
                return new Vector3(0f, despawnHeight, 0f);
            }

            int laneIndex = Mathf.Clamp(lane, 0, lanes.Length - 1);
            return new Vector3(lanes[laneIndex].xPosition, despawnHeight, 0f);
        }

        /// <summary>Recycle toutes les notes actives et vide le champ.</summary>
        public void Clear()
        {
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                Despawn(activeNotes[i]);
            }

            activeNotes.Clear();
            activeData.Clear();
        }

        private void Update()
        {
            if (!isReady || conductor == null)
            {
                return;
            }

            float songTime = conductor.SongPositionSeconds;

            SpawnDueNotes(songTime);
            AdvanceActiveNotes(songTime);
        }

        private void SpawnDueNotes(float songTime)
        {
            while (spawnCursor < orderedNotes.Count
                   && songTime >= orderedNotes[spawnCursor].beatTime - lookahead)
            {
                Spawn(orderedNotes[spawnCursor]);
                spawnCursor++;
            }
        }

        private void Spawn(NoteData note)
        {
            if (lanes == null || lanes.Length == 0)
            {
                return;
            }

            int laneIndex = Mathf.Clamp(note.laneIndex, 0, lanes.Length - 1);
            float laneX = lanes[laneIndex].xPosition;

            Vector3 topPosition = new Vector3(laneX, spawnHeight, 0f);
            Vector3 bottomPosition = new Vector3(laneX, despawnHeight, 0f);

            FallingNote fallingNote = Rent();

            if (fallingNote == null)
            {
                return;
            }

            float spawnSongTime = note.beatTime - lookahead;
            float totalTravelTime = lookahead + noteTrailSeconds;

            fallingNote.Initialize(note, spawnSongTime, totalTravelTime, topPosition, bottomPosition);

            activeNotes.Add(fallingNote);
            activeData.Add(note);

            OnNoteSpawned?.Invoke(note, fallingNote);
        }

        private void AdvanceActiveNotes(float songTime)
        {
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                FallingNote note = activeNotes[i];
                NoteData data = activeData[i];

                note.Tick(songTime);

                // A note that flew past the good window without being hit is a definitive miss.
                if (!note.IsResolved && songTime - note.TargetTime > NoteJudge.GoodWindowSeconds)
                {
                    note.Resolve();
                    OnNoteExpired?.Invoke(data);
                }

                if (note.IsResolved || note.HasPassed(songTime, noteTrailSeconds))
                {
                    activeNotes.RemoveAt(i);
                    activeData.RemoveAt(i);
                    Despawn(note);
                }
            }
        }

        private void EnsurePrewarmed()
        {
            if (prewarmed || notePrefab == null)
            {
                return;
            }

            for (int i = 0; i < poolPrewarm; i++)
            {
                FallingNote note = CreatePooledNote();

                if (note == null)
                {
                    break;
                }

                pool.Enqueue(note);
            }

            prewarmed = true;
        }

        private FallingNote CreatePooledNote()
        {
            GameObject instance = Instantiate(notePrefab, transform);
            FallingNote note = instance.GetComponent<FallingNote>();

            if (note == null)
            {
                Debug.LogError("Note prefab is missing a FallingNote component.");
                Destroy(instance);
                return null;
            }

            instance.SetActive(false);
            return note;
        }

        private FallingNote Rent()
        {
            FallingNote note = pool.Count > 0 ? pool.Dequeue() : CreatePooledNote();

            if (note != null)
            {
                note.gameObject.SetActive(true);
            }

            return note;
        }

        private void Despawn(FallingNote note)
        {
            if (note == null)
            {
                return;
            }

            note.gameObject.SetActive(false);
            pool.Enqueue(note);
        }
    }
}
