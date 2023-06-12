using MageGame.Behaviours.Statuses;
using MageGame.Events;
using MageGame.GUI.Data;
using System.Collections.Generic;
using UnityEngine;

namespace MageGame.GUI.Components
{
    /// <summary>
    /// An icon bar for the various statuses a character (or any other entity owning a StatusController) can have.
    /// </summary>
    public class StatusIconBar : BasicCellContainer<BasicStatus, StatusCell>
    {
        #region configuration
        public GameObject cellTemplate;
        public Vector2 cellOfset;
        public Vector2 anchorPosition;
        public IconCellStyle cellStyle;
        #endregion

        private List<BasicStatus> statuses;
        private StatusController statusController;

        #region de/-init

        // we don't specify constructors for components

        ~StatusIconBar() => Deinit();

        internal void Init(StatusController statusController)
        {
            this.statusController = statusController;

            statusController.StatusChanged.AddListener(HandleStatusChange);

            CreateInitialCells();
        }

        private void Deinit()
        {
            if(statusController != null)
                statusController.StatusChanged.RemoveListener(HandleStatusChange);
        }

        private void CreateInitialCells()
        {
            statuses = new List<BasicStatus>(statusController.GetAllStatuses());
            cellList = new List<StatusCell>(statuses.Count);

            for (int i = 0; i < statuses.Count; i++)
            {
                if(!(statuses[i] is ISilentStatus))
                    CreateCell(i, statuses[i]);
            }
        }

        private void CreateCell(int cellIndex, BasicStatus status)
        {
            GameObject cellObject = Instantiate<GameObject>(cellTemplate);
            cellObject.transform.SetParent(this.transform);
            cellObject.transform.localScale = Vector3.one;

            StatusCell cell = cellObject.GetComponent<StatusCell>();

            if (cellStyle != null)
                cell.style = cellStyle;

            cell.contentSource = this;
            //cell.SetInteractionManager(interactionManager);
            cell.SetContent(status);
            cellList.Add(cell);

            ArrangeCell(cellList[cellIndex], cellIndex);
        }

        #endregion

        #region status change

        private void AddCellFor(BasicStatus status)
        {
            statuses.Add(status);
            CreateCell(statuses.Count-1, status);
        }

        private void RemoveCellOf(BasicStatus status)
        {
            statuses.Remove(status);

            StatusCell cell = GetCell(status);
            cellList.Remove(cell);
            GameObject.Destroy(cell.gameObject);

            RearrangeCells();
        }

        private void RearrangeCells()
        {
            for (int i = 0; i < cellList.Count; i++)
                ArrangeCell(cellList[i], i);
        }
        
        private void ArrangeCell(StatusCell cell, int cellIndex)
        {
            cell.transform.localPosition = anchorPosition + new Vector2(cellIndex * cellOfset.x, cellIndex * cellOfset.y);
        }

        #endregion

        #region event handlers

        private void HandleStatusChange(StatusEventType eventType, StatusType statusType, BasicStatus status)
        {
            switch(eventType)
            {
                case StatusEventType.Added:
                    if (!(status is ISilentStatus))
                        AddCellFor(status);
                    break;

                case StatusEventType.Removed:
                    if (!(status is ISilentStatus))
                        RemoveCellOf(status);
                    break;
            }
        }

        #endregion
    }
}