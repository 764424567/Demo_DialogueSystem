// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers
{

    /// <summary>
    /// Abstract base class for "storage providers" that store saved game 
    /// data somewhere, such as PlayerPrefs or a disk file.
    /// </summary>
    public abstract class SavedGameDataStorer : MonoBehaviour
    {

        public abstract bool HasDataInSlot(int slotNumber);

        public abstract void StoreSavedGameData(int slotNumber, SavedGameData savedGameData);

        public abstract SavedGameData RetrieveSavedGameData(int slotNumber);

        public abstract void DeleteSavedGameData(int slotNumber);

    }

}
