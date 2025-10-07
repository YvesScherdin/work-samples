#if UNITY_EDITOR
using MageGame.Data.Items;
using MageGameEditor.Core;
using MageGameEditor.CustomComponents;
using MageGameEditor.Data;
using MageGameEditor.Utils;
using UnityEditor;
using UnityEngine;

namespace MageGameEditor.CustomEditors
{
    [CustomEditor(typeof(Item), true)]
    public class ItemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            Item item = (Item)target;

            GUIContent content = new GUIContent(LocalizationChanger.RetrieveItemTitle(item));
            GUILayout.Label(content, EditorStyles.boldLabel);

            content = new GUIContent((item != null && item.icon != null) ? MageEditorGUICache.GetItemIconTexture(item) : null);
            GUILayout.Label(content);
            IconFields.Change(item.icon, "items_", (Sprite newIcon) => { item.icon = newIcon; EditorUtility.SetDirty(item); MageEditorGUICache.Clear(item); });
            GUILayout.Space(10f);

            LocalizationFields.Edit(new LocalizationWrapper_ItemTitle(item), "Title");
            GUILayout.Space(10f);
            LocalizationFields.Edit(new LocalizationWrapper_ItemDescription(item), "Description");
            GUILayout.Space(10f);

            base.OnInspectorGUI();
        }
    }
}
#endif