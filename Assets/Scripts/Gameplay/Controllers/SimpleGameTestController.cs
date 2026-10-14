using UnityEngine;
using UnityEngine.InputSystem;
using ChasingLight.Gameplay.Views;

namespace ChasingLight.Gameplay.Controllers
{
    public class SimpleGameTestController : MonoBehaviour
    {
        [Header("Dữ liệu & View")]
        [SerializeField] private StageAsset stageAsset;
        [SerializeField] private GridView gridView;
        [SerializeField] private EntityView playerPrefab;

        [Header("Cấu hình di chuyển")]
        [SerializeField] private float moveDuration = 0.15f;

        [Header("Debug UI")]
        [SerializeField] private bool showOnScreenTestButtons = true;

        private StageData _stageData;
        private EntityView _playerView;
        private Vector2Int _currentPos = Vector2Int.zero;
        private Vector2Int _initialPos = Vector2Int.zero;

        private void Start()
        {
            InitializeStage();
        }

        private void InitializeStage()
        {
            if (stageAsset != null)
            {
                _stageData = stageAsset.ToData();

                if (_stageData.Grid.Width <= 0 || _stageData.Grid.Height <= 0)
                {
                    Debug.LogWarning("[SimpleGameTestController] StageAsset có Width hoặc Height <= 0! Hãy chỉnh kích thước lưới lớn hơn 0 trong file StageAsset.");
                }
            }
            else
            {
                Debug.LogWarning("[SimpleGameTestController] Chưa gán StageAsset vào Inspector. Đang khởi tạo lưới mặc định (10x10) để bạn test di chuyển.");
                _stageData = new StageData(
                    "test_default",
                    1,
                    Difficulty.Easy,
                    new GridData(10, 10, null),
                    new System.Collections.Generic.List<EntityData>(),
                    0,
                    new System.Collections.Generic.List<SwitchData>(),
                    new System.Collections.Generic.List<GateData>(),
                    new System.Collections.Generic.List<TeleportData>(),
                    new System.Collections.Generic.List<StateStar>()
                );
            }

            // 2. Dựng bản đồ qua GridView
            if (gridView != null)
            {
                gridView.BuildMap(_stageData);
            }

            // 3. Tìm vị trí xuất phát của Player
            _initialPos = FindPlayerStartPosition(_stageData);
            _currentPos = _initialPos;

            // 4. Sinh Player EntityView nếu chưa có
            if (_playerView == null)
            {
                if (playerPrefab != null)
                {
                    _playerView = Instantiate(playerPrefab);
                }
                else
                {
                    Debug.LogWarning("[SimpleGameTestController] Chưa gán Player Prefab! Tự động tạo GameObject Player (Cyan) để test.");
                    var go = new GameObject("Player_AutoInstance");
                    _playerView = go.AddComponent<EntityView>();
                }

                _playerView.name = "Player_TestInstance";
                _playerView.SetColor(new Color(0.2f, 0.85f, 1f)); // Màu Cyan sáng
            }

            _playerView.SetGridPosition(_currentPos);
            Debug.Log($"[SimpleGameTestController] Khởi tạo thành công! Player bắt đầu tại ô {_currentPos}");
        }

        private Vector2Int FindPlayerStartPosition(StageData data)
        {
            if (data?.Entities != null)
            {
                foreach (var entity in data.Entities)
                {
                    if (entity.Type == EntityType.Player)
                        return entity.Position;
                }
            }
            return Vector2Int.zero;
        }

        private void Update()
        {
            HandleNewInputSystem();
            HandleLegacyInput();
        }

        /// <summary>
        /// Bắt phím thông qua New Input System (UnityEngine.InputSystem)
        /// </summary>
        private void HandleNewInputSystem()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                    MoveUp();
                else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                    MoveDown();
                else if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                    MoveLeft();
                else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                    MoveRight();
                else if (keyboard.rKey.wasPressedThisFrame)
                    Restart();
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.dpad.up.wasPressedThisFrame)
                    MoveUp();
                else if (gamepad.dpad.down.wasPressedThisFrame)
                    MoveDown();
                else if (gamepad.dpad.left.wasPressedThisFrame)
                    MoveLeft();
                else if (gamepad.dpad.right.wasPressedThisFrame)
                    MoveRight();
            }
        }

        /// <summary>
        /// Fallback cho Legacy Input nếu project chạy cả 2 hệ thống
        /// </summary>
        private void HandleLegacyInput()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                    MoveUp();
                else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                    MoveDown();
                else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                    MoveLeft();
                else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                    MoveRight();
                else if (Input.GetKeyDown(KeyCode.R))
                    Restart();
            }
            catch
            {
                // Bỏ qua lỗi nếu Unity tắt hoàn toàn Legacy Input
            }
#endif
        }

        public void MoveUp() => TryMove(Vector2Int.up);
        public void MoveDown() => TryMove(Vector2Int.down);
        public void MoveLeft() => TryMove(Vector2Int.left);
        public void MoveRight() => TryMove(Vector2Int.right);

        public void Restart()
        {
            _currentPos = _initialPos;
            if (_playerView != null)
            {
                _playerView.SetGridPosition(_currentPos);
            }
            Debug.Log("[SimpleGameTestController] Đã Reset về vị trí ban đầu.");
        }

        private void TryMove(Vector2Int delta)
        {
            if (_playerView == null)
            {
                Debug.LogWarning("[SimpleGameTestController] PlayerView chưa được khởi tạo!");
                return;
            }

            if (_playerView.IsMoving)
            {
                // Đang trong animation di chuyển, tránh nhận đúp lệnh
                return;
            }

            Vector2Int nextPos = _currentPos + delta;

            // Kiểm tra rìa bản đồ (chỉ chặn nếu Width và Height hợp lệ > 0)
            if (_stageData != null && _stageData.Grid.Width > 0 && _stageData.Grid.Height > 0)
            {
                if (!_stageData.Grid.InBounds(nextPos))
                {
                    Debug.Log($"[SimpleGameTestController] Chạm rìa bản đồ: Không thể đi sang ô {nextPos} (Lưới {_stageData.Grid.Width}x{_stageData.Grid.Height})");
                    return;
                }
            }

            Debug.Log($"[SimpleGameTestController] Di chuyển: {_currentPos} -> {nextPos}");
            _currentPos = nextPos;
            _playerView.MoveTo(_currentPos, moveDuration);
        }

        /// <summary>
        /// Hiển thị 4 nút ảo trên màn hình để bạn có thể dùng chuột click thử ngay
        /// </summary>
        private void OnGUI()
        {
            if (!showOnScreenTestButtons) return;

            float btnSize = 60f;
            float startX = 50f;
            float startY = Screen.height - 200f;

            GUI.Box(new Rect(startX - 10, startY - 40, btnSize * 3 + 20, btnSize * 3 + 50), "Test Controls");

            // Nút Lên
            if (GUI.Button(new Rect(startX + btnSize, startY, btnSize, btnSize), "▲ W"))
                MoveUp();

            // Nút Trái
            if (GUI.Button(new Rect(startX, startY + btnSize, btnSize, btnSize), "◄ A"))
                MoveLeft();

            // Nút Xuống
            if (GUI.Button(new Rect(startX + btnSize, startY + btnSize, btnSize, btnSize), "▼ S"))
                MoveDown();

            // Nút Phải
            if (GUI.Button(new Rect(startX + btnSize * 2, startY + btnSize, btnSize, btnSize), "► D"))
                MoveRight();

            // Nút Reset
            if (GUI.Button(new Rect(startX + btnSize * 2, startY, btnSize, btnSize), "Reset"))
                Restart();
        }
    }
}
