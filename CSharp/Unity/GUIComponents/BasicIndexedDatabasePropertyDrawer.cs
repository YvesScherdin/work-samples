#if UNITY_EDITOR
using MageGame.Data.DB;
using MageGame.Utils;
using MageGameEditor.CustomComponents.Lists;
using MageGameEditor.CustomMenus;
using MageGameEditor.Utils;
using UnityEditor;
using UnityEngine;

namespace MageGameEditor.CustomPropertyDrawers
{
    public class BasicIndexedDatabasePropertyDrawer<DatabaseType, DataType> : PropertyDrawer
        where DatabaseType : IndexedObjectDatabase<DataType>
        where DataType : UnityEngine.Object
    {
        protected IElementList list;
        protected IndexedObjectDatabase<DataType> database;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (list == null || !property.isExpanded)
                return EditorGUIUtility.singleLineHeight * 1;
            else
                return EditorGUIUtility.singleLineHeight * 3 + list.DetermineTotalHeight();
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect r = position;
            r.height = EditorGUIUtility.singleLineHeight;

            property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(r, property.isExpanded, label.text);
            r.y += r.height;
            EditorGUI.EndFoldoutHeaderGroup();

            if (property.isExpanded)
            {
                if (GUI.Button(r.WithWidth(.5f, true), "Add Missing"))
                {
                    AddMissing();
                    UpdateChanges();
                }

                if (GUI.Button(r.WithOffsetX(.5f, true), "Reset DB"))
                {
                    if (EditorUtility.DisplayDialog("CAUTION: Resetting this Database", "Do you really want to reset the DB?", "Yes, reset!", "No, leave it be."))
                    {
                        ResetDatabase();
                        UpdateChanges();
                        return;
                    }
                }

                r.y += r.height;

                //EditorGUI.LabelField(position, label, EditorStyles.boldLabel);
                if (list == null)
                    list = CreateList(property);

                list.TotalPosition = r;
                list.Update();
                r.y += list.DetermineTotalHeight();
            }
        }

        virtual protected void UpdateChanges()
        {
            
        }

        virtual protected void AddMissing()
        {
            GameDatabaseMenu.CompleteDatabase<IndexedObjectDatabase<DataType>, DataType>(database);
        }

        virtual protected void ResetDatabase()
        {
            GameDatabaseMenu.ResetDatabase<IndexedObjectDatabase<DataType>, DataType>(database);
            //throw new System.NotImplementedException();
        }

        virtual protected IElementList CreateList(SerializedProperty property)
        {
            object data = PropertyDrawerUtility.GetValue(property);
            database = (IndexedObjectDatabase<DataType>)data;
            return new ObjectDatabaseList<DataType>(database, property.serializedObject, property.FindPropertyRelative("items"));
        }
    }
}
#endif