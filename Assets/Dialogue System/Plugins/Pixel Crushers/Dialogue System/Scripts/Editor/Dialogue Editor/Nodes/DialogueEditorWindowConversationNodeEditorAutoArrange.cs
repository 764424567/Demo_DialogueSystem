using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.DialogueEditor
{

    /// <summary>
    /// This part of the Dialogue Editor window handles the auto-arrange
    /// feature for the conversation node editor.
    /// </summary>
    public partial class DialogueEditorWindow
    {

        private const float AutoWidthBetweenNodes = 20f;
        private const float AutoHeightBetweenNodes = 20f;

        private const float AutoStartX = 20f;
        private const float AutoStartY = 20f;

        private void CheckNodeArrangement()
        {
            if (startEntry == null) return;
            if ((startEntry.canvasRect.x == 0) && (startEntry.canvasRect.y == 0)) AutoArrangeNodes(addNewNodesToRight);
        }

        private void ConfirmAndAutoArrangeNodes()
        {
            var result = EditorUtility.DisplayDialogComplex("Auto-Arrange Nodes", "Are you sure you want to auto-arrange the nodes in this conversation?", "Vertically", "Horizontally", "Cancel");
            switch (result)
            {
                case 0:
                    AutoArrangeNodes(true);
                    break;
                case 1:
                    AutoArrangeNodes(false);
                    break;
            }
        }

        private void AutoArrangeNodes(bool vertically)
        {
            InitializeDialogueTree();
            var tree = new List<List<DialogueEntry>>();
            ArrangeGatherChildren(dialogueTree, 0, tree);
            ArrangeTree(tree, vertically);
            ArrangeOrphans(vertically);
            SetDatabaseDirty("Auto-Arrange Nodes");
        }

        private void ArrangeGatherChildren(DialogueNode node, int level, List<List<DialogueEntry>> tree)
        {
            if (node == null) return;
            while (tree.Count <= level)
            {
                tree.Add(new List<DialogueEntry>());
            }
            if (!tree[level].Contains(node.entry)) tree[level].Add(node.entry);
            if (node.hasFoldout)
            {
                for (int i = 0; i < node.children.Count; i++)
                {
                    var child = node.children[i];
                    ArrangeGatherChildren(child, level + 1, tree);
                }
            }
        }

        private float GetTreeWidth(List<List<DialogueEntry>> tree)
        {
            float maxWidth = 0;
            for (int i = 0; i < tree.Count; i++)
            {
                var level = tree[i];
                float levelWidth = level.Count * (DialogueEntry.CanvasRectWidth + AutoWidthBetweenNodes);
                maxWidth = Mathf.Max(maxWidth, levelWidth);
            }
            return maxWidth;
        }

        private float GetTreeHeight(List<List<DialogueEntry>> tree)
        {
            float maxHeight = 0;
            for (int i = 0; i < tree.Count; i++)
            {
                var level = tree[i];
                float levelHeight = level.Count * (DialogueEntry.CanvasRectHeight + AutoHeightBetweenNodes);
                maxHeight = Mathf.Max(maxHeight, levelHeight);
            }
            return maxHeight;
        }

        private void ArrangeTree(List<List<DialogueEntry>> tree, bool vertically)
        {
            if (vertically)
            {
                float treeWidth = GetTreeWidth(tree);
                float x = AutoStartX;
                if (orphans.Count > 0) x += DialogueEntry.CanvasRectWidth + AutoWidthBetweenNodes;
                float y = AutoStartY;
                for (int level = 0; level < tree.Count; level++)
                {
                    ArrangeLevel(tree[level], x, y, treeWidth, 0, vertically);
                    y += DialogueEntry.CanvasRectHeight + AutoHeightBetweenNodes;
                }
            }
            else
            {
                float treeHeight = GetTreeHeight(tree);
                float y = AutoStartY;
                if (orphans.Count > 0) y += DialogueEntry.CanvasRectHeight + AutoHeightBetweenNodes;
                float x = AutoStartX;
                for (int level = 0; level < tree.Count; level++)
                {
                    ArrangeLevel(tree[level], x, y, 0, treeHeight, vertically);
                    x += DialogueEntry.CanvasRectWidth + AutoWidthBetweenNodes;
                }
            }
        }

        private void ArrangeLevel(List<DialogueEntry> nodes, float x, float y, float treeWidth, float treeHeight, bool vertically)
        {
            if (nodes == null || nodes.Count == 0) return;
            if (vertically)
            {
                float nodeCanvasWidth = treeWidth / nodes.Count;
                float nodeCanvasOffset = (nodeCanvasWidth - DialogueEntry.CanvasRectWidth) / 2;
                for (int i = 0; i < nodes.Count; i++)
                {
                    float nodeX = x + (i * nodeCanvasWidth) + nodeCanvasOffset;
                    nodes[i].canvasRect = new Rect(nodeX, y, DialogueEntry.CanvasRectWidth, DialogueEntry.CanvasRectHeight);
                }
            }
            else
            {
                float nodeCanvasHeight = treeHeight / nodes.Count;
                float nodeCanvasOffset = (nodeCanvasHeight - DialogueEntry.CanvasRectHeight) / 2;
                for (int i = 0; i < nodes.Count; i++)
                {
                    float nodeY = y + (i * nodeCanvasHeight) + nodeCanvasOffset;
                    nodes[i].canvasRect = new Rect(x, nodeY, DialogueEntry.CanvasRectWidth, DialogueEntry.CanvasRectHeight);
                }
            }
        }

        private void ArrangeOrphans(bool vertically)
        {
            if (vertically)
            {
                float y = AutoStartY;
                foreach (var orphan in orphans)
                {
                    orphan.entry.canvasRect.x = AutoStartX;
                    orphan.entry.canvasRect.y = y;
                    y += orphan.entry.canvasRect.height + AutoHeightBetweenNodes;
                }
            }
            else
            {
                float x = AutoStartX;
                foreach (var orphan in orphans)
                {
                    orphan.entry.canvasRect.x = x;
                    x += orphan.entry.canvasRect.width + AutoWidthBetweenNodes;
                    orphan.entry.canvasRect.y = AutoStartY;
                }
            }
        }

    }

}