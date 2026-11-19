using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ChasingLight.Editor
{
    public static class GameplayModalsBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/UI/GameplayModals.prefab";
        private const string ScenePath = "Assets/Scenes/Game.unity";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string CardSpritePath = "Assets/Image/Login/login-card.png";
        private const string PrimaryBtnSpritePath = "Assets/Image/Login/login-btn-primary.png";
        private const string OutlineBtnSpritePath = "Assets/Image/Login/login-btn-outline.png";
        private const string Ui2SpritePath = "Assets/Sprites/UI/ui2.png";
        private const string StageCatalogPath = "Assets/Data/StageCatalog.asset";

        [MenuItem("Chasing Light/Build Gameplay Modals")]
        public static void Build()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
            var primaryBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PrimaryBtnSpritePath);
            var outlineBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OutlineBtnSpritePath);
            var starSprite = AssetDatabase.LoadAllAssetsAtPath(Ui2SpritePath)
                .OfType<Sprite>()
                .FirstOrDefault(s => s.name == "ui2_34");

            // Tạo thư mục Prefabs/UI nếu chưa có
            var directory = Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            // 1. Dựng cây GameObject của GameplayModals
            var rootGo = new GameObject("GameplayModals", typeof(RectTransform), typeof(StageModalsView));
            var rootRect = rootGo.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            // Overlay_Dim
            var overlayGo = CreateImage("Overlay_Dim", rootGo, null, Image.Type.Simple, new Color(0f, 0f, 0f, 0.75f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, raycastTarget: true);
            var overlayRect = overlayGo.GetComponent<RectTransform>();
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // ================== PANEL PAUSE ==================
            var pauseGo = CreateImage("Panel_Pause", rootGo, cardSprite, Image.Type.Sliced, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(440, 380), Vector2.zero, raycastTarget: true);
            
            CreateText("Txt_Title", pauseGo, "TẠM DỪNG", 32, new Color(1f, 0.91f, 0.52f, 1f),
                TextAlignmentOptions.Center, font, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(340, 50), new Vector2(0, -35));

            var (btnResume, _) = CreateButton("Btn_Resume", pauseGo, primaryBtnSprite, "TIẾP TỤC", font,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280, 56), new Vector2(0, 45));

            var (btnPauseRestart, _) = CreateButton("Btn_Restart", pauseGo, outlineBtnSprite, "CHƠI LẠI", font,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280, 56), new Vector2(0, -25));

            var (btnPauseExit, _) = CreateButton("Btn_Exit", pauseGo, outlineBtnSprite, "CHỌN MÀN", font,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280, 56), new Vector2(0, -95));

            // ================== PANEL VICTORY ==================
            var victoryGo = CreateImage("Panel_Victory", rootGo, cardSprite, Image.Type.Sliced, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(480, 500), Vector2.zero, raycastTarget: true);

            var txtVictoryTitle = CreateText("Txt_Title", victoryGo, "CHIẾN THẮNG!", 32, new Color(1f, 0.91f, 0.52f, 1f),
                TextAlignmentOptions.Center, font, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420, 60), new Vector2(0, -30));

            // Star icons container
            var starsContainer = new GameObject("StarsContainer", typeof(RectTransform));
            starsContainer.transform.SetParent(victoryGo.transform, false);
            var starsRect = starsContainer.GetComponent<RectTransform>();
            starsRect.anchorMin = new Vector2(0.5f, 1f);
            starsRect.anchorMax = new Vector2(0.5f, 1f);
            starsRect.pivot = new Vector2(0.5f, 1f);
            starsRect.sizeDelta = new Vector2(280, 80);
            starsRect.anchoredPosition = new Vector2(0, -95);

            var star1 = CreateImage("Star_1", starsContainer, starSprite, Image.Type.Simple, new Color(0.3f, 0.3f, 0.3f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64, 64), new Vector2(-75, 0), false);
            var star2 = CreateImage("Star_2", starsContainer, starSprite, Image.Type.Simple, new Color(0.3f, 0.3f, 0.3f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(76, 76), new Vector2(0, 10), false);
            var star3 = CreateImage("Star_3", starsContainer, starSprite, Image.Type.Simple, new Color(0.3f, 0.3f, 0.3f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64, 64), new Vector2(75, 0), false);

            var starImages = new[]
            {
                star1.GetComponent<Image>(),
                star2.GetComponent<Image>(),
                star3.GetComponent<Image>()
            };

            // Stats box
            var statsBox = CreateImage("StatsBox", victoryGo, outlineBtnSprite, Image.Type.Sliced, new Color(0f, 0f, 0f, 0.4f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(380, 80), new Vector2(0, 15), false);

            var txtVictoryTime = CreateText("Txt_Time", statsBox, "Thời gian: 00:00", 20, Color.white,
                TextAlignmentOptions.Center, font, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(340, 30), new Vector2(0, 16));

            var txtVictorySteps = CreateText("Txt_Steps", statsBox, "Số bước: 0", 20, Color.white,
                TextAlignmentOptions.Center, font, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(340, 30), new Vector2(0, -16));

            var (btnNextStage, txtNextLabel) = CreateButton("Btn_NextStage", victoryGo, primaryBtnSprite, "Màn tiếp theo", font,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(320, 56), new Vector2(0, 115));

            var (btnVictoryReplay, _) = CreateButton("Btn_Replay", victoryGo, outlineBtnSprite, "Chơi lại", font,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(145, 50), new Vector2(-85, 45));

            var (btnVictoryExit, _) = CreateButton("Btn_Exit", victoryGo, outlineBtnSprite, "Chọn màn", font,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(145, 50), new Vector2(85, 45));

            // ================== PANEL DEFEAT ==================
            var defeatGo = CreateImage("Panel_Defeat", rootGo, cardSprite, Image.Type.Sliced, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(440, 390), Vector2.zero, raycastTarget: true);

            CreateText("Txt_Title", defeatGo, "THẤT BẠI", 32, new Color(1f, 0.42f, 0.42f, 1f),
                TextAlignmentOptions.Center, font, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(340, 50), new Vector2(0, -40));

            var txtDefeatReason = CreateText("Txt_Reason", defeatGo, "Bị bóng đêm bắt giữ!", 22, Color.white,
                TextAlignmentOptions.Center, font, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(380, 60), new Vector2(0, 50));

            var (btnDefeatRetry, _) = CreateButton("Btn_Retry", defeatGo, primaryBtnSprite, "THỬ LẠI", font,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280, 56), new Vector2(0, -25));

            var (btnDefeatExit, _) = CreateButton("Btn_Exit", defeatGo, outlineBtnSprite, "CHỌN MÀN", font,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280, 56), new Vector2(0, -95));

            // ================== HOOK SERIALIZED FIELDS ==================
            var modalsView = rootGo.GetComponent<StageModalsView>();
            var so = new SerializedObject(modalsView);
            so.FindProperty("overlayDim").objectReferenceValue = overlayGo;
            so.FindProperty("pausePanel").objectReferenceValue = pauseGo;
            so.FindProperty("victoryPanel").objectReferenceValue = victoryGo;
            so.FindProperty("defeatPanel").objectReferenceValue = defeatGo;

            so.FindProperty("btnResume").objectReferenceValue = btnResume;
            so.FindProperty("btnPauseRestart").objectReferenceValue = btnPauseRestart;
            so.FindProperty("btnPauseExit").objectReferenceValue = btnPauseExit;

            so.FindProperty("txtVictoryTitle").objectReferenceValue = txtVictoryTitle;
            var starArrayProp = so.FindProperty("starIcons");
            starArrayProp.arraySize = starImages.Length;
            for (int i = 0; i < starImages.Length; i++)
            {
                starArrayProp.GetArrayElementAtIndex(i).objectReferenceValue = starImages[i];
            }
            so.FindProperty("starActiveColor").colorValue = Color.white;
            so.FindProperty("starInactiveColor").colorValue = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            so.FindProperty("txtVictoryTime").objectReferenceValue = txtVictoryTime;
            so.FindProperty("txtVictorySteps").objectReferenceValue = txtVictorySteps;
            so.FindProperty("btnNextStage").objectReferenceValue = btnNextStage;
            so.FindProperty("txtNextButtonLabel").objectReferenceValue = txtNextLabel;
            so.FindProperty("btnVictoryReplay").objectReferenceValue = btnVictoryReplay;
            so.FindProperty("btnVictoryExit").objectReferenceValue = btnVictoryExit;

            so.FindProperty("txtDefeatReason").objectReferenceValue = txtDefeatReason;
            so.FindProperty("btnDefeatRetry").objectReferenceValue = btnDefeatRetry;
            so.FindProperty("btnDefeatExit").objectReferenceValue = btnDefeatExit;

            so.ApplyModifiedPropertiesWithoutUndo();

            // Mặc định ẩn tất cả panels
            overlayGo.SetActive(false);
            pauseGo.SetActive(false);
            victoryGo.SetActive(false);
            defeatGo.SetActive(false);

            // Lưu Prefab
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath);
            Object.DestroyImmediate(rootGo);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GameplayModalsBuilder] Đã tạo prefab thành công tại {PrefabPath}");

            // ================== CẬP NHẬT SCENE GAME ==================
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var hudGo = GameObject.Find("Canvas_HUD");
            if (hudGo == null)
            {
                Debug.LogError("[GameplayModalsBuilder] Không tìm thấy Canvas_HUD trong scene Game!");
                return;
            }

            // 1. Gắn GameplayModals instance nếu chưa có
            var existingModals = hudGo.transform.Find("GameplayModals");
            GameObject modalsInstanceGo;
            if (existingModals == null)
            {
                modalsInstanceGo = (GameObject)PrefabUtility.InstantiatePrefab(savedPrefab, hudGo.transform);
                modalsInstanceGo.name = "GameplayModals";
            }
            else
            {
                modalsInstanceGo = existingModals.gameObject;
            }

            // 2. Tạo nút Pause trên HUD nếu chưa có
            var existingPauseBtn = hudGo.transform.Find("Btn_Pause");
            Button hudPauseButton;
            if (existingPauseBtn == null)
            {
                var pauseBtnGo = CreateImage("Btn_Pause", hudGo, outlineBtnSprite, Image.Type.Sliced, Color.white,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(56, 56), new Vector2(-30, -30), raycastTarget: true);
                hudPauseButton = pauseBtnGo.AddComponent<Button>();
                hudPauseButton.targetGraphic = pauseBtnGo.GetComponent<Image>();

                // 2 thanh dọc icon Pause
                CreateImage("Bar1", pauseBtnGo, null, Image.Type.Simple, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(4, 18), new Vector2(-5, 0), false);
                CreateImage("Bar2", pauseBtnGo, null, Image.Type.Simple, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(4, 18), new Vector2(5, 0), false);
            }
            else
            {
                hudPauseButton = existingPauseBtn.GetComponent<Button>();
            }

            // 3. Gán tham chiếu cho StageController
            var stageController = Object.FindAnyObjectByType<StageController>();
            if (stageController != null)
            {
                var stageCatalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(StageCatalogPath);
                var scSo = new SerializedObject(stageController);
                scSo.FindProperty("modalsView").objectReferenceValue = modalsInstanceGo.GetComponent<StageModalsView>();
                scSo.FindProperty("stageCatalog").objectReferenceValue = stageCatalog;
                scSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(stageController);
                Debug.Log("[GameplayModalsBuilder] Đã gán modalsView & stageCatalog vào StageController.");
            }

            // 4. Gán tham chiếu cho GameplayHUDView
            var hudView = hudGo.GetComponent<GameplayHUDView>();
            if (hudView != null)
            {
                var hudSo = new SerializedObject(hudView);
                hudSo.FindProperty("btnPause").objectReferenceValue = hudPauseButton;
                hudSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(hudView);
                Debug.Log("[GameplayModalsBuilder] Đã gán btnPause vào GameplayHUDView.");
            }

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[GameplayModalsBuilder] Hoàn tất cập nhật Game.unity!");
        }

        private static GameObject CreateImage(string name, GameObject parent, Sprite sprite, Image.Type type, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition, bool raycastTarget)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = type;
            img.color = color;
            img.raycastTarget = raycastTarget;
            return go;
        }

        private static (Button, TextMeshProUGUI) CreateButton(string name, GameObject parent, Sprite bgSprite, string labelText,
            TMP_FontAsset font, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            var go = CreateImage(name, parent, bgSprite, Image.Type.Sliced, Color.white,
                anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition, raycastTarget: true);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();

            var text = CreateText("Label", go, labelText, 22, Color.white, TextAlignmentOptions.Center, font,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var textRect = text.GetComponent<RectTransform>();
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return (btn, text);
        }

        private static TextMeshProUGUI CreateText(string name, GameObject parent, string text, float fontSize, Color color,
            TextAlignmentOptions alignment, TMP_FontAsset font, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            if (font != null)
                tmp.font = font;

            return tmp;
        }
    }
}
