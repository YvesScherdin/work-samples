using MageGame.Behaviours.EntityType.Furniture;
using MageGame.Controls;
using MageGame.Data;
using MageGame.Data.Items;
using MageGame.GUI.Actions;
using MageGame.GUI.Components.Garden;
using System.Collections;
using UnityEngine;

namespace MageGame.GUI.Components
{
    public class GardenCellInteractionManager : BasicCellInteractionManager<GardenPlotChangeAction>
    {
        [Header("Cell containers")]
        public InventoryView optionCells;
        public GardenPlotCellView selectionCells;

        private IGardenActionHandler actionHandler;
        private GardenPlot garden;
        private Inventory inventory;

        #region de-/init
        override protected void Start()
        {
            base.Start();

            optionCells.SetInteractionManager(this);
            selectionCells.SetInteractionManager(this);

            if (action == null)
                action = new GardenPlotChangeAction();

            action.itemCellView = optionCells;
            action.gardenCellView = selectionCells;
            action.Reset();

            if (garden != null)
                action.garden = garden;

            if (actionHandler != null)
                action.handler = actionHandler;
        }

        public void SetGardenPlot(GardenPlot garden, Inventory inventory, IGardenActionHandler actionHandler)
        {
            this.garden = garden;
            this.inventory = inventory;
            this.actionHandler = actionHandler;

            if (action != null)
            {
                action.garden = garden;
                action.handler = actionHandler;
            }
        }
        #endregion

        #region dragging

        protected override bool IsDraggable(CellInteractivity content)
        {
            if (content.contentData is ItemStack)
                return true;
            else if (content.contentData is GardenPlotSpot)
            {
                GardenPlotSpot spot = (GardenPlotSpot)content.contentData;
                return !spot.IsEmpty();
            }

            return false;
        }

        public override Sprite GetItemDragGraphic(CellInteractivity content)
        {
            if (content.contentData is Item)
                return ((Item)content.contentData).icon;
            else if (content.contentData is ItemStack)
                return ((ItemStack)content.contentData).item.icon;
            else if (content.contentData is GardenPlotSpot)
                return ((GardenPlotSpot)content.contentData).Icon;

            return null;
        }
        #endregion

        #region event handlers
        protected override void NotifyChange()
        {
            PlayerStoryAPI.NotifyChange(GameProgressChangeType.Tools | GameProgressChangeType.Inventory);
        }
        #endregion
    }

    public interface IGardenActionHandler
    {
        void RemovePlant(GardenPlotSpot spot);
        void ReplacePlant(ItemStack item, GardenPlotSpot spot);
        void AddPlant(ItemStack sourceStack, GardenPlotSpot targetSlot);
    }

    public class GardenPlotChangeAction : BasicCellsChangeAction
    {
        internal GardenPlot garden;
        internal InventoryView itemCellView;
        internal GardenPlotCellView gardenCellView;
        internal IGardenActionHandler handler;

        private ItemStack sourceItem;
        private ItemCell sourceItemCell;
        
        private ItemStack targetItem;
        private ItemCell targetItemCell;

        private GardenPlotSpot targetSpot;
        private GardenPlotCell targetSpotCell;
        
        private GardenPlotSpot sourceSpot;
        private GardenPlotCell sourceSpotCell;

        private bool fromInventory;
        private bool toInventory;

        private Inventory inventory => itemCellView.GetInventory();


        public override void Reset()
        {
            base.Reset();

            sourceItem = null;
            sourceItemCell = null;

            targetItem = null;
            targetItemCell= null;

            targetSpot = null;
            targetSpotCell = null;

            sourceSpot = null;
            sourceSpotCell = null;

            fromInventory = false;
            toInventory = false;
        }

        public override void SetSource(CellInteractivity source)
        {
            base.SetSource(source);

            fromInventory = source != null && source.cell is ItemCell;

            if (source != null)
            {
                if (source.cell is GardenPlotCell)
                {
                    sourceSpotCell = (GardenPlotCell)source.cell;
                    sourceSpot = sourceSpotCell.GetContent();
                    fromInventory = false;
                }
                else
                {
                    sourceItemCell = (ItemCell)source.cell;
                    sourceItem = sourceItemCell.GetContent();
                    fromInventory = true;
                }
            }
            else
            {
                sourceItem = null;
            }
        }

        public override bool ChangeTarget(CellInteractivity target)
        {
            if (base.ChangeTarget(target))
            {
                if (target != null)
                {
                    if (target.cell is ItemCell)
                    {
                        targetItemCell = (ItemCell)target.cell;
                        targetItem = targetItemCell.GetItemStack();
                        toInventory = true;
                    }
                    else if(target.cell is GardenPlotCell)
                    {
                        targetSpotCell = ((GardenPlotCell)target.cell);
                        targetSpot = targetSpotCell.GetContent();
                        toInventory = false;
                    }
                    else // could be status cell
                    {
                        targetItemCell = null;
                        targetItem = null;
                        toInventory = true;
                    }
                }
                return true;
            }
            else
                return false;
        }

