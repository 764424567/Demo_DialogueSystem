// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelCrushers
{

    /// <summary>
    /// Holds the data for a saved game.
    /// </summary>
    [Serializable]
    public class SavedGameData : ISerializationCallbackReceiver
    {

        /// <summary>
        /// Holds the data returned by a Saver along with the Saver's key
        /// and the index of the scene that the Saver was in.
        /// </summary>
        [Serializable]
        public class SaveRecord
        {
            public string key;
            public int sceneIndex;
            public string data;

            public SaveRecord() { }

            public SaveRecord(string key, int sceneIndex, string data)
            {
                this.key = key;
                this.sceneIndex = sceneIndex;
                this.data = data;
            }
        }

        [SerializeField]
        private string m_sceneName;

        private Dictionary<string, SaveRecord> m_dict = new Dictionary<string, SaveRecord>();

        [SerializeField]
        private List<SaveRecord> m_list = new List<SaveRecord>();

        /// <summary>
        /// The scene in which the game was saved.
        /// </summary>
        public string sceneName
        {
            get { return m_sceneName; }
            set { m_sceneName = value; }
        }

        public void OnBeforeSerialize()
        {
            m_list.Clear();
            foreach (var kvp in m_dict)
            {
                m_list.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            m_dict = new Dictionary<string, SaveRecord>();
            for (int i = 0; i < m_list.Count; i++)
            {
                if (m_list[i] == null) continue;
                m_dict.Add(m_list[i].key, m_list[i]);
            }
        }

        /// <summary>
        /// Retrieves the previously-saved data for a Saver.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetData(string key)
        {
            return m_dict.ContainsKey(key) ? m_dict[key].data : null;
        }

        /// <summary>
        /// Stores a Saver's data.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="sceneIndex"></param>
        /// <param name="data"></param>
        public void SetData(string key, int sceneIndex, string data)
        {
            if (m_dict.ContainsKey(key))
            {
                m_dict[key].sceneIndex = sceneIndex;
                m_dict[key].data = data;
            }
            else
            {
                m_dict.Add(key, new SaveRecord(key, sceneIndex, data));
            }
        }

        /// <summary>
        /// Removes all save records except those in the current scene and those that are
        /// configured to remember across scene changes.
        /// </summary>
        /// <param name="currentSceneIndex"></param>
        public void DeleteObsoleteSaveData(int currentSceneIndex)
        {
            m_dict = m_dict.Where(element => element.Value.sceneIndex == currentSceneIndex || element.Value.sceneIndex == SaveSystem.NoSceneIndex)
                .ToDictionary(element => element.Key, element => element.Value);
        }

    }

}