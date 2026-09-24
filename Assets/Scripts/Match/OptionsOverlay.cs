using System;
using System.Collections.Generic;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class OptionsOverlay : MonoBehaviour
    {
        #region Fields
        [SerializeField] Button backButton;
        [SerializeField] Toggle notationToggle;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Dropdown languageDropdown;
        [SerializeField] OptionSliderView animationSlider;
        [SerializeField] OptionSliderView uiAnimSlider;
        [SerializeField] OptionSliderView shakeSlider;
        [SerializeField] OptionSliderView musicSlider;
        [SerializeField] OptionSliderView sfxSlider;
        [SerializeField] OptionSliderView historySlider;
        [SerializeField] Transform listParent;
        static OptionsOverlay _instance;
        bool _fromMatch;
        bool _wired;
        public static OptionsOverlay Instance => Ensure();
        public static bool IsOpen => _instance != null && _instance.gameObject.activeSelf;
        public event Action OpeningFromMenu;
        public event Action OpeningFromMatch;
        public event Action ClosedFromMenu;
        public event Action ClosedFromMatch;
        #endregion

        #region Unity
        void Awake()
        {
            Wake();
        }
        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
        #endregion

        #region Public Methods
        public static OptionsOverlay Ensure()
        {
            if (_instance != null)
                return _instance;
            OptionsOverlay existing = FindAnyObjectByType<OptionsOverlay>(FindObjectsInactive.Include);
            if (existing == null)
                return null;
            existing.Wake();
            return existing;
        }
        public void OpenFromMenu()
        {
            Wake();
            _fromMatch = false;
            OpeningFromMenu?.Invoke();
            OverlayMotion.Ensure(gameObject)?.PlayEnter();
        }
        public void OpenFromMatch()
        {
            Wake();
            _fromMatch = true;
            OpeningFromMatch?.Invoke();
            OverlayMotion.Ensure(gameObject)?.PlayEnter();
        }
        public void Close()
        {
            OverlayMotion.Ensure(gameObject)?.PlayExit();
            bool fromMatch = _fromMatch;
            _fromMatch = false;
            if (fromMatch)
                ClosedFromMatch?.Invoke();
            else
                ClosedFromMenu?.Invoke();
        }
        public void Refresh()
        {
            Wake();
            BindSliders();
            BindNotation();
            BindLanguage();
            BindName();
            LocalizedText.Bind(backButton, "options.back");
            Transform title = FindChild(transform, "Title");
            if (title != null)
                LocalizedText.Bind(title, "menu.options");
        }
        #endregion

        #region Private Methods
        void Wake()
        {
            _instance = this;
            if (_wired)
                return;
            Resolve();
            GameAudio.Bind(backButton, Close);
            _wired = true;
            Refresh();
        }
        void Resolve()
        {
            if (backButton == null)
                backButton = FindButton("BackButton");
            if (notationToggle == null)
            {
                Transform row = FindChild(transform, "NotationToggle");
                if (row != null)
                    notationToggle = row.GetComponent<Toggle>();
            }
            if (nameLabel == null)
            {
                Transform row = FindChild(transform, "NameLabel");
                if (row != null)
                {
                    nameLabel = row.GetComponent<TMP_Text>();
                    if (nameLabel == null)
                        nameLabel = row.GetComponentInChildren<TMP_Text>(true);
                }
            }
            if (languageDropdown == null)
            {
                Transform row = FindChild(transform, "LanguageDropdown");
                if (row != null)
                {
                    languageDropdown = row.GetComponent<TMP_Dropdown>();
                    if (languageDropdown == null)
                        languageDropdown = row.GetComponentInChildren<TMP_Dropdown>(true);
                }
            }
            if (listParent == null)
            {
                Transform scroll = FindChild(transform, "OptionsScroll");
                ScrollRect rect = scroll != null ? scroll.GetComponent<ScrollRect>() : null;
                if (rect != null && rect.content != null)
                    listParent = rect.content;
                else
                    listParent = FindChild(transform, "ButtonGroup");
            }
            animationSlider = ResolveSlider(animationSlider, "GameAnimation", "OptionSlider");
            uiAnimSlider = ResolveSlider(uiAnimSlider, "UIAnimation", "UiAnimSlider");
            shakeSlider = ResolveSlider(shakeSlider, "CameraShake", "CameraShakeSlider");
            musicSlider = ResolveSlider(musicSlider, "MusicSlider");
            sfxSlider = ResolveSlider(sfxSlider, "SfxSlider");
            historySlider = ResolveSlider(historySlider, "HistorySize", "HistorySlider");
        }
        void BindSliders()
        {
            animationSlider = BindSlider(
                animationSlider,
                Loc.Get("options.anim"),
                AnimationPrefs.MinMultiplier,
                AnimationPrefs.SkipStep,
                AnimationPrefs.SliderValue,
                v => AnimationPrefs.SliderValue = v,
                () => AnimationPrefs.Instant
                    ? Loc.Get("options.anim.off")
                    : Loc.Format("options.anim.times", Mathf.RoundToInt(AnimationPrefs.Multiplier)));
            uiAnimSlider = BindSlider(
                uiAnimSlider,
                Loc.Get("options.uiAnim"),
                UiAnimPrefs.MinMultiplier,
                UiAnimPrefs.SkipStep,
                UiAnimPrefs.SliderValue,
                v => UiAnimPrefs.SliderValue = v,
                () => UiAnimPrefs.Instant
                    ? Loc.Get("options.uiAnim.off")
                    : Loc.Format("options.uiAnim.times", Mathf.RoundToInt(UiAnimPrefs.Multiplier)));
            shakeSlider = BindSlider(
                shakeSlider,
                Loc.Get("options.shake"),
                CameraShakePrefs.Min,
                CameraShakePrefs.Max,
                CameraShakePrefs.SliderValue,
                v => CameraShakePrefs.SliderValue = Mathf.RoundToInt(v),
                () => CameraShakePrefs.SliderValue <= CameraShakePrefs.Min
                    ? Loc.Get("options.shake.off")
                    : Loc.Format("options.volume", CameraShakePrefs.SliderValue));
            musicSlider = BindSlider(
                musicSlider,
                Loc.Get("options.music"),
                AudioPrefs.Min,
                AudioPrefs.Max,
                AudioPrefs.Music,
                v => AudioPrefs.Music = Mathf.RoundToInt(v),
                () => Loc.Format("options.volume", AudioPrefs.Music));
            sfxSlider = BindSlider(
                sfxSlider,
                Loc.Get("options.sfx"),
                AudioPrefs.Min,
                AudioPrefs.Max,
                AudioPrefs.Sfx,
                v => AudioPrefs.Sfx = Mathf.RoundToInt(v),
                () => Loc.Format("options.volume", AudioPrefs.Sfx));
            if (historySlider != null)
                historySlider.gameObject.SetActive(false);
        }
        void BindNotation()
        {
            if (notationToggle == null)
                return;
            notationToggle.gameObject.SetActive(false);
        }
        void BindLanguage()
        {
            if (languageDropdown == null)
            {
                if (listParent == null)
                    return;
                languageDropdown = UiFactory.Dropdown(listParent, LanguageLabels(), Loc.LanguageIndex(), null);
                languageDropdown.name = "LanguageDropdown";
                languageDropdown.gameObject.name = "LanguageDropdown";
                PlaceBeforeBack(languageDropdown.transform);
                LayoutElement element = languageDropdown.GetComponent<LayoutElement>();
                if (element == null)
                    element = languageDropdown.gameObject.AddComponent<LayoutElement>();
                element.minWidth = 200f;
                element.preferredWidth = 200f;
                element.minHeight = 32f;
                element.preferredHeight = 32f;
            }
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string>(LanguageLabels()));
            languageDropdown.SetValueWithoutNotify(Loc.LanguageIndex());
            languageDropdown.onValueChanged.RemoveAllListeners();
            languageDropdown.onValueChanged.AddListener(index =>
            {
                GameAudio.PlayUi();
                if (index >= 0 && index < Loc.Codes.Length)
                    Loc.SetLanguage(Loc.Codes[index]);
            });
        }
        void BindName()
        {
            if (!PlayerIdentity.HasName)
            {
                if (nameLabel != null)
                    nameLabel.gameObject.SetActive(false);
                return;
            }
            if (nameLabel == null)
            {
                if (listParent == null)
                    return;
                nameLabel = UiFactory.Label(listParent, PlayerIdentity.DisplayName, 16, TextAlignmentOptions.Center);
                nameLabel.gameObject.name = "NameLabel";
                nameLabel.color = Color.black;
                nameLabel.raycastTarget = false;
                LayoutElement element = nameLabel.gameObject.AddComponent<LayoutElement>();
                element.minWidth = 200f;
                element.preferredWidth = 200f;
                element.minHeight = 32f;
                element.preferredHeight = 32f;
                nameLabel.transform.SetSiblingIndex(0);
            }
            nameLabel.gameObject.SetActive(true);
            nameLabel.text = Loc.Format("options.name", PlayerIdentity.DisplayName);
        }
        OptionSliderView BindSlider(
            OptionSliderView view,
            string label,
            float min,
            float max,
            float current,
            UnityAction<float> changed,
            Func<string> valueText)
        {
            if (view == null)
                return null;
            view.Bind(label, min, max, true, current, changed, valueText);
            return view;
        }
        OptionSliderView ResolveSlider(OptionSliderView view, params string[] names)
        {
            if (view != null)
                return view;
            Transform row = null;
            for (int i = 0; i < names.Length; i++)
            {
                row = FindChild(transform, names[i]);
                if (row != null)
                    break;
            }
            if (row == null)
            {
                if (listParent == null || names.Length == 0)
                    return null;
                GameObject prefab = RuntimePrefabs.OptionSlider;
                if (prefab == null)
                    return null;
                GameObject instance = Instantiate(prefab, listParent);
                instance.name = names[0];
                row = instance.transform;
                PlaceBeforeBack(row);
            }
            view = row.GetComponent<OptionSliderView>();
            if (view == null)
                view = row.gameObject.AddComponent<OptionSliderView>();
            return view;
        }
        Button FindButton(string name)
        {
            Transform child = FindChild(transform, name);
            return child != null ? child.GetComponent<Button>() : null;
        }
        static void PlaceBeforeBack(Transform row)
        {
            if (row == null || row.parent == null)
                return;
            Transform parent = row.parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == "BackButton")
                {
                    row.SetSiblingIndex(i);
                    return;
                }
            }
        }
        static string[] LanguageLabels()
        {
            return Loc.LanguageLabels();
        }
        static Transform FindChild(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }
        #endregion
    }
}
