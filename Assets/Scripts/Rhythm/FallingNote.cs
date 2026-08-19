using UnityEngine;

namespace Magma.Rhythm
{
    /// <summary>
    /// Instance à l'exécution d'une note. Porte la logique de timing (progression le
    /// long de sa chute, appartenance à la fenêtre parfaite) et délègue les visuels à
    /// des composants interchangeables : le mouvement via <see cref="INoteMovement"/>
    /// et le glow via un <see cref="INoteHighlight"/> optionnel. Le glow est piloté par
    /// la même fenêtre que le jugement Perfect, pour que ce que voit le joueur
    /// corresponde à ce qui est noté.
    /// </summary>
    public class FallingNote : MonoBehaviour
    {
        private NoteData noteData;
        private float spawnSongTime;
        private float totalTravelTime;
        private INoteMovement movement;
        private INoteHighlight highlight;

        /// <summary>Index de la colonne dans laquelle défile la note.</summary>
        public int LaneIndex => noteData.laneIndex;

        /// <summary>Temps du morceau, en secondes, où la note est Perfect (centre du glow).</summary>
        public float TargetTime => noteData.beatTime;

        /// <summary>Indique si la note a déjà été frappée ou ratée.</summary>
        public bool IsResolved { get; private set; }

        /// <summary>
        /// Marque la note comme frappée ou ratée pour qu'elle ne soit plus jugée, et coupe son glow.
        /// </summary>
        public void Resolve()
        {
            IsResolved = true;

            highlight?.SetHighlight(false);
        }

        /// <summary>
        /// Initialise la note et configure les extrémités de son déplacement.
        /// </summary>
        /// <param name="note">Les données de chart de cette note.</param>
        /// <param name="spawnSongTime">Temps du morceau, en secondes, où la note apparaît en haut.</param>
        /// <param name="totalTravelTime">Durée, en secondes, de la chute complète haut-bas.</param>
        /// <param name="topPosition">Position monde en haut de la colonne.</param>
        /// <param name="bottomPosition">Position monde en bas de la colonne.</param>
        public void Initialize(
            NoteData note,
            float spawnSongTime,
            float totalTravelTime,
            Vector3 topPosition,
            Vector3 bottomPosition
        )
        {
            // Reset resolution so a pooled note can be reused cleanly (neutral for a fresh note).
            IsResolved = false;

            noteData = note;
            this.spawnSongTime = spawnSongTime;
            this.totalTravelTime = totalTravelTime;

            movement = GetComponent<INoteMovement>();
            highlight = GetComponent<INoteHighlight>();

            if (movement == null)
            {
                Debug.LogError(
                    "FallingNote requires a component implementing INoteMovement."
                );

                return;
            }

            movement.Configure(topPosition, bottomPosition);
            highlight?.SetHighlight(false);
        }

        /// <summary>
        /// Fait avancer le visuel de la note (chute et glow) selon le temps du morceau.
        /// </summary>
        /// <param name="songTime">Temps courant du morceau en secondes.</param>
        public void Tick(float songTime)
        {
            if (movement == null)
            {
                return;
            }

            float progress = totalTravelTime > 0f
                ? (songTime - spawnSongTime) / totalTravelTime
                : 1f;

            movement.UpdateMovement(progress);

            if (highlight != null && !IsResolved)
            {
                bool inPerfectWindow =
                    Mathf.Abs(songTime - noteData.beatTime) <= NoteJudge.PerfectWindowSeconds;

                highlight.SetHighlight(inPerfectWindow);
            }
        }

        /// <summary>
        /// Indique si la note est tombée assez bas pour être recyclée.
        /// </summary>
        /// <param name="songTime">Temps courant du morceau en secondes.</param>
        /// <param name="trailSeconds">Délai après le temps cible où la note atteint le bas.</param>
        /// <returns>Vrai quand la note peut être recyclée.</returns>
        public bool HasPassed(float songTime, float trailSeconds)
        {
            return songTime > noteData.beatTime + trailSeconds;
        }
    }
}
