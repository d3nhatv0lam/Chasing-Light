using System;
using System.Collections;
using UnityEngine;

namespace ChasingLight
{
    public class EntityView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        [Header("Movement Settings")]
        [SerializeField] private float defaultMoveDuration = 0.15f;
        [SerializeField] private Vector2 cellOffset = new Vector2(0.5f, 0.07f);
        [SerializeField] private float cellSize = 1f;

        public Vector2Int CurrentGridPosition { get; private set; }
        public bool IsMoving { get; private set; }
        private Coroutine _moveCoroutine;

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");

        private void Awake()
        {
            EnsureComponents();
        }

        private void EnsureComponents()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            if (animator == null)
                animator = GetComponent<Animator>();

            // Đảm bảo SpriteRenderer luôn hiển thị phía trước Tilemap (nền tilemap thường có sortingOrder = 0)
            if (spriteRenderer.sortingOrder < 10)
                spriteRenderer.sortingOrder = 10;

            // Nếu chưa gán Sprite, tự sinh 1 hình tròn/vuông cơ bản để luôn luôn nhìn thấy
            if (spriteRenderer.sprite == null)
            {
                var tex = Texture2D.whiteTexture;
                spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1f);
            }
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(
                (gridPos.x + cellOffset.x) * cellSize,
                (gridPos.y + cellOffset.y) * cellSize,
                0f
            );
        }

        /// <summary>
        /// Đặt vị trí tức thời (lúc khởi tạo màn chơi hoặc dịch chuyển).
        /// </summary>
        public void SetGridPosition(Vector2Int gridPos)
        {
            EnsureComponents();

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            CurrentGridPosition = gridPos;
            transform.position = GridToWorld(gridPos);
            IsMoving = false;

            if (animator != null)
            {
                animator.SetBool(IsMovingHash, false);
            }
        }

        /// <summary>
        /// Di chuyển mượt mà từ vị trí hiện tại sang ô mục tiêu.
        /// </summary>
        public void MoveTo(Vector2Int targetGridPos, float duration = -1f, Action onComplete = null)
        {
            EnsureComponents();

            if (duration <= 0f)
                duration = defaultMoveDuration;

            // Tự động lật mặt trái / phải theo hướng di chuyển
            if (targetGridPos.x < CurrentGridPosition.x)
            {
                spriteRenderer.flipX = true;
            }
            else if (targetGridPos.x > CurrentGridPosition.x)
            {
                spriteRenderer.flipX = false;
            }

            // Gán tham số hướng cho Animator nếu có
            if (animator != null)
            {
                int dx = targetGridPos.x - CurrentGridPosition.x;
                int dy = targetGridPos.y - CurrentGridPosition.y;
                animator.SetFloat(MoveXHash, dx);
                animator.SetFloat(MoveYHash, dy);
                animator.SetBool(IsMovingHash, true);
            }

            CurrentGridPosition = targetGridPos;

            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(MoveCoroutine(GridToWorld(targetGridPos), duration, onComplete));
        }

        private IEnumerator MoveCoroutine(Vector3 targetWorldPos, float duration, Action onComplete)
        {
            IsMoving = true;
            Vector3 startPos = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Hàm SmoothStep làm mượt chuyển động gia tốc đầu và cuối
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(startPos, targetWorldPos, smoothT);
                yield return null;
            }

            transform.position = targetWorldPos;
            IsMoving = false;
            _moveCoroutine = null;

            if (animator != null)
            {
                animator.SetBool(IsMovingHash, false);
            }

            onComplete?.Invoke();
        }

        public void SetColor(Color color)
        {
            EnsureComponents();
            if (spriteRenderer != null)
                spriteRenderer.color = color;
        }
    }
}