        private GardenPlotCell FindTargetCellFor(Item item)
        {
            for(int i=0; i<gardenCellView.cells.Length; i++)
            {
                if (gardenCellView.cells[i].GetContent().IsEmpty())
                    return gardenCellView.cells[i];
            }

            return null;
        }

        override public void Evaluate()
        {
            problematic = null;

            if (source == null || target == source)
            {
                type = CellActionType.None;
            }
            else
            {
                if (target != null && (target.ContentSource == null || target.ContentSource.CellContentID == CellContentSourceID.ParallelApplication))
                {
                    type = CellActionType.None;
                }
                else
                {
                    switch (source.ContentSource.CellContentID)
                    {
                        case CellContentSourceID.AvailableOptions:

                            //////////////////////////////////////
                            ////   DRAGGING FROM INVENTORY    ////
                            //////////////////////////////////////

                            if (quickAction)
                            {
                                if( sourceItem.item is SeedsItem)
                                {
                                    if (target == null)
                                    {
                                        targetSpotCell = FindTargetCellFor(sourceItem.item);
                                        if (targetSpotCell == null)
                                        {
                                            targetSpot = null;
                                            type = CellActionType.None;
                                        }
                                        else
                                        {
                                            targetSpot = targetSpotCell.GetContent();
                                            target = targetSpotCell.cellInteractivity;
                                            type = CellActionType.Set;
                                        }
                                    }
                                    else
                                        type = CellActionType.Set;
                                }
                                else
                                {
                                    NotifyFail(); // can neither equip nor do anything else with it.
                                    type = CellActionType.None;
                                }
                            }
                            else
                            {
                                // dropped somewhere
                                if (target == null)
                                {
                                    // TODO: anticipate target

                                    // do nothing for now.
                                    type = CellActionType.None;
                                }
                                else if (target.ContentSource.CellContentID == CellContentSourceID.AvailableOptions)
                                {
                                    ItemCell targetCell = (ItemCell)target.cell;

                                    if (sourceItem == targetItem && !targetCell.GetContent().IsFull())
                                        type = CellActionType.Combine;
                                    else
                                        type = CellActionType.None;
                                }
                                else
                                {
                                    // dragged to plot spots
                                    if (targetSpot.CanBePlanted(sourceItem.item))
                                    {
                                        if (!targetSpotCell.GetContent().IsEmpty())
                                            type = CellActionType.Replace;
                                        else
                                            type = CellActionType.Set;
                                    }
                                    else
                                    {
                                        NotifyFail();
                                        type = CellActionType.None;
                                    }
                                }
                            }

                            break;

                        case CellContentSourceID.Application:

                            //////////////////////////////////////
                            ////   DRAGGING FROM GARDEN PLOTS ////
                            //////////////////////////////////////

                            if (quickAction)
                            {
                                // clear/harvest
                                if (sourceSpot != null && sourceSpot.CanBeCleared())
                                {
                                    type = CellActionType.Unset;
                                    toInventory = true;
                                }
                                else
                                {
                                    type = CellActionType.None;
                                }
                            }
                            else
                            {
                                // dragging from selection
                                if (target == null || target.ContentSource.CellContentID == CellContentSourceID.AvailableOptions)
                                {
                                    // dragging to nowhere
                                    type = CellActionType.Unset;
                                    toInventory = true;
                                }
                                else
                                {
                                    // dragging to other equipment cell
                                    if (sourceSpot.CanBPlantBeSwapped() && targetSpot.CanBePlanted(sourceSpot.plant))
                                    {
                                        // swap or replace
                                        if (targetSpot.IsEmpty())
                                            type = CellActionType.Move;
                                        else if (sourceSpot.CanBePlanted(targetSpot.plant))
                                            type = CellActionType.Swap;
                                        else
                                        {
                                            //type = CellActionType.Replace;
                                            problematic = target;

                                            // do nothing. invalid move.
                                            type = CellActionType.None;
                                            NotifyFail();
                                        }
                                    }
                                    else
                                    {
                                        problematic = target;

                                        // do nothing. invalid move.
                                        type = CellActionType.None;
                                        NotifyFail();
                                    }
                                }
                            }
                            break;

                    }
                }
            }

            NotifyIntendedAction();
        }

        override public bool Execute()
        {
            ResetVisuals();

            switch (type)
            {
                case CellActionType.None:
                    // do nothing
                    break;

                case CellActionType.Set:

                    if (fromInventory && !toInventory)
                        ApplyItem(sourceItem, targetSpot);
                    else
                        NotifyUnhandledCase();
                    break;


                case CellActionType.Unset:

                    if (!fromInventory && toInventory)
                        ClearPlot(sourceSpot);
                    else
                        NotifyUnhandledCase();
                    break;


                case CellActionType.Move:
                    if (!fromInventory && !toInventory)
                        MovePlants(sourceSpot, targetSpot);
                    else
                        NotifyUnhandledCase();

                    break;

                case CellActionType.Replace:
                    if (!fromInventory && !toInventory)
                        ReplacePlant(sourceItem, targetSpot);
                    else if (fromInventory && !toInventory)
                        ApplyItem(sourceItem, targetSpot);
                    else
                        NotifyUnhandledCase();
                    break;

                case CellActionType.Swap:
                    if (!fromInventory && !toInventory)
                        SwapPlants(sourceSpot, targetSpot);
                    else
                        NotifyUnhandledCase();

                    break;

                case CellActionType.Use:
/*
                    if (fromInventory)
                        ApplyItem(sourceStack);
                    else*/
                        NotifyUnhandledCase();

                    break;

                case CellActionType.Combine:
                    if (fromInventory && toInventory)
                        CombineItems(sourceItem, targetItem);
                    else
                        NotifyUnhandledCase();

                    break;
            }

            if (type != CellActionType.None)
                UpdateVisuals();

            return type != CellActionType.None;
        }

