using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EchoWorkSpace.Login;

namespace EchoWorkSpace
{
    [DisallowMultipleComponent]
    public sealed class MainMenuFlowController : MonoBehaviour
    {
        [Header("页面按钮")]
        [SerializeField] private Button _startAnalysisButton;
        [SerializeField] private Button _game1Button;
        [SerializeField] private Button _game2Button;
        [SerializeField] private Button _game3Button;
        [SerializeField] private Button _game4Button;

        [Header("Game1 选择面板")]
        [SerializeField] private GameObject _game1ChoosePanel;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _singleButton;
        [SerializeField] private Button _multiButton;

        [Header("知识答题卡片（场景未手动配置时，按 Game3 卡片运行时克隆生成，不改动场景层级）")]
        [SerializeField] private string _game4IconResource = "QuizGame/quiz_card";
        [SerializeField] private string _game4Title = "知识答题游戏";
        [SerializeField] private string _game4Description = "篮球足球趣味答题";

        [Header("排行榜入口（运行时自动挂到主 Canvas 右上角，不改场景层级）")]
        [SerializeField] private bool _showLeaderboardEntry = true;

        [Header("场景")]
        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private string _analysisScene = "Analysis";
        [SerializeField] private string _singleScene = "TrainingSingleBean";
        [SerializeField] private string _multiScene = "TrainingMultiBean";
        [SerializeField] private string _game2Scene = "TrainingMultiFruit";
        [SerializeField] private string _game3Scene = "TrainingSingleJumpingJacks";
        [SerializeField] private string _game4Scene = "TrainingQuiz";

        private bool _game4CardEnsured;
        private bool _leaderboardEntryEnsured;

        private void Awake()
        {
            EnsureEventSystemAlive();
            CloseGame1Choose();
        }

        /// <summary>
        /// 确保场景里有可用的 EventSystem 和输入模块；
        /// 场景自带的输入模块若失效，手机上会出现“所有按钮都点不动”的情况。
        /// </summary>
        private static void EnsureEventSystemAlive()
        {
            EventSystem es = EventSystem.current;
            if (es == null)
            {
                es = FindObjectOfType<EventSystem>();
                if (es == null)
                {
                    new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                    Debug.Log("[MainMenuFlow] 场景缺 EventSystem，已自动创建。");
                    return;
                }
            }

            bool hasActiveModule = false;
            foreach (BaseInputModule module in es.GetComponentsInChildren<BaseInputModule>(true))
            {
                if (module != null && module.isActiveAndEnabled)
                {
                    hasActiveModule = true;
                    break;
                }
            }

            if (!hasActiveModule)
            {
                es.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("[MainMenuFlow] EventSystem 缺少输入模块，已自动补上 StandaloneInputModule。");
            }

            Debug.Log($"[MainMenuFlow] EventSystem={es.name} 输入模块正常。");
        }

        private void OnEnable()
        {
            AddListeners();
        }

        private void OnDisable()
        {
            RemoveListeners();
        }

        public void OpenGame1Choose()
        {
            if (_game1ChoosePanel != null)
                _game1ChoosePanel.SetActive(true);
        }

        public void CloseGame1Choose()
        {
            if (_game1ChoosePanel != null)
                _game1ChoosePanel.SetActive(false);
        }

        public void LoadAnalysis()
        {
            LoadScene(_analysisScene);
        }

        public void LoadSingleTraining()
        {
            LoadScene(_singleScene);
        }

        public void LoadMultiTraining()
        {
            LoadScene(_multiScene);
        }

        public void LoadGame2()
        {
            LoadScene(_game2Scene);
        }

        public void LoadGame3()
        {
            LoadScene(_game3Scene);
        }

        public void LoadGame4()
        {
            LoadScene(_game4Scene);
        }

        private void LoadScene(string sceneName)
        {
            if (_sceneLoader == null)
            {
                Debug.LogError("[MainMenuFlow] 没有配置 SceneLoader。", this);
                return;
            }

            _sceneLoader.LoadScene(sceneName);
        }

