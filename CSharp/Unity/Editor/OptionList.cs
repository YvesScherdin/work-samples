#if UNITY_EDITOR
using MageGame.Scripting.Logic;
using MageGame.State.Conversations;
using MageGame.Utils;
using MageGameEditor.CustomPropertyDrawers;
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MageGameEditor.CustomComponents.Lists
{
    internal class OptionList : ElementList<ConversationNode, ConversationOption>
    {
        private ConversationTree tree;
        private ConversationBlock block;

        public OptionList(ConversationTree tree, SerializedObject treeObject, ConversationBlock block, ConversationNode node) : base(node, treeObject)
        {
            this.tree = tree;
            this.block = block;

            GenerateList();
        }

        protected override SerializedProperty ExtractProperty()
        {
            int blockIndex = Array.IndexOf(tree.blocks, block);
            int nodeIndex = Array.IndexOf(block.nodes, parent);

            SerializedProperty p = holderObject.FindProperty("blocks").GetArrayElementAtIndex(blockIndex);
            p = p.FindPropertyRelative("nodes").GetArrayElementAtIndex(nodeIndex);
            p = p.FindPropertyRelative("options");

            return p;
        }

        #region element management
        protected override void HandleAdd(ReorderableList list)
        {
            ConversationOption o = new ConversationOption();
            o.rawConditions = new LogicConditionDefinition[0];
            o.id = FindUniqueElementName("option", parent.GetOptionIDs());

            Array.Resize(ref parent.options, parent.options.Length + 1);
            parent.options[parent.options.Length - 1] = o;

            base.HandleAdd(list);
        }

        protected override void HandleRemove(ReorderableList list)
        {
            ConversationOption o = parent.options[list.index];
            CheckRemoveChild(!o.IsEmpty(), list);
            base.HandleRemove(list);
        }
        #endregion

        #region drawing
        override protected void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Options");
        }

        override protected void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.LabelField(rect, new GUIContent(parent.options[index].id));
            ConversationTreeFields.EditOptionID(rect.WithOffsetX(.6f, true), "Rename", parent.options[index], parent);

            EditorGUILayout.EndHorizontal();
        }

        override protected float DetermineElementHeight(int index)
        {
            return ElementListStyle.nodeHeight;
        }

        public override float DetermineTotalHeight()
        {
            return parent.options.Length * ElementListStyle.nodeHeight + ElementListStyle.listAdditionHeight;
        }
        #endregion
    }


}
#endif