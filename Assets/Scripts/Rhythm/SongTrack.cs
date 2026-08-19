using System.Collections.Generic;
using UnityEngine;

namespace Magma.Rhythm
{
    /// <summary>
    /// ScriptableObject décrivant un morceau de répétition complet : sa musique, son
    /// tempo, son chart de notes et le temps de trajet d'une note jusqu'à la ligne de
    /// frappe. Piloté par les données pour qu'un nouveau morceau soit créé sans code.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SongTrack",
        menuName = "MiniGames/PianoTitle/Song Track"
    )]
    public class SongTrack : ScriptableObject
    {
        [Header("Music")]
        [Tooltip("Clip audio joué pendant la répétition.")]
        public AudioClip music;

        [Tooltip("Tempo du morceau en battements par minute.")]
        public float bpm = DefaultBpm;

        [Tooltip("Décalage audio de calibration du morceau, en secondes.")]
        public float offset = DefaultOffset;

        [Header("Timing")]
        [Tooltip("Temps en secondes qu'une note met pour aller de son apparition à la ligne de frappe.")]
        public float travelTime = DefaultTravelTime;

        [Header("Chart")]
        [Tooltip("Liste ordonnée des notes à faire apparaître pendant le morceau.")]
        public List<NoteData> notes = new List<NoteData>();

        private const float DefaultBpm = 120f;
        private const float DefaultOffset = 0f;
        private const float DefaultTravelTime = 2f;

        /// <summary>
        /// Trie automatiquement les notes par temps croissant lors de l'édition,
        /// pour que le spawner puisse avancer un simple curseur de progression.
        /// </summary>
        private void OnValidate()
        {
            if (notes == null)
            {
                return;
            }

            notes.Sort((left, right) => left.beatTime.CompareTo(right.beatTime));
        }
    }
}