        /// <summary>
        /// 场景里没有手动摆放第 4 张卡片时，运行时克隆 Game3 卡片生成，
        /// 完全不修改 Main 场景原有层级；GridLayoutGroup 会自动把它排到第 4 格。
        /// </summary>
        private void EnsureGame4Card()
        {
            if (_game4CardEnsured)
            {
                return;
            }

            _game4CardEnsured = true;

            if (_game4Button != null || _game3Button == null)
            {
                return;
            }

            GameObject template = _game3Button.gameObject;
            Transform parent = template.transform.parent;
            GameObject card = Instantiate(template, parent, worldPositionStays: false);
            card.name = "Game4";
            card.SetActive(true);
            card.transform.SetAsLastSibling();

            SetChildText(card.transform, "Name", _game4Title);
            SetChildText(card.transform, "Des", _game4Description);
            ReplaceChildIcon(card.transform, "Icon", _game4IconResource);

            _game4Button = card.GetComponent<Button>();
        }

        private static void SetChildText(Transform root, string childName, string value)
        {
            Transform child = root.Find(childName);
            if (child == null)
            {
                return;
            }

            Text text = child.GetComponent<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void ReplaceChildIcon(Transform root, string childName, string resourcePath)
        {
            Transform child = root.Find(childName);
            if (child == null)
            {
                return;
            }

            Image image = child.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                image.sprite = sprite;
            }
            else
            {
                Debug.LogWarning("[MainMenuFlow] 加载答题卡片图标失败：Resources/" + resourcePath);
            }
        }

        /// <summary>
        /// 运行时在主 Canvas 右上角挂奖杯形“排行榜”小圆钮，左上角挂微信风格的
        /// 灰白人物线条头像“我的”按钮；完全不改 Main 场景层级，多次进入场景也不会重复创建。
        /// </summary>
        private void EnsureLeaderboardEntry()
        {
            if (_leaderboardEntryEnsured || !_showLeaderboardEntry)
            {
                return;
            }
            _leaderboardEntryEnsured = true;

            // 独立置顶 Canvas：场景里自带的全屏 Mask / 面板会拦截主 Canvas 上的点击，
            // 入口按钮放更高排序的 overlay Canvas 上，保证永远在最上层、永远可点。
            Canvas entryCanvas = EnsureEntryOverlayCanvas();

            CreateRoundEntryButton(entryCanvas, "LeaderboardEntryButton", -24f,
                Color.white, null, Leaderboard.ShowPanel);
            CreateLogoutButton(entryCanvas);
            EnsureMyTab();
            RenameRecognitionTitle();
            EnsureRecognitionLayout();
        }

        [Header("教学视频（识别页运行时挂载，不改场景层级）")]
        [SerializeField] private string _videoCarouselResource = "VideoCarousel";

        private GameObject _videoOverlay;

