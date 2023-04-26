using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.AI.States;
using MageGame.Behaviours.Ability;
using MageGame.Behaviours.EntityType;
using MageGame.Behaviours.EntityType.Furniture;
using MageGame.Behaviours.Mechanisms;
using MageGame.Core;
using MageGame.Data;
using MageGame.Data.Items;
using MageGame.Data.Residences;
using MageGame.Items;
using MageGame.Utils;
using MageGame.World.Data;
using System.Collections.Generic;
using UnityEngine;

namespace MageGame.AI.Agents.Default
{
    [System.Serializable]
    public class AIStorageParameters : AIActionParameters
    {
        public BasicItemStorageContainer container;
        public List<ItemStack> items;
        public bool drop;

        static public AIStorageParameters Create(BasicItemStorageContainer container, bool drop, List<ItemStack> items=null)
        {
            AIStorageParameters parameters = new AIStorageParameters();
            parameters.container = container;
            parameters.items = items;
            parameters.drop = drop;
            return parameters;
        }
    }

    public class AIAS_StoreItems : AIActionState
    {
        public override AIActionType ActionType => AIActionType.Store;

        private Inventory inventory;
        private AIStorageParameters concreteParams;
        private ObjectUser objectUser;
        private bool success;

        public override void Initialize()
        {
            base.Initialize();

            inventory = InventoryUtil.GetOrCreateFor(context.gameObject);
            objectUser = context.gameObject.GetComponent<ObjectUser>();
        }

        public override void Enter()
        {
            base.Enter();

            success = false;

            if (parameters != null && parameters is AIStorageParameters)
            {
                concreteParams = (AIStorageParameters)parameters;
            }
            else
            {
                concreteParams = AIStorageParameters.Create(null, true, null);
            }

            if (concreteParams.container != null)
            {
                context.MakeCurrentTarget(new AITargetInfo().Analyze(concreteParams.container.gameObject));
                context.targetInvalid = false;

                if (concreteParams.container.IsInUse())
                {
                    TransferItems();
                    Interrupt();
                }
            }
            else
            {
                TransferItems();
                Interrupt();
            }

            if(!WasInterrupted())
            {
                executionCoroutine = agent.StartCoroutine(ExecuteCoroutine());
            }
        }

        public override void Execute()
        {
            base.Execute();

            if (context.targetInvalid)
                Interrupt(success ? AIActionSituation.None : AIActionSituation.Failed);
        }

        private System.Collections.IEnumerator ExecuteCoroutine()
        {
            yield return null;

            if (concreteParams.container != null)
            {
                while(!checks.nearObject.Check())
                {
                    context.movement.Move(false);
                    yield return new WaitUntil(context.movement.HasStoppedMovement);
                }
            }

            context.movement.AbortMovement();

            if (concreteParams.container != null)
            {
                objectUser.UseObject(concreteParams.container);
            }

            yield return new WaitForSeconds(.5f);

            TransferItems();
            success = true;
            yield return new WaitForSeconds(.25f);

            if (concreteParams.container != null && concreteParams.container.IsUsedBy(objectUser.gameObject))
            {
                concreteParams.container.AbortUse();
            }

            Interrupt();
        }

        private void TransferItems()
        {
            if (concreteParams.items == null)
            {
                concreteParams.items = ItemUtil.Clone(inventory.allItems);
            }

            inventory.RemoveItems(concreteParams.items.ToArray());

            if (concreteParams.container == null)
            {
                // just drop the items
                List<CollectableItem> list = new List<CollectableItem>(ItemGenerator.ToCollectables(concreteParams.items));
                CollectableItemUtil.Arrange(list, agent.context.myObjectInfo.collider, EmitSpawnAreaType.Center, agent.transform.parent.gameObject);

                bool persistent = WorldResidenceUtil.IsInOwnResidence();
                CollectableItemUtil.Spread(list, 1f, persistent, persistent ? GameSettings.ItemDespawnDelayAtResidence : TimeLimitType.Default);
            }
            else
            {
                concreteParams.container.Inventory.AddItems(concreteParams.items.ToArray());
            }
        }

        public override void Exit()
        {
            if(context.movement.IsMoving())
                context.movement.AbortMovement();

            /*if (executionCoroutine != null)
            {
                agent.StopCoroutine(executionCoroutine);
                executionCoroutine = null;
            }
*/
            base.Exit();

        }
    }
}
