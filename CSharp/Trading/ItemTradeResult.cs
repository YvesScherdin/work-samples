using MageGame.Data.Items;
using MageGame.Data.Staff;
using MageGame.Data.Vehicles;
using System.Collections.Generic;

namespace MageGame.Actions.PersonBased
{

    public class ItemTradeResult : BasicTradeResult
    {
        public int itemStackDelta;
        internal int newSourceStackSize;

        public Item transferredItem;
        public ItemStack sourceStack;
        public List<ItemStack> targetStacks;

        static public int DetermineAmount(ItemStack itemStack, TransactionFlags flags)
        {
            if (flags.HasFlag(TransactionFlags.All)) return itemStack.amount;
            else if (flags.HasFlag(TransactionFlags.Single)) return 1;
            else if (itemStack.amount <= 2) return 1;
            else return -1;
        }
    }
    
    public class VehicleTradeResult : BasicTradeResult
    {
        public VehicleStatus transferredVehicle;
    }
    
    public class StaffMemberTradeResult : BasicTradeResult
    {
        public StaffMemberStatus transferredStaff;
    }

    public class BasicTradeResult
    {
        public int goldDelta;
    }
}
