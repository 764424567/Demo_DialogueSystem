// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System.Collections;

namespace PixelCrushers
{

    /// <summary>
    /// Manages localization settings.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class UILocalizationManager : MonoBehaviour
    {

        [Tooltip("The PlayerPrefs key to store the player's selected language code.")]
        [SerializeField]
        private string m_currentLanguagePlayerPrefsKey = "Language";

        [Tooltip("Overrides the global text table.")]
        [SerializeField]
        private TextTable m_textTable = null;

        private string m_currentLanguage = string.Empty;

        private static UILocalizationManager m_instance = null;

        /// <summary>
        /// Current global instance of UILocalizationManager. If one doesn't exist,
        /// a default one will be created.
        /// </summary>
        public static UILocalizationManager instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = FindObjectOfType<UILocalizationManager>();
                    if (m_instance == null)
                    {
                        var globalTextTable = FindObjectOfType<GlobalTextTable>();
                        m_instance = (globalTextTable != null) ? globalTextTable.gameObject.AddComponent<UILocalizationManager>()
                            : new GameObject("UILocalizationManager").AddComponent<UILocalizationManager>();
                    }
                }
                return m_instance;
            }
            set
            {
                m_instance = value;
            }
        }

        /// <summary>
        /// Overrides the global text table.
        /// </summary>
        public TextTable textTable
        {
            get { return (instance.m_textTable != null) ? instance.m_textTable : GlobalTextTable.textTable; }
            set { instance.m_textTable = value; }
        }

        /// <summary>
        /// Gets or sets the current language. Setting the current language also updates localized UIs.
        /// </summary>
        public string currentLanguage
        {
            get
            {
                return instance.m_currentLanguage;
            }
            set
            {
                instance.m_currentLanguage = value;
                instance.UpdateUIs(value);
            }
        }

        /// <summary>
        /// The PlayerPrefs key to store the player's selected language code.
        /// </summary>
        public string currentLanguagePlayerPrefsKey
        {
            get { return m_currentLanguagePlayerPrefsKey; }
            set { m_currentLanguagePlayerPrefsKey = value; }
        }

        private void Awake()
        {
            m_instance = this;
            if (!string.IsNullOrEmpty(currentLanguagePlayerPrefsKey) && PlayerPrefs.HasKey(currentLanguagePlayerPrefsKey))
            {
                m_currentLanguage = PlayerPrefs.GetString(currentLanguagePlayerPrefsKey);
            }
        }

        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame(); // Wait for Text components to start.
            UpdateUIs(currentLanguage);
        }

        /// <summary>
        /// Updates the current language and all localized UIs.
        /// </summary>
        /// <param name="language">Language code defined in your Text Table.</param>
        public void UpdateUIs(string language)
        {
            m_currentLanguage = language;
            if (!string.IsNullOrEmpty(currentLanguagePlayerPrefsKey))
            {
                PlayerPrefs.SetString(currentLanguagePlayerPrefsKey, language);
            }
            var localizeUIs = FindObjectsOfType<LocalizeUI>();
            for (int i = 0; i < localizeUIs.Length; i++)
            {
                localizeUIs[i].UpdateText();
            }
        }

    }
}
