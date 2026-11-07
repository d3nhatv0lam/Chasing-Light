using System;
using UnityEngine;

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
        public Sprite gateNonSolid;
        public Sprite gateSolid;
    }
}
