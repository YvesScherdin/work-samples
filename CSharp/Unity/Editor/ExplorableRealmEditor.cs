#if UNITY_EDITOR
using GataryLabs.Localization;
using MageGame.Data.DB;
using MageGame.Data.World;
using MageGame.World.Data;
using MageGame.World.Maps;
using MageGameEditor.Core;
using MageGameEditor.CustomComponents;
using MageGameEditor.CustomWindows;
using MageGameEditor.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MageGameEditor.CustomEditors
{
    [CustomEditor(typeof(ExplorableRealm))]
    public class ExplorableRealmEditor : Editor
    {
        static private bool invalid;

        private List<WorldSceneID> availableSceneIDs;

        static internal void InvalidateSceneIDs()
        {
            invalid = true;
        }

        static private int CompareWorldSceneIDs(WorldSceneID a, WorldSceneID b)
        {
            int i = a.ToString().CompareTo(b.ToString());
            return i == 0 ? 0 : (i < 0 ? -1 : 1);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            ExplorableRealm realm = (ExplorableRealm)target;

            SerializedProperty sceneIDProperty = serializedObject.FindProperty("sceneID");

            if(availableSceneIDs == null || invalid)
            {
                WorldRegionID regionID = EditorSceneLinker.AnticipateRegion();
                string pattern = regionID.ToString().Replace("World_", "");

                if (string.IsNullOrEmpty(pattern))
                    pattern += "_";

                availableSceneIDs = new List<WorldSceneID>();
                var allEnumValues = Enum.GetValues(typeof(WorldSceneID));

                foreach (WorldSceneID sceneID in allEnumValues)
                {
                    if (string.IsNullOrEmpty(pattern) || sceneID.ToString().IndexOf(pattern) == 0)
                        availableSceneIDs.Add(sceneID);
                }

                availableSceneIDs.Sort(CompareWorldSceneIDs);

                invalid = false;
            }

            WorldSceneID selectedSceneID = (WorldSceneID)sceneIDProperty.intValue;
            WorldSceneID newSelectedSceneID = WorldLocationComponents.SceneSelection("Scene", (WorldSceneID)sceneIDProperty.intValue, availableSceneIDs);

            if (selectedSceneID != newSelectedSceneID)
            {
                sceneIDProperty.intValue = Convert.ToInt32(newSelectedSceneID);

                Debug.Log("Change: " + selectedSceneID + "= " + newSelectedSceneID + " | " + ((WorldSceneID)sceneIDProperty.intValue) + " | " + sceneIDProperty.intValue);

                realm.sceneID = newSelectedSceneID;
                EditorUtility.SetDirty(realm);
            }

            //EditorGUILayout.EnumFlagsField()

            WorldMapSegment segment = realm.segment;
            WorldMapSegment selectedSegment = (WorldMapSegment)EditorGUILayout.EnumPopup("Segment", segment);

            if (segment != selectedSegment)
            {
                realm.segment = selectedSegment;
                EditorUtility.SetDirty(realm);
            }

            if (GUILayout.Button("Select and edit..."))
            {
                string path = Path.Combine(ResourcePath.WorldDirectory, realm.sceneID.ToString());
                UnityEngine.Object asset = Resources.Load<UnityEngine.Object>(path);
                //Debug.Log(asset + " | " + path);

                if (SelectionUtil.ConfigureAsset(asset) == 1)
                {
                    if (EditorUtility.DisplayDialog("Note", "Does not exist. Shall create scene '"+ newSelectedSceneID + "'?" , "Yes", "No"))
                    {
                        EditorSceneLinker.CreateScene(newSelectedSceneID);
                    }
                }
            }

            if (GUILayout.Button("Localize title..."))
            {
                string name = LocalizationChanger.RetrieveSafe(realm.sceneID.ToString(), LanguageCategory.WorldSceneTitle);

                // only default language for now
                name = EditorInputDialog.Show("Edit scene name", "Thy name shall be:", name, "Accept", "Cancel");
                LocalizationChanger.Change(realm.sceneID.ToString(), LanguageCategory.WorldSceneTitle, name);
            }
        }

    }
}
#endif