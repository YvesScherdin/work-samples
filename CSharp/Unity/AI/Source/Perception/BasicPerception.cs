using MageGame.AI.Agents;
using MageGame.AI.Core;
using MageGame.Common.Data;
using System.Collections.Generic;
using UnityEngine;

namespace MageGame.AI.Perception
{
    public class BasicPerception
    {
        public AIAgent agent;
        protected AIAgentContext context;

        protected List<GameObject> justPerceived;
        protected List<GameObject> justLost;

        public BasicPerception()
        {
            
        }

        internal void Configure(AIAgentContext context)
        {
            this.context = context;
            Init();
        }

        virtual protected void Init()
        {
            
        }

        virtual public void Activate()
        {
            
        }

        virtual public void Deactivate()
        {

        }
        
        virtual public void Update()
        {
            
        }

        protected bool Register(GameObject whosoever)
        {
            if (justLost.Contains(whosoever))
                justLost.Remove(whosoever);
            else
                justPerceived.Add(whosoever);

            return true;
        }

        protected bool Unregister(GameObject whosoever)
        {
            if (justPerceived.Contains(whosoever))
                justPerceived.Remove(whosoever);
            else
                justLost.Add(whosoever);

            return true;
        }

        protected bool CanPerceive(GameObject gameObject)
        {
            return gameObject.tag != GameObjectTag.Zone
                && gameObject.tag != GameObjectTag.GUI
                && gameObject.tag != GameObjectTag.MainCamera
                && gameObject.tag != GameObjectTag.Relay;
        }
    }
}