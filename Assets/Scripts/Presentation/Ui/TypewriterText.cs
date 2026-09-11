using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ModularChess.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TypewriterText : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] TMP_Text target;
        [SerializeField] [Min(0f)] float charactersPerSecond = 20f;
        [SerializeField] bool playOnEnable = true;
        [SerializeField] bool unscaledTime;
        [SerializeField] bool completeOnClick = true;

        float _elapsed;
        int _total;
        bool _playing;

        public bool IsPlaying => _playing;

        void Awake()
        {
            if (target == null)
                target = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            if (playOnEnable)
                Play();
        }

        void OnDisable()
        {
            _playing = false;
        }

        void Update()
        {
            if (!_playing || target == null)
                return;

            if (charactersPerSecond <= 0f)
            {
                Complete();
                return;
            }

            _elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            int visible = Mathf.Min(_total, Mathf.FloorToInt(_elapsed * charactersPerSecond));
            target.maxVisibleCharacters = visible;
            if (visible >= _total)
                _playing = false;
        }

        public void Play()
        {
            if (target == null)
                target = GetComponent<TMP_Text>();
            if (target == null)
                return;

            target.ForceMeshUpdate();
            _total = target.textInfo != null ? target.textInfo.characterCount : 0;
            _elapsed = 0f;
            target.maxVisibleCharacters = 0;
            _playing = _total > 0;
            if (!_playing)
                target.maxVisibleCharacters = int.MaxValue;
        }

        public void Play(string text)
        {
            if (target == null)
                target = GetComponent<TMP_Text>();
            if (target == null)
                return;

            target.text = text;
            Play();
        }

        public void Complete()
        {
            _playing = false;
            if (target != null)
                target.maxVisibleCharacters = int.MaxValue;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (completeOnClick && _playing)
                Complete();
        }
    }
}
