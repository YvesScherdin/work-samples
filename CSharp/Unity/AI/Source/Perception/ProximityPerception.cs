using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.Scripting.Triggers;
using System.Collections.Generic;
using UnityEngine;

namespace MageGame.AI.Perception
{
    public class ProximityPerception : BasicPerception
    {
        private float range = 10f;
        private float offset = 2f;

        private SimpleTriggerArea inArea;
        private SimpleTriggerArea outArea;

        public ProximityPerception(float range, float offet)
        {
            this.range = range;
            this.offset = offet;
        }

        protected override void Init()
        {
            base.Init();

            justPerceived = new List<GameObject>();
            justLost = new List<GameObject>();

            Vector3 origin = new Vector3(0f, context.myObjectInfo.GetLocalCenter().y, 0f);
            PerceptionUtil.CreateSensorIn (out inArea,  context.gameObject, origin, range, CanPerceive, Register);
            PerceptionUtil.CreateSensorOut(out outArea, context.gameObject, origin, range + offset, CanPerceive, Unregister);
        }
        
        public override void Activate()
        {
            base.Activate();

            inArea.gameObject.SetActive(true);
            outArea.gameObject.SetActive(true);
        }

        public override void Deactivate()
        {
            base.Deactivate();

            inArea.gameObject.SetActive(false);
            outArea.gameObject.SetActive(false);
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        override public void Update()
        {
            base.Update();

            foreach (GameObject go in justPerceived)
                context.awareness.Notice(go);
            justPerceived.Clear();

            foreach (GameObject go in justLost)
                context.awareness.Unnotice(go);
            justLost.Clear();
        }

    }
}