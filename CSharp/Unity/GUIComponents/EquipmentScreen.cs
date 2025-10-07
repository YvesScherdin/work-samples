using GataryLabs.Localization;
using MageGame.Behaviours.Ability;
using MageGame.Controls;
using MageGame.Effects;
using MageGame.Events;
using MageGame.GUI.Behaviours;
using MageGame.GUI.Components;
using MageGame.GUI.Components.Informative;
using MageGame.Stats;
using MageGame.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MageGame.GUI.Screens
{
    public class EquipmentScreen : IngameScreen, IPointerFocusObserver
    {
        #region configuration
        public Image characterView;

        [Header("Cells")]
        public InventoryView optionCells;
        public EquipmentSelectionView selectionCells;
        public EffectsInfoDisplay effectsInfoDisplay;
        public StatsInfoDisplay statInfoDisplay;

        [Header("Functionality")]
        public EquipmentCellInteractionManager interactions;
        public ItemFilterButtonBar inventoryFilterButtons;

        [Header("Texts")]
        public Text charTitleTextField;
        public Text goldAmountTextField;

        public FocusableGUIAreaController focusableAreaController;
        #endregion

        protected override IngameScreenID IngameScreenID => IngameScreenID.Equipment;

        public override LanguageCategory LocaCategory => LanguageCategory.EquipmentScreen;

        #region de-/init
        protected override void Awake()
        {
            base.Awake();

            focusableAreaController.Init(this);
            GlobalEventManager.PlayerGoldChanged.AddListener(HandleGoldChanged);
        }
        #endregion

        protected override void Refresh()
        {
            GameObject mainChar = PlayerControl.GetMainCharacter();
            ItemCollector itemCollector = mainChar.GetComponent<ItemCollector>();
            Equippable equippable = mainChar.GetComponent<Equippable>();

            effectsInfoDisplay.Init(mainChar.GetComponent<EffectController>());
            statInfoDisplay.Init(mainChar.GetComponent<CharStats>(), mainChar.GetComponent<Resistance>());

            if (characterView.sprite != equippable.preview)
                characterView.sprite = equippable.preview;

            ItemUtil.Sort(itemCollector.Inventory.allItems);

            optionCells.SetInventory(itemCollector.Inventory);
            selectionCells.SetEquippableOne(equippable);
            interactions.SetEquippableOne(equippable, itemCollector.Inventory);

            optionCells.Activate();

            charTitleTextField.text = PlayerControl.GetCharacterName();
            RefreshGoldDisplay();

            focusableAreaController.Reset();
        }

        private void RefreshGoldDisplay()
        {
            goldAmountTextField.text = PlayerControl.GetMainEndeavour().gold.ToString("#,#", Loca.Culture);
        }

        protected override void OnBeforeHidden()
        {
            base.OnBeforeHidden();
            optionCells.Deactivate();
            GUIContext.items.interactionType = GUIItemInteraction.Default;
        }


        private void Update()
        {
            if (inventoryFilterButtons != null)
            {
                if (Input.GetKeyUp(KeyCode.A)) inventoryFilterButtons.Prev();
                if (Input.GetKeyUp(KeyCode.D)) inventoryFilterButtons.Next();
                if (Input.GetKeyUp(KeyCode.W)) optionCells.scroller.Scroll(-1);
                if (Input.GetKeyUp(KeyCode.S)) optionCells.scroller.Scroll( 1);
            }
        }

        #region events

        private void HandleGoldChanged(int goldDelta)
        {
            RefreshGoldDisplay();
        }

        public void ChangePointerFocus(GameObject target, bool focused)
        {
            if (focused)
            {
                if (target == focusableAreaController.areas[0].connectedObject)
                    GUIContext.items.interactionType = GUIItemInteraction.Unequip;
                else
                    GUIContext.items.interactionType = GUIItemInteraction.Default;
            }
        }
        #endregion
    }
}