// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{

    /// <summary>
    /// This script implements the sequencer command LoadLevel(levelName).
    /// It tries, in order:
    /// - SaveSystem (to handle scene transitions and persistent data), or
    /// - LevelManager (to handle persistent data), or
    /// - manually calls PersistentDataManager.Record() to
    /// tell objects in the current level to record their persistent data first,
    /// and then it calls LevelWillBeUnloaded() to tell the objects to ignore
    /// the upcoming OnDestroy() if they have OnDestroy() handlers.
    /// </summary>
    [AddComponentMenu("")] // Hide from menu.
    public class SequencerCommandLoadLevel : SequencerCommand
    {

        public void Start()
        {
            string levelName = GetParameter(0);
            string spawnpoint = GetParameter(1);
            if (string.IsNullOrEmpty(levelName))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning(string.Format("{0}: Sequencer: LoadLevel() level name is an empty string", DialogueDebug.Prefix));
            }
            else
            {
                if (DialogueDebug.logInfo) Debug.Log(string.Format("{0}: Sequencer: LoadLevel({1})", DialogueDebug.Prefix, GetParameters()));
                DialogueLua.SetActorField("Player", "Spawnpoint", spawnpoint);
                var saveSystem = FindObjectOfType<SaveSystem>();
                if (saveSystem != null)
                {
                    PersistentDataManager.LevelWillBeUnloaded();
                    SaveSystem.LoadScene(string.IsNullOrEmpty(spawnpoint) ? levelName : levelName + "@" + spawnpoint);
                }
                else
                {
                    var levelManager = FindObjectOfType<LevelManager>();
                    if (levelManager != null)
                    {
                        levelManager.LoadLevel(levelName);
                    }
                    else
                    {
                        PersistentDataManager.Record();
                        PersistentDataManager.LevelWillBeUnloaded();
                        UnityEngine.SceneManagement.SceneManager.LoadScene(levelName, UnityEngine.SceneManagement.LoadSceneMode.Single);
                        PersistentDataManager.Apply();
                    }
                }
            }
            Stop();
        }
    }
}
