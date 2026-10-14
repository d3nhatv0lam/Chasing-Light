using System;
using System.Collections;
using UnityEngine;

namespace ChasingLight.Gameplay.Views
{
    public class EntityView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float defaultMoveDuration = 0.15f;
        [SerializeField] private Vector2 cellOffset = new Vector2(0.5f, 0.5f);
        [SerializeField] private float cellSize = 1f;

        public bool IsMoving { get; private set; }
        private Coroutine _moveCoroutine;

        private void Awake()
        {
            EnsureSpriteRenderer();
        }

        private void EnsureSpriteRenderer()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

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
            EnsureSpriteRenderer();

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            transform.position = GridToWorld(gridPos);
            IsMoving = false;
        }

        /// <summary>
        /// Di chuyển mượt mà từ vị trí hiện tại sang ô mục tiêu.
        /// </summary>
        public void MoveTo(Vector2Int targetGridPos, float duration = -1f, Action onComplete = null)
        {
            EnsureSpriteRenderer();

            if (duration <= 0f)
                duration = defaultMoveDuration;

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
            onComplete?.Invoke();
        }

        public void SetColor(Color color)
        {
            EnsureSpriteRenderer();
            if (spriteRenderer != null)
                spriteRenderer.color = color;
        }
    }
}
