using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class RoguelikeLobbyView : MonoBehaviour
    {
        #region Fields
        static readonly PieceType[] StartPieces =
        {
            PieceType.Rook,
            PieceType.Knight,
            PieceType.Bishop
        };
        static readonly HostColor[] Colors =
        {
            HostColor.White,
            HostColor.Black,
            HostColor.Random
        };
        static readonly RoguelikeDifficulty[] Difficulties =
        {
            RoguelikeDifficulty.Easy,
            RoguelikeDifficulty.Normal,
            RoguelikeDifficulty.Hard
        };
        static readonly Color WhiteSwatch = Color.white;
        static readonly Color BlackSwatch = Color.black;
        static readonly Color RandomSwatch = new Color(0.75f, 0.75f, 0.2f, 1f);
        [SerializeField] Button startButton;
        [SerializeField] Button backButton;
        [SerializeField] Button startPieceButton;
        [SerializeField] Button colorButton;
        [SerializeField] Button difficultyButton;
        [SerializeField] Image startPieceIcon;
        [SerializeField] Image colorIcon;
        [SerializeField] Image difficultyIcon;
        [SerializeField] TMP_Text statusLabel;
        RoguelikeRunSettings _settings = new RoguelikeRunSettings();
        int _pieceIndex;
        int _colorIndex = 2;
        int _difficultyIndex = 1;
        UnityAction _onStart;
        UnityAction _onBack;
        #endregion

        #region Public Methods
        public RoguelikeRunSettings Settings => _settings;
        public void Bind(UnityAction onStart, UnityAction onBack)
        {
            Resolve();
            _onStart = onStart;
            _onBack = onBack;
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                GameAudio.Bind(startButton, () => _onStart?.Invoke());
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                GameAudio.Bind(backButton, () => _onBack?.Invoke());
            }
            if (startPieceButton != null)
            {
                startPieceButton.onClick.RemoveAllListeners();
                GameAudio.Bind(startPieceButton, CycleStartPiece);
            }
            if (colorButton != null)
            {
                colorButton.onClick.RemoveAllListeners();
                GameAudio.Bind(colorButton, CycleColor);
            }
            if (difficultyButton != null)
            {
                difficultyButton.onClick.RemoveAllListeners();
                GameAudio.Bind(difficultyButton, CycleDifficulty);
            }
            Refresh();
        }
        public void Present()
        {
            Resolve();
            Refresh();
            OverlayMotion.Ensure(gameObject)?.PlayEnter();
        }
        public void Dismiss()
        {
            OverlayMotion.Ensure(gameObject)?.PlayExit();
        }
        public void Refresh()
        {
            Resolve();
            if (statusLabel != null)
                statusLabel.text = string.Empty;
            LocalizedText.Bind(startButton, "roguelike.lobby.start");
            LocalizedText.Bind(backButton, "menu.back");
            ApplyStartPiece();
            ApplyColor();
            ApplyDifficulty();
        }
        #endregion

        #region Private Methods
        void CycleStartPiece()
        {
            _pieceIndex = (_pieceIndex + 1) % StartPieces.Length;
            _settings.StartingPiece = StartPieces[_pieceIndex];
            ApplyStartPiece();
        }
        void CycleColor()
        {
            _colorIndex = (_colorIndex + 1) % Colors.Length;
            _settings.PlayerColor = Colors[_colorIndex];
            ApplyColor();
            ApplyStartPiece();
        }
        void CycleDifficulty()
        {
            _difficultyIndex = (_difficultyIndex + 1) % Difficulties.Length;
            _settings.Difficulty = Difficulties[_difficultyIndex];
            ApplyDifficulty();
        }
        void ApplyStartPiece()
        {
            SetButtonText(startPieceButton, Loc.PieceName(_settings.StartingPiece));
            if (startPieceIcon == null)
                return;
            Side side = PreviewSide(_settings.PlayerColor);
            Sprite sprite = ChessGlyphs.GetSprite(_settings.StartingPiece, side);
            startPieceIcon.sprite = sprite;
            startPieceIcon.SetNativeSize();
        }
        void ApplyColor()
        {
            SetButtonText(colorButton, ColorLabel(_settings.PlayerColor));
            if (colorIcon == null)
                return;
            colorIcon.color = ColorSwatch(_settings.PlayerColor);
        }
        void ApplyDifficulty()
        {
            SetButtonText(difficultyButton, DifficultyLabel(_settings.Difficulty));
        }
        static Side PreviewSide(HostColor color)
        {
            return color == HostColor.Black ? Side.Black : Side.White;
        }
        static Color ColorSwatch(HostColor color)
        {
            switch (color)
            {
                case HostColor.White:
                    return WhiteSwatch;
                case HostColor.Black:
                    return BlackSwatch;
                default:
                    return RandomSwatch;
            }
        }
        static string ColorLabel(HostColor color)
        {
            switch (color)
            {
                case HostColor.White:
                    return Loc.Get("settings.color.white");
                case HostColor.Black:
                    return Loc.Get("settings.color.black");
                default:
                    return Loc.Get("settings.color.random");
            }
        }
        static string DifficultyLabel(RoguelikeDifficulty difficulty)
        {
            switch (difficulty)
            {
                case RoguelikeDifficulty.Easy:
                    return Loc.Get("settings.ai.easy");
                case RoguelikeDifficulty.Hard:
                    return Loc.Get("settings.ai.hard");
                default:
                    return Loc.Get("settings.ai.medium");
            }
        }
        static void SetButtonText(Button button, string text)
        {
            if (button == null)
                return;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = text;
        }
        void Resolve()
        {
            if (startButton == null)
                startButton = FindButton("StartButton");
            if (backButton == null)
                backButton = FindButton("BackButton");
            if (startPieceButton == null)
                startPieceButton = FindButton("StartPieceButton");
            if (colorButton == null)
                colorButton = FindButton("ColorButton");
            if (difficultyButton == null)
                difficultyButton = FindButton("DifficultyButton");
            if (startPieceIcon == null)
                startPieceIcon = FindImage("StartPieceIcon");
            if (colorIcon == null)
                colorIcon = FindImage("ColorIcon");
            if (difficultyIcon == null)
                difficultyIcon = FindImage("DifficultyIcon");
            if (statusLabel == null)
                statusLabel = FindTmp("StatusLabel");
            if (_settings == null)
                _settings = new RoguelikeRunSettings();
            SyncIndicesFromSettings();
        }
        void SyncIndicesFromSettings()
        {
            _pieceIndex = 0;
            for (int i = 0; i < StartPieces.Length; i++)
            {
                if (StartPieces[i] == _settings.StartingPiece)
                {
                    _pieceIndex = i;
                    break;
                }
            }
            _colorIndex = 2;
            for (int i = 0; i < Colors.Length; i++)
            {
                if (Colors[i] == _settings.PlayerColor)
                {
                    _colorIndex = i;
                    break;
                }
            }
            _difficultyIndex = 1;
            for (int i = 0; i < Difficulties.Length; i++)
            {
                if (Difficulties[i] == _settings.Difficulty)
                {
                    _difficultyIndex = i;
                    break;
                }
            }
        }
        Button FindButton(string name)
        {
            Transform t = FindChild(transform, name);
            return t != null ? t.GetComponent<Button>() : null;
        }
        Image FindImage(string name)
        {
            Transform t = FindChild(transform, name);
            return t != null ? t.GetComponent<Image>() : null;
        }
        TMP_Text FindTmp(string name)
        {
            Transform t = FindChild(transform, name);
            return t != null ? t.GetComponent<TMP_Text>() : null;
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
