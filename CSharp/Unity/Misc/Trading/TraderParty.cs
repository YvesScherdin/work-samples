using MageGame.Behaviours.Ability;
using MageGame.Behaviours.EntityType.Professions;
using MageGame.Data;
using MageGame.Utils;
using MageGame.World.Data;
using UnityEngine;

namespace MageGame.Actions.PersonBased
{
    public class TraderParty
    {
        public string name;
        public Inventory inventory;
        public Endeavour endeavour;

        public int Gold
        {
            get => endeavour.gold;
            set { endeavour.gold = value; }
        }

        internal static TraderParty FromCustomer(GameObject user, TraderParty party)
        {
            if (party == null)
                party = new TraderParty();

            party.name = LocaUtil.GetPersonTitle(user);

            IEndeavourProvider provider = user.GetComponent<IEndeavourProvider>();

            if (provider != null)
                party.endeavour = provider.GetEndeavour();
            else
                Debug.LogWarning("Customer lacks endeavourProvider");

            ItemCollector icol = user.GetComponent<ItemCollector>();
            if (icol != null)
                party.inventory = icol.Inventory;
            else if (party.endeavour != null)
                party.inventory = party.endeavour.inventory;

            if (party.inventory == null)
            {
                Debug.LogWarning("endeavour null in " + user + ". Items will be lost.");
                party.inventory = new Inventory();
            }

            return party;
        }

        internal static TraderParty FromTrader(ITrader trader, TraderParty party)
        {
            if (party == null)
                party = new TraderParty();

            party.name = LocaUtil.GetPersonTitle(trader.gameObject);
            party.endeavour = trader.GetEndeavour();
            party.inventory = trader.GetEndeavour().inventory;

            return party;
        }
    }
}
