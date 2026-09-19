using System;
using System.Collections.Generic;
using ModularChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class MatchSettingsOverlay : MonoBehaviour
    {
        #region Fields
        [SerializeField] TMP_Dropdown timeDropdown;
        [SerializeField] TMP_Dropdown extraTimeDropdown;
        [SerializeField] TMP_Dropdown hostColorDropdown;
        [SerializeField] TMP_Dropdown aiDropdown;
        [SerializeField] GameObject aiGroup;
        [SerializeField] ScrollRect modesScroll;
        [SerializeField] Button confirmButton;
        [SerializeField] Button backButton;
        [SerializeField] Transform title;
        [SerializeField] GameObject selectionRowPrefab;
        [SerializeField] ModeSettingsPopup modeSettingsPopup;
        readonly List<GameObject> _spawnedRows = new List<GameObject>();
        List<ModeId> _selectedModes;
        HostModeSettings _modeSettings;
        Activity _activity;
        int _timePreset;
        int _incrementPreset;
        HostColor _hostColor = HostColor.White;
        AiStrength _aiStrength = AiStrength.Medium;
        #endregion

        #region Public Methods
        public void Bind(UnityAction onConfirm, UnityAction onBack)
        {
            Resolve();
            GameAudio.Bind(confirmButton, onConfirm);
            GameAudio.Bind(backButton, onBack);
            RefreshLoc();
        }
        public void Present(
            Activity activity,
            List<ModeId> selectedModes,
            HostModeSettings modeSettings,
            int timePreset,
            int incrementPreset,
            HostColor hostColor,
            AiStrength aiStrength)
        {
            Resolve();
            _activity = activity;
            _selectedModes = selectedModes;
            _modeSettings = modeSettings;
            _timePreset = timePreset;
            _incrementPreset = incrementPreset;
            _hostColor = hostColor;
            _aiStrength = aiStrength;
            BindTime();
            BindExtraTime();
            RefreshExtraTimeVisible();
            BindHostColor();
            if (aiGroup != null)
                aiGroup.SetActive(activity == Activity.VersusAi);
            if (activity == Activity.VersusAi)
                BindAi();
            if (confirmButton != null)
                LocalizedText.Bind(confirmButton, activity == Activity.VersusAi ? "settings.start" : "settings.createLobby");
            RebuildModes();
        }
        public int TimePreset => _timePreset;
        public int IncrementPreset => _incrementPreset;
        public HostColor HostColor => _hostColor;
        public AiStrength AiStrength => _aiStrength;
        public bool ModeSettingsOpen => modeSettingsPopup != null && modeSettingsPopup.IsOpen;
        public void CloseModeSettings()
        {
            modeSettingsPopup?.Close();
        }
        public void HideModeSettingsImmediate()
        {
            modeSettingsPopup?.HideImmediate();
        }
        public void RefreshLoc()
        {
            Resolve();
            LocalizedText.Bind(title, "settings.title");
            LocalizedText.Bind(backButton, "menu.back");
        }
        #endregion

        #region Private Methods
        void Resolve()
        {
            if (timeDropdown == null)
                timeDropdown = DropdownNamed("TimeDropdown");
            if (extraTimeDropdown == null)
                extraTimeDropdown = DropdownNamed("ExtraTimeDropdown");
            if (hostColorDropdown == null)
                hostColorDropdown = DropdownNamed("HostColorDropdown");
            if (aiDropdown == null)
                aiDropdown = DropdownNamed("AiDropdown");
            if (aiGroup == null)
            {
                Transform named = FindChild(transform, "AiGroup");
                if (named != null)
                    aiGroup = named.gameObject;
            }
            if (modesScroll == null)
                modesScroll = FindModesScroll();
            if (confirmButton == null)
                confirmButton = ButtonNamed("ConfirmButton");
            if (backButton == null)
                backButton = ButtonNamed("BackButton");
            if (title == null)
                title = FindChild(transform, "Title");
            if (selectionRowPrefab == null)
                selectionRowPrefab = RuntimePrefabs.SelectionRow;
            if (modeSettingsPopup != null && !modeSettingsPopup.gameObject.scene.IsValid())
                modeSettingsPopup = null;
            if (modeSettingsPopup == null)
                modeSettingsPopup = ModeSettingsPopup.Ensure(transform);
        }
        void BindTime()
        {
            BindDropdown(
                timeDropdown,
                new[]
                {
                    Loc.Get("settings.time.none"),
                    Loc.Get("settings.time.bullet"),
                    Loc.Get("settings.time.blitz"),
                    Loc.Get("settings.time.rapid"),
                    Loc.Get("settings.time.standard"),
                    Loc.Get("settings.time.extended")
                },
                _timePreset,
                v =>
                {
                    _timePreset = v;
                    if (v == 0)
                        _incrementPreset = 0;
                    RefreshExtraTimeVisible();
                });
        }
        void BindExtraTime()
        {
            BindDropdown(
                extraTimeDropdown,
                new[]
                {
                    Loc.Get("settings.inc.none"),
                    Loc.Get("settings.inc.1"),
                    Loc.Get("settings.inc.2"),
                    Loc.Get("settings.inc.5"),
                    Loc.Get("settings.inc.10"),
                    Loc.Get("settings.inc.15"),
                    Loc.Get("settings.inc.30"),
                    Loc.Get("settings.inc.60")
                },
                _incrementPreset,
                v => _incrementPreset = v);
        }
        void BindHostColor()
        {
            BindDropdown(
                hostColorDropdown,
                new[]
                {
                    Loc.Get("settings.color.white"),
                    Loc.Get("settings.color.black"),
                    Loc.Get("settings.color.random")
                },
                (int)_hostColor,
                v => _hostColor = (HostColor)v);
        }
        void BindAi()
        {
            BindDropdown(
                aiDropdown,
                new[] { Loc.Get("settings.ai.easy"), Loc.Get("settings.ai.medium"), Loc.Get("settings.ai.hard") },
                (int)_aiStrength,
                v => _aiStrength = (AiStrength)v);
        }
        void RefreshExtraTimeVisible()
        {
            if (extraTimeDropdown == null)
                return;
            bool on = _timePreset != 0;
            if (!on)
            {
                _incrementPreset = 0;
                extraTimeDropdown.SetValueWithoutNotify(0);
            }
            extraTimeDropdown.gameObject.SetActive(on);
        }
        void RebuildModes()
        {
            if (modesScroll == null || modesScroll.content == null || _selectedModes == null)
                return;
            if (modeSettingsPopup == null)
                modeSettingsPopup = ModeSettingsPopup.Ensure(transform);
            Transform content = modesScroll.content;
            for (int i = 0; i < _spawnedRows.Count; i++)
            {
                if (_spawnedRows[i] != null)
                    Destroy(_spawnedRows[i]);
            }
            _spawnedRows.Clear();
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
            ModeDefinition[] modes = ModeCatalog.All;
            for (int i = 0; i < modes.Length; i++)
            {
                ModeDefinition def = modes[i];
                if (!def.Allows(_activity))
                    continue;
                bool owned = ModeDlc.IsOwned(def.Id);
                if (selectionRowPrefab == null)
                    continue;
                GameObject row = Instantiate(selectionRowPrefab, content);
                row.name = def.Id.ToString();
                row.SetActive(true);
                Toggle toggle = row.GetComponentInChildren<Toggle>(true);
                if (toggle == null)
                {
                    Destroy(row);
                    continue;
                }
                TMP_Text text = toggle.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    text.text = owned
                        ? Loc.ModeName(def.Id)
                        : Loc.Format("mode.unlocks.suffix", Loc.ModeName(def.Id));
                }
                ModeId captured = def.Id;
                toggle.interactable = owned;
                toggle.isOn = owned && _selectedModes.Contains(captured);
                toggle.onValueChanged.RemoveAllListeners();
                Transform settingsTransform = row.transform.Find("SettingsButton");
                Button settings = settingsTransform != null
                    ? settingsTransform.GetComponent<Button>()
                    : null;
                if (settings != null)
                    GameAudio.Bind(settings, () => OpenModeSettings(captured));
                void RefreshSettingsAccess()
                {
                    if (settings != null)
                        settings.interactable = owned && toggle.isOn;
                }
                if (owned)
                {
                    toggle.onValueChanged.AddListener(value =>
                    {
                        GameAudio.PlayUi();
                        if (value && !_selectedModes.Contains(captured))
                            _selectedModes.Add(captured);
                        if (!value)
                            _selectedModes.Remove(captured);
                        RefreshSettingsAccess();
                    });
                }
                RefreshSettingsAccess();
                var group = row.GetComponent<CanvasGroup>();
                if (group == null)
                    group = row.AddComponent<CanvasGroup>();
                group.alpha = owned ? 1f : 0.45f;
                group.interactable = owned;
                group.blocksRaycasts = true;
                _spawnedRows.Add(row);
            }
        }
        void OpenModeSettings(ModeId id)
        {
            if (!ModeDlc.IsOwned(id) || _selectedModes == null || !_selectedModes.Contains(id))
                return;
            if (modeSettingsPopup == null)
                modeSettingsPopup = ModeSettingsPopup.Ensure(transform);
            if (modeSettingsPopup == null || _modeSettings == null)
                return;
            RectTransform slideFrom = modesScroll != null ? modesScroll.transform as RectTransform : null;
            modeSettingsPopup.Open(id, _modeSettings, slideFrom);
        }
        static void BindDropdown(TMP_Dropdown dropdown, string[] options, int selected, Action<int> changed)
        {
            if (dropdown == null)
                return;
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            dropdown.SetValueWithoutNotify(selected);
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(v =>
            {
                GameAudio.PlayUi();
                changed?.Invoke(v);
            });
        }
        TMP_Dropdown DropdownNamed(string name)
        {
            Transform child = FindChild(transform, name);
            if (child == null)
                return null;
            TMP_Dropdown dropdown = child.GetComponent<TMP_Dropdown>();
            return dropdown != null ? dropdown : child.GetComponentInChildren<TMP_Dropdown>(true);
        }
        Button ButtonNamed(string name)
        {
            Transform child = FindChild(transform, name);
            return child != null ? child.GetComponent<Button>() : null;
        }
        ScrollRect FindModesScroll()
        {
            Transform named = FindChild(transform, "ModesScroll");
            if (named == null)
                named = FindChild(transform, "Scroll View");
            if (named != null)
            {
                ScrollRect namedScroll = named.GetComponent<ScrollRect>();
                if (namedScroll == null)
                    namedScroll = named.GetComponentInChildren<ScrollRect>(true);
                if (namedScroll != null)
                    return namedScroll;
            }
            ScrollRect[] rects = GetComponentsInChildren<ScrollRect>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i].GetComponentInParent<TMP_Dropdown>(true) == null)
                    return rects[i];
            }
            return null;
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
