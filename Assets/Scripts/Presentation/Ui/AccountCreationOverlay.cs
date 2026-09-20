using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class AccountCreationOverlay : MonoBehaviour
    {
        #region Fields
        [SerializeField] TMP_InputField nameInput;
        [SerializeField] TMP_Dropdown languageDropdown;
        [SerializeField] Button confirmButton;
        [SerializeField] Transform title;
        [SerializeField] Transform languageLabel;
        #endregion

        #region Public Methods
        public void Bind(UnityAction onConfirm)
        {
            Resolve();
            if (nameInput != null)
            {
                nameInput.characterLimit = PlayerIdentity.StemMax;
                nameInput.contentType = TMP_InputField.ContentType.Alphanumeric;
                nameInput.onSubmit.RemoveAllListeners();
                nameInput.onSubmit.AddListener(_ =>
                {
                    GameAudio.PlayUi();
                    onConfirm?.Invoke();
                });
                nameInput.onValueChanged.RemoveAllListeners();
                nameInput.onValueChanged.AddListener(RefreshConfirm);
            }
            GameAudio.Bind(confirmButton, onConfirm);
            BindLanguage();
            RefreshLoc();
            RefreshConfirm(nameInput != null ? nameInput.text : string.Empty);
        }
        public string Stem
        {
            get
            {
                Resolve();
                return nameInput != null ? nameInput.text : string.Empty;
            }
        }
        public void RefreshLoc()
        {
            Resolve();
            LocalizedText.Bind(title, "account.title");
            LocalizedText.Bind(languageLabel, "options.language");
            LocalizedText.Bind(confirmButton, "account.confirm");
            BindLanguage();
        }
        #endregion

        #region Private Methods
        void Resolve()
        {
            if (nameInput == null)
            {
                Transform named = FindChild(transform, "NameInput");
                if (named != null)
                    nameInput = named.GetComponentInChildren<TMP_InputField>(true);
            }
            if (languageDropdown == null)
            {
                Transform named = FindChild(transform, "LanguageDropdown");
                if (named != null)
                {
                    languageDropdown = named.GetComponent<TMP_Dropdown>();
                    if (languageDropdown == null)
                        languageDropdown = named.GetComponentInChildren<TMP_Dropdown>(true);
                }
            }
            if (confirmButton == null)
            {
                Transform named = FindChild(transform, "ConfirmButton");
                if (named != null)
                    confirmButton = named.GetComponent<Button>();
            }
            if (title == null)
                title = FindChild(transform, "Title");
            if (languageLabel == null)
                languageLabel = FindChild(transform, "LanguageText");
        }
        void BindLanguage()
        {
            if (languageDropdown == null)
                return;
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string>(Loc.LanguageLabels()));
            languageDropdown.SetValueWithoutNotify(Loc.LanguageIndex());
            languageDropdown.onValueChanged.RemoveAllListeners();
            languageDropdown.onValueChanged.AddListener(index =>
            {
                GameAudio.PlayUi();
                if (index >= 0 && index < Loc.Codes.Length)
                    Loc.SetLanguage(Loc.Codes[index]);
            });
        }
        void RefreshConfirm(string stem)
        {
            if (confirmButton != null)
                confirmButton.interactable = PlayerIdentity.Sanitize(stem).Length > 0;
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
