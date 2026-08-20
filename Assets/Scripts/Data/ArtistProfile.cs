using System;
using UnityEngine;

namespace Magma.Data
{
    /// <summary>Valeur de départ d'une statistique d'artiste pour une fiche donnée.</summary>
    [Serializable]
    public struct StatValue
    {
        public StatType Stat;

        [Range(0, 100)]
        public int Value;
    }

    /// <summary>
    /// Fiche d'artiste : identité et statistiques de départ (0-100, équilibrées : un point
    /// fort et des valeurs plus modestes ailleurs). Chaque artiste jouable est une fiche
    /// séparée, listée et parcourue à l'écran de sélection.
    /// </summary>
    [CreateAssetMenu(fileName = "ArtistProfile", menuName = "Magma/Artist Profile")]
    public class ArtistProfile : ScriptableObject
    {
        [Header("Identité")]
        public string DisplayName;
        public string MusicGenre;

        [Header("Statistiques de départ (0-100)")]
        public StatValue[] BaseStats;

        /// <summary>Renvoie la valeur de départ de la statistique demandée, 0 si non définie sur cette fiche.</summary>
        public int GetBaseStat(StatType stat)
        {
            foreach (StatValue entry in BaseStats)
            {
                if (entry.Stat == stat)
                {
                    return entry.Value;
                }
            }

            return 0;
        }
    }
}