        /// <summary>
        /// 重排识别页：取消原来“开始识别”按钮与教学视频的位置，
        /// 在标题下方并排放两张 4:3 卡片——「开始识别」和「教学视频」，
        /// 点教学卡片弹出原视频轮播浮层。
        /// </summary>
        private void EnsureRecognitionLayout()
        {
            GameObject recog = GameObject.Find("Panel识别");
            if (recog == null)
                return;
            RectTransform page = recog.transform as RectTransform;

            // 取消原来的“开始识别”按钮（场景物体直接隐藏，不改场景文件）
            Transform start = page.Find("Start");
            if (start != null)
                start.gameObject.SetActive(false);

            // 场景里原本的教学视频轮播先藏起来（进教学视频页时再搬出来显示），
            // 否则它会盖住下面的两张卡片
            Transform canvasRoot = page.transform.parent;
            if (canvasRoot != null)
            {
                foreach (EchoWorkSpace.UI.VideoCarousel old in
                    canvasRoot.GetComponentsInChildren<EchoWorkSpace.UI.VideoCarousel>(true))
                {
                    old.gameObject.SetActive(false);
                }
            }

            // 两张 4:3 卡片已建过就不再重复
            if (page.Find("RecogCard") != null)
                return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 尺寸：4:3 竖排，上下两张，居中
            float cardW = 330f;
            float cardH = cardW * 3f / 4f; // 247.5
            float cardTop = -175f;
            float cardGap = 20f;

            // ── 上卡：开始识别（黑底白字）─────────────────────────
            GameObject recogGo = new GameObject("RecogCard", typeof(RectTransform));
            recogGo.layer = LayerMask.NameToLayer("UI");
            RectTransform recogRect = (RectTransform)recogGo.transform;
            recogRect.SetParent(page, false);
            recogRect.anchorMin = new Vector2(0.5f, 1f);
            recogRect.anchorMax = new Vector2(0.5f, 1f);
            recogRect.pivot = new Vector2(0.5f, 1f);
            recogRect.anchoredPosition = new Vector2(0f, cardTop);
            recogRect.sizeDelta = new Vector2(cardW, cardH);

            Image recogImage = recogGo.AddComponent<Image>();
            recogImage.color = HexColor("#1A1A1A");
            RoundedUIImage recogRounded = recogGo.AddComponent<RoundedUIImage>();
            recogRounded.CornerRadius = 28f;

            Button recogButton = recogGo.AddComponent<Button>();
            recogButton.transition = Selectable.Transition.None;
            recogButton.targetGraphic = recogImage;
            recogButton.onClick.AddListener(LoadAnalysis);

            // 文字必须放独立子物体：同一 GameObject 上多个 Graphic 共用一个
            // CanvasRenderer，会互相覆盖导致文字画不出来
            AddChildLabel(recogGo, "开始识别", 46, Color.white, FontStyle.Bold);

            // ── 下卡：教学视频（白底黑边，进入教学视频页面）─────────
            GameObject videoGo = new GameObject("VideoCard", typeof(RectTransform));
            videoGo.layer = LayerMask.NameToLayer("UI");
            RectTransform videoRect = (RectTransform)videoGo.transform;
            videoRect.SetParent(page, false);
            videoRect.anchorMin = new Vector2(0.5f, 1f);
            videoRect.anchorMax = new Vector2(0.5f, 1f);
            videoRect.pivot = new Vector2(0.5f, 1f);
            videoRect.anchoredPosition = new Vector2(0f, cardTop - cardH - cardGap);
            videoRect.sizeDelta = new Vector2(cardW, cardH);

            Image videoImage = videoGo.AddComponent<Image>();
            videoImage.color = Color.white;
            RoundedUIImage videoRounded = videoGo.AddComponent<RoundedUIImage>();
            videoRounded.CornerRadius = 28f;
            videoRounded.BorderColor = HexColor("#1A1A1A");
            videoRounded.BorderWidth = 3f;

            Button videoButton = videoGo.AddComponent<Button>();
            videoButton.transition = Selectable.Transition.None;
            videoButton.targetGraphic = videoImage;
            videoButton.onClick.AddListener(ShowVideoPage);

            AddChildLabel(videoGo, "教学视频", 46, HexColor("#1A1A1A"), FontStyle.Bold);
        }

        /// <summary>
        /// 给按钮/卡片加一个铺满的子物体文字标签。
        /// 同一 GameObject 上挂多个 Graphic（Image + Text 等）会共用一个
        /// CanvasRenderer 互相覆盖，文字必须放独立子物体才能显示。
        /// </summary>
        private static void AddChildLabel(GameObject parent, string text, int fontSize, Color color,
            FontStyle style = FontStyle.Bold)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = (RectTransform)labelGo.transform;
            rect.SetParent(parent.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text label = labelGo.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
        }