        #region actual actions
        private void CombineItems(ItemStack sourceStack, ItemStack targetStack)
        {
            NotifyDebug();
        }

        private void MovePlants(GardenPlotSpot sourceSpot, GardenPlotSpot targetSpot)
        {
            // moving
            if (sourceSpot.IsEmpty())
            {
                Debug.LogWarning("Source spot is empty: " + sourceSpot);
                return;
            }
            if (!targetSpot.IsEmpty())
            {
                Debug.LogWarning("Target spot is not empty: " + targetSpot);
                return;
            }

            if(!garden.MovePlant(sourceSpot, targetSpot))
                NotifyFail();
        }


        private void ReplacePlant(ItemStack item, GardenPlotSpot spot)
        {
            if (spot.IsEmpty())
            {
                Debug.LogWarning("target spot is empty: " + spot);
                return;
            }

            handler.ReplacePlant(item, spot);
        }

        private void SwapPlants(GardenPlotSpot sourceSpot, GardenPlotSpot targetSpot)
        {
            if (targetSpot.IsEmpty() || sourceSpot.IsEmpty())
            {
                Debug.LogWarning("Cannot replace between Slots if one is empty: " + sourceSpot + " | " + targetSpot);
                return;
            }

            if(sourceSpot == targetSpot)
            {
                Debug.LogWarning("Cannot replace ");
                return;
            }

            garden.SwapPlants(sourceSpot, targetSpot);
        }


        private void ClearPlot(GardenPlotSpot from)
        {
            if (from == null || from.IsEmpty())
            {
                Debug.LogWarning("Slot null or empty: " + from);
                return;
            }

            handler.RemovePlant(from);

            //UpdateVisuals();
        }

        private void ApplyItem(ItemStack sourceStack, GardenPlotSpot targetSpot)
        {
            if (!targetSpot.IsEmpty())
            {
                handler.ReplacePlant(sourceStack, targetSpot);
                return;
            }

            handler.AddPlant(sourceStack, targetSpot);
        }

        #endregion

        #region visualization

        override public void Visualize()
        {
            if (problematic != null) { Colorize(problematic, Color.red); }

            switch (type)
            {
                case CellActionType.None:
                    if (source != null) Colorize(source, Color.grey);
                    break;

                case CellActionType.Set:
                    if (source != null) Colorize(source, Color.yellow);
                    if (target != null) Colorize(target, Color.green);
                    break;

                case CellActionType.Unset:
                    if (source != null) Colorize(source, Color.red);
                    break;

                case CellActionType.Move:
                    if (source != null) Colorize(source, Color.grey);
                    if (target != null) Colorize(target, Color.green);
                    break;

                case CellActionType.Replace:
                    if (source != null) Colorize(source, Color.grey);
                    if (target != null) Colorize(target, Color.yellow);
                    break;

                case CellActionType.Swap:
                    if (source != null) Colorize(source, Color.yellow);
                    if (target != null) Colorize(target, Color.yellow);
                    break;
            }

        }

        override public void ResetVisuals()
        {
            if (source != null) Colorize(source, Color.white);
            if (target != null) Colorize(target, Color.white);
            if (problematic != null) Colorize(problematic, Color.white);
        }

        override protected void UpdateVisuals()
        {
            itemCellView.Refresh();
            gardenCellView.StartCoroutine(RefresGardenPlotView());
        }

        private IEnumerator RefresGardenPlotView()
        {
            yield return null;
            yield return new WaitForSeconds(.05f);
            gardenCellView.Refresh();
        }

        protected override void Colorize(CellInteractivity fcc, Color color)
        {
            if (fcc.cell is IItemCell)
            {
                base.Colorize(fcc, color);
            }
            else if(fcc.cell is GardenPlotCell)
            {
                GardenPlotCell cell = (GardenPlotCell)fcc.cell;
                cell.borders.color = color;
            }
        }

        #endregion

        #region notifications
        private void NotifyUnhandledCase()
        {
            Debug.Log("Unhandled case | " + type + " | " + fromInventory + ", " + toInventory);
        }

        private void NotifyIntendedAction()
        {
            //Debug.Log("Would do now: " + type + " | " + fromInventory + ", " + toInventory);
        }

        private void NotifyDebug()
        {
            Debug.Log("Doing now: " + type + " | " + fromInventory + ", " + toInventory);
        }
        #endregion

        
    }
}