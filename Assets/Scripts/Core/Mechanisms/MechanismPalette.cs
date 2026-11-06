using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChasingLight
{
    /// <summary>
    /// Sprite palette for a mechanism color group (Switch + Bridge).
    /// </summary>
    [Serializable]
    public struct MechanismPalette
    {
        [Header("Switch Sprites")]
        public Sprite switchUnpressed;
        public Sprite switchPressed;

        [Header("Bridge Sprites")]
        [FormerlySerializedAs("gateClosed")]
        public Sprite gateNonSolid;

        [FormerlySerializedAs("gateOpened")]
        public Sprite gateSolid;
    }
}
