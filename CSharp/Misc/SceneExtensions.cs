using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MageGame.Utils
{
    static public class SceneExtensions
    {
        static public List<Component> GetAllSceneComponents(this Scene scene, Type type)
        {
            List<Component> list = new List<Component>();
            GameObject[] rootObjects = scene.GetRootGameObjects();

            for (int i = 0; i < rootObjects.Length; i++)
            {
                Component[] components = rootObjects[i].GetComponentsInChildren(type);
                list.AddRange(components);
            }

            return list;
        }

        static public Component GetSceneComponent(this Scene scene, Type type)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();

            for (int i = 0; i < rootObjects.Length; i++)
            {
                Component component = rootObjects[i].GetComponentInChildren(type);
                if (component != null)
                    return component;
            }

            return null;
        }
        
        static public T GetSceneComponent<T>(this Scene scene) where T : Component
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();

            for (int i = 0; i < rootObjects.Length; i++)
            {
                Component component = rootObjects[i].GetComponentInChildren<T>();
                if (component != null)
                    return (T)(component);
            }

            return null;
        }

        static public List<T> GetAllSceneComponents<T>(this Scene scene, bool includeInactive=false) // where T : Component
        {
            List<T> list = new List<T>();
            GameObject[] rootObjects = scene.GetRootGameObjects();

            for (int i = 0; i < rootObjects.Length; i++)
            {
                T[] components = rootObjects[i].GetComponentsInChildren<T>(includeInactive);
                list.AddRange(components);
            }

            return list;
        }

        /// <summary>
        /// This is actually a dangerous method.
        /// Specified Type t must be either same like or sub class of T. There is no compile time check for this!
        /// 
        /// TICKET: #1
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scene"></param>
        /// <param name="type"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        //[Obsolete("Replace by ...")]
        static public List<T> GetAllSceneComponents<T>(this Scene scene, Type type, List<T> result=null) where T : Component
        {
            if (result == null)
                result = new List<T>();

            GameObject[] rootObjects = scene.GetRootGameObjects();

            for (int i = 0; i < rootObjects.Length; i++)
            {
                Component[] components = rootObjects[i].GetComponentsInChildren(type);

                if (components.Length != 0)
                {
                    for (int j = 0; j < components.Length; j++)
                    {
                        result.Add((T)components[j]); // TODO: make safer! (e.g. add typecheck in front of loop)
                    }
                }
            }

            return result;
        }

        static public void SetActive(this Scene scene, bool value)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();

            for (int i = 0; i < rootObjects.Length; i++)
            {
                rootObjects[i].SetActive(value);
            }
        }
    }
}
