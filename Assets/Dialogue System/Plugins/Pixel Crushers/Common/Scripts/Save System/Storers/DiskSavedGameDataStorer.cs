// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System.Collections.Generic;
#if !(UNITY_WEBGL || UNITY_WSA)
using System.IO;
#endif

namespace PixelCrushers
{

    /// <summary>
    /// Implements SavedGameDataStorer using local disk files.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper instead.
    public class DiskSavedGameDataStorer : SavedGameDataStorer
    {

#if !(UNITY_WEBGL || UNITY_WSA)

        [Tooltip("Encrypt saved game files.")]
        public bool encrypt = true;

        [Tooltip("If encrypting, use this password.")]
        public string encryptionPassword = "My Password";

        [Tooltip("Log debug info.")]
        [SerializeField]
        private bool m_debug;

        private class SavedGameInfo
        {
            public string sceneName;

            public SavedGameInfo(string sceneName)
            {
                this.sceneName = sceneName;
            }
        }

        private List<SavedGameInfo> m_savedGameInfo = new List<SavedGameInfo>();

        public bool debug
        {
            get { return m_debug && Debug.isDebugBuild; }
            set { m_debug = value; }
        }

        public void Start()
        {
            LoadSavedGameInfoFromFile();
        }

        public string GetSaveGameFilename(int slotNumber)
        {
            return Application.persistentDataPath + "/save_" + slotNumber + ".dat";
        }

        public string GetSavedGameInfoFilename()
        {
            return Application.persistentDataPath + "/saveinfo.dat";
        }

        public void LoadSavedGameInfoFromFile()
        {
            var filename = GetSavedGameInfoFilename();
            if (string.IsNullOrEmpty(filename) || !File.Exists(filename)) return;
            if (debug) Debug.Log("Save System: DiskSavedGameDataStorer loading " + filename);
            try
            {
                using (StreamReader streamReader = new StreamReader(filename))
                {
                    m_savedGameInfo.Clear();
                    int safeguard = 0;
                    while (!streamReader.EndOfStream && safeguard < 999)
                    {
                        var sceneName = streamReader.ReadLine().Replace("<cr>", "\n");
                        m_savedGameInfo.Add(new SavedGameInfo(sceneName));
                        safeguard++;
                    }
                }
            }
            catch (System.Exception)
            {
                Debug.Log("Save System: DiskSavedGameDataStorer - Error reading file: " + filename);
            }
        }

        public void UpdateSavedGameInfoToFile(int slotNumber, SavedGameData savedGameData)
        {
            var slotIndex = slotNumber;
            for (int i = m_savedGameInfo.Count; i <= slotIndex; i++)
            {
                m_savedGameInfo.Add(new SavedGameInfo(string.Empty));
            }
            m_savedGameInfo[slotIndex].sceneName = (savedGameData != null) ? savedGameData.sceneName : string.Empty;
            var filename = GetSavedGameInfoFilename();
            if (debug) Debug.Log("Save System: DiskSavedGameDataStorer updating " + filename);
            try
            {
                using (StreamWriter streamWriter = new StreamWriter(filename))
                {
                    for (int i = 0; i < m_savedGameInfo.Count; i++)
                    {
                        streamWriter.WriteLine(m_savedGameInfo[i].sceneName.Replace("\n", "<cr>"));
                    }
                }
            }
            catch (System.Exception)
            {
                Debug.LogError("Save System: DiskSavedGameDataStorer - Can't create file: " + filename);
            }
        }

        public override bool HasDataInSlot(int slotNumber)
        {
            var slotIndex = slotNumber;
            return 0 <= slotIndex && slotIndex < m_savedGameInfo.Count && !string.IsNullOrEmpty(m_savedGameInfo[slotIndex].sceneName);
        }

        public override void StoreSavedGameData(int slotNumber, SavedGameData savedGameData)
        {
            var s = SaveSystem.Serialize(savedGameData);
            if (debug) Debug.Log("Save System: DiskSavedGameDataStorer - Saving " + GetSaveGameFilename(slotNumber) + ": " + s);
            WriteStringToFile(GetSaveGameFilename(slotNumber), encrypt ? EncryptionUtility.Encrypt(s, encryptionPassword) : s);
            UpdateSavedGameInfoToFile(slotNumber, savedGameData);
        }

        public override SavedGameData RetrieveSavedGameData(int slotNumber)
        {
            var s = ReadStringFromFile(GetSaveGameFilename(slotNumber));
            if (encrypt)
            {
                string plainText;
                s = EncryptionUtility.TryDecrypt(s, encryptionPassword, out plainText) ? plainText : string.Empty;
            }
            if (debug) Debug.Log("Save System: DiskSavedGameDataStorer - Loading " + GetSaveGameFilename(slotNumber) + ": " + s);
            return SaveSystem.Deserialize<SavedGameData>(s);
        }

        public override void DeleteSavedGameData(int slotNumber)
        {
            try
            {
                var filename = GetSaveGameFilename(slotNumber);
                if (File.Exists(filename)) File.Delete(filename);
            }
            catch (System.Exception)
            {
            }
            UpdateSavedGameInfoToFile(slotNumber, null);
        }

        public static void WriteStringToFile(string filename, string data)
        {
            try
            {
                using (StreamWriter streamWriter = new StreamWriter(filename))
                {
                    streamWriter.WriteLine(data);
                }
            }
            catch (System.Exception)
            {
                Debug.LogError("Save System: Can't create file: " + filename);
            }
        }

        public static string ReadStringFromFile(string filename)
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(filename))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (System.Exception)
            {
                Debug.Log("Save System: Error reading file: " + filename);
                return string.Empty;
            }
        }

#else
        void Start()
        {
            Debug.LogError("DiskSavedGameDataStorer is not supported on this build platform.");
        }

        public override bool HasDataInSlot(int slotNumber)
        {
            Debug.LogError("DiskSavedGameDataStorer is not supported on this build platform.");
            return false;
        }

        public override SavedGameData RetrieveSavedGameData(int slotNumber)
        {
            Debug.LogError("DiskSavedGameDataStorer is not supported on this build platform.");
            return null;
        }

        public override void StoreSavedGameData(int slotNumber, SavedGameData savedGameData)
        {
            Debug.LogError("DiskSavedGameDataStorer is not supported on this build platform.");
        }

        public override void DeleteSavedGameData(int slotNumber)
        {
            Debug.LogError("DiskSavedGameDataStorer is not supported on this build platform.");
        }

#endif

    }

}
