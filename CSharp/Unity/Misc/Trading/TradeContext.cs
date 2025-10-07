using MageGame.Actions.ToolBased;
using MageGame.Behaviours.EntityType.Professions;
using MageGame.Data;
using MageGame.Data.Items;
using MageGame.Data.World;
using MageGame.GUI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MageGame.Actions.PersonBased
{
    public class TradeContext : InteractionContext, IItemCostComputer
    {
        public TraderParty customer;
        public TraderParty service;

        public Trader trader;

        internal CultureData traderCulture;

        private float buyModifier;
        public float BuyModifier => buyModifier;

        private float sellModifier;
        public float SellModifier => sellModifier;

        public TradeContext(GameObject user, Trader trader) : base(InteractionPurposeType.Trade, user, trader.gameObject)
        {
            this.trader = trader;

            customer = TraderParty.FromCustomer(user, null);
            service = TraderParty.FromTrader(trader, null);

            RefreshModifiers();
        }

        public void Begin()
        {
            TooltipGenerator.itemCostComputer = this;
        }

        public void End()
        {
            TooltipGenerator.itemCostComputer = null;
        }

        public override void Abort()
        {
            trader.EndService();
        }

        #region actions

        public ItemTradeResult Buy(ItemStack itemStack, int amount)
        {
            if (!CanBuy(itemStack.item, amount))
            {
                Debug.LogError("Cannot buy that item: " + itemStack.item + " " + amount);
                return null;
            }

            ItemTradeResult result = new ItemTradeResult();
            result.goldDelta = ComputeBuyCost(itemStack.item, amount);

            result.itemStackDelta = customer.inventory.NumItemStacks;
            customer.inventory.AddItem(itemStack.item, amount, result.targetStacks = new List<ItemStack>());
            service.inventory.ReduceItem(itemStack, amount);
            result.sourceStack = itemStack;
            result.itemStackDelta = customer.inventory.NumItemStacks - result.itemStackDelta;

            customer.Gold -= result.goldDelta;
            service.Gold += result.goldDelta;

            return result;
        }

        public ItemTradeResult Sell(ItemStack itemStack, int amount)
        {
            if (!CanSell(itemStack.item, amount))
            {
                Debug.LogError("Cannot sell that item: " + itemStack.item + " " + amount);
                return null;
            }

            ItemTradeResult result = new ItemTradeResult();
            result.goldDelta = ComputeSellCost(itemStack.item, amount);

            result.newSourceStackSize = itemStack.amount - amount;
            result.itemStackDelta = service.inventory.NumItemStacks;
            service.inventory.AddItem(itemStack.item, amount, result.targetStacks = new List<ItemStack>());
            customer.inventory.ReduceItem(itemStack, amount);
            result.sourceStack = itemStack;
            result.itemStackDelta = service.inventory.NumItemStacks - result.itemStackDelta;

            service.Gold -= result.goldDelta;
            customer.Gold += result.goldDelta;

            return result;
        }

        public bool CanBuy(Item item, int amount)
        {
            return customer.Gold >= ComputeBuyCost(item, amount);
        }

        public bool CanSell(Item item, int amount)
        {
            // TODO: add unique item-check (if we get something like that)
            return service.Gold >= ComputeSellCost(item, amount);
        }

        internal bool IsRefusedAtAll()
        {
            return false;
        }

        public int ComputeBuyCost(Item item, int amount=1)
        {
            return ComputeCost(item, amount, buyModifier);
        }

        public int ComputeSellCost(Item item, int amount = 1)
        {
            return ComputeCost(item, amount, sellModifier);
        }
        
        public int ComputeCost(Item item, int amount, float modifier)
        {
            float popularityModifier = GetPopularityModifier(item);
            return Mathf.CeilToInt(popularityModifier * ((float)(item != null ? item.baseCost : 0) * (float)amount * modifier));
        }

        public float GetPopularityModifier(Item item)
        {
            if (traderCulture == null)
                return 1f;

            if (traderCulture != null)
            {
                     if (Array.IndexOf(traderCulture.favouriteItems, item) != -1) return 1.33f;
                else if (Array.IndexOf(traderCulture.dislikedItems,  item) != -1) return 0.66f;
            }

            return 1f;
        }

        public int HowManyAreBuyable(ItemStack itemStack, int amount=-1)
        {
            return HowManyAreAchievable(itemStack, amount, buyModifier, customer.Gold);
        }
        
        public int HowManyAreSellable(ItemStack itemStack, int amount=-1)
        {
            return HowManyAreAchievable(itemStack, amount, sellModifier, service.Gold);
        }
        
        public int HowManyAreAchievable(ItemStack itemStack, int amount, float modifier, int maxToSpend)
        {
            if (amount < 1 || amount > itemStack.amount)
                amount = itemStack.amount;

            int costSingleItem = ComputeCost(itemStack.item, 1, modifier);

            return (int)Mathf.Min(maxToSpend / costSingleItem, (float)amount);
        }

        #endregion

        private void RefreshModifiers()
        {
            buyModifier = 1.5f;
            sellModifier = .5f;
        }
    }
}
