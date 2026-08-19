using System;
using UnityEngine;

namespace Magma.Rhythm
{
    /// <summary>
    /// Horloge audio du rythme, fondée exclusivement sur AudioSettings.dspTime pour
    /// rester synchronisée à l'échantillon près (pas de dérive). Peut tourner sans
    /// SongTrack (mode métronome pur) pour la calibration. La position est négative
    /// avant le début du morceau (compte à rebours) et n'est jamais clampée.
    /// </summary>
    public class Conductor : MonoBehaviour
    {
        [Tooltip("Source audio utilisée pour lire le morceau.")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Décalage de calibration du joueur, en millisecondes.")]
        [SerializeField] private float userOffsetMs = 0f;

        [Tooltip("Délai de planification avant le début du morceau, en secondes (PlayScheduled).")]
        [SerializeField] private float scheduleAheadSeconds = DefaultScheduleAheadSeconds;

        [Tooltip("Tempo utilisé en mode métronome (sans SongTrack), en BPM.")]
        [SerializeField] private float metronomeBpm = DefaultMetronomeBpm;

        private double dspStartTime;
        private double pauseDspTime;
        private SongTrack currentTrack;
        private bool isPlaying;
        private bool isPaused;
        private bool songFinished;
        private int lastBeat;

        private const float DefaultScheduleAheadSeconds = 0.5f;
        private const float DefaultMetronomeBpm = 120f;

        /// <summary>Déclenché une fois lorsque le morceau se termine.</summary>
        public event Action OnSongFinished;

        /// <summary>Déclenché à chaque nouveau temps (beat) franchi.</summary>
        public event Action<int> OnBeat;

        /// <summary>Position courante dans le morceau, en secondes. Négative avant le début.</summary>
        public float SongPositionSeconds
        {
            get
            {
                double reference = isPaused ? pauseDspTime : AudioSettings.dspTime;
                float offset = currentTrack != null ? currentTrack.offset : 0f;
                return (float)(reference - dspStartTime) - offset - userOffsetMs / 1000f;
            }
        }

        /// <summary>Position courante dans le morceau, exprimée en temps (beats).</summary>
        public float SongPositionBeats => SongPositionSeconds * (CurrentBpm / 60f);

        /// <summary>Indique si l'horloge est active (et non en pause).</summary>
        public bool IsPlaying => isPlaying && !isPaused;

        /// <summary>Morceau en cours, ou null en mode métronome.</summary>
        public SongTrack CurrentTrack => currentTrack;

        private float CurrentBpm =>
            currentTrack != null && currentTrack.bpm > 0f ? currentTrack.bpm : metronomeBpm;

        /// <summary>
        /// Démarre l'horloge sur un morceau. Passer null lance le mode métronome pur.
        /// </summary>
        /// <param name="track">Morceau à jouer, ou null pour un métronome sans audio.</param>
        public void Play(SongTrack track)
        {
            currentTrack = track;
            dspStartTime = AudioSettings.dspTime + scheduleAheadSeconds;
            isPlaying = true;
            isPaused = false;
            songFinished = false;
            lastBeat = int.MinValue;

            if (track != null && track.music != null && audioSource != null)
            {
                audioSource.clip = track.music;
                audioSource.PlayScheduled(dspStartTime);
            }
        }

        /// <summary>Arrête l'horloge et l'audio.</summary>
        public void Stop()
        {
            isPlaying = false;
            isPaused = false;

            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }

        /// <summary>Met l'horloge et l'audio en pause en gelant la position.</summary>
        public void Pause()
        {
            if (!isPlaying || isPaused)
            {
                return;
            }

            pauseDspTime = AudioSettings.dspTime;
            isPaused = true;

            if (audioSource != null)
            {
                audioSource.Pause();
            }
        }

        /// <summary>Reprend l'horloge après une pause en conservant la synchronisation.</summary>
        public void Resume()
        {
            if (!isPaused)
            {
                return;
            }

            dspStartTime += AudioSettings.dspTime - pauseDspTime;
            isPaused = false;

            if (audioSource != null)
            {
                audioSource.UnPause();
            }
        }

        private void Update()
        {
            if (!isPlaying || isPaused)
            {
                return;
            }

            EmitBeats();
            DetectSongEnd();
        }

        private void EmitBeats()
        {
            float beats = SongPositionBeats;

            if (beats < 0f)
            {
                return;
            }

            int currentBeat = Mathf.FloorToInt(beats);

            if (currentBeat <= lastBeat)
            {
                return;
            }

            for (int beat = Mathf.Max(lastBeat + 1, 0); beat <= currentBeat; beat++)
            {
                OnBeat?.Invoke(beat);
            }

            lastBeat = currentBeat;
        }

        private void DetectSongEnd()
        {
            if (songFinished || currentTrack == null || currentTrack.music == null)
            {
                return;
            }

            if (SongPositionSeconds >= currentTrack.music.length)
            {
                songFinished = true;
                isPlaying = false;
                OnSongFinished?.Invoke();
            }
        }
    }
}
