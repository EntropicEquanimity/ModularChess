using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    [DefaultExecutionOrder(-60)]
    public sealed class GameAudio : MonoBehaviour
    {
        #region Fields
        const string MusicParam = "MusicVol";
        const string SfxParam = "SfxVol";
        const string MixerPath = "Assets/Audio/GameAudio.mixer";
        const string ClickPath = "Assets/Audio/button-click.wav";
        const string TapPath = "Assets/Audio/tap.wav";
        const string MovePath = "Assets/Audio/piece-move.wav";
        const string CapturePath = "Assets/Audio/piece-capture.wav";
        const string SelectPath = "Assets/Audio/piece-select.wav";
        const string IllegalPath = "Assets/Audio/invalid-click.wav";
        const string CheckPath = "Assets/Audio/match-check.wav";
        const string MenuMusicPath = "Assets/Audio/bgm-menu.wav";
        const string MatchMusicPath = "Assets/Audio/bgm-match.wav";
        [SerializeField] AudioMixer mixer;
        [SerializeField] AudioClip uiClick;
        [SerializeField] AudioClip uiTap;
        [SerializeField] AudioClip pieceMove;
        [SerializeField] AudioClip pieceCapture;
        [SerializeField] AudioClip pieceSelect;
        [SerializeField] AudioClip invalidClick;
        [SerializeField] AudioClip matchCheck;
        [SerializeField] AudioClip menuMusic;
        [SerializeField] AudioClip matchMusic;
        AudioSource _sfx;
        AudioSource _music;
        Bgm _bgm;
        static GameAudio _instance;
        public enum Bgm
        {
            None,
            Menu,
            Match
        }
        #endregion

        #region Unity
        void Awake()
        {
            Wake();
        }
        #endregion

        #region Public Methods
        public static GameAudio Ensure()
        {
            if (_instance != null)
            {
                return _instance;
            }

            GameAudio existing = FindAnyObjectByType<GameAudio>();
            if (existing != null)
            {
                existing.Wake();
                return existing;
            }

            var go = new GameObject("GameAudio");
            return go.AddComponent<GameAudio>();
        }

        public static void PlayUi()
        {
            Ensure().PlayUiClip();
        }

        public static void PlaySelect()
        {
            GameAudio audio = Ensure();
            audio.PlayOne(audio.pieceSelect);
        }

        public static void PlayTap()
        {
            GameAudio audio = Ensure();
            audio.PlayOne(audio.uiTap);
        }

        public static void PlayMove()
        {
            GameAudio audio = Ensure();
            audio.PlayOne(audio.pieceMove);
        }

        public static void PlayCapture()
        {
            GameAudio audio = Ensure();
            audio.PlayOne(audio.pieceCapture);
        }

        public static void PlayHidden()
        {
            GameAudio audio = Ensure();
            audio.PlayOne(audio.pieceMove);
        }

        public static void PlayIllegal()
        {
            GameAudio audio = Ensure();
            audio.PlayOne(audio.invalidClick);
        }

        public static void PlayCheck()
        {
            GameAudio audio = Ensure();
            audio.PlayOne(audio.matchCheck);
        }

        public static void PlayMenuMusic()
        {
            Ensure().SetBgm(Bgm.Menu);
        }

        public static void PlayMatchMusic()
        {
            Ensure().SetBgm(Bgm.Match);
        }

        public static void StopMusic()
        {
            Ensure().SetBgm(Bgm.None);
        }

        public static void ApplyVolumes()
        {
            if (_instance != null)
            {
                _instance.PushVolumes();
            }
        }

        public static void Bind(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(PlayUi);
            if (action != null)
            {
                button.onClick.AddListener(action);
            }
        }
        #endregion

        #region Private Methods
        void Wake()
        {
            _instance = this;
            LoadAssets();
            EnsureSources();
            PushVolumes();
        }

        void PlayUiClip()
        {
            PlayOne(uiClick);
        }

        void PlayOne(AudioClip clip)
        {
            if (clip == null || _sfx == null)
            {
                return;
            }

            _sfx.PlayOneShot(clip);
        }

        void SetBgm(Bgm next)
        {
            _bgm = next;
            AudioClip clip = next == Bgm.Menu ? menuMusic : next == Bgm.Match ? matchMusic : null;
            if (_music == null)
            {
                return;
            }

            if (clip == null)
            {
                _music.Stop();
                _music.clip = null;
                return;
            }

            if (_music.clip == clip && _music.isPlaying)
            {
                return;
            }

            _music.clip = clip;
            _music.loop = true;
            _music.Play();
        }

        void PushVolumes()
        {
            bool mixerSfx = mixer != null && mixer.SetFloat(SfxParam, AudioPrefs.SfxDb);
            bool mixerMusic = mixer != null && mixer.SetFloat(MusicParam, AudioPrefs.MusicDb);
            if (_sfx != null)
            {
                _sfx.volume = mixerSfx ? 1f : AudioPrefs.SfxLinear;
            }

            if (_music != null)
            {
                _music.volume = mixerMusic ? 1f : AudioPrefs.MusicLinear;
            }
        }

        void EnsureSources()
        {
            if (_sfx == null)
            {
                _sfx = CreateSource("Sfx");
            }

            if (_music == null)
            {
                _music = CreateSource("Music");
                _music.loop = true;
                _music.playOnAwake = false;
            }

            Route(_sfx, "SFX");
            Route(_music, "Music");
        }

        AudioSource CreateSource(string name)
        {
            Transform child = transform.Find(name);
            AudioSource source = child != null ? child.GetComponent<AudioSource>() : null;
            if (source == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform, false);
                source = go.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = false;
            return source;
        }

        void Route(AudioSource source, string groupName)
        {
            if (source == null || mixer == null)
            {
                return;
            }

            AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
            if (groups != null && groups.Length > 0)
            {
                source.outputAudioMixerGroup = groups[0];
            }
        }

        void LoadAssets()
        {
            if (mixer == null)
            {
                mixer = LoadMixer();
            }

            if (uiClick == null)
            {
                uiClick = LoadClip(ClickPath, "Audio/button-click");
            }

            if (uiTap == null)
            {
                uiTap = LoadClip(TapPath, "Audio/tap");
            }

            if (pieceMove == null)
            {
                pieceMove = LoadClip(MovePath, "Audio/piece-move");
            }

            if (pieceCapture == null)
            {
                pieceCapture = LoadClip(CapturePath, "Audio/piece-capture");
            }

            if (pieceSelect == null)
            {
                pieceSelect = LoadClip(SelectPath, "Audio/piece-select");
            }

            if (invalidClick == null)
            {
                invalidClick = LoadClip(IllegalPath, "Audio/invalid-click");
            }

            if (matchCheck == null)
            {
                matchCheck = LoadClip(CheckPath, "Audio/match-check");
            }

            if (menuMusic == null)
            {
                menuMusic = LoadClip(MenuMusicPath, "Audio/bgm-menu");
            }

            if (matchMusic == null)
            {
                matchMusic = LoadClip(MatchMusicPath, "Audio/bgm-match");
            }
        }

        static AudioMixer LoadMixer()
        {
#if UNITY_EDITOR
            AudioMixer editor = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (editor != null)
            {
                return editor;
            }
#endif
            return Resources.Load<AudioMixer>("Audio/GameAudio");
        }

        static AudioClip LoadClip(string assetPath, string resourcesName)
        {
#if UNITY_EDITOR
            AudioClip editor = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (editor != null)
            {
                return editor;
            }
#endif
            return Resources.Load<AudioClip>(resourcesName);
        }
        #endregion
    }
}
