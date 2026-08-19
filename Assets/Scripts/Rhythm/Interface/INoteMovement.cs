using UnityEngine;

namespace Magma.Rhythm
{
    /// <summary>
    /// Abstraction du déplacement visuel d'une note, de son point d'apparition à la
    /// ligne de frappe. La logique pilote la progression normalisée pendant qu'une
    /// implémentation concrète décide du mouvement à l'écran.
    /// </summary>
    public interface INoteMovement
    {
        /// <summary>
        /// Configure les extrémités du déplacement. Appelé une fois à l'apparition.
        /// </summary>
        /// <param name="spawnPosition">Position monde à la progression 0.</param>
        /// <param name="hitPosition">Position monde à la progression 1 (ligne de frappe).</param>
        void Configure(Vector3 spawnPosition, Vector3 hitPosition);

        /// <summary>
        /// Met à jour le visuel de la note pour la progression normalisée donnée.
        /// </summary>
        /// <param name="progress">
        /// Progression normalisée de 0 (apparition) à 1 (ligne de frappe). Au-delà de 1,
        /// la note a dépassé la ligne de frappe.
        /// </param>
        void UpdateMovement(float progress);
    }
}
