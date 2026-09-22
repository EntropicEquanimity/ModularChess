using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    [DefaultExecutionOrder(-20)]
    public sealed class PopupDirector : MonoBehaviour
    {
        #region Fields
        static PopupDirector _instance;
        static readonly List<RaycastResult> Hits = new List<RaycastResult>(16);
        [SerializeField] DetailsPopup popup;
        [SerializeField] RectTransform questionMark;
        [SerializeField] Image questionFill;
        PopupTrigger _hover;
        PopupTrigger _shown;
        float _hoverTime;
        bool _popupOpen;
        #endregion

        #region Unity
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            Wire();
            HideQuestion();
            popup?.Hide();
        }
        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
        void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                ClearHover();
                return;
            }
            Vector2 screen = pointer.position.ReadValue();
            PopupTrigger under = FindTrigger(screen);
            if (pointer.press.wasPressedThisFrame)
                HandlePress(under);
            if (under != _hover)
                BeginHover(under);
            if (_hover != null && _hover.AllowsHover && !_popupOpen)
            {
                ShowQuestion(screen);
                float need = Mathf.Max(0.01f, _hover.HoverDurationToTrigger);
                _hoverTime += Time.unscaledDeltaTime;
                if (questionFill != null)
                    questionFill.fillAmount = Mathf.Clamp01(_hoverTime / need);
                if (_hoverTime >= need)
                    ShowPopup(_hover);
            }
            else if (_popupOpen)
                HideQuestion();
            else
                HideQuestion();
        }
        #endregion

        #region Public Methods
        public static PopupDirector Ensure()
        {
            if (_instance != null)
                return _instance;
            PopupDirector existing = FindAnyObjectByType<PopupDirector>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                existing.Wire();
                _instance = existing;
                return existing;
            }
            GameObject prefab = RuntimePrefabs.PopupLayer;
            GameObject root;
            if (prefab != null)
            {
                root = Instantiate(prefab);
                root.name = "PopupLayer";
            }
            else
            {
                root = new GameObject("PopupLayer", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 400;
                canvas.pixelPerfect = true;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(960f, 540f);
                scaler.referencePixelsPerUnit = 16f;
            }
            PopupDirector director = root.GetComponent<PopupDirector>();
            if (director == null)
                director = root.AddComponent<PopupDirector>();
            director.Wire();
            _instance = director;
            return director;
        }
        #endregion

        #region Private Methods
        void HandlePress(PopupTrigger under)
        {
            if (under != null && under.AllowsClick)
            {
                ShowPopup(under);
                return;
            }
            if (_popupOpen && _shown != null && _shown.DisableMode == PopupDisableMode.OnUnselect && under != _shown)
                HidePopup();
        }
        void BeginHover(PopupTrigger next)
        {
            PopupTrigger previous = _hover;
            _hover = next;
            _hoverTime = 0f;
            if (questionFill != null)
                questionFill.fillAmount = 0f;
            if (_popupOpen && previous != null && previous.DisableMode == PopupDisableMode.MouseExit && next != previous)
                HidePopup();
            if (next == null)
                HideQuestion();
        }
        void ClearHover()
        {
            BeginHover(null);
        }
        void ShowPopup(PopupTrigger trigger)
        {
            if (trigger == null)
                return;
            Wire();
            if (popup == null)
                return;
            HideQuestion();
            popup.ConfigureFade(trigger.PopupFadeIn, trigger.PopupFadeAway);
            trigger.Fill(popup);
            popup.AnchorTo(trigger.transform);
            _shown = trigger;
            _popupOpen = true;
            _hoverTime = trigger.HoverDurationToTrigger;
        }
        void HidePopup()
        {
            _popupOpen = false;
            _shown = null;
            popup?.Hide();
        }
        void ShowQuestion(Vector2 screen)
        {
            if (questionMark == null)
                return;
            questionMark.gameObject.SetActive(true);
            Canvas canvas = GetComponent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            RectTransform parent = questionMark.parent as RectTransform;
            if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out Vector2 local))
                questionMark.anchoredPosition = local + new Vector2(16f, 16f);
        }
        void HideQuestion()
        {
            if (questionMark != null)
                questionMark.gameObject.SetActive(false);
            if (questionFill != null)
                questionFill.fillAmount = 0f;
        }
        PopupTrigger FindTrigger(Vector2 screen)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                var data = new PointerEventData(eventSystem) { position = screen };
                Hits.Clear();
                eventSystem.RaycastAll(data, Hits);
                for (int i = 0; i < Hits.Count; i++)
                {
                    GameObject hit = Hits[i].gameObject;
                    if (hit == null)
                        continue;
                    if (questionMark != null && hit.transform.IsChildOf(questionMark))
                        continue;
                    if (popup != null && hit.transform.IsChildOf(popup.transform))
                        continue;
                    PopupTrigger trigger = hit.GetComponentInParent<PopupTrigger>();
                    if (trigger != null && trigger.isActiveAndEnabled)
                        return trigger;
                }
            }
            Camera camera = Camera.main;
            if (camera == null)
                return null;
            Ray ray = camera.ScreenPointToRay(screen);
            RaycastHit2D hit2d = Physics2D.GetRayIntersection(ray);
            if (hit2d.collider != null)
            {
                PopupTrigger trigger = hit2d.collider.GetComponentInParent<PopupTrigger>();
                if (trigger != null && trigger.isActiveAndEnabled)
                    return trigger;
            }
            if (Physics.Raycast(ray, out RaycastHit hit3d, 1000f))
            {
                PopupTrigger trigger = hit3d.collider.GetComponentInParent<PopupTrigger>();
                if (trigger != null && trigger.isActiveAndEnabled)
                    return trigger;
            }
            return null;
        }
        void Wire()
        {
            if (popup == null)
            {
                popup = GetComponentInChildren<DetailsPopup>(true);
                if (popup == null)
                {
                    GameObject prefab = RuntimePrefabs.DetailsPopup;
                    if (prefab != null)
                    {
                        GameObject instance = Instantiate(prefab, transform);
                        instance.name = "DetailsPopup";
                        popup = instance.GetComponent<DetailsPopup>();
                        if (popup == null)
                            popup = instance.AddComponent<DetailsPopup>();
                    }
                }
            }
            if (questionMark == null)
            {
                Transform existing = transform.Find("QuestionMark");
                if (existing != null)
                    questionMark = existing as RectTransform;
                if (questionMark == null)
                {
                    GameObject prefab = RuntimePrefabs.QuestionMark;
                    if (prefab != null)
                    {
                        GameObject instance = Instantiate(prefab, transform);
                        instance.name = "QuestionMark";
                        questionMark = instance.transform as RectTransform;
                    }
                }
            }
            if (questionMark != null)
            {
                Image[] images = questionMark.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < images.Length; i++)
                    images[i].raycastTarget = false;
                if (questionFill == null)
                    questionFill = questionMark.GetComponent<Image>();
                questionMark.gameObject.SetActive(false);
            }
            if (popup != null)
                popup.gameObject.SetActive(false);
        }
        #endregion
    }
}