        /// <summary>
        /// 进入独立的教学视频页面：标题 + 原本页面顶部的视频轮播（整体搬进来）。
        /// 底部页签栏保持可见，切页签自动收起本页；右上角 ✕ 返回识别页。
        /// </summary>
        private void ShowVideoPage()
        {
            if (_videoOverlay != null)
            {
                _videoOverlay.SetActive(true);
                return;
            }

            GameObject recog = GameObject.Find("Panel识别");
            RectTransform parent = recog != null ? recog.transform.parent as RectTransform : null;
            if (parent == null)
            {
                Debug.LogWarning("[MainMenuFlow] 找不到主 Canvas，无法创建教学视频页面。", this);
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 页面本体：与识别 / 游戏同级，底部给页签栏留空间
            GameObject pageGo = new GameObject("Panel教学视频", typeof(RectTransform));
            pageGo.layer = LayerMask.NameToLayer("UI");
            RectTransform root = (RectTransform)pageGo.transform;
            root.SetParent(parent, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = new Vector2(0f, 150f);
            root.offsetMax = Vector2.zero;
            _videoOverlay = pageGo;

            // 进入教学视频页时把识别页收起来，返回时再显示
            if (recog != null)
                recog.SetActive(false);

            Image bg = pageGo.AddComponent<Image>();
            bg.color = HexColor("#FFFFFF");
            bg.raycastTarget = true;

            // 大标题（与识别页标题同款位置）
            GameObject titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.layer = LayerMask.NameToLayer("UI");
            RectTransform titleRect = (RectTransform)titleGo.transform;
            titleRect.SetParent(root, false);
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -63f);
            titleRect.sizeDelta = new Vector2(0f, 90f);
            Text title = titleGo.AddComponent<Text>();
            title.font = font;
            title.text = "教学视频 ♥";
            title.fontSize = 64;
            title.fontStyle = FontStyle.Bold;
            title.color = HexColor("#111111");
            title.alignment = TextAnchor.MiddleCenter;
            title.raycastTarget = false;

            // 竖排视频列表：视频源优先读场景里原有的轮播配置（轮播保持隐藏，不再使用横排轮播），
            // 只保留真正配置了视频的条目；一条都没有时退回 Resources 里的教学视频
            EchoWorkSpace.UI.VideoCarousel.Entry[] entries = null;
            EchoWorkSpace.UI.VideoCarousel source =
                parent.GetComponentInChildren<EchoWorkSpace.UI.VideoCarousel>(true);
            if (source != null && source.Videos != null && source.Videos.Length > 0)
            {
                List<EchoWorkSpace.UI.VideoCarousel.Entry> withSource =
                    new List<EchoWorkSpace.UI.VideoCarousel.Entry>();
                foreach (EchoWorkSpace.UI.VideoCarousel.Entry e in source.Videos)
                    if (e != null && e.HasSource)
                        withSource.Add(e);
                if (withSource.Count > 0)
                    entries = withSource.ToArray();
            }
            if (entries == null || entries.Length == 0)
            {
                EchoWorkSpace.UI.VideoCarousel carouselPrefab =
                    Resources.Load<EchoWorkSpace.UI.VideoCarousel>(_videoCarouselResource);
                if (carouselPrefab != null && carouselPrefab.Videos != null)
                {
                    List<EchoWorkSpace.UI.VideoCarousel.Entry> withSource =
                        new List<EchoWorkSpace.UI.VideoCarousel.Entry>();
                    foreach (EchoWorkSpace.UI.VideoCarousel.Entry e in carouselPrefab.Videos)
                        if (e != null && e.HasSource)
                            withSource.Add(e);
                    if (withSource.Count > 0)
                        entries = withSource.ToArray();
                }
            }
            if (entries == null || entries.Length == 0)
            {
                // 原轮播配置一直是空的：直接用 Resources 里的教学视频
                UnityEngine.Video.VideoClip clip = Resources.Load<UnityEngine.Video.VideoClip>("TeachingVideo");
                if (clip != null)
                {
                    entries = new[]
                    {
                        new EchoWorkSpace.UI.VideoCarousel.Entry { title = "篮球姿势教学", clip = clip }
                    };
                }
            }

            if (entries != null && entries.Length > 0)
            {
                // 场景实例可能把同一个视频配了多条（历史配置），按 clip/url 去重
                entries = DistinctEntries(entries);
                Debug.Log($"[MainMenuFlow] 教学视频条目数（去重后）：{entries.Length}");

                // 滚动视图：ScrollRoot -> Viewport(RectMask2D) -> Content(VerticalVideoList)
                GameObject scrollGo = new GameObject("VideoScroll", typeof(RectTransform));
                scrollGo.layer = LayerMask.NameToLayer("UI");
                RectTransform scrollRectRt = (RectTransform)scrollGo.transform;
                scrollRectRt.SetParent(root, false);
                scrollRectRt.anchorMin = Vector2.zero;
                scrollRectRt.anchorMax = Vector2.one;
                scrollRectRt.offsetMin = new Vector2(24f, 180f);
                scrollRectRt.offsetMax = new Vector2(-24f, -168f);
                ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.inertia = true;

                GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform));
                viewportGo.layer = LayerMask.NameToLayer("UI");
                RectTransform viewport = (RectTransform)viewportGo.transform;
                viewport.SetParent(scrollRectRt, false);
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.offsetMin = Vector2.zero;
                viewport.offsetMax = Vector2.zero;
                viewportGo.AddComponent<RectMask2D>();
                scroll.viewport = viewport;

                GameObject listGo = new GameObject("Content", typeof(RectTransform));
                listGo.layer = LayerMask.NameToLayer("UI");
                RectTransform listRect = (RectTransform)listGo.transform;
                listRect.SetParent(viewport, false);
                scroll.content = listRect;
                listGo.AddComponent<EchoWorkSpace.UI.VerticalVideoList>().Init(entries);
            }
            else
            {
                Debug.LogWarning("[MainMenuFlow] 没找到教学视频配置（场景与 Resources 均无）。", this);

                GameObject emptyGo = new GameObject("EmptyHint", typeof(RectTransform));
                emptyGo.layer = LayerMask.NameToLayer("UI");
                RectTransform emptyRect = (RectTransform)emptyGo.transform;
                emptyRect.SetParent(root, false);
                emptyRect.anchorMin = new Vector2(0f, 1f);
                emptyRect.anchorMax = new Vector2(1f, 1f);
                emptyRect.pivot = new Vector2(0.5f, 1f);
                emptyRect.anchoredPosition = new Vector2(0f, -300f);
                emptyRect.sizeDelta = new Vector2(0f, 80f);
                Text emptyText = emptyGo.AddComponent<Text>();
                emptyText.font = font;
                emptyText.text = "暂无教学视频";
                emptyText.fontSize = 40;
                emptyText.color = HexColor("#8C8C8C");
                emptyText.alignment = TextAnchor.MiddleCenter;
                emptyText.raycastTarget = false;
            }

