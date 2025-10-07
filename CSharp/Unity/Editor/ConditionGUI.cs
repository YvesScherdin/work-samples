#if UNITY_EDITOR
using MageGame.Scripting.Logic;
using MageGame.States.Logic;
using MageGame.Utils;
using MageGameEditor.CustomPropertyDrawers;
using MageGameEditor.CustomWindows;
using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MageGameEditor.Scripting
{
    static public class ConditionGUI
    {
        #region creation
        static private string[] parameterTypeChoices;

        static private void CheckOptions()
        {
            if (parameterTypeChoices == null)
            {
                int max = (int)ConditionParameterType.__INTERNAL_Last;
                parameterTypeChoices = new string[max];

                for (int i = 0; i < max; i++)
                {
                    parameterTypeChoices[i] = ((ConditionParameterType)i).ToString().Replace('_', '/');
                }
            }
        }

        static public ConditionParameterType ParameterTypeField(Rect position, ConditionParameterType type)
        {
            CheckOptions();
            int choice = EditorGUI.Popup(position, (int)type, parameterTypeChoices);
            return (ConditionParameterType)choice;
        }


        static public void ShowCreationMenu(Rect buttonRect, ReorderableList list, GenericMenu.MenuFunction2 clickHandler)
        {
            CheckOptions();

            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < parameterTypeChoices.Length; i++)
            {
                menu.AddItem(new GUIContent(parameterTypeChoices[i]), false, clickHandler, (ConditionParameterType)i);
            }
            menu.ShowAsContext();
        }
        #endregion

        #region editing
        internal static bool EditConditionDefinition(Rect pos, string label, LogicConditionDefinition def)
        {
            if (GUI.Button(pos, label))
            {
                def = EditConditionWindow.Show(def);
                return true;
            }

            return false;
        }

        static public void WriteDebugData(Rect rect, LogicConditionDefinition condition)
        {
            EditorGUI.LabelField(rect, "'" + condition.leftValue + "' " + condition.op + " '" + condition.rightValue + "'");
        }

        static public void EditCondition(Rect rect, ILogicCondition condition)
        {
            float shrinkFactor = .9f;
            float offsetY = EditorGUIUtility.singleLineHeight;
            rect.height = EditorGUIUtility.singleLineHeight;

            GUIStyle headlineStyle = EditorStyles.boldLabel;

            Type holderType = condition.GetType();

            rect.width *= .33333f;

            if (holderType.GetField("leftValue") != null)
            {
                EditPropertyField(condition, holderType.GetField("leftValue"), rect.ShrinkX(shrinkFactor), rect.ShrinkX(shrinkFactor).StaticOffset(0f, offsetY));
            }

            rect.x += rect.width;

            if (holderType.GetField("Operator") != null)
            {
                EditorGUI.LabelField(rect.ShrinkX(shrinkFactor), "Operator", headlineStyle);
                EditOperator(rect.ShrinkX(shrinkFactor).StaticOffset(0f, offsetY), condition);
            }

            rect.x += rect.width;

            if (holderType.GetField("rightValue") != null)
            {
                EditPropertyField(condition, holderType.GetField("rightValue"), rect.ShrinkX(shrinkFactor), rect.ShrinkX(shrinkFactor).StaticOffset(0f, offsetY));
            }
        }

        static private void EditPropertyField(object holder, FieldInfo fieldInfo, Rect labelRect, Rect valueRect)
        {
            EditorGUI.LabelField(labelRect, ReflectionUtil.GetFieldTypeName(fieldInfo), EditorStyles.boldLabel);

            object value = fieldInfo.GetValue(holder);
            DefinitionPropertyDrawers.AnyField(valueRect, fieldInfo.FieldType, ref value);
            fieldInfo.SetValue(holder, value);
        }

        static public void EditOperator(Rect rect, ILogicCondition condition)
        {
            if(condition is IFlagCondition)
                ((IFlagCondition)condition).FlagOperator = (FlagOperator)EditorGUI.EnumPopup(rect, ((IFlagCondition)condition).FlagOperator);
            else if(condition is IBooleanCondition)
                condition.Operator = (LogicOperator)EditorGUI.EnumPopup(rect, (BooleanOperator)condition.Operator);
            else
                condition.Operator = (LogicOperator)EditorGUI.EnumPopup(rect, condition.Operator);
        }

        static public void EditButton(Rect rect, string text, System.Action handler)
        {
            if (GUI.Button(rect, text))
                handler();
        }

        #endregion

    }
}
#endif