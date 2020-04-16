// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System.Collections;

namespace PixelCrushers
{

    /// <summary>
    /// Enables a scrollbar only if the content is larger than the scroll rect. This component only
    /// only shows or hides the scrollbar when the component is enabled or when CheckScrollbar is invoked.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class UIScrollbarEnabler : MonoBehaviour
    {

        [Tooltip("The scroll rect.")]
        [UnityEngine.Serialization.FormerlySerializedAs("container")]
        public UnityEngine.UI.ScrollRect scrollRect = null;

        [Tooltip("The content inside the scroll rect. The scrollbar will be enabled if the content is taller than the scroll rect.")]
        [UnityEngine.Serialization.FormerlySerializedAs("content")]
        public RectTransform scrollContent = null;

        [Tooltip("The scrollbar to enable or disable.")]
        public UnityEngine.UI.Scrollbar scrollbar = null;

        private bool m_started = false;
        private bool m_checking = false;

        private void Start()
        {
            m_started = true;
            CheckScrollbar();
        }

        public void OnEnable()
        {
            if (m_started) CheckScrollbar();
        }

        public void OnDisable()
        {
            m_checking = false;
        }

        public void CheckScrollbar()
        {
            if (m_checking || scrollRect == null || scrollContent == null || scrollbar == null || !gameObject.activeInHierarchy || !enabled) return;
            StartCoroutine(CheckScrollbarAfterUIUpdate(false, 0));
        }

        public void CheckScrollbarWithResetValue(float value)
        {
            if (m_checking || scrollRect == null || scrollContent == null || scrollbar == null || !gameObject.activeInHierarchy || !enabled) return;
            StartCoroutine(CheckScrollbarAfterUIUpdate(true, value));
        }

        private IEnumerator CheckScrollbarAfterUIUpdate(bool useResetValue, float resetValue)
        {
            m_checking = true;
            yield return null;
            scrollbar.gameObject.SetActive(scrollContent.rect.height > scrollRect.GetComponent<RectTransform>().rect.height);
            m_checking = false;
            yield return null;
            if (useResetValue)
            {
                scrollbar.value = resetValue;
                scrollRect.verticalNormalizedPosition = resetValue;
            }
        }

    }
}
