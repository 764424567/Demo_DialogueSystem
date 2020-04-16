// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;

namespace PixelCrushers
{

    /// <summary>
    /// Saves a GameObject's position.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper instead.
    public class PositionSaver : Saver
    {

        [SerializeField]
        private bool m_usePlayerSpawnpoint = false;

        [Serializable]
        public class PositionData
        {
            public Vector3 position;
            public Quaternion rotation;
        }

        private PositionData m_data = new PositionData();

        public bool usePlayerSpawnpoint
        {
            get { return m_usePlayerSpawnpoint; }
            set { m_usePlayerSpawnpoint = value; }
        }

        public override string RecordData()
        {
            m_data.position = transform.position;
            m_data.rotation = transform.rotation;
            return SaveSystem.Serialize(m_data);
        }

        public override void ApplyData(string s)
        {
            if (usePlayerSpawnpoint && SaveSystem.playerSpawnpoint != null)
            {
                transform.position = SaveSystem.playerSpawnpoint.transform.position;
                transform.rotation = SaveSystem.playerSpawnpoint.transform.rotation;
            }
            else if (!string.IsNullOrEmpty(s))
            { 
                var data = SaveSystem.Deserialize<PositionData>(s, m_data);
                if (data == null) return;
                m_data = data;
                transform.position = data.position;
                transform.rotation = data.rotation;
            }
        }

    }
}
