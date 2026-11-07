using UnityEngine;

namespace ChasingLight
{
    public class GoalView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        private static readonly int WinHash = Animator.StringToHash("Win");

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (animator == null)
                animator = GetComponent<Animator>();
        }

        public void PlayWinAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger(WinHash);
            }
        }
    }
}
