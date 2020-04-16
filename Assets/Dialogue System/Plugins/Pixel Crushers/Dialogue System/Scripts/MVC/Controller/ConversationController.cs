// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Mediates between a ConversationModel (data) and ConversationView (user interface) to run a
    /// conversation.
    /// </summary>
    public class ConversationController
    {

        /// <summary>
        /// The data model of the conversation.
        /// </summary>
        private ConversationModel m_model = null;

        /// <summary>
        /// The view (user interface) of the current state of the conversation.
        /// </summary>
        private ConversationView m_view = null;

        /// <summary>
        /// The current state of the conversation.
        /// </summary>
        private ConversationState state = null;

        /// <summary>
        /// Indicates whether the ConversationController is currently running a conversation.
        /// </summary>
        /// <value>
        /// <c>true</c> if a conversation is active; <c>false</c> if the conversation is done.
        /// </value>
        public bool isActive { get; private set; }

        /// <summary>
        /// Gets the actor info for this conversation.
        /// </summary>
        /// <value>
        /// The actor info.
        /// </value>
        public CharacterInfo actorInfo { get { return (m_model != null) ? m_model.actorInfo : null; } }

        public ConversationModel conversationModel { get { return m_model; } }

        public ConversationView conversationView { get { return m_view; } }

        /// <summary>
        /// Gets or sets the IsDialogueEntryValid delegate.
        /// </summary>
        public IsDialogueEntryValidDelegate isDialogueEntryValid
        {
            get { return m_model.isDialogueEntryValid; }
            set { m_model.isDialogueEntryValid = value; }
        }

        /// <summary>
        /// Set true to choice randomly from the next list of valid NPC subtitles instead of
        /// using the first one in the list.
        /// </summary>
        public bool randomizeNextEntry { get; set; }

        /// <summary>
        /// Gets the conversant info for this conversation.
        /// </summary>
        /// <value>
        /// The conversant info.
        /// </value>
        public CharacterInfo conversantInfo { get { return (m_model != null) ? m_model.conversantInfo : null; } }

        /// @cond FOR_V1_COMPATIBILITY
        public bool IsActive { get { return isActive; } private set { isActive = value; } }
        public CharacterInfo ActorInfo { get { return actorInfo; } }
        public ConversationModel ConversationModel { get { return conversationModel; } }
        public ConversationView ConversationView { get { return conversationView; } }
        public IsDialogueEntryValidDelegate IsDialogueEntryValid { get { return isDialogueEntryValid; } set { isDialogueEntryValid = value; } }
        public CharacterInfo ConversantInfo { get { return conversantInfo; } }
        /// @endcond

        public delegate void EndConversationDelegate(ConversationController ConversationController);

        private EndConversationDelegate m_endConversationHandler = null;
        private int m_currentConversationID;
        private Response m_currentResponse = null;

        /// <summary>
        /// Initializes a new ConversationController and starts the conversation in the model.
        /// Also sends OnConversationStart messages to the participants.
        /// </summary>
        /// <param name='model'>
        /// Data model of the conversation.
        /// </param>
        /// <param name='view'>
        /// View to use to provide a user interface for the conversation.
        /// </param>
        /// <param name='endConversationHandler'>
        /// Handler to call to inform when the conversation is done.
        /// </param>
        public ConversationController(ConversationModel model, ConversationView view, bool alwaysForceResponseMenu, EndConversationDelegate endConversationHandler)
        {
            isActive = true;
            this.m_model = model;
            this.m_view = view;
            this.m_endConversationHandler = endConversationHandler;
            this.randomizeNextEntry = false;
            model.InformParticipants(DialogueSystemMessages.OnConversationStart);
            view.FinishedSubtitleHandler += OnFinishedSubtitle;
            view.SelectedResponseHandler += OnSelectedResponse;
            m_currentConversationID = model.GetConversationID(model.firstState);
            SetConversationOverride(model.firstState);
            GotoState(model.firstState);
        }

        /// <summary>
        /// Closes the currently-running conversation, which also sends OnConversationEnd messages
        /// to the participants.
        /// </summary>
        public void Close()
        {
            if (isActive)
            {
                isActive = false;
                if (DialogueDebug.logInfo) Debug.Log(string.Format("{0}: Ending conversation.", new System.Object[] { DialogueDebug.Prefix }));
                m_view.displaySettings.conversationOverrideSettings = null;
                m_view.FinishedSubtitleHandler -= OnFinishedSubtitle;
                m_view.SelectedResponseHandler -= OnSelectedResponse;
                m_view.Close();
                m_model.InformParticipants(DialogueSystemMessages.OnConversationEnd, true);
                if (m_endConversationHandler != null) m_endConversationHandler(this);
                DialogueManager.instance.currentConversationState = null;
            }
        }

        /// <summary>
        /// Goes to a conversation state. If the state is <c>null</c>, the conversation ends.
        /// </summary>
        /// <param name='state'>
        /// State.
        /// </param>
        public void GotoState(ConversationState state)
        {
            this.state = state;
            DialogueManager.instance.currentConversationState = state;
            if (state != null)
            {
                // Check for change of conversation:
                var newConversationID = m_model.GetConversationID(state);
                if (newConversationID != m_currentConversationID)
                {
                    m_currentConversationID = newConversationID;
                    m_model.InformParticipants(DialogueSystemMessages.OnLinkedConversationStart, true);
                    m_model.UpdateParticipantsOnLinkedConversation(newConversationID);
                    m_view.SetPCPortrait(m_model.GetPCTexture(), m_model.GetPCName());
                    SetConversationOverride(state);
                }
                // Use view to show current state:
                if (state.isGroup)
                {
                    m_view.ShowLastNPCSubtitle();
                }
                else
                {
                    bool isPCResponseMenuNext, isPCAutoResponseNext;
                    AnalyzePCResponses(state, out isPCResponseMenuNext, out isPCAutoResponseNext);
                    m_view.StartSubtitle(state.subtitle, isPCResponseMenuNext, isPCAutoResponseNext);
                }
            }
            else
            {
                Close();
            }
        }

        private void AnalyzePCResponses(ConversationState state, out bool isPCResponseMenuNext, out bool isPCAutoResponseNext)
        {
            var alwaysForceMenu = m_view.displaySettings.GetAlwaysForceResponseMenu();
            var hasForceMenu = false;
            var hasForceAuto = false;
            var numPCResponses = (state.pcResponses != null) ? state.pcResponses.Length : 0;
            for (int i = 0; i < numPCResponses; i++)
            {
                if (state.pcResponses[i].formattedText.forceMenu)
                {
                    hasForceMenu = true;
                }
                if (state.pcResponses[i].formattedText.forceAuto)
                {
                    hasForceAuto = true;
                    break; // [auto] takes precedence over [f].
                }
            }
            isPCResponseMenuNext = !state.hasNPCResponse && !hasForceAuto &&
                (numPCResponses > 1 || hasForceMenu || (numPCResponses == 1 && alwaysForceMenu));
            isPCAutoResponseNext = !state.hasNPCResponse && hasForceAuto || (numPCResponses == 1 && !hasForceMenu && (!alwaysForceMenu || state.pcResponses[0].destinationEntry.isGroup));
        }

        private void SetConversationOverride(ConversationState state)
        {
            m_view.displaySettings.conversationOverrideSettings = m_model.GetConversationOverrideSettings(state);
        }

        /// <summary>
        /// Handles the finished subtitle event. If the current conversation state has an NPC 
        /// response, the conversation proceeds to that response. Otherwise, if the current
        /// state has PC responses, then the response menu is shown (or if it has a single
        /// auto-response, the conversation proceeds directly to that response). If there are no
        /// responses, the conversation ends.
        /// </summary>
        /// <param name='sender'>
        /// Sender.
        /// </param>
        /// <param name='e'>
        /// Event args.
        /// </param>
        public void OnFinishedSubtitle(object sender, EventArgs e)
        {
            var randomize = randomizeNextEntry;
            randomizeNextEntry = false;
            if (state.hasNPCResponse)
            {
                GotoState(m_model.GetState(randomize ? state.GetRandomNPCEntry() : state.firstNPCResponse.destinationEntry));
            }
            else if (state.hasPCResponses)
            {
                bool isPCResponseMenuNext, isPCAutoResponseNext;
                AnalyzePCResponses(state, out isPCResponseMenuNext, out isPCAutoResponseNext);
                if (isPCAutoResponseNext)
                {
                    GotoState(m_model.GetState(state.pcAutoResponse.destinationEntry));
                }
                else
                {
                    m_view.StartResponses(state.subtitle, state.pcResponses);
                }
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// Handles the selected response event by proceeding to the state associated with the
        /// selected response.
        /// </summary>
        /// <param name='sender'>
        /// Sender.
        /// </param>
        /// <param name='e'>
        /// Selected response event args.
        /// </param>
        public void OnSelectedResponse(object sender, SelectedResponseEventArgs e)
        {
            GotoState(m_model.GetState(e.DestinationEntry));
        }

        /// <summary>
        /// Follows the first PC response in the current state.
        /// </summary>
        public void GotoFirstResponse()
        {
            if (state != null)
            {
                if (state.pcResponses.Length > 0)
                {
                    m_view.SelectResponse(new SelectedResponseEventArgs(state.pcResponses[0]));
                }
            }
        }

        /// <summary>
        /// Follows the last PC response in the current state.
        /// </summary>
        public void GotoLastResponse()
        {
            if (state != null)
            {
                if (state.pcResponses.Length > 0)
                {
                    m_view.SelectResponse(new SelectedResponseEventArgs(state.pcResponses[state.pcResponses.Length - 1]));
                }
            }
        }

        /// <summary>
        /// Follows a random PC response in the current state.
        /// </summary>
        public void GotoRandomResponse()
        {
            if (state != null)
            {
                if (state.pcResponses.Length > 0)
                {
                    m_view.SelectResponse(new SelectedResponseEventArgs(state.pcResponses[UnityEngine.Random.Range(0, state.pcResponses.Length)]));
                }
            }
        }

        /// <summary>
        /// Follows a response that has been set as the current response by SetCurrentResponse.
        /// </summary>
        public void GotoCurrentResponse()
        {
            if (m_currentResponse != null)
            {
                m_view.SelectResponse(new SelectedResponseEventArgs(m_currentResponse));
            }
            else
            {
                GotoFirstResponse();
            }
        }

        /// <summary>
        /// Sets the current response, which can be used by GotoCurrentResponse. Typically only
        /// used if the dialogue UI's timeout action specifies to select the current response.
        /// </summary>
        /// <param name="response"></param>
        public void SetCurrentResponse(Response response)
        {
            m_currentResponse = response;
        }

        public void UpdateResponses()
        {
            if (state != null)
            {
                m_model.UpdateResponses(state);
                OnFinishedSubtitle(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Sets the portrait texture to use in the UI for an actor.
        /// This is used when the SetPortrait() sequencer command changes an actor's image.
        /// </summary>
        /// <param name="actorName">Actor name.</param>
        /// <param name="portraitTexture">Portrait texture.</param>
        public void SetActorPortraitTexture(string actorName, Texture2D portraitTexture)
        {
            m_model.SetActorPortraitTexture(actorName, portraitTexture);
            m_view.SetActorPortraitTexture(actorName, portraitTexture);
        }

    }

}
