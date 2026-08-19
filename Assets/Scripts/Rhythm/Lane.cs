using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Magma.Rhythm
{
    /// <summary>
    /// Configuration d'une colonne dans laquelle défilent les notes.
    /// L'index de colonne correspond à sa position dans le tableau du spawner.
    /// </summary>
    [Serializable]
    public class Lane
    {
        [Tooltip("Position X monde de cette colonne.")]
        public float xPosition;

        [Tooltip("Touche clavier qui frappe les notes de cette colonne.")]
        public Key key = Key.None;
    }
}
