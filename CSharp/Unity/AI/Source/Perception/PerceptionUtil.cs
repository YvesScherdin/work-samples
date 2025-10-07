using MageGame.Collisions.Behaviours;
using MageGame.Common.Data;
using MageGame.Scripting.Triggers;
using UnityEngine;

namespace MageGame.AI.Perception
{
    static public class PerceptionUtil
    {
        static public void CreateSensorIn(out SimpleTriggerArea triggerArea, GameObject parent, Vector3 position, float radius, ProximtyCheck filter, ProximtyCheck inHandler)
        {
            triggerArea = TriggerAreaUtil.CreateCircle(CreateSubObject(GameObjectName.Perception + "_In", parent, position), radius);
            triggerArea.proximity.AddHandles(filter, inHandler, null);
        }

        static public void CreateSensorOut(out SimpleTriggerArea triggerArea, GameObject parent, Vector3 position, float radius, ProximtyCheck filter, ProximtyCheck outHandler)
        {
            triggerArea = TriggerAreaUtil.CreateCircle(CreateSubObject(GameObjectName.Perception + "_Out", parent, position), radius);
            triggerArea.proximity.AddHandles(filter, null, outHandler);
        }
        
        static public void CreateSensor(out SimpleTriggerArea triggerArea, GameObject parent, Vector3 position, float radius, ProximtyCheck filter, ProximtyCheck inHandler, ProximtyCheck outHandler)
        {
            triggerArea = TriggerAreaUtil.CreateCircle(CreateSubObject(GameObjectName.Perception, parent, position), radius);
            triggerArea.proximity.AddHandles(filter, inHandler, outHandler);
        }


        static public ProximityCollector CreateSensor(out SimpleTriggerArea triggerArea, bool isInSensor, GameObject parent, Vector3 position, float radius)
        {
            triggerArea = TriggerAreaUtil.CreateCircle(CreateSubObject(GameObjectName.Perception + "_" + (isInSensor ? "In" : "Out"), parent, position), radius);
            return triggerArea.proximity;
        }

        static public ProximityCollector CreateSensor(out SimpleTriggerArea triggerArea, string name, GameObject parent, Vector3 position, float radius)
        {
            triggerArea = TriggerAreaUtil.CreateCircle(CreateSubObject(name, parent, position), radius);
            return triggerArea.proximity;
        }

        static public GameObject CreateSubObject(string name, GameObject parent, Vector3 position)
        {
            GameObject sub = new GameObject();
            sub.name = name;
            sub.tag = GameObjectTag.Zone;
            sub.transform.SetParent(parent.transform, true);
            sub.transform.localPosition = position;

            return sub;
        }
    }
}
