using TMPro;
using UnityEngine;

namespace ModularChess.Presentation
{
    public sealed class LocalizedText : MonoBehaviour
    {
        #region Fields
        [SerializeField] string key;
        [SerializeField] TMP_Text label;
        public string Key
        {
            get => key;
            set
            {
                key = value;
                Apply();
            }
        }
        #endregion

        #region Unity
        void OnEnable()
        {
            Loc.Changed += Apply;
            Apply();
        }
        void OnDisable()
        {
            Loc.Changed -= Apply;
        }
        #endregion

        #region Public Methods
        public static LocalizedText Bind(Component host, string locKey)
        {
            if (host == null || string.IsNullOrEmpty(locKey))
            {
                return null;
            }

            TMP_Text tmp = host as TMP_Text;
            if (tmp == null)
            {
                tmp = host.GetComponentInChildren<TMP_Text>(true);
            }

            if (tmp == null)
            {
                return null;
            }

            LocalizedText loc = tmp.GetComponent<LocalizedText>();
            if (loc == null)
            {
                loc = tmp.gameObject.AddComponent<LocalizedText>();
            }

            loc.label = tmp;
            loc.Key = locKey;
            return loc;
        }
        #endregion

        #region Private Methods
        void Apply()
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (label == null)
            {
                label = GetComponent<TMP_Text>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>(true);
            }

            if (label != null)
            {
                label.text = Loc.Get(key);
            }
        }
        #endregion
    }
}