            // 底部「返回」按钮：回到识别页
            GameObject backGo = new GameObject("BackVideoButton", typeof(RectTransform));
            backGo.layer = LayerMask.NameToLayer("UI");
            RectTransform backRect = (RectTransform)backGo.transform;
            backRect.SetParent(root, false);
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.anchoredPosition = new Vector2(0f, 40f);
            backRect.sizeDelta = new Vector2(320f, 96f);
            Image backImage = backGo.AddComponent<Image>();
            backImage.color = HexColor("#1A1A1A");
            RoundedUIImage backRounded = backGo.AddComponent<RoundedUIImage>();
            backRounded.CornerRadius = 48f;
            Button backButton = backGo.AddComponent<Button>();
            backButton.transition = Selectable.Transition.None;
            backButton.targetGraphic = backImage;
            backButton.onClick.AddListener(() =>
            {
                _videoOverlay.SetActive(false);
                GameObject back = GameObject.Find("Panel识别");
                if (back != null)
                    back.SetActive(true);
            });
            AddChildLabel(backGo, "返回 ♥", 38, Color.white, FontStyle.Bold);

            // 切底部页签时自动收起教学视频页
            BottomNavigationController nav = GetComponent<BottomNavigationController>();
            if (nav != null)
                nav.RegisterCloseWhenSwitching(pageGo);
        }

