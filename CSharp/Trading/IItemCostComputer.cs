using MageGame.Data.Items;

namespace MageGame.Actions.PersonBased
{
    public interface IItemCostComputer
    {
        int ComputeBuyCost (Item item, int amount = 1);
        int ComputeSellCost(Item item, int amount = 1);

        float GetPopularityModifier(Item item);
    }

}
