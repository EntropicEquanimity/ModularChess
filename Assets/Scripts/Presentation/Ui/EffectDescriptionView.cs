using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class EffectDescriptionView : MonoBehaviour
    {
        #region Fields
        const float HeightPad = 4f;
        TMP_Text _name;
        TMP_Text _body;
        LayoutElement _bodyLayout;
        LayoutElement _rootLayout;
        #endregion

        #region Public Methods
        public void Bind(string effectName, string description)
        {
            EnsureLabels();
            if (_name != null)
                _name.text = effectName ?? string.Empty;
            if (_body != null)
                _body.text = description ?? string.Empty;
            RefreshLayout();
        }
        public void RefreshLayout()
        {
            EnsureLabels();
            if (_body == null)
                return;

            var root = (RectTransform)transform;
            VerticalLayoutGroup group = GetComponent<VerticalLayoutGroup>();
            float horizontal = group != null ? group.padding.left + group.padding.right : 0f;
            float width = root.rect.width - horizontal;
            if (width < 8f)
                width = Mathf.Max(8f, root.sizeDelta.x - horizontal);

            float height = MeasureBodyHeight(width);
            if (_bodyLayout != null)
            {
                _bodyLayout.minHeight = height;
                _bodyLayout.preferredHeight = height;
            }

            if (_rootLayout != null)
            {
                float nameHeight = 32f;
                if (_name != null)
                    nameHeight = Mathf.Max(32f, _name.preferredHeight);
                float spacing = group != null ? group.spacing : 0f;
                float vertical = group != null ? group.padding.top + group.padding.bottom : 0f;
                float rootHeight = nameHeight + height + spacing + vertical;
                _rootLayout.minHeight = rootHeight;
                _rootLayout.preferredHeight = rootHeight;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }
        #endregion

        #region Private Methods
        float MeasureBodyHeight(float width)
        {
            RectTransform rect = _body.rectTransform;
            Vector2 oldSize = rect.sizeDelta;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 2048f);
            _body.ForceMeshUpdate();

            float height = _body.renderedHeight;
            if (height < 1f)
                height = _body.GetPreferredValues(_body.text, width, float.PositiveInfinity).y;
            if (_body.textInfo != null && _body.textInfo.lineCount > 0)
            {
                TMP_LineInfo last = _body.textInfo.lineInfo[_body.textInfo.lineCount - 1];
                TMP_LineInfo first = _body.textInfo.lineInfo[0];
                float meshHeight = first.ascender - last.descender;
                if (meshHeight > height)
                    height = meshHeight;
            }

            rect.sizeDelta = oldSize;
            return Mathf.Max(_body.fontSize, height) + HeightPad;
        }
        void EnsureLabels()
        {
            if (_body != null && _name != null)
                return;

            Transform nameTf = transform.Find("Name");
            if (nameTf == null)
                nameTf = transform.Find("EffectName");
            Transform bodyTf = transform.Find("Description");
            if (bodyTf == null)
                bodyTf = transform.Find("EffectDescription");

            if (nameTf != null)
                _name = nameTf.GetComponent<TMP_Text>();
            if (bodyTf != null)
            {
                _body = bodyTf.GetComponent<TMP_Text>();
                _bodyLayout = bodyTf.GetComponent<LayoutElement>();
                if (_bodyLayout == null)
                    _bodyLayout = bodyTf.gameObject.AddComponent<LayoutElement>();
                ContentSizeFitter fitter = bodyTf.GetComponent<ContentSizeFitter>();
                if (fitter != null)
                    fitter.enabled = false;
                if (_body != null)
                    _body.verticalAlignment = VerticalAlignmentOptions.Top;
            }

            _rootLayout = GetComponent<LayoutElement>();
        }
        #endregion
    }
}
