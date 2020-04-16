// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This is a saver that saves the Dialogue System's save data 
    /// to the Pixel Crushers Common Library Save System.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class DialogueSystemSaver : Saver
    {

        [Serializable]
        public class RawData
        {
            public byte[] bytes;
        }

        [Tooltip("Save using raw data dump. If your database is extremely large, this method is faster but generates larger saved game data. If you use this option, use BinaryDataSerializer instead of JsonDataSerializer or data will be ridiculously large.")]
        public bool saveRawData = false;

        public override string RecordData()
        {
            if (saveRawData)
            {
                var rawData = new RawData();
                rawData.bytes = PersistentDataManager.GetRawData();
                return SaveSystem.Serialize(rawData);
            }
            else
            {
                return PersistentDataManager.GetSaveData();
            }
        }

        public override void ApplyData(string data)
        {
            if (saveRawData)
            {
                var rawData = SaveSystem.Deserialize<RawData>(data);
                if (rawData != null && rawData.bytes != null) PersistentDataManager.ApplyRawData(rawData.bytes);
            }
            else
            {
                PersistentDataManager.ApplySaveData(data);
            }
        }

        public override void OnBeforeSceneChange()
        {
            PersistentDataManager.LevelWillBeUnloaded();
        }

    }

}
