using UnityEngine;

namespace Magma.Rhythm
{
    /// <summary>
    /// Présentation en colonne d'une note (façon Piano Tiles) : la note défile en
    /// ligne droite d'un point d'apparition vers la ligne de frappe. Implémente
    /// <see cref="INoteMovement"/> pour pouvoir être remplacée par une autre
    /// présentation sans changer la logique de jeu.
    /// </summary>
    public class LaneNoteMovement : MonoBehaviour, INoteMovement
    {
        private Vector3 spawnPosition;
        private Vector3 hitPosition;

        /// <inheritdoc />
        public void Configure(Vector3 spawnPosition, Vector3 hitPosition)
        {
            this.spawnPosition = spawnPosition;
            this.hitPosition = hitPosition;

            transform.position = spawnPosition;
        }

        /// <inheritdoc />
        public void UpdateMovement(float progress)
        {
            // Unclamped so the note keeps travelling past the hit line when missed.
            transform.position = Vector3.LerpUnclamped(
                spawnPosition,
                hitPosition,
                progress
            );
        }
    }
}
