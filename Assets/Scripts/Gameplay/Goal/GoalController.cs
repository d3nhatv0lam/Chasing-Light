using UnityEngine;

namespace ChasingLight
{
    public sealed class GoalController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
    }
}
