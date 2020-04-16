using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.DialogueEditor
{

    /// <summary>
    /// This part of the Dialogue Editor window handles the top
    /// controls for the conversation node editor.
    /// </summary>
    public partial class DialogueEditorWindow
    {

        [SerializeField]
        private string[] conversationTitles = null;

        [SerializeField]
        private int conversationIndex = -1;

        private DialogueEntry nodeToDrag = null;

        [SerializeField]
        private float snapToGridAmount = 0;

        private bool hasStartedSnapToGrid = false;

        [SerializeField]
        private bool confirmDelete = true;

        [SerializeField]
        private bool trimWhitespaceAroundPipes = false;

        private void SetConversationDropdownIndex(int index)
        {
            conversationIndex = index;
        }

        private void ResetConversationNodeEditor()
        {
            conversationTitles = null;
            conversationIndex = -1;
            ResetConversationNodeSection();
        }

        private void ResetConversationNodeSection()
        {
            isMakingLink = false;
            multinodeSelection.nodes.Clear();
            currentHoveredEntry = null;
            currentHoverGUIContent = null;
        }

        private void ValidateConversationMenuTitleIndex()
        {
            UpdateConversationTitles();
            if (database != null && conversationIndex >= database.conversations.Count) conversationIndex = -1;
        }

        private void SetShowNodeEditor(bool value)
        {
            showNodeEditor = value;
            EditorPrefs.SetBool(ShowNodeEditorKey, value);
        }

        private void ActivateOutlineMode()
        {
            SetShowNodeEditor(false);
        }

        private void ActivateNodeEditorMode()
        {
            SetShowNodeEditor(true);
            ResetNodeEditorConversationList();
            if (currentConversation != null) OpenConversation(currentConversation);
            isMakingLink = false;
        }

        private void ResetNodeEditorConversationList()
        {
            conversationTitles = GetConversationTitles();
            SetConversationDropdownIndex(GetCurrentConversationIndex());
        }

        private void DrawNodeEditorTopControls()
        {
            EditorGUILayout.BeginHorizontal();
            DrawNodeEditorConversationPopup();
            if (GUILayout.Button(new GUIContent("+", "Create a new conversation"), EditorStyles.miniButtonRight, GUILayout.Width(21)))
            {
                AddNewConversationToNodeEditor();
            }
            DrawZoomSlider();
            DrawNodeEditorMenu();
            EditorGUILayout.EndHorizontal();
        }

        private void AddNewConversationToNodeEditor()
        {
            AddNewConversation();
            ActivateNodeEditorMode();
            inspectorSelection = currentConversation;
        }

        private void DrawNodeEditorMenu()
        {
            if (GUILayout.Button("Menu", "MiniPullDown", GUILayout.Width(56)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Home Position"), false, GotoCanvasHomePosition);
                if (currentConversation != null)
                {
                    menu.AddItem(new GUIContent("Center on START"), false, GotoStartNodePosition);
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Center on START"));
                }
                menu.AddItem(new GUIContent("New Conversation"), false, AddNewConversationToNodeEditor);
                if (currentConversation != null)
                {
                    menu.AddItem(new GUIContent("Copy Conversation"), false, CopyConversationCallback, null);
                    menu.AddItem(new GUIContent("Delete Conversation"), false, DeleteConversationCallback, null);
                    menu.AddItem(new GUIContent("Split Pipes Into Nodes/Process Conversation"), false, SplitPipesIntoEntries, null);
                    menu.AddItem(new GUIContent("Split Pipes Into Nodes/Trim Whitespace Around Pipes"), trimWhitespaceAroundPipes, ToggleTrimWhitespaceAroundPipes);
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Copy Conversation"));
                    menu.AddDisabledItem(new GUIContent("Delete Conversation"));
                    menu.AddDisabledItem(new GUIContent("Split Pipes Into Nodes/Process Conversation"));
                    menu.AddDisabledItem(new GUIContent("Split Pipes Into Nodes/Trim Whitespace Around Pipes"));
                }
                menu.AddItem(new GUIContent("Sort/By Title"), false, SortConversationsByTitle);
                menu.AddItem(new GUIContent("Sort/By ID"), false, SortConversationsByID);
                menu.AddItem(new GUIContent("Sort/Reorder IDs/This Conversation"), false, ConfirmReorderIDsThisConversation);
                menu.AddItem(new GUIContent("Sort/Reorder IDs/All Conversations"), false, ConfirmReorderIDsAllConversations);
                menu.AddItem(new GUIContent("Show/Show All Actor Names"), showAllActorNames, ToggleShowAllActorNames);
                menu.AddItem(new GUIContent("Show/Show Non-Primary Actor Names"), showOtherActorNames, ToggleShowOtherActorNames);
                menu.AddItem(new GUIContent("Show/Show Actor Portraits"), showActorPortraits, ToggleShowActorPortraits);
                menu.AddItem(new GUIContent("Show/Show Full Text On Hover"), showFullTextOnHover, ToggleShowFullTextOnHover);
                menu.AddItem(new GUIContent("Show/Show End Node Markers"), showEndNodeMarkers, ToggleShowEndNodeMarkers);
                menu.AddItem(new GUIContent("Show/Show Node IDs"), showNodeIDs, ToggleShowNodeIDs);
                menu.AddItem(new GUIContent("Show/Show Primary Actors in Lower Right"), showParticipantNames, ToggleShowParticipantNames);
                menu.AddItem(new GUIContent("Grid/No Snap"), snapToGridAmount < MinorGridLineWidth, SetSnapToGrid, 0f);
                menu.AddItem(new GUIContent("Grid/12 pixels"), Mathf.Approximately(12f, snapToGridAmount), SetSnapToGrid, 12f);
                menu.AddItem(new GUIContent("Grid/24 pixels"), Mathf.Approximately(24f, snapToGridAmount), SetSnapToGrid, 24f);
                menu.AddItem(new GUIContent("Grid/36 pixels"), Mathf.Approximately(36f, snapToGridAmount), SetSnapToGrid, 36f);
                menu.AddItem(new GUIContent("Grid/48 pixels"), Mathf.Approximately(48f, snapToGridAmount), SetSnapToGrid, 48f);
                menu.AddItem(new GUIContent("Grid/Snap All Nodes To Grid"), false, SnapAllNodesToGrid);
                menu.AddItem(new GUIContent("Search/Search Bar"), isSearchBarOpen, ToggleDialogueTreeSearchBar);
                menu.AddItem(new GUIContent("Search/Global Search and Replace..."), false, OpenGlobalSearchAndReplace);
                menu.AddItem(new GUIContent("Settings/Add New Nodes to Right"), addNewNodesToRight, ToggleAddNewNodesToRight);
                menu.AddItem(new GUIContent("Settings/Confirm Node and Link Deletion"), confirmDelete, ToggleConfirmDelete);
                menu.AddItem(new GUIContent("Outline Mode"), false, ActivateOutlineMode);
                AddRelationsInspectorMenuItems(menu);
                menu.ShowAsContext();
            }
        }

        private void DrawZoomSlider()
        {
            _zoom = EditorGUILayout.Slider(_zoom, kZoomMin, kZoomMax, GUILayout.Width(200));
            zoomLocked = GUILayout.Toggle(zoomLocked, GUIContent.none, "IN LockButton");
        }

        private void ToggleShowAllActorNames()
        {
            showAllActorNames = !showAllActorNames;
            dialogueEntryNodeText.Clear();
        }

        private void ToggleShowOtherActorNames()
        {
            showOtherActorNames = !showOtherActorNames;
            dialogueEntryNodeText.Clear();
        }

        private void ToggleShowParticipantNames()
        {
            showParticipantNames = !showParticipantNames;
        }

        private void ToggleShowActorPortraits()
        {
            showActorPortraits = !showActorPortraits;
            ClearActorInfoCaches();
        }

        private void ToggleShowFullTextOnHover()
        {
            showFullTextOnHover = !showFullTextOnHover;
        }

        private void ToggleShowEndNodeMarkers()
        {
            showEndNodeMarkers = !showEndNodeMarkers;
        }

        private void ToggleShowNodeIDs()
        {
            showNodeIDs = !showNodeIDs;
            dialogueEntryNodeText.Clear();
        }

        private void ToggleAddNewNodesToRight()
        {
            addNewNodesToRight = !addNewNodesToRight;
        }

        private void ToggleConfirmDelete()
        {
            confirmDelete = !confirmDelete;
        }

        private void ToggleTrimWhitespaceAroundPipes()
        {
            trimWhitespaceAroundPipes = !trimWhitespaceAroundPipes;
        }

        private void DrawNodeEditorConversationPopup()
        {
            if (conversationTitles == null) conversationTitles = GetConversationTitles();
            int newIndex = EditorGUILayout.Popup(conversationIndex, conversationTitles, GUILayout.Height(30));
            if (newIndex != conversationIndex)
            {
                SetConversationDropdownIndex(newIndex);
                OpenConversation(GetConversationByTitleIndex(conversationIndex));
                InitializeDialogueTree();
                inspectorSelection = currentConversation;
            }
        }

        private string[] GetConversationTitles()
        {
            List<string> titles = new List<string>();
            if (database != null)
            {
                foreach (var conversation in database.conversations)
                {
                    titles.Add(conversation.Title.Replace("&", "<AMPERSAND>"));
                }
            }
            return titles.ToArray();
        }

        private int GetCurrentConversationIndex()
        {
            if (currentConversation != null)
            {
                if (conversationTitles == null) conversationTitles = GetConversationTitles();
                for (int i = 0; i < conversationTitles.Length; i++)
                {
                    if (string.Equals(currentConversation.Title, conversationTitles[i])) return i;
                }
            }
            return -1;
        }

        private Conversation GetConversationByTitleIndex(int index)
        {
            if (conversationTitles == null) conversationTitles = GetConversationTitles();
            if (0 <= index && index < conversationTitles.Length)
            {
                return database.GetConversation(conversationTitles[index].Replace("<AMPERSAND>", "&"));
            }
            else
            {
                return null;
            }
        }

        public void UpdateConversationTitles()
        {
            conversationTitles = GetConversationTitles();
        }

        private void GotoStartNodePosition()
        {
            var startEntry = currentConversation.GetFirstDialogueEntry();
            if (startEntry == null) return;
            canvasScrollPosition = new Vector2(Mathf.Max(0, startEntry.canvasRect.x - ((position.width - startEntry.canvasRect.width) / 2)), Mathf.Max(0, startEntry.canvasRect.y - 8));
        }

        private void SetSnapToGrid(object data)
        {
            snapToGridAmount = (data == null || data.GetType() != typeof(float)) ? 0 : (float)data;
        }

    }

}