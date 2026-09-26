using System;
using System.Text;
using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class MatchSettingsInfoView : MonoBehaviour
    {
        #region Fields
        [SerializeField] Button toggleButton;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;
        TypewriterText _typewriter;
        MatchSession _session;
        bool _wired;
        bool _open;
        #endregion

        #region Unity
        void Awake()
        {
            Wire();
            HideImmediate();
        }
        void OnEnable()
        {
            Wire();
            Loc.Changed -= Refresh;
            Loc.Changed += Refresh;
        }
        void OnDisable()
        {
            Loc.Changed -= Refresh;
        }
        void OnDestroy()
        {
            Loc.Changed -= Refresh;
        }
        #endregion

        #region Public Methods
        public void Present(MatchSession session)
        {
            Wire();
            _session = session;
            Refresh();
        }
        public void Toggle()
        {
            if (_open)
                HideImmediate();
            else
                Show();
        }
        public void HideImmediate()
        {
            Wire();
            _open = false;
            if (panel != null)
                panel.SetActive(false);
        }
        #endregion

        #region Private Methods
        void Wire()
        {
            if (_wired)
                return;
            if (toggleButton == null)
            {
                Transform child = FindChild(transform, "OpenInfoPanelButton");
                if (child != null)
                    toggleButton = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>(true);
            }
            if (panel == null)
            {
                Transform child = FindChild(transform, "DescriptionBox");
                if (child != null)
                    panel = child.gameObject;
            }
            if (title == null)
            {
                Transform child = FindChild(transform, "Title");
                if (child != null)
                    title = child.GetComponent<TMP_Text>();
            }
            if (body == null)
            {
                Transform child = FindChild(transform, "Settings");
                if (child != null)
                    body = child.GetComponent<TMP_Text>();
            }
            if (body != null)
                _typewriter = body.GetComponent<TypewriterText>();
            if (toggleButton != null)
                GameAudio.Bind(toggleButton, Toggle);
            _wired = true;
        }
        void Show()
        {
            Wire();
            Refresh();
            _open = true;
            if (panel != null)
            {
                panel.SetActive(true);
                panel.transform.SetAsLastSibling();
            }
            if (toggleButton != null)
                toggleButton.transform.SetAsLastSibling();
            _typewriter?.Play();
        }
        void Refresh()
        {
            Wire();
            if (title != null)
                title.text = Loc.Get("hud.matchSettings");
            if (body != null)
                body.text = FormatBody(_session);
            if (_open)
                _typewriter?.Play();
        }
        static string FormatBody(MatchSession session)
        {
            MatchRules rules = session?.Rules;
            MatchSettings settings = rules != null ? rules.Settings : MatchSettings.Default;
            var builder = new StringBuilder();
            builder.AppendLine(FormatTime(settings.Time));
            builder.AppendLine(FormatActivity(session, settings));
            builder.AppendLine();
            builder.AppendLine(Loc.Get("hud.matchSettings.modes"));
            if (rules == null || rules.Modes.Count == 0)
            {
                builder.Append(Loc.Get("hud.matchSettings.empty"));
                return builder.ToString();
            }
            for (int i = 0; i < rules.Modes.Count; i++)
            {
                if (i > 0)
                    builder.AppendLine();
                AppendMode(builder, rules.Modes[i], settings);
            }
            return builder.ToString();
        }
        static string FormatTime(TimeControl time)
        {
            if (time.IsNone)
                return Loc.Get("settings.time.none");
            if (time.Equals(TimeControl.Bullet)) return Loc.Get("settings.time.bullet");
            if (time.Equals(TimeControl.Blitz)) return Loc.Get("settings.time.blitz");
            if (time.Equals(TimeControl.Rapid)) return Loc.Get("settings.time.rapid");
            if (time.Equals(TimeControl.Standard)) return Loc.Get("settings.time.standard");
            if (time.Equals(TimeControl.Extended)) return Loc.Get("settings.time.extended");
            return Loc.Format("hud.matchSettings.clock", time.BaseMinutes, time.IncrementSeconds);
        }
        static string FormatActivity(MatchSession session, MatchSettings settings)
        {
            Activity activity = session != null ? session.Activity : Activity.VersusAi;
            switch (activity)
            {
                case Activity.VersusAi:
                    return Loc.Get("play.versusAi") + " - " + AiName(settings.AiStrength);
                case Activity.VersusFriend:
                    return Loc.Get("play.versusFriend");
                case Activity.Campaign:
                    return Loc.Get("play.campaign");
                default:
                    throw new ArgumentOutOfRangeException(nameof(activity), activity, null);
            }
        }
        static string AiName(AiStrength strength)
        {
            switch (strength)
            {
                case AiStrength.Easy: return Loc.Get("settings.ai.easy");
                case AiStrength.Medium: return Loc.Get("settings.ai.medium");
                case AiStrength.Hard: return Loc.Get("settings.ai.hard");
                default: throw new ArgumentOutOfRangeException(nameof(strength), strength, null);
            }
        }
        static void AppendMode(StringBuilder builder, ModeId id, MatchSettings settings)
        {
            builder.Append(Loc.ModeName(id));
            switch (id)
            {
                case ModeId.FogOfWar:
                    return;
                case ModeId.PowerfulPieces:
                    AppendSetting(builder, "mode.setting.empowered", settings.EmpowerBudget.ToString());
                    return;
                case ModeId.Martyr:
                    AppendSetting(builder, "mode.setting.lost", settings.MartyrThreshold.ToString());
                    AppendSetting(builder, "mode.setting.draft", settings.MartyrDraftOptions.ToString());
                    return;
                case ModeId.ActionEconomy:
                    AppendSetting(builder, "mode.setting.actions", settings.ActionPoints.ToString());
                    return;
                case ModeId.ComplexTerrain:
                    AppendSetting(builder, "mode.setting.layout", Loc.TerrainLayoutName((int)settings.TerrainLayout));
                    AppendSetting(builder, "mode.setting.terrainSpawn", Flag(settings.TerrainOnPieces));
                    return;
                case ModeId.Randomizer:
                    AppendSetting(builder, "mode.setting.shuffle", Flag(settings.RandomShuffle));
                    AppendSetting(builder, "mode.setting.colors", Flag(settings.RandomColors));
                    AppendSetting(builder, "mode.setting.placement", Flag(settings.RandomPlacement));
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }
        static void AppendSetting(StringBuilder builder, string labelKey, string value)
        {
            builder.AppendLine();
            builder.Append("- ");
            builder.Append(Loc.Format("hud.matchSettings.line", Loc.Get(labelKey), value));
        }
        static string Flag(bool on) => Loc.Get(on ? "mode.setting.on" : "mode.setting.off");
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
