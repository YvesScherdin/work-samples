using GataryLabs.Localization;
using MageGame.Controls;
using MageGame.Data.DB;
using MageGame.Data.Vehicles;
using MageGame.GUI.Components;
using MageGame.Utils;
using Snobertas;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MageGame.GUI.Screens.MapRelated
{
    /// <summary>
    /// A widget which displays the currently used vehicle and which also allows to swap it with different options.
    /// </summary>
    public class CurrentVehicleWidget : MonoBehaviour, ICurrentVehicleChanger
    {
        static private CurrentVehicleWidget instance;
        static public CurrentVehicleWidget Instance => instance;

        #region configuration
        public Text headline;
        public VehicleCell vehicleCell;
        public CanvasGroup canvasGroup;

        public VehicleOptionPanel options;
        public VehicleChoiceCellInteractionManager optionsManager;
        #endregion

        #region de-/init

        private void Awake()
        {
            instance = this;
            headline.text = Loca.Text("CurrentVehicleHeadline", LanguageCategory.MapScreen);

            optionsManager.currentCell = vehicleCell;
            optionsManager.optionCells = options;
            optionsManager.changer = this;

            if (AreOptionsShown())
                HideOptions();
        }

        #endregion

        #region actions

        public void Refresh()
        {
            vehicleCell.SetContent(PlayerControl.GetCurrentVehicle());

            List<VehicleStatus> vehicles = new List<VehicleStatus>(PlayerControl.GetVehicles(PlayerControl.GetCurrentWhereabout(), VehicleCategory.All));
            vehicles.Insert(0, null);
            options.Build(vehicles);

            if (vehicles.Count > 1)
            {
                Activate(); 
            }
            else
            {
                Deactivate();
            }
        }

        public void AbortActions()
        {
            if (AreOptionsShown())
                HideOptions();
        }

        public void Activate()
        {
            canvasGroup.alpha = 1f;
            vehicleCell.Activate();
        }

        public void Deactivate()
        {
            canvasGroup.alpha = .5f;
            vehicleCell.Deactivate();
        }

        public bool AreOptionsShown()
        {
            return options != null ? options.gameObject.activeSelf : false;
        }

        public void ShowOptions()
        {
            options.Refresh();
            options.gameObject.SetActive(true);
            SimpleTooltipController.HideCurrent();
        }

        public void HideOptions()
        {
            options.gameObject.SetActive(false);
            SimpleTooltipController.HideCurrent();
        }

        public void ChooseVehicle(VehicleStatus newVehicle)
        {
            AudioUtil.PlayUISound(SoundDatabase.Instance.ConversationConfirm);
            PlayerControl.SetCurrentVehicle(newVehicle);
            vehicleCell.SetContent(newVehicle);

            HideOptions();
        }

        #endregion
    }

    public interface ICurrentVehicleChanger
    {
        bool AreOptionsShown();
        void ShowOptions();
        void HideOptions();
        void ChooseVehicle(VehicleStatus newVehicle);
    }
}