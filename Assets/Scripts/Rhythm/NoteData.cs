using System;
using UnityEngine;

namespace Magma.Rhythm
{
    /// <summary>
    /// Données sérialisables d'une note d'un morceau de répétition.
    /// Décrit à quel instant la note doit être frappée et dans quelle colonne.
    /// </summary>
    [Serializable]
    public class NoteData
    {
        [Tooltip("Temps en secondes, depuis le début du morceau, où la note doit être frappée.")]
        public float beatTime;

        [Tooltip("Index de la colonne dans laquelle la note défile.")]
        public int laneIndex;
    }
}
