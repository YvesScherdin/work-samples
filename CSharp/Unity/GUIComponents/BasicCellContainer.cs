using System.Collections.Generic;
using UnityEngine;

namespace MageGame.GUI.Components
{
    /// <summary>
    /// A cell container is a GUI component that displays a list of elements as cells and allows interactions with them.
    /// It may specify a source id to clarify later in the interaction handling the origin of the cell interacted with.
    /// </summary>
    /// <typeparam name="ContentType"></typeparam>
    /// <typeparam name="CellType">The stated generic CellType must have the same ContentType</typeparam>
    public class BasicCellContainer<ContentType, CellType> : MonoBehaviour, ICellContentSource, IRefreshable
        where CellType:ContentCell<ContentType>
    {

        protected List<CellType> cellList;

        protected bool active;

        virtual public CellContentSourceID CellContentID => CellContentSourceID.Application;

        protected ICellInteractionManager interactionManager;


        public void SetInteractionManager(ICellInteractionManager value, bool shallApply=true)
        {
            interactionManager = value;

            if (cellList == null || !shallApply)
                return;

            for(int i=0; i < cellList.Count; i++)
            {
                cellList[i].SetInteractionManager(interactionManager);
            }
        }

        virtual public void Refresh()
        {
            if (interactionManager != null && interactionManager.IsDragging())
                interactionManager.AbortAction();
        }

        public void Activate()
        {
            active = true;

            if (cellList == null)
                return;

            for (int i = 0; i < cellList.Count; i++)
            {
                cellList[i].Activate();
            }
        }
        
        public void Deactivate()
        {
            active = false;

            if (cellList == null)
                return;

            for (int i = 0; i < cellList.Count; i++)
            {
                cellList[i].Deactivate();
            }
        }

        public void ClearCells(bool shallRemoveContent=true)
        {
            if (cellList == null)
                return;

            for (int i = 0; i < cellList.Count; i++)
            {
                try
                {
                    if (shallRemoveContent)
                        cellList[i].RemoveContent();
                    else
                        cellList[i].Clear();
                }
                catch (System.Exception e)
                {
                    Debug.LogError(i + ": " + e);
                }
            }
        }

        protected void DestroyCells()
        {
            for (int i=cellList.Count-1; i >= 0; i--)
            {
                Destroy(cellList[i].gameObject);
            }

            cellList.Clear();
        }

        virtual public CellType GetCell(ContentType content)
        {
            if (content == null)
                return GetFirstEmptyCell();

            for (int i = 0; i < cellList.Count; i++)
            {
                if (cellList[i].GetContent() != null && cellList[i].GetContent().Equals(content))
                    return cellList[i];
            }

            return null;
        }

        virtual public CellType GetFirstEmptyCell()
        {
            // Let's not use Linq at runtime, it is not desirable for performance reasons.
            //return System.Linq.Enumerable.FirstOrDefault(cellList, (CellType cell) => cell.GetContent() == null);

            for (int i = 0; i < cellList.Count; i++)
            {
                if (cellList[i].GetContent() == null)
                    return cellList[i];
            }

            return null;
        }
    }
}