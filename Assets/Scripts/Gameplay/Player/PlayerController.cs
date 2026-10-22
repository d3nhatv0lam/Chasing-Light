using System.Collections;
using ChasingLight.Core.Actions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChasingLight
{
    /// <summary>
    /// Nhận input và hiển thị chuyển động Player đã được Core chấp nhận.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private GameplayManager _gameplayManager;
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Visual movement")]
        [SerializeField, Min(0.01f)] private float _moveDuration = 0.12f;
        [SerializeField, Min(0.01f)] private float _fallDurationPerCell = 0.12f;

        private bool _isAnimating;
        private Vector2Int _visualGridPosition;

        public Vector2Int GridPosition => _visualGridPosition;
        public bool IsAnimating => _isAnimating;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
        }

        private void Update()
        {
            if (_isAnimating || _gameplayManager == null || !_gameplayManager.CanAcceptInput)
            {
                return;
            }

            EntityCommand command = ReadCommand();
            if (command != EntityCommand.None)
            {
                _gameplayManager.SubmitPlayerCommand(command);
            }
        }

        private EntityCommand ReadCommand()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return EntityCommand.None;
            }

            if (keyboard.aKey.wasPressedThisFrame ||
                keyboard.leftArrowKey.wasPressedThisFrame)
            {
                return EntityCommand.Left;
            }

            if (keyboard.dKey.wasPressedThisFrame ||
                keyboard.rightArrowKey.wasPressedThisFrame)
            {
                return EntityCommand.Right;
            }

            if (keyboard.wKey.wasPressedThisFrame ||
                keyboard.upArrowKey.wasPressedThisFrame)
            {
                return EntityCommand.Up;
            }

            if (keyboard.sKey.wasPressedThisFrame ||
                keyboard.downArrowKey.wasPressedThisFrame)
            {
                return EntityCommand.Down;
            }

            return EntityCommand.None;
        }

        /// <summary>Đặt hình ảnh Player tại vị trí runtime do StageState cung cấp.</summary>
        public void Initialize(Vector2Int startPosition)
        {
            _visualGridPosition = startPosition;
            _isAnimating = false;

            if (_gridManager != null)
            {
                transform.position = _gridManager.GridToWorld(startPosition);
            }

            SetAnimation(EntityCommand.None, false);
        }

        /// <summary>
        /// Hoạt họa một hành động do Core đã phân giải. attemptedGrid là ô lệnh hướng tới;
        /// resolvedGrid là vị trí cuối cùng sau khi rơi/teleport được Core xử lý.
        /// </summary>
        public IEnumerator AnimateCommand(
            Vector2Int fromGrid,
            Vector2Int attemptedGrid,
            Vector2Int resolvedGrid,
            EntityCommand command)
        {
            if (_gridManager == null)
            {
                Debug.LogError("PlayerController: GridManager chưa được gán.", this);
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

        /// <summary>Giữ Idle khi Core từ chối lệnh.</summary>
        public void SetIdle(EntityCommand lastCommand)
        {
            SetAnimation(lastCommand, false);
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
