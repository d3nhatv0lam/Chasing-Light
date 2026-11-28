using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Dialog hiển thị khi người chơi có dữ liệu tiến trình Khách cũ
    /// và vừa đăng nhập/đăng ký tài khoản thành công.
    /// </summary>
    public class GuestSyncDialog : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Root panel của Dialog (chứa popup background, text và buttons)")]
        [SerializeField] private GameObject _dialogPanel;

        [Tooltip("Text hiển thị nội dung câu hỏi")]
        [SerializeField] private TMP_Text _messageText;

        [Tooltip("Nút Đồng ý lưu / đồng bộ tiến trình vào tài khoản")]
        [SerializeField] private Button _syncButton;

        [Tooltip("Nút Bỏ qua / Không lưu (khởi tạo tiến trình mới)")]
        [SerializeField] private Button _discardButton;

        private Action _onConfirmSync;
        private Action _onDiscard;

        private static Sprite _cachedCardSprite;
        private static Sprite _cachedPrimaryBtnSprite;
        private static Sprite _cachedOutlineBtnSprite;

        private void Awake()
        {
            if (_dialogPanel != null)
            {
                _dialogPanel.SetActive(false);
            }

            if (_syncButton != null)
            {
                _syncButton.onClick.AddListener(HandleSyncClicked);
                AttachHoverEffect(_syncButton, isPrimary: true);
            }

            if (_discardButton != null)
            {
                _discardButton.onClick.AddListener(HandleDiscardClicked);
                AttachHoverEffect(_discardButton, isPrimary: false);
            }
        }

        private void OnDestroy()
        {
            if (_syncButton != null) _syncButton.onClick.RemoveListener(HandleSyncClicked);
            if (_discardButton != null) _discardButton.onClick.RemoveListener(HandleDiscardClicked);
        }

        /// <summary>
        /// Hiển thị popup hỏi người chơi có muốn đồng bộ tiến trình Guest hay không.
        /// </summary>
        public void Show(Action onConfirmSync, Action onDiscard, string message = null)
        {
            _onConfirmSync = onConfirmSync;
            _onDiscard = onDiscard;

            if (_messageText != null)
            {
                _messageText.text = message ?? "Previous local progress detected.\nWould you like to sync your progress to this account?";
            }

            if (_dialogPanel != null)
            {
                _dialogPanel.SetActive(true);
            }
            else
            {
                // Fallback: tạo một dialog tạm thời trên Canvas để không chặn luồng game
                CreateFallbackDialog();
            }
        }

        public void Hide()
        {
            if (_dialogPanel != null)
            {
                _dialogPanel.SetActive(false);
            }
        }

        private void HandleSyncClicked()
        {
            Hide();
            _onConfirmSync?.Invoke();
        }

        private void HandleDiscardClicked()
        {
            Hide();
            _onDiscard?.Invoke();
        }

        private void CreateFallbackDialog()
        {
            var canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[GuestSyncDialog] Không tìm thấy Canvas để hiển thị dialog. Mặc định đồng bộ.");
                HandleSyncClicked();
                return;
            }

            EnsureFallbackSprites();

            // Full-screen dim overlay matching Login backdrop
            var overlayGo = new GameObject("RuntimeGuestSyncDialog", typeof(RectTransform), typeof(Image));
            overlayGo.transform.SetParent(canvas.transform, false);
            var overlayRect = (RectTransform)overlayGo.transform;
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
            overlayGo.GetComponent<Image>().color = new Color(0.035f, 0.051f, 0.086f, 0.78f);

            _dialogPanel = overlayGo;

            // Rounded card container matching LoginPanel
            var cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(overlayGo.transform, false);
            var cardRect = (RectTransform)cardGo.transform;
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(560f, 248f);
            var cardImg = cardGo.GetComponent<Image>();
            cardImg.sprite = _cachedCardSprite;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = Color.white;

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(cardGo.transform, false);
            var titleRect = (RectTransform)titleGo.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -34f);
            titleRect.sizeDelta = new Vector2(-88f, 42f);

            var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Sync Local Progress";
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleTmp.fontSize = 34;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;

            // Message Text
            var textGo = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(cardGo.transform, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0f, -84f);
            textRect.sizeDelta = new Vector2(-88f, 52f);

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = _messageText != null ? _messageText.text : "Previous local progress detected.\nWould you like to sync your progress to this account?";
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.fontSize = 19;
            tmp.color = new Color(0.612f, 0.639f, 0.71f, 1f);
            _messageText = tmp;

            // Horizontal Button Row
            var rowGo = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(cardGo.transform, false);
            var rowRect = (RectTransform)rowGo.transform;
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -160f);
            rowRect.sizeDelta = new Vector2(-88f, 54f);

            var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Button Sync (Primary blue)
            var btnSyncGo = new GameObject("BtnSync", typeof(RectTransform), typeof(Image), typeof(Button));
            btnSyncGo.transform.SetParent(rowGo.transform, false);
            var btnSyncImg = btnSyncGo.GetComponent<Image>();
            btnSyncImg.sprite = _cachedPrimaryBtnSprite;
            btnSyncImg.type = Image.Type.Sliced;
            btnSyncImg.color = Color.white;

            _syncButton = btnSyncGo.GetComponent<Button>();
            _syncButton.onClick.AddListener(HandleSyncClicked);
            CreateButtonText(btnSyncGo.transform, "Sync to Account", Color.white, FontStyles.Bold, 18);
            AttachHoverEffect(_syncButton, isPrimary: true);

            // Button Discard (Outline, Bold)
            var btnDiscardGo = new GameObject("BtnDiscard", typeof(RectTransform), typeof(Image), typeof(Button));
            btnDiscardGo.transform.SetParent(rowGo.transform, false);
            var btnDiscardImg = btnDiscardGo.GetComponent<Image>();
            btnDiscardImg.sprite = _cachedOutlineBtnSprite;
            btnDiscardImg.type = Image.Type.Sliced;
            btnDiscardImg.color = Color.white;

            _discardButton = btnDiscardGo.GetComponent<Button>();
            _discardButton.onClick.AddListener(HandleDiscardClicked);
            CreateButtonText(btnDiscardGo.transform, "Start Fresh (Discard)", new Color(0.231f, 0.51f, 0.965f, 1f), FontStyles.Bold, 18);
            AttachHoverEffect(_discardButton, isPrimary: false);
        }

        private static void CreateButtonText(Transform parent, string text, Color color, FontStyles style, float fontSize)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
        }

        private static void AttachHoverEffect(Button button, bool isPrimary)
        {
            if (button == null) return;
            var hover = button.GetComponent<SyncDialogButtonHover>() ?? button.gameObject.AddComponent<SyncDialogButtonHover>();
            hover.Configure(button, isPrimary);
        }

        private static void EnsureFallbackSprites()
        {
            if (_cachedCardSprite == null)
            {
                _cachedCardSprite = CreateRoundedRectSprite(
                    size: 64,
                    radius: 16f,
                    borderWidth: 1.5f,
                    fillColor: new Color(0.082f, 0.102f, 0.153f, 0.98f),
                    borderColor: new Color(0.184f, 0.231f, 0.341f, 0.95f)
                );
            }

            if (_cachedPrimaryBtnSprite == null)
            {
                _cachedPrimaryBtnSprite = CreateRoundedRectSprite(
                    size: 48,
                    radius: 11f,
                    borderWidth: 0f,
                    fillColor: new Color(0.231f, 0.51f, 0.965f, 1f),
                    borderColor: Color.clear
                );
            }

            if (_cachedOutlineBtnSprite == null)
            {
                _cachedOutlineBtnSprite = CreateRoundedRectSprite(
                    size: 48,
                    radius: 11f,
                    borderWidth: 2f,
                    fillColor: new Color(0.082f, 0.102f, 0.153f, 0.95f),
                    borderColor: new Color(0.231f, 0.51f, 0.965f, 1f)
                );
            }
        }

        private static Sprite CreateRoundedRectSprite(int size, float radius, float borderWidth, Color fillColor, Color borderColor)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float half = (size - 1) * 0.5f;
            float innerHalf = half - radius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - half) - innerHalf, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - half) - innerHalf, 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;

                    float outerAlpha = Mathf.Clamp01(0.5f - dist);
                    if (outerAlpha <= 0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    Color pixel = fillColor;
                    if (borderWidth > 0f)
                    {
                        float borderFactor = Mathf.Clamp01(dist + borderWidth + 0.5f);
                        pixel = Color.Lerp(fillColor, borderColor, borderFactor);
                    }

                    pixel.a *= outerAlpha;
                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            int b = Mathf.CeilToInt(radius) + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        private sealed class SyncDialogButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
        {
            private Button _button;
            private Image _image;
            private TMP_Text _label;
            private bool _isPrimary;
            private bool _isHovered;
            private bool _isPressed;
            private Color _baseLabelColor;

            public void Configure(Button button, bool isPrimary)
            {
                _button = button;
                _isPrimary = isPrimary;
                _image = button.GetComponent<Image>();
                _label = button.GetComponentInChildren<TMP_Text>();
                if (_label != null) _baseLabelColor = _label.color;

                // Ensure Unity's default ColorBlock doesn't fight our custom hover tint
                button.transition = Selectable.Transition.None;
                ApplyVisualState(immediate: true);
            }

            private void Update()
            {
                ApplyVisualState(immediate: false);
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                _isHovered = true;
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                _isHovered = false;
                _isPressed = false;
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                _isPressed = true;
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                _isPressed = false;
            }

            private void ApplyVisualState(bool immediate)
            {
                if (_button == null || !_button.interactable) return;

                Color targetImgColor;
                Color targetLabelColor;

                if (_isPrimary)
                {
                    targetImgColor = _isPressed
                        ? new Color(0.68f, 0.75f, 0.88f, 1f)
                        : (_isHovered ? new Color(1.15f, 1.15f, 1.15f, 1f) : new Color(0.88f, 0.92f, 0.98f, 1f));
                    targetLabelColor = Color.white;
                }
                else
                {
                    targetImgColor = _isPressed
                        ? new Color(0.75f, 0.82f, 0.95f, 1f)
                        : (_isHovered ? new Color(1.12f, 1.18f, 1.3f, 1f) : Color.white);
                    targetLabelColor = _isPressed
                        ? _baseLabelColor
                        : (_isHovered ? new Color(0.58f, 0.78f, 1f, 1f) : _baseLabelColor);
                }

                float speed = immediate ? 1f : Mathf.Clamp01(Time.unscaledDeltaTime * 16f);
                if (_image != null) _image.color = Color.Lerp(_image.color, targetImgColor, speed);
                if (_label != null) _label.color = Color.Lerp(_label.color, targetLabelColor, speed);
            }
        }
    }
}
