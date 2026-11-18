#if UNITY_EDITOR
using System.IO;
using ChasingLight.UI.Leaderboard;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ChasingLight.Editor
{
    public static class LeaderboardPrefabBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/UI";
        private const string ItemPrefabPath = PrefabDir + "/LeaderboardItem.prefab";
        private const string PopupPrefabPath = PrefabDir + "/LeaderboardPopup.prefab";

        [MenuItem("Chasing Light/Build Leaderboard Prefabs")]
        public static void BuildPrefabs()
        {
            if (!Directory.Exists(PrefabDir))
                Directory.CreateDirectory(PrefabDir);

            // 1. Build Item Prefab
            var itemPrefab = BuildItemPrefab();

            // 2. Build Popup Prefab
            BuildPopupPrefab(itemPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LeaderboardPrefabBuilder] Build Leaderboard Prefabs completed successfully!");
        }

        private static LeaderboardItemView BuildItemPrefab()
        {
            var root = new GameObject("LeaderboardItem", typeof(RectTransform), typeof(Image), typeof(LeaderboardItemView));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(720, 60);

            var rootBg = root.GetComponent<Image>();
            rootBg.color = new Color(0.12f, 0.14f, 0.18f, 0.6f);

            // Self highlight indicator (viền sáng)
            var highlight = new GameObject("SelfHighlightIndicator", typeof(RectTransform), typeof(Image));
            highlight.transform.SetParent(root.transform, false);
            var hlRect = highlight.GetComponent<RectTransform>();
            hlRect.anchorMin = Vector2.zero;
            hlRect.anchorMax = Vector2.one;
            hlRect.sizeDelta = Vector2.zero;
            var hlImg = highlight.GetComponent<Image>();
            hlImg.color = new Color(0.3f, 0.7f, 1f, 0.35f);
            highlight.SetActive(false);

            // Rank Text
            var rankGo = CreateTMPText("RankText", root.transform, "1", 22, TextAlignmentOptions.Center);
            var rankRect = rankGo.GetComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0f, 0.5f);
            rankRect.anchorMax = new Vector2(0f, 0.5f);
            rankRect.pivot = new Vector2(0.5f, 0.5f);
            rankRect.anchoredPosition = new Vector2(35, 0);
            rankRect.sizeDelta = new Vector2(60, 40);

            // Avatar Image
            var avatarGo = new GameObject("AvatarImage", typeof(RectTransform), typeof(Image));
            avatarGo.transform.SetParent(root.transform, false);
            var avatarRect = avatarGo.GetComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0f, 0.5f);
            avatarRect.anchorMax = new Vector2(0f, 0.5f);
            avatarRect.pivot = new Vector2(0.5f, 0.5f);
            avatarRect.anchoredPosition = new Vector2(95, 0);
            avatarRect.sizeDelta = new Vector2(44, 44);
            var avatarImg = avatarGo.GetComponent<Image>();
            avatarImg.sprite = AvatarImageLoader.DefaultPlaceholder;

            // Name Text
            var nameGo = CreateTMPText("NameText", root.transform, "Player Name", 20, TextAlignmentOptions.MidlineLeft);
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(0f, 0.5f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.anchoredPosition = new Vector2(135, 0);
            nameRect.sizeDelta = new Vector2(280, 40);

            // Score Text
            var scoreGo = CreateTMPText("ScoreText", root.transform, "50 ⭐", 20, TextAlignmentOptions.MidlineRight);
            var scoreRect = scoreGo.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(1f, 0.5f);
            scoreRect.anchorMax = new Vector2(1f, 0.5f);
            scoreRect.pivot = new Vector2(1f, 0.5f);
            scoreRect.anchoredPosition = new Vector2(-20, 0);
            scoreRect.sizeDelta = new Vector2(260, 40);

            // Bind Serialized Properties
            var itemView = root.GetComponent<LeaderboardItemView>();
            var so = new SerializedObject(itemView);
            so.FindProperty("_rankText").objectReferenceValue = rankGo.GetComponent<TMP_Text>();
            so.FindProperty("_avatarImage").objectReferenceValue = avatarImg;
            so.FindProperty("_nameText").objectReferenceValue = nameGo.GetComponent<TMP_Text>();
            so.FindProperty("_scoreText").objectReferenceValue = scoreGo.GetComponent<TMP_Text>();
            so.FindProperty("_backgroundImage").objectReferenceValue = rootBg;
            so.FindProperty("_selfHighlightIndicator").objectReferenceValue = highlight;
            so.ApplyModifiedProperties();

            // Save Prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ItemPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<LeaderboardItemView>();
        }

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
            bdImg.color = new Color(0f, 0f, 0f, 0.72f);
            var bdButton = backdrop.GetComponent<Button>();

            // 2. Modal Window
            var modal = new GameObject("ModalWindow", typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(root.transform, false);
            var modalRect = modal.GetComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.sizeDelta = new Vector2(820, 680);
            var modalImg = modal.GetComponent<Image>();
            modalImg.color = new Color(0.10f, 0.12f, 0.17f, 0.98f);

            // ── Header ──
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(modal.transform, false);
            var hRect = header.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.anchoredPosition = Vector2.zero;
            hRect.sizeDelta = new Vector2(0, 60);

            var titleGo = CreateTMPText("TitleText", header.transform, "BẢNG XẾP HẠNG", 28, TextAlignmentOptions.Center);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.sizeDelta = Vector2.zero;
            var titleTmp = titleGo.GetComponent<TMP_Text>();
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(0.95f, 0.85f, 0.35f, 1f);

            var closeButtonGo = CreateButton("CloseButton", header.transform, "✕", 24, new Vector2(46, 46));
            var closeRect = closeButtonGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-15, 0);
            var closeButton = closeButtonGo.GetComponent<Button>();

            // ── Tab Bar ──
            var tabBar = new GameObject("TabBar", typeof(RectTransform));
            tabBar.transform.SetParent(modal.transform, false);
            var tbRect = tabBar.GetComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0f, 1f);
            tbRect.anchorMax = new Vector2(1f, 1f);
            tbRect.pivot = new Vector2(0.5f, 1f);
            tbRect.anchoredPosition = new Vector2(0, -60);
            tbRect.sizeDelta = new Vector2(0, 48);

            var globalTabGo = CreateButton("GlobalTabButton", tabBar.transform, "Toàn Cầu", 18, new Vector2(360, 40));
            var gTabRect = globalTabGo.GetComponent<RectTransform>();
            gTabRect.anchorMin = new Vector2(0.25f, 0.5f);
            gTabRect.anchorMax = new Vector2(0.25f, 0.5f);
            gTabRect.pivot = new Vector2(0.5f, 0.5f);
            var globalTabButton = globalTabGo.GetComponent<Button>();
            var globalTabBg = globalTabGo.GetComponent<Image>();

            var stageTabGo = CreateButton("StageTabButton", tabBar.transform, "Theo Màn", 18, new Vector2(360, 40));
            var sTabRect = stageTabGo.GetComponent<RectTransform>();
            sTabRect.anchorMin = new Vector2(0.75f, 0.5f);
            sTabRect.anchorMax = new Vector2(0.75f, 0.5f);
            sTabRect.pivot = new Vector2(0.5f, 0.5f);
            var stageTabButton = stageTabGo.GetComponent<Button>();
            var stageTabBg = stageTabGo.GetComponent<Image>();

            // ── Stage Selector Panel (Chỉ hiển thị khi ở Tab Stage) ──
            var stageSelPanel = new GameObject("StageSelectorPanel", typeof(RectTransform));
            stageSelPanel.transform.SetParent(modal.transform, false);
            var ssRect = stageSelPanel.GetComponent<RectTransform>();
            ssRect.anchorMin = new Vector2(0f, 1f);
            ssRect.anchorMax = new Vector2(1f, 1f);
            ssRect.pivot = new Vector2(0.5f, 1f);
            ssRect.anchoredPosition = new Vector2(0, -112);
            ssRect.sizeDelta = new Vector2(0, 44);

            var easyBtn = CreateButton("EasyButton", stageSelPanel.transform, "Easy", 16, new Vector2(80, 32)).GetComponent<Button>();
            easyBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280, 0);

            var normalBtn = CreateButton("NormalButton", stageSelPanel.transform, "Normal", 16, new Vector2(80, 32)).GetComponent<Button>();
            normalBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-190, 0);

            var hardBtn = CreateButton("HardButton", stageSelPanel.transform, "Hard", 16, new Vector2(80, 32)).GetComponent<Button>();
            hardBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-100, 0);

            var prevStageBtn = CreateButton("PrevStageButton", stageSelPanel.transform, "<", 18, new Vector2(36, 32)).GetComponent<Button>();
            prevStageBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(60, 0);

            var stageTitleGo = CreateTMPText("StageTitleText", stageSelPanel.transform, "Easy - Màn 1", 18, TextAlignmentOptions.Center);
            var stRect = stageTitleGo.GetComponent<RectTransform>();
            stRect.anchoredPosition = new Vector2(170, 0);
            stRect.sizeDelta = new Vector2(180, 32);

            var nextStageBtn = CreateButton("NextStageButton", stageSelPanel.transform, ">", 18, new Vector2(36, 32)).GetComponent<Button>();
            nextStageBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(280, 0);

            stageSelPanel.SetActive(false);

            // ── ScrollView Content ──
            var scrollView = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
            scrollView.transform.SetParent(modal.transform, false);
            var svRect = scrollView.GetComponent<RectTransform>();
            svRect.anchorMin = new Vector2(0.04f, 0.12f);
            svRect.anchorMax = new Vector2(0.96f, 0.81f);
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
            vlg.spacing = 6;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;

            // ── UX States: Loading, Empty, Error ──
            var loadingGo = CreateTMPText("LoadingPanel", modal.transform, "Đang tải dữ liệu xếp hạng...", 20, TextAlignmentOptions.Center);
            var loadingRect = loadingGo.GetComponent<RectTransform>();
            loadingRect.anchorMin = new Vector2(0.1f, 0.2f);
            loadingRect.anchorMax = new Vector2(0.9f, 0.8f);
            loadingRect.sizeDelta = Vector2.zero;
            loadingGo.SetActive(false);

            var emptyGo = CreateTMPText("EmptyPanel", modal.transform, "Chưa có lượt chơi nào!", 20, TextAlignmentOptions.Center);
            var emptyRect = emptyGo.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0.1f, 0.2f);
            emptyRect.anchorMax = new Vector2(0.9f, 0.8f);
            emptyRect.sizeDelta = Vector2.zero;
            emptyGo.SetActive(false);

            var errorGo = new GameObject("ErrorPanel", typeof(RectTransform));
            errorGo.transform.SetParent(modal.transform, false);
            var errRect = errorGo.GetComponent<RectTransform>();
            errRect.anchorMin = new Vector2(0.1f, 0.2f);
            errRect.anchorMax = new Vector2(0.9f, 0.8f);
            errRect.sizeDelta = Vector2.zero;

            var errMsgGo = CreateTMPText("ErrorMessage", errorGo.transform, "Không thể kết nối máy chủ.", 20, TextAlignmentOptions.Center);
            var errMsgRect = errMsgGo.GetComponent<RectTransform>();
            errMsgRect.anchoredPosition = new Vector2(0, 30);
            errMsgRect.sizeDelta = new Vector2(400, 50);

            var retryBtnGo = CreateButton("RetryButton", errorGo.transform, "Thử Lại", 18, new Vector2(160, 40));
            var retryRect = retryBtnGo.GetComponent<RectTransform>();
            retryRect.anchoredPosition = new Vector2(0, -30);
            var retryBtn = retryBtnGo.GetComponent<Button>();
            errorGo.SetActive(false);

            // ── Pinned My Rank Footer ──
            var footer = new GameObject("MyRankFooter", typeof(RectTransform), typeof(Image));
            footer.transform.SetParent(modal.transform, false);
            var fRect = footer.GetComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0f, 0f);
            fRect.anchorMax = new Vector2(1f, 0f);
            fRect.pivot = new Vector2(0.5f, 0f);
            fRect.anchoredPosition = Vector2.zero;
            fRect.sizeDelta = new Vector2(0, 70);
            var footerImg = footer.GetComponent<Image>();
            footerImg.color = new Color(0.08f, 0.10f, 0.14f, 1f);

            // Row View trong Footer
            var myRowGo = Object.Instantiate(itemPrefab.gameObject, footer.transform);
            myRowGo.name = "MyRankItemView";
            var myRowRect = myRowGo.GetComponent<RectTransform>();
            myRowRect.anchorMin = new Vector2(0.02f, 0.1f);
            myRowRect.anchorMax = new Vector2(0.98f, 0.9f);
            myRowRect.sizeDelta = Vector2.zero;
            var myItemView = myRowGo.GetComponent<LeaderboardItemView>();

            var guestNoticeGo = CreateTMPText("GuestNoticeText", footer.transform, "Đăng nhập tài khoản để lưu tiến trình và đua top!", 18, TextAlignmentOptions.Center);
            var gnRect = guestNoticeGo.GetComponent<RectTransform>();
            gnRect.anchorMin = Vector2.zero;
            gnRect.anchorMax = Vector2.one;
            gnRect.sizeDelta = Vector2.zero;
            guestNoticeGo.SetActive(false);

            // ── Bind to Controller ──
            var ctrl = root.GetComponent<LeaderboardPopupController>();
            var so = new SerializedObject(ctrl);

            so.FindProperty("_rootModal").objectReferenceValue = modal;
            so.FindProperty("_backdropButton").objectReferenceValue = bdButton;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;

            so.FindProperty("_globalTabButton").objectReferenceValue = globalTabButton;
            so.FindProperty("_stageTabButton").objectReferenceValue = stageTabButton;
            so.FindProperty("_globalTabBg").objectReferenceValue = globalTabBg;
            so.FindProperty("_stageTabBg").objectReferenceValue = stageTabBg;

            so.FindProperty("_stageSelectorPanel").objectReferenceValue = stageSelPanel;
            so.FindProperty("_easyDiffButton").objectReferenceValue = easyBtn;
            so.FindProperty("_normalDiffButton").objectReferenceValue = normalBtn;
            so.FindProperty("_hardDiffButton").objectReferenceValue = hardBtn;
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

            // Gán config & catalog nếu tìm thấy
            var authConfig = Resources.Load<AuthConfig>("AuthConfig");
            if (authConfig != null) so.FindProperty("_config").objectReferenceValue = authConfig;

            var stageCatalog = AssetDatabase.LoadAssetAtPath<StageCatalog>("Assets/Data/StageCatalog.asset");
            if (stageCatalog != null) so.FindProperty("_stageCatalog").objectReferenceValue = stageCatalog;

            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, PopupPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static GameObject CreateTMPText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, float fontSize, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.20f, 0.25f, 0.35f, 1f);

            var textGo = CreateTMPText("Label", go.transform, label, fontSize, TextAlignmentOptions.Center);
            var tRect = textGo.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            return go;
        }
    }
}
#endif
