// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Add this script to your Dialogue Manager to keep track of the 
    /// current conversation and dialogue entry. When you load a game,
    /// it will resume the conversation at that point.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class ConversationStateSaver : MonoBehaviour
    {

        protected virtual void OnEnable()
        {
            PersistentDataManager.RegisterPersistentData(this.gameObject);
        }

        protected virtual void OnDisable()
        {
            PersistentDataManager.UnregisterPersistentData(this.gameObject);
        }

        public virtual void OnConversationStart(Transform actor)
        {

            var actorName = (DialogueManager.CurrentActor != null) ? DialogueManager.CurrentActor.name : string.Empty;
            var conversantName = (DialogueManager.CurrentConversant != null) ? DialogueManager.CurrentConversant.name : string.Empty;
            DialogueLua.SetVariable("CurrentConversationActor", actorName);
            DialogueLua.SetVariable("CurrentConversationConversant", conversantName);
        }

        public virtual void OnConversationLine(Subtitle subtitle)
        {
            DialogueLua.SetVariable("CurrentConversationID", subtitle.dialogueEntry.conversationID);
            DialogueLua.SetVariable("CurrentEntryID", subtitle.dialogueEntry.id);
        }

        public virtual void OnConversationEnd(Transform actor)
        {
            DialogueLua.SetVariable("CurrentConversationID", -1);
        }

        public virtual void OnApplyPersistentData()
        {
            if (!enabled) return;
            var conversationID = DialogueLua.GetVariable("CurrentConversationID").AsInt;
            var entryID = DialogueLua.GetVariable("CurrentEntryID").AsInt;
            DialogueManager.StopConversation();
            if (conversationID >= 0 && entryID > 0)
            {
                var conversation = DialogueManager.MasterDatabase.GetConversation(conversationID);
                var actorName = DialogueLua.GetVariable("CurrentConversationActor").AsString;
                var conversantName = DialogueLua.GetVariable("CurrentConversationConversant").AsString;
                if (DialogueDebug.logInfo) Debug.Log("Dialogue System: ConversationStateSaver is resuming conversation " + conversation.Title + " with actor=" + actorName + " and conversant=" + conversantName + " at entry " + entryID + ".", this);
                var actor = string.IsNullOrEmpty(actorName) ? null : GameObject.Find(actorName);
                var conversant = string.IsNullOrEmpty(conversantName) ? null : GameObject.Find(conversantName);
                var actorTransform = (actor != null) ? actor.transform : null;
                var conversantTransform = (conversant != null) ? conversant.transform : null;
                DialogueManager.StartConversation(conversation.Title, actorTransform, conversantTransform, entryID);
            }
        }
    }
}
