using System.Collections;
using ChasingLight.Core.Actions;
using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Chỉ hiển thị Shadow và hoạt họa lệnh replay do GameplayManager/Core cung cấp.
    /// </summary>
    public sealed class ShadowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Visual movement")]
        [SerializeField, Min(0.01f)] private float _moveDuration = 0.12f;
        [SerializeField, Min(0.01f)] private float _fallDurationPerCell = 0.12f;

        private bool _isAnimating;
        private bool _isVisible;
        private Vector2Int _visualGridPosition;

        public Vector2Int GridPosition => _visualGridPosition;
        public bool IsVisible => _isVisible;
        public bool IsAnimating => _isAnimating;

        private void Awake()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }

        /// <summary>Đặt hình ảnh Shadow ở vị trí ban đầu rồi ẩn nó.</summary>
        public void Initialize(Vector2Int startPosition)
        {
            _visualGridPosition = startPosition;
            _isAnimating = false;
            _isVisible = false;

            if (_gridManager != null)
            {
                transform.position = _gridManager.GridToWorld(startPosition);
            }

            SetAnimation(EntityCommand.None, false);
            gameObject.SetActive(false);
        }

        /// <summary>Hiển thị Shadow tại vị trí runtime do StageState cung cấp.</summary>
        public void ShowAt(Vector2Int position)
        {
            if (_gridManager == null)
            {
                Debug.LogError("ShadowController: GridManager chưa được gán.", this);
                return;
            }

            gameObject.SetActive(true);
            _visualGridPosition = position;
            transform.position = _gridManager.GridToWorld(position);
            _isAnimating = false;
            _isVisible = true;
            SetAnimation(EntityCommand.None, false);
        }

        /// <summary>Ẩn Shadow khi Core đánh dấu nó đã rơi khỏi màn hoặc màn kết thúc.</summary>
        public void Hide()
        {
            _isVisible = false;
            _isAnimating = false;
            SetAnimation(EntityCommand.None, false);
            gameObject.SetActive(false);
        }

        /// <summary>Giữ Shadow đứng yên nếu lệnh replay không làm thay đổi vị trí.</summary>
        public void SetIdle(EntityCommand lastCommand)
        {
            SetAnimation(lastCommand, false);
        }

        /// <summary>Hoạt họa một lệnh Shadow do GameplayManager lấy từ Core History.</summary>
        public IEnumerator AnimateCommand(
            Vector2Int fromGrid,
            Vector2Int attemptedGrid,
            Vector2Int resolvedGrid,
            EntityCommand command)
        {
            if (!_isVisible || _gridManager == null)
            {
                yield break;
            }

            _isAnimating = true;
            SetAnimation(command, true);

            Vector3 attemptedWorld = _gridManager.GridToWorld(attemptedGrid);
            Vector3 resolvedWorld = _gridManager.GridToWorld(resolvedGrid);

            if (attemptedGrid != fromGrid)
            {
                yield return MoveSegment(transform.position, attemptedWorld, _moveDuration);
            }

            if (resolvedGrid != attemptedGrid)
            {
                float fallDuration = Mathf.Max(
                    _fallDurationPerCell,
                    Mathf.Abs(resolvedGrid.y - attemptedGrid.y) * _fallDurationPerCell);

                yield return MoveSegment(transform.position, resolvedWorld, fallDuration);
            }

            transform.position = resolvedWorld;
            _visualGridPosition = resolvedGrid;
            _isAnimating = false;
            SetAnimation(command, false);
        }

        private IEnumerator MoveSegment(Vector3 start, Vector3 end, float duration)
        {
            float elapsed = 0f;
            duration = Mathf.Max(0.01f, duration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            transform.position = end;
        }

        private void SetAnimation(EntityCommand command, bool isMoving)
        {
            if (_spriteRenderer != null)
            {
                if (command == EntityCommand.Left) _spriteRenderer.flipX = true;
                else if (command == EntityCommand.Right) _spriteRenderer.flipX = false;
            }

            if (_animator == null) return;

            Vector2Int direction = GameRules.ToDelta(command);
            _animator.SetFloat("MoveX", direction.x);
            _animator.SetFloat("MoveY", direction.y);
            _animator.SetBool("IsMoving", isMoving);
        }
    }
}
