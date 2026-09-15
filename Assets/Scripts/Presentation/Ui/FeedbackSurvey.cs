using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class FeedbackSurvey : MonoBehaviour
    {
        #region Fields
        const string FormUrl =
            "https://docs.google.com/forms/d/e/1FAIpQLSewAdINyXc1rxQq98Jj9q0Jhz41HEAYPu2WkG9AxWhhxG1W7g/formResponse";
        const string GameNameEntry = "entry.1591633300";
        const string UsernameEntry = "entry.1055565306";
        const string ExperienceEntry = "entry.701084324";
        const string FeedbackEntry = "entry.326955045";
        const string SuggestionsEntry = "entry.1696159737";
        const string DefaultGameName = "Modular Chess";
        [SerializeField] TMP_InputField feedbackField;
        [SerializeField] TMP_InputField suggestionsField;
        [SerializeField] Button[] experienceStars;
        [SerializeField] Button submitButton;
        [SerializeField] Button backButton;
        [SerializeField] GameObject formRoot;
        [SerializeField] GameObject thanksRoot;
        [SerializeField] TMP_Text statusLabel;
        int _experience;
        bool _sending;
        bool _wired;
        public event Action Closed;
        #endregion

        #region Unity
        void Awake()
        {
            Wire();
        }
        #endregion

        #region Public Methods
        public void Open()
        {
            Wire();
            gameObject.SetActive(true);
            ShowForm();
            Prefill();
            RefreshSubmit();
        }
        #endregion

        #region Private Methods
        void Wire()
        {
            if (_wired)
                return;
            Resolve();
            BindStars(experienceStars, value => _experience = value);
            if (feedbackField != null)
                feedbackField.onValueChanged.AddListener(_ => RefreshSubmit());
            GameAudio.Bind(submitButton, Submit);
            GameAudio.Bind(backButton, Close);
            LocalizedText.Bind(FindNamed("Title"), "survey.title");
            LocalizedText.Bind(FindNamed("ExperienceLabel"), "survey.experience");
            LocalizedText.Bind(FindNamed("FeedbackLabel"), "survey.feedback");
            LocalizedText.Bind(FindNamed("SuggestionsLabel"), "survey.suggestions");
            LocalizedText.Bind(submitButton, "survey.submit");
            LocalizedText.Bind(backButton, "survey.back");
            LocalizedText.Bind(FindNamed("Thanks"), "survey.thanks");
            _wired = true;
        }

        void Resolve()
        {
            if (formRoot == null)
            {
                Transform form = FindNamed("Form");
                if (form != null)
                    formRoot = form.gameObject;
            }

            if (thanksRoot == null)
            {
                Transform thanks = FindNamed("Thanks");
                if (thanks != null)
                    thanksRoot = thanks.gameObject;
            }

            if (feedbackField == null)
                feedbackField = Field("FeedbackInput");
            if (suggestionsField == null)
                suggestionsField = Field("SuggestionsInput");
            if (submitButton == null)
                submitButton = ButtonNamed("SubmitButton");
            if (backButton == null)
                backButton = ButtonNamed("BackButton");
            if (statusLabel == null)
            {
                Transform status = FindNamed("Status");
                if (status != null)
                    statusLabel = status.GetComponentInChildren<TMP_Text>(true);
            }

            if (experienceStars == null || experienceStars.Length == 0)
                experienceStars = Stars("Experience");
        }

        void BindStars(Button[] stars, Action<int> set)
        {
            if (stars == null)
                return;
            for (int i = 0; i < stars.Length; i++)
            {
                int value = i + 1;
                GameAudio.Bind(stars[i], () =>
                {
                    set(value);
                    PaintStars(stars, value);
                    RefreshSubmit();
                });
            }
        }

        void PaintStars(Button[] stars, int value)
        {
            if (stars == null)
                return;
            for (int i = 0; i < stars.Length; i++)
            {
                TMP_Text label = stars[i] != null ? stars[i].GetComponentInChildren<TMP_Text>(true) : null;
                if (label == null)
                    continue;
                bool on = i < value;
                label.color = on ? new Color(0.85f, 0.55f, 0.08f, 1f) : Color.black;
            }
        }

        void Prefill()
        {
            _experience = 0;
            PaintStars(experienceStars, 0);
            if (feedbackField != null)
                feedbackField.text = string.Empty;
            if (suggestionsField != null)
                suggestionsField.text = string.Empty;
            SetStatus(string.Empty);
        }

        void RefreshSubmit()
        {
            if (submitButton != null)
                submitButton.interactable = !_sending && RequiredFilled();
        }

        bool RequiredFilled()
        {
            return HasText(feedbackField);
        }

        static bool HasText(TMP_InputField field)
        {
            return field != null && !string.IsNullOrWhiteSpace(field.text);
        }

        void Submit()
        {
            if (_sending || !RequiredFilled())
            {
                SetStatus(Loc.Get("survey.status.required"));
                return;
            }

            StartCoroutine(Send());
        }

        IEnumerator Send()
        {
            _sending = true;
            RefreshSubmit();
            SetStatus(Loc.Get("survey.status.sending"));
            var form = new WWWForm();
            form.AddField(GameNameEntry, DefaultGameName);
            form.AddField(UsernameEntry, PlayerIdentity.DisplayName);
            if (_experience > 0)
                form.AddField(ExperienceEntry, _experience.ToString());
            form.AddField(FeedbackEntry, feedbackField.text.Trim());
            if (HasText(suggestionsField))
                form.AddField(SuggestionsEntry, suggestionsField.text.Trim());
            using (UnityWebRequest request = UnityWebRequest.Post(FormUrl, form))
            {
                yield return request.SendWebRequest();
                _sending = false;
                if (request.result == UnityWebRequest.Result.Success || request.responseCode == 200)
                {
                    ShowThanks();
                    StartCoroutine(CloseAfterThanks());
                    yield break;
                }
            }

            RefreshSubmit();
            SetStatus(Loc.Get("survey.status.failed"));
        }

        void ShowForm()
        {
            if (formRoot != null)
                formRoot.SetActive(true);
            if (thanksRoot != null)
                thanksRoot.SetActive(false);
        }

        void ShowThanks()
        {
            if (formRoot != null)
                formRoot.SetActive(false);
            if (thanksRoot != null)
                thanksRoot.SetActive(true);
            SetStatus(string.Empty);
        }

        void Close()
        {
            Closed?.Invoke();
        }

        IEnumerator CloseAfterThanks()
        {
            yield return new WaitForSecondsRealtime(1.6f);
            Close();
        }

        void SetStatus(string text)
        {
            if (statusLabel != null)
                statusLabel.text = text ?? string.Empty;
        }

        TMP_InputField Field(string name)
        {
            Transform child = FindNamed(name);
            return child != null ? child.GetComponentInChildren<TMP_InputField>(true) : null;
        }

        Button ButtonNamed(string name)
        {
            Transform child = FindNamed(name);
            return child != null ? child.GetComponent<Button>() : null;
        }

        Button[] Stars(string prefix)
        {
            var list = new List<Button>(5);
            for (int i = 1; i <= 5; i++)
            {
                Button button = ButtonNamed(prefix + i);
                if (button != null)
                    list.Add(button);
            }

            return list.ToArray();
        }

        Transform FindNamed(string name)
        {
            return FindChild(transform, name);
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