        private static Color HexColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;
        }

        /// <summary>按 clip 引用（或 url）对视频条目去重，保留首次出现的标题。</summary>
        private static EchoWorkSpace.UI.VideoCarousel.Entry[] DistinctEntries(
            EchoWorkSpace.UI.VideoCarousel.Entry[] entries)
        {
            List<EchoWorkSpace.UI.VideoCarousel.Entry> result =
                new List<EchoWorkSpace.UI.VideoCarousel.Entry>();
            HashSet<int> seenClips = new HashSet<int>();
            HashSet<string> seenUrls = new HashSet<string>();

            foreach (EchoWorkSpace.UI.VideoCarousel.Entry e in entries)
            {
                if (e == null)
                    continue;
                if (e.clip != null)
                {
                    int id = e.clip.GetInstanceID();
                    if (!seenClips.Add(id))
                        continue;
                }
                else if (!string.IsNullOrWhiteSpace(e.url))
                {
                    string u = e.url.Trim();
                    if (!seenUrls.Add(u))
                        continue;
                }
                result.Add(e);
            }

            return result.ToArray();
        }

        /// <summary>
        /// 把“我的”做进底部导航：克隆“识别”页签生成第三个页签，
        /// 运行时创建 Panel我的 页面（与 Panel识别 / Panel游戏 同级），
        /// 并注册进 BottomNavigationController。不改场景文件。
        /// </summary>
        private void EnsureMyTab()
        {
            if (GameObject.Find("Panel我的") != null)
                return;

            BottomNavigationController nav = GetComponent<BottomNavigationController>();
            if (nav == null)
            {
                Debug.LogWarning("[MainMenuFlow] 找不到底部导航控制器，无法添加“我的”页签。", this);
                return;
            }

            Transform foot = GameObject.Find("Foot") != null ? GameObject.Find("Foot").transform : null;
            if (foot == null)
            {
                Debug.LogWarning("[MainMenuFlow] 找不到底部页签栏 Foot，无法添加“我的”页签。", this);
                return;
            }

            // 用“识别”页签当模板克隆
            Transform template = null;
            foreach (Transform child in foot)
            {
                Text label = child.GetComponentInChildren<Text>(true);
                if (label != null && label.text == "识别")
                {
                    template = child;
                    break;
                }
            }
            if (template == null)
            {
                Debug.LogWarning("[MainMenuFlow] 找不到“识别”页签模板，无法添加“我的”页签。", this);
                return;
            }

            Transform clone = Instantiate(template, foot);
            clone.name = "我的";
            Text cloneLabel = clone.GetComponentInChildren<Text>(true);
            if (cloneLabel != null)
                cloneLabel.text = "我的";
            Button myButton = clone.GetComponent<Button>();

            // 三个页签均分底部栏
            int count = foot.childCount;
            for (int i = 0; i < count; i++)
            {
                if (!(foot.GetChild(i) is RectTransform rt))
                    continue;
                rt.anchorMin = new Vector2((float)i / count, 0f);
                rt.anchorMax = new Vector2((float)(i + 1) / count, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(10f, 8f);
                rt.offsetMax = new Vector2(-10f, -8f);
            }

            // 页面挂在 Panel识别 的父节点（主 Canvas）下
            GameObject recog = GameObject.Find("Panel识别");
            RectTransform parent = recog != null ? recog.transform.parent as RectTransform : null;
            if (parent == null)
            {
                Debug.LogWarning("[MainMenuFlow] 找不到主 Canvas，无法创建“我的”页面。", this);
                return;
            }
            GameObject myPage = MyTabPage.CreatePage(parent);

            nav.AddRuntimeItem(myButton, myPage);
            Debug.Log("[MainMenuFlow] 已添加底部“我的”页签与页面。", this);
        }

        /// <summary>把识别页面的大标题改成“篮球姿势识别”。</summary>
        private void RenameRecognitionTitle()
        {
            GameObject recog = GameObject.Find("Panel识别");
            if (recog == null)
                return;

            Transform title = recog.transform.Find("Title");
            Text text = title != null ? title.GetComponent<Text>() : null;
            if (text == null)
            {
                Debug.LogWarning("[MainMenuFlow] 识别页面没有找到 Title 文本。", this);
                return;
            }

            text.text = "篮球姿势识别";
            // 标题变长，开启自适应缩放避免超出屏幕
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 40;
            text.resizeTextMaxSize = Mathf.Max(40, text.fontSize);
        }

        /// <summary>入口按钮专用的高排序 overlay Canvas（多次进入场景不重复创建）。</summary>
        private Canvas EnsureEntryOverlayCanvas()
        {
            GameObject existing = GameObject.Find("EntryOverlayCanvas");
            if (existing != null)
                return existing.GetComponent<Canvas>();

            GameObject go = new GameObject("EntryOverlayCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // 压过场景里的普通 UI 面板

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 1600f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.187f; // 与登录 / 主菜单一致

            return canvas;
        }

        /// <summary>
        /// 左上角“退出账号”按钮：保留微信风格的灰白人物线条头像外观，
        /// 点击后清除服务器令牌与本地会话，回到登录界面。
        /// </summary>
        private void CreateLogoutButton(Canvas canvas)
        {
            if (canvas.transform.Find("LogoutButton") != null)
            {
                return;
            }

            Color avatarBg = new Color(0.949f, 0.957f, 0.973f, 0.97f); // 灰白 #F2F4F8

            GameObject go = new GameObject("LogoutButton", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = new Vector2(0f, 1f); // 左上角
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(96f, 96f);

            Image image = go.AddComponent<Image>();
            image.color = avatarBg;
            RoundedUIImage rounded = go.AddComponent<RoundedUIImage>();
            rounded.CornerRadius = 22f; // 圆角方形，微信头像风格
            rounded.BorderColor = new Color(0.878f, 0.878f, 0.878f, 1f); // #E0E0E0
            rounded.BorderWidth = 2f;

            Button button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;

            DoorIcon.Build(rect, "DoorIcon", 68f, avatarBg);

            // 头像下方的小字提示
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.layer = LayerMask.NameToLayer("UI");
            RectTransform labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -8f);
            labelRect.sizeDelta = new Vector2(96f, 30f);

            Text label = labelGo.AddComponent<Text>();
            label.font = font;
            label.text = "退出";
            label.fontSize = 22;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;

            button.onClick.AddListener(() =>
            {
                Debug.Log("[EntryButton] 点击了 退出账号");
                LogoutToLoginScene();
            });
        }

        /// <summary>退出登录：清服务器令牌 + 本地会话，回登录界面。</summary>
        private static void LogoutToLoginScene()
        {
            Net.OnlineAuth.ClearServerToken();
            LoginSession.Logout();
            SceneBundleLoader.Load("Login");
        }

        /// <summary>在主 Canvas 顶部生成一个圆形入口按钮；text 为 null 时画奖杯图形。</summary>
        private void CreateRoundEntryButton(Canvas canvas, string buttonName, float xOffset,
            Color circleColor, string text, System.Action onClick)
        {
            if (canvas.transform.Find(buttonName) != null)
            {
                return;
            }

            GameObject go = new GameObject(buttonName, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(xOffset, -24f);
            rect.sizeDelta = new Vector2(96f, 96f);

            Image image = go.AddComponent<Image>();
            image.color = circleColor;
            RoundedUIImage rounded = go.AddComponent<RoundedUIImage>();
            rounded.CornerRadius = 48f;
            rounded.BorderColor = new Color(0.1f, 0.1f, 0.1f, 1f); // 黑白风：白底黑边
            rounded.BorderWidth = 3f;

            Button button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;

            if (string.IsNullOrEmpty(text))
            {
                // 奖杯图标（排行榜）
                TrophyIcon.Build(rect, "Trophy", 64f);
            }
            else
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null)
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                GameObject labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.layer = LayerMask.NameToLayer("UI");
                RectTransform labelRect = (RectTransform)labelGo.transform;
                labelRect.SetParent(rect, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                Text label = labelGo.AddComponent<Text>();
                label.font = font;
                label.text = text;
                label.fontSize = 40;
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;
            }

            button.onClick.AddListener(() =>
            {
                Debug.Log($"[EntryButton] 点击了 {buttonName}");
                onClick();
            });
        }

        private void Update()
        {
            // 触摸诊断：松开构建前用于定位“按钮点不动”的问题
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began &&
                EventSystem.current != null)
            {
                Vector2 pos = Input.GetTouch(0).position;
                PointerEventData ped = new PointerEventData(EventSystem.current) { position = pos };
                List<RaycastResult> hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(ped, hits);
                System.Text.StringBuilder sb =
                    new System.Text.StringBuilder($"[Touch] pos={pos} hits={hits.Count}");
                for (int i = 0; i < hits.Count && i < 4; i++)
                    sb.Append($" | #{i} {hits[i].gameObject.name}");
                Debug.Log(sb.ToString());
            }
        }

        private void AddListeners()
        {
            EnsureGame4Card();
            EnsureLeaderboardEntry();

            if (_startAnalysisButton != null) _startAnalysisButton.onClick.AddListener(LoadAnalysis);
            if (_game1Button != null) _game1Button.onClick.AddListener(OpenGame1Choose);
            if (_game2Button != null) _game2Button.onClick.AddListener(LoadGame2);
            if (_game3Button != null) _game3Button.onClick.AddListener(LoadGame3);
            if (_game4Button != null) _game4Button.onClick.AddListener(LoadGame4);
            if (_backButton != null) _backButton.onClick.AddListener(CloseGame1Choose);
            if (_singleButton != null) _singleButton.onClick.AddListener(LoadSingleTraining);
            if (_multiButton != null) _multiButton.onClick.AddListener(LoadMultiTraining);
        }

        private void RemoveListeners()
        {
            if (_startAnalysisButton != null) _startAnalysisButton.onClick.RemoveListener(LoadAnalysis);
            if (_game1Button != null) _game1Button.onClick.RemoveListener(OpenGame1Choose);
            if (_game2Button != null) _game2Button.onClick.RemoveListener(LoadGame2);
            if (_game3Button != null) _game3Button.onClick.RemoveListener(LoadGame3);
            if (_game4Button != null) _game4Button.onClick.RemoveListener(LoadGame4);
            if (_backButton != null) _backButton.onClick.RemoveListener(CloseGame1Choose);
            if (_singleButton != null) _singleButton.onClick.RemoveListener(LoadSingleTraining);
            if (_multiButton != null) _multiButton.onClick.RemoveListener(LoadMultiTraining);
        }
    }
}
