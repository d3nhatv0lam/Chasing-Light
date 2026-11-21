#if UNITY_EDITOR
using System.IO;
using System.Linq;
using ChasingLight.UI.Leaderboard;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ChasingLight.Editor
{
    /// <summary>
    /// Builder tool to construct Leaderboard UI prefabs
    /// </summary>
    public static class LeaderboardPrefabBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/UI";
        private const string ItemPrefabPath = PrefabDir + "/LeaderboardItem.prefab";
        private const string PopupPrefabPath = PrefabDir + "/LeaderboardPopup.prefab";

        private static TMP_FontAsset _waldsFont;
        private static Sprite _closeSprite;
        private static Sprite _cardSprite;
        private static Sprite _uiSprite;

        [MenuItem("Chasing Light/Build Leaderboard Prefabs")]
        public static void BuildPrefabs()
        {
            if (!Directory.Exists(PrefabDir))
                Directory.CreateDirectory(PrefabDir);

            LoadAssets();

            // 1. Build Item Prefab
            var itemPrefab = BuildItemPrefab();

            // 2. Build Popup Prefab
            BuildPopupPrefab(itemPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#FCD34D>[LeaderboardPrefabBuilder]</color> Successfully built Leaderboard prefabs");
        }

        private static void LoadAssets()
        {
            _waldsFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/WaldsMisery SDF.asset");

            _closeSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Image/DifficultySelect/close.png")
                .OfType<Sprite>()
                .FirstOrDefault();
            if (_closeSprite == null)
            {
                _closeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/DifficultySelect/close.png");
            }

            _cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/Login/login-card.png");
            _uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.asset");
        }

        private static Sprite GetSlicedSprite()
        {
            return _cardSprite != null ? _cardSprite : _uiSprite;
        }

        // ════════════════════════════════════════════════════════════════════════
        // 1. BUILD LEADERBOARD ITEM PREFAB
        // ════════════════════════════════════════════════════════════════════════

        private static LeaderboardItemView BuildItemPrefab()
        {
            var root = new GameObject("LeaderboardItem", typeof(RectTransform), typeof(Image), typeof(LeaderboardItemView), typeof(LayoutElement));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(800, 60);

            var le = root.GetComponent<LayoutElement>();
            le.minHeight = 60;
            le.preferredHeight = 60;
            le.flexibleHeight = 0;

            var rootBg = root.GetComponent<Image>();
            rootBg.sprite = GetSlicedSprite();
            rootBg.type = Image.Type.Sliced;
            rootBg.color = new Color(0.08f, 0.11f, 0.16f, 0.82f);

            // Self highlight indicator
            var highlight = new GameObject("SelfHighlightIndicator", typeof(RectTransform), typeof(Image));
            highlight.transform.SetParent(root.transform, false);
            var hlRect = highlight.GetComponent<RectTransform>();
            hlRect.anchorMin = Vector2.zero;
            hlRect.anchorMax = Vector2.one;
            hlRect.sizeDelta = Vector2.zero;
            var hlImg = highlight.GetComponent<Image>();
            hlImg.sprite = GetSlicedSprite();
            hlImg.type = Image.Type.Sliced;
            hlImg.color = new Color(0.22f, 0.74f, 0.97f, 0.50f);
            highlight.SetActive(false);

            // Thin inner border outline
            var innerBorder = new GameObject("InnerBorder", typeof(RectTransform), typeof(Image));
            innerBorder.transform.SetParent(root.transform, false);
            var ibRect = innerBorder.GetComponent<RectTransform>();
            ibRect.anchorMin = Vector2.zero;
            ibRect.anchorMax = Vector2.one;
            ibRect.sizeDelta = Vector2.zero;
            var ibImg = innerBorder.GetComponent<Image>();
            ibImg.sprite = GetSlicedSprite();
            ibImg.type = Image.Type.Sliced;
            ibImg.color = new Color(0.96f, 0.78f, 0.28f, 0.22f); // Subtle gold edge

            // Rank Text
            var rankGo = CreateTMPText("RankText", root.transform, "1", 24, TextAlignmentOptions.Center, null);
            var rankRect = rankGo.GetComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0f, 0.5f);
            rankRect.anchorMax = new Vector2(0f, 0.5f);
            rankRect.pivot = new Vector2(0.5f, 0.5f);
            rankRect.anchoredPosition = new Vector2(32, 0);
            rankRect.sizeDelta = new Vector2(48, 44);
            var rankTmp = rankGo.GetComponent<TextMeshProUGUI>();
            rankTmp.fontStyle = FontStyles.Bold;
            rankTmp.color = new Color(1f, 0.84f, 0.15f, 1f); // Gold

            // Avatar Frame Ring
            var avatarFrameGo = new GameObject("AvatarFrame", typeof(RectTransform), typeof(Image));
            avatarFrameGo.transform.SetParent(root.transform, false);
            var afRect = avatarFrameGo.GetComponent<RectTransform>();
            afRect.anchorMin = new Vector2(0f, 0.5f);
            afRect.anchorMax = new Vector2(0f, 0.5f);
            afRect.pivot = new Vector2(0.5f, 0.5f);
            afRect.anchoredPosition = new Vector2(86, 0);
            afRect.sizeDelta = new Vector2(46, 46);
            var afImg = avatarFrameGo.GetComponent<Image>();
            afImg.sprite = GetSlicedSprite();
            afImg.type = Image.Type.Sliced;
            afImg.color = new Color(0.96f, 0.78f, 0.28f, 0.65f); // Golden frame ring

            // Avatar Image
            var avatarGo = new GameObject("AvatarImage", typeof(RectTransform), typeof(Image), typeof(Mask));
            avatarGo.transform.SetParent(avatarFrameGo.transform, false);
            var avatarRect = avatarGo.GetComponent<RectTransform>();
            avatarRect.anchorMin = Vector2.zero;
            avatarRect.anchorMax = Vector2.one;
            avatarRect.offsetMin = new Vector2(2, 2);
            avatarRect.offsetMax = new Vector2(-2, -2);
            var avatarImg = avatarGo.GetComponent<Image>();
            avatarImg.sprite = AvatarImageLoader.DefaultPlaceholder;
            var mask = avatarGo.GetComponent<Mask>();
            mask.showMaskGraphic = true;

            // Player Name Text
            var nameGo = CreateTMPText("NameText", root.transform, "Player Name", 19, TextAlignmentOptions.MidlineLeft, null);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.offsetMin = new Vector2(126, 0);
            nameRect.offsetMax = new Vector2(-220, 0); // ~450px width allocated
            var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = Color.white;
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;

            // Score Text
            var scoreGo = CreateTMPText("ScoreText", root.transform, "50 Stars", 19, TextAlignmentOptions.MidlineRight, null);
            var scoreRect = scoreGo.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(1f, 0.5f);
            scoreRect.anchorMax = new Vector2(1f, 0.5f);
            scoreRect.pivot = new Vector2(1f, 0.5f);
            scoreRect.anchoredPosition = new Vector2(-20, 0);
            scoreRect.sizeDelta = new Vector2(200, 36);
            var scoreTmp = scoreGo.GetComponent<TextMeshProUGUI>();
            scoreTmp.fontStyle = FontStyles.Bold;
            scoreTmp.color = new Color(1f, 0.90f, 0.45f, 1f); // Warm Gold

            // SubScore Text (Steps / Stage stars)
            var subScoreGo = CreateTMPText("SubScoreText", root.transform, "3/3 Stars", 14, TextAlignmentOptions.MidlineRight, null);
            var subScoreRect = subScoreGo.GetComponent<RectTransform>();
            subScoreRect.anchorMin = new Vector2(1f, 0.5f);
            subScoreRect.anchorMax = new Vector2(1f, 0.5f);
            subScoreRect.pivot = new Vector2(1f, 0.5f);
            subScoreRect.anchoredPosition = new Vector2(-20, -16);
            subScoreRect.sizeDelta = new Vector2(200, 22);
            var subScoreTmp = subScoreGo.GetComponent<TextMeshProUGUI>();
            subScoreTmp.color = new Color(0.70f, 0.78f, 0.88f, 1f);
            subScoreGo.SetActive(false);

            // Bind Serialized Properties
            var itemView = root.GetComponent<LeaderboardItemView>();
            var so = new SerializedObject(itemView);
            so.FindProperty("_rankText").objectReferenceValue = rankGo.GetComponent<TMP_Text>();
            so.FindProperty("_avatarImage").objectReferenceValue = avatarImg;
            so.FindProperty("_nameText").objectReferenceValue = nameGo.GetComponent<TMP_Text>();
            so.FindProperty("_scoreText").objectReferenceValue = scoreGo.GetComponent<TMP_Text>();
            so.FindProperty("_subScoreText").objectReferenceValue = subScoreGo.GetComponent<TMP_Text>();
            so.FindProperty("_backgroundImage").objectReferenceValue = rootBg;
            so.FindProperty("_selfHighlightIndicator").objectReferenceValue = highlight;
            so.ApplyModifiedProperties();

            // Save Prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ItemPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<LeaderboardItemView>();
        }

        // ════════════════════════════════════════════════════════════════════════
        // 2. BUILD LEADERBOARD POPUP PREFAB
        // ════════════════════════════════════════════════════════════════════════

        private static void BuildPopupPrefab(LeaderboardItemView itemPrefab)
        {
            var root = new GameObject("LeaderboardPopup", typeof(RectTransform), typeof(LeaderboardPopupController));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            // 1. Dimming Backdrop
            var backdrop = new GameObject("DimmingBackdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdrop.transform.SetParent(root.transform, false);
            var bdRect = backdrop.GetComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;
            var bdImg = backdrop.GetComponent<Image>();
            bdImg.color = new Color(0.04f, 0.06f, 0.10f, 0.85f); // Deep midnight vignette dim
            var bdButton = backdrop.GetComponent<Button>();

            // 2. Modal Window with Elastic Pop-In (UIModalCard)
            var modal = new GameObject("ModalWindow", typeof(RectTransform), typeof(CanvasGroup), typeof(UIModalCard));
            modal.transform.SetParent(root.transform, false);
            var modalRect = modal.GetComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.sizeDelta = new Vector2(880, 710);

            // Modal Outer Glowing Golden Frame Border
            var modalBorder = new GameObject("ModalBorder", typeof(RectTransform), typeof(Image));
            modalBorder.transform.SetParent(modal.transform, false);
            var mbRect = modalBorder.GetComponent<RectTransform>();
            mbRect.anchorMin = Vector2.zero;
            mbRect.anchorMax = Vector2.one;
            mbRect.sizeDelta = Vector2.zero;
            var mbImg = modalBorder.GetComponent<Image>();
            mbImg.sprite = GetSlicedSprite();
            mbImg.type = Image.Type.Sliced;
            mbImg.color = new Color(0.96f, 0.78f, 0.28f, 1f); // Radiant Gold #F6C843

            // Modal Inner Dark Obsidian Body
            var modalBody = new GameObject("ModalBody", typeof(RectTransform), typeof(Image));
            modalBody.transform.SetParent(modalBorder.transform, false);
            var bodyRect = modalBody.GetComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(3, 3);
            bodyRect.offsetMax = new Vector2(-3, -3); // 3px gold frame
            var bodyImg = modalBody.GetComponent<Image>();
            bodyImg.sprite = GetSlicedSprite();
            bodyImg.type = Image.Type.Sliced;
            bodyImg.color = new Color(0.08f, 0.10f, 0.15f, 0.98f); // Deep Enchanted Slate

            // Ambient Top Light Glow Banner
            var topGlow = new GameObject("TopGlowBanner", typeof(RectTransform), typeof(Image));
            topGlow.transform.SetParent(modalBody.transform, false);
            var tgRect = topGlow.GetComponent<RectTransform>();
            tgRect.anchorMin = new Vector2(0f, 1f);
            tgRect.anchorMax = new Vector2(1f, 1f);
            tgRect.pivot = new Vector2(0.5f, 1f);
            tgRect.sizeDelta = new Vector2(0, 74);
            var tgImg = topGlow.GetComponent<Image>();
            tgImg.color = new Color(0.18f, 0.28f, 0.48f, 0.25f);

            // ── Header ──
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(modalBody.transform, false);
            var hRect = header.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.anchoredPosition = Vector2.zero;
            hRect.sizeDelta = new Vector2(0, 68);

            // Title "LEADERBOARD"
            var titleGo = CreateTMPText("TitleText", header.transform, "LEADERBOARD", 32, TextAlignmentOptions.Center, _waldsFont);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.sizeDelta = Vector2.zero;
            var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.characterSpacing = 2.5f;
            titleTmp.color = new Color(1f, 0.88f, 0.35f, 1f); // Radiant warm gold #FFE059
            titleTmp.margin = new Vector4(0, 5, 0, 0);

            // Close Button
            var closeButtonGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeButtonGo.transform.SetParent(header.transform, false);
            var closeRect = closeButtonGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-16, 0);
            closeRect.sizeDelta = new Vector2(46, 46);

            var closeImg = closeButtonGo.GetComponent<Image>();
            if (_closeSprite != null)
            {
                closeImg.sprite = _closeSprite;
                closeImg.color = Color.white;
            }
            else
            {
                closeImg.color = new Color(0.96f, 0.78f, 0.28f, 1f);
                var xText = CreateTMPText("XLabel", closeButtonGo.transform, "X", 22, TextAlignmentOptions.Center, _waldsFont);
                var xr = xText.GetComponent<RectTransform>();
                xr.anchorMin = Vector2.zero;
                xr.anchorMax = Vector2.one;
                xr.sizeDelta = Vector2.zero;
            }
            var closeButton = closeButtonGo.GetComponent<Button>();

            // Header Golden Divider Line
            var headerDivider = new GameObject("HeaderDivider", typeof(RectTransform), typeof(Image));
            headerDivider.transform.SetParent(modalBody.transform, false);
            var hdRect = headerDivider.GetComponent<RectTransform>();
            hdRect.anchorMin = new Vector2(0.04f, 1f);
            hdRect.anchorMax = new Vector2(0.96f, 1f);
            hdRect.pivot = new Vector2(0.5f, 1f);
            hdRect.anchoredPosition = new Vector2(0, -68);
            hdRect.sizeDelta = new Vector2(0, 2);
            var hdImg = headerDivider.GetComponent<Image>();
            hdImg.color = new Color(0.96f, 0.78f, 0.28f, 0.45f);

            // Diamond Center using a rotated Image
            var diamondGo = new GameObject("DiamondCenter", typeof(RectTransform), typeof(Image));
            diamondGo.transform.SetParent(headerDivider.transform, false);
            var dRect = diamondGo.GetComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 0.5f);
            dRect.anchorMax = new Vector2(0.5f, 0.5f);
            dRect.pivot = new Vector2(0.5f, 0.5f);
            dRect.anchoredPosition = Vector2.zero;
            dRect.sizeDelta = new Vector2(9, 9);
            dRect.localEulerAngles = new Vector3(0, 0, 45f);
            var dImg = diamondGo.GetComponent<Image>();
            dImg.color = new Color(0.96f, 0.78f, 0.28f, 1f);

            // ── Tab Bar (GLOBAL & BY STAGE) ──
            var tabBar = new GameObject("TabBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabBar.transform.SetParent(modalBody.transform, false);
            var tbRect = tabBar.GetComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0f, 1f);
            tbRect.anchorMax = new Vector2(1f, 1f);
            tbRect.pivot = new Vector2(0.5f, 1f);
            tbRect.anchoredPosition = new Vector2(0, -78);
            tbRect.sizeDelta = new Vector2(0, 44);

            var tbHlg = tabBar.GetComponent<HorizontalLayoutGroup>();
            tbHlg.padding = new RectOffset(28, 28, 0, 0);
            tbHlg.spacing = 16;
            tbHlg.childControlWidth = true;
            tbHlg.childControlHeight = true;
            tbHlg.childForceExpandWidth = true;
            tbHlg.childForceExpandHeight = true;

            var globalTabGo = CreateStyledButton("GlobalTabButton", tabBar.transform, "GLOBAL", 18, Vector2.zero, _waldsFont);
            var globalTabButton = globalTabGo.GetComponent<Button>();
            var globalTabBg = globalTabGo.GetComponent<Image>();
            var globalTabText = globalTabGo.GetComponentInChildren<TMP_Text>();

            var stageTabGo = CreateStyledButton("StageTabButton", tabBar.transform, "BY STAGE", 18, Vector2.zero, _waldsFont);
            var stageTabButton = stageTabGo.GetComponent<Button>();
            var stageTabBg = stageTabGo.GetComponent<Image>();
            var stageTabText = stageTabGo.GetComponentInChildren<TMP_Text>();

            // ── Stage Selector Panel (Active on BY STAGE tab) ──
            var stageSelPanel = new GameObject("StageSelectorPanel", typeof(RectTransform));
            stageSelPanel.transform.SetParent(modalBody.transform, false);
            var ssRect = stageSelPanel.GetComponent<RectTransform>();
            ssRect.anchorMin = new Vector2(0f, 1f);
            ssRect.anchorMax = new Vector2(1f, 1f);
            ssRect.pivot = new Vector2(0.5f, 1f);
            ssRect.anchoredPosition = new Vector2(0, -130);
            ssRect.sizeDelta = new Vector2(0, 38);

            // Difficulty Group
            var diffGroup = new GameObject("DifficultyGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            diffGroup.transform.SetParent(stageSelPanel.transform, false);
            var dgRect = diffGroup.GetComponent<RectTransform>();
            dgRect.anchorMin = new Vector2(0f, 0f);
            dgRect.anchorMax = new Vector2(0f, 1f);
            dgRect.pivot = new Vector2(0f, 0.5f);
            dgRect.anchoredPosition = new Vector2(28, 0);
            dgRect.sizeDelta = new Vector2(300, 0);

            var dgHlg = diffGroup.GetComponent<HorizontalLayoutGroup>();
            dgHlg.spacing = 8;
            dgHlg.childControlWidth = true;
            dgHlg.childControlHeight = true;
            dgHlg.childForceExpandWidth = true;
            dgHlg.childForceExpandHeight = true;

            var easyBtnGo = CreateStyledButton("EasyButton", diffGroup.transform, "EASY", 15, Vector2.zero, _waldsFont);
            var easyBtn = easyBtnGo.GetComponent<Button>();
            var easyBg = easyBtnGo.GetComponent<Image>();
            var easyText = easyBtnGo.GetComponentInChildren<TMP_Text>();

            var normalBtnGo = CreateStyledButton("NormalButton", diffGroup.transform, "NORMAL", 15, Vector2.zero, _waldsFont);
            var normalBtn = normalBtnGo.GetComponent<Button>();
            var normalBg = normalBtnGo.GetComponent<Image>();
            var normalText = normalBtnGo.GetComponentInChildren<TMP_Text>();

            var hardBtnGo = CreateStyledButton("HardButton", diffGroup.transform, "HARD", 15, Vector2.zero, _waldsFont);
            var hardBtn = hardBtnGo.GetComponent<Button>();
            var hardBg = hardBtnGo.GetComponent<Image>();
            var hardText = hardBtnGo.GetComponentInChildren<TMP_Text>();

            // Stage Navigation Group
            var stageNavGroup = new GameObject("StageNavGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            stageNavGroup.transform.SetParent(stageSelPanel.transform, false);
            var sngRect = stageNavGroup.GetComponent<RectTransform>();
            sngRect.anchorMin = new Vector2(1f, 0f);
            sngRect.anchorMax = new Vector2(1f, 1f);
            sngRect.pivot = new Vector2(1f, 0.5f);
            sngRect.anchoredPosition = new Vector2(-28, 0);
            sngRect.sizeDelta = new Vector2(300, 0);

            var sngHlg = stageNavGroup.GetComponent<HorizontalLayoutGroup>();
            sngHlg.spacing = 8;
            sngHlg.childControlWidth = true;
            sngHlg.childControlHeight = true;
            sngHlg.childForceExpandWidth = false;
            sngHlg.childForceExpandHeight = true;

            var prevStageBtnGo = CreateStyledButton("PrevStageButton", stageNavGroup.transform, "<", 18, new Vector2(38, 0), null);
            var prevStageBtn = prevStageBtnGo.GetComponent<Button>();
            var prevLe = prevStageBtnGo.AddComponent<LayoutElement>();
            prevLe.preferredWidth = 38;
            prevLe.flexibleWidth = 0;

            // StageTitle
            var stageTitleGo = CreateTMPText("StageTitleText", stageNavGroup.transform, "EASY - STAGE 1", 17, TextAlignmentOptions.Center, null);
            var stLe = stageTitleGo.AddComponent<LayoutElement>();
            stLe.preferredWidth = 200;
            stLe.flexibleWidth = 1;
            var stTmp = stageTitleGo.GetComponent<TextMeshProUGUI>();
            stTmp.fontStyle = FontStyles.Bold;
            stTmp.color = new Color(1f, 0.88f, 0.40f, 1f);

            var nextStageBtnGo = CreateStyledButton("NextStageButton", stageNavGroup.transform, ">", 18, new Vector2(38, 0), null);
            var nextStageBtn = nextStageBtnGo.GetComponent<Button>();
            var nextLe = nextStageBtnGo.AddComponent<LayoutElement>();
            nextLe.preferredWidth = 38;
            nextLe.flexibleWidth = 0;

            stageSelPanel.SetActive(false);

            // ── ScrollView Content ──
            var scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
            scrollView.transform.SetParent(modalBody.transform, false);
            var svRect = scrollView.GetComponent<RectTransform>();
            svRect.anchorMin = new Vector2(0.035f, 0.145f);
            svRect.anchorMax = new Vector2(0.965f, 0.81f);
            svRect.sizeDelta = Vector2.zero;
            var scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollView.transform, false);
            var vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            scrollRect.viewport = vpRect;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0, 0);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;

            // ── UX Status States: Loading, Empty, Error ──
            var loadingGo = CreateTMPText("LoadingPanel", modalBody.transform, "Loading leaderboard data...", 20, TextAlignmentOptions.Center, null);
            var loadingRect = loadingGo.GetComponent<RectTransform>();
            loadingRect.anchorMin = new Vector2(0.1f, 0.2f);
            loadingRect.anchorMax = new Vector2(0.9f, 0.75f);
            loadingRect.sizeDelta = Vector2.zero;
            loadingGo.GetComponent<TextMeshProUGUI>().color = new Color(0.96f, 0.78f, 0.28f, 0.9f);
            loadingGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            loadingGo.SetActive(false);

            var emptyGo = CreateTMPText("EmptyPanel", modalBody.transform, "No records yet! Be the first to conquer.", 20, TextAlignmentOptions.Center, null);
            var emptyRect = emptyGo.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0.1f, 0.2f);
            emptyRect.anchorMax = new Vector2(0.9f, 0.75f);
            emptyRect.sizeDelta = Vector2.zero;
            emptyGo.GetComponent<TextMeshProUGUI>().color = new Color(0.70f, 0.75f, 0.85f, 1f);
            emptyGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            emptyGo.SetActive(false);

            var errorGo = new GameObject("ErrorPanel", typeof(RectTransform));
            errorGo.transform.SetParent(modalBody.transform, false);
            var errRect = errorGo.GetComponent<RectTransform>();
            errRect.anchorMin = new Vector2(0.1f, 0.2f);
            errRect.anchorMax = new Vector2(0.9f, 0.75f);
            errRect.sizeDelta = Vector2.zero;

            var errMsgGo = CreateTMPText("ErrorMessage", errorGo.transform, "Unable to connect to server.", 19, TextAlignmentOptions.Center, null);
            var errMsgRect = errMsgGo.GetComponent<RectTransform>();
            errMsgRect.anchoredPosition = new Vector2(0, 30);
            errMsgRect.sizeDelta = new Vector2(500, 50);
            errMsgGo.GetComponent<TextMeshProUGUI>().color = new Color(0.96f, 0.45f, 0.45f, 1f);

            var retryBtnGo = CreateStyledButton("RetryButton", errorGo.transform, "RETRY", 18, new Vector2(160, 42), _waldsFont);
            var retryRect = retryBtnGo.GetComponent<RectTransform>();
            retryRect.anchoredPosition = new Vector2(0, -30);
            var retryBtn = retryBtnGo.GetComponent<Button>();
            errorGo.SetActive(false);

            // ── Pinned My Rank Footer ──
            var footer = new GameObject("MyRankFooter", typeof(RectTransform), typeof(Image));
            footer.transform.SetParent(modalBody.transform, false);
            var fRect = footer.GetComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0f, 0f);
            fRect.anchorMax = new Vector2(1f, 0f);
            fRect.pivot = new Vector2(0.5f, 0f);
            fRect.anchoredPosition = Vector2.zero;
            fRect.sizeDelta = new Vector2(0, 92);
            var footerImg = footer.GetComponent<Image>();
            footerImg.sprite = GetSlicedSprite();
            footerImg.type = Image.Type.Sliced;
            footerImg.color = new Color(0.05f, 0.07f, 0.11f, 0.98f); // Dark grounding slate

            // Golden Top Divider for Footer
            var footerDivider = new GameObject("FooterDivider", typeof(RectTransform), typeof(Image));
            footerDivider.transform.SetParent(footer.transform, false);
            var fdRect = footerDivider.GetComponent<RectTransform>();
            fdRect.anchorMin = new Vector2(0f, 1f);
            fdRect.anchorMax = new Vector2(1f, 1f);
            fdRect.pivot = new Vector2(0.5f, 1f);
            fdRect.anchoredPosition = Vector2.zero;
            fdRect.sizeDelta = new Vector2(0, 2);
            var fdImg = footerDivider.GetComponent<Image>();
            fdImg.color = new Color(0.96f, 0.78f, 0.28f, 0.45f);

            // "YOUR RANK" Badge Tag Header
            var badgeGo = CreateTMPText("FooterBadge", footer.transform, "YOUR RANK", 12, TextAlignmentOptions.MidlineLeft, _waldsFont);
            var badgeRect = badgeGo.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 1f);
            badgeRect.anchoredPosition = new Vector2(28, -5);
            badgeRect.sizeDelta = new Vector2(160, 18);
            var badgeTmp = badgeGo.GetComponent<TextMeshProUGUI>();
            badgeTmp.fontStyle = FontStyles.Bold;
            badgeTmp.characterSpacing = 2f;
            badgeTmp.color = new Color(0.96f, 0.78f, 0.28f, 0.95f);

            // Personal Row View inside Footer
            var myRowGo = Object.Instantiate(itemPrefab.gameObject, footer.transform);
            myRowGo.name = "MyRankItemView";
            var myRowRect = myRowGo.GetComponent<RectTransform>();
            myRowRect.anchorMin = new Vector2(0f, 0f);
            myRowRect.anchorMax = new Vector2(1f, 0f);
            myRowRect.pivot = new Vector2(0.5f, 0f);
            myRowRect.anchoredPosition = new Vector2(0, 8);
            myRowRect.sizeDelta = new Vector2(-48, 56);
            var myItemView = myRowGo.GetComponent<LeaderboardItemView>();

            var guestNoticeGo = CreateTMPText("GuestNoticeText", footer.transform, "Sign in to save progress and compete on the leaderboard!", 17, TextAlignmentOptions.Center, null);
            var gnRect = guestNoticeGo.GetComponent<RectTransform>();
            gnRect.anchorMin = Vector2.zero;
            gnRect.anchorMax = Vector2.one;
            gnRect.sizeDelta = Vector2.zero;
            guestNoticeGo.GetComponent<TextMeshProUGUI>().color = new Color(0.85f, 0.88f, 0.95f, 1f);
            guestNoticeGo.SetActive(false);

            // ── Bind to LeaderboardPopupController ──
            var ctrl = root.GetComponent<LeaderboardPopupController>();
            var so = new SerializedObject(ctrl);

            so.FindProperty("_rootModal").objectReferenceValue = modal;
            so.FindProperty("_backdropButton").objectReferenceValue = bdButton;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;

            so.FindProperty("_globalTabButton").objectReferenceValue = globalTabButton;
            so.FindProperty("_stageTabButton").objectReferenceValue = stageTabButton;
            so.FindProperty("_globalTabBg").objectReferenceValue = globalTabBg;
            so.FindProperty("_stageTabBg").objectReferenceValue = stageTabBg;
            so.FindProperty("_globalTabText").objectReferenceValue = globalTabText;
            so.FindProperty("_stageTabText").objectReferenceValue = stageTabText;

            so.FindProperty("_stageSelectorPanel").objectReferenceValue = stageSelPanel;
            so.FindProperty("_easyDiffButton").objectReferenceValue = easyBtn;
            so.FindProperty("_normalDiffButton").objectReferenceValue = normalBtn;
            so.FindProperty("_hardDiffButton").objectReferenceValue = hardBtn;
            so.FindProperty("_easyDiffBg").objectReferenceValue = easyBg;
            so.FindProperty("_normalDiffBg").objectReferenceValue = normalBg;
            so.FindProperty("_hardDiffBg").objectReferenceValue = hardBg;
            so.FindProperty("_easyDiffText").objectReferenceValue = easyText;
            so.FindProperty("_normalDiffText").objectReferenceValue = normalText;
            so.FindProperty("_hardDiffText").objectReferenceValue = hardText;

            so.FindProperty("_prevStageButton").objectReferenceValue = prevStageBtn;
            so.FindProperty("_nextStageButton").objectReferenceValue = nextStageBtn;
            so.FindProperty("_stageTitleText").objectReferenceValue = stageTitleGo.GetComponent<TMP_Text>();

            so.FindProperty("_contentContainer").objectReferenceValue = contentRect;
            so.FindProperty("_itemPrefab").objectReferenceValue = itemPrefab;
            so.FindProperty("_scrollRect").objectReferenceValue = scrollRect;

            so.FindProperty("_loadingPanel").objectReferenceValue = loadingGo;
            so.FindProperty("_emptyPanel").objectReferenceValue = emptyGo;
            so.FindProperty("_emptyMessageText").objectReferenceValue = emptyGo.GetComponent<TMP_Text>();
            so.FindProperty("_errorPanel").objectReferenceValue = errorGo;
            so.FindProperty("_errorMessageText").objectReferenceValue = errMsgGo.GetComponent<TMP_Text>();
            so.FindProperty("_retryButton").objectReferenceValue = retryBtn;

            so.FindProperty("_myRankFooter").objectReferenceValue = footer;
            so.FindProperty("_myRankItemView").objectReferenceValue = myItemView;
            so.FindProperty("_guestNoticeText").objectReferenceValue = guestNoticeGo.GetComponent<TMP_Text>();

            var authConfig = Resources.Load<AuthConfig>("AuthConfig");
            if (authConfig != null) so.FindProperty("_config").objectReferenceValue = authConfig;

            var stageCatalog = AssetDatabase.LoadAssetAtPath<StageCatalog>("Assets/Data/StageCatalog.asset");
            if (stageCatalog != null) so.FindProperty("_stageCatalog").objectReferenceValue = stageCatalog;

            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, PopupPrefabPath);
            Object.DestroyImmediate(root);
        }

        // ════════════════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════════════════

        private static GameObject CreateTMPText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return go;
        }

        private static GameObject CreateStyledButton(string name, Transform parent, string label, float fontSize, Vector2 size, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.sprite = GetSlicedSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.12f, 0.16f, 0.24f, 0.90f);

            // Subtle gold edge on button
            var edge = new GameObject("ButtonEdge", typeof(RectTransform), typeof(Image));
            edge.transform.SetParent(go.transform, false);
            var edgeRect = edge.GetComponent<RectTransform>();
            edgeRect.anchorMin = Vector2.zero;
            edgeRect.anchorMax = Vector2.one;
            edgeRect.sizeDelta = Vector2.zero;
            var edgeImg = edge.GetComponent<Image>();
            edgeImg.sprite = GetSlicedSprite();
            edgeImg.type = Image.Type.Sliced;
            edgeImg.color = new Color(0.96f, 0.78f, 0.28f, 0.35f);

            var textGo = CreateTMPText("Label", go.transform, label, fontSize, TextAlignmentOptions.Center, font);
            var tRect = textGo.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            if (font == _waldsFont)
            {
                var labelTmp = textGo.GetComponent<TextMeshProUGUI>();
                float topOffset = fontSize >= 18f ? 4f : 3f;
                labelTmp.margin = new Vector4(0f, topOffset, 0f, 0f);
            }

            return go;
        }
    }
}
#endif
