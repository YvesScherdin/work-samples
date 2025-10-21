package gtsinf.ents.components 
{
	import away3d.containers.ObjectContainer3D;
	import gts.display.mdl.IModel;
	import gts.ents.components.ModelComponent;
	import gts.events.ModelEvent;
	import yaway.models.basic.AwayRootContainer;
	import yaway.models.basic.IBasicAwayModel;
	
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class AwayModelComponent extends ModelComponent 
	{
		private var rootContainer:ObjectContainer3D;
		public function getContainer():ObjectContainer3D { return rootContainer; }
		
		public function AwayModelComponent() 
		{
			super();
			
		}
		
		override protected function Deinit():void 
		{
			super.Deinit();
			rootContainer = null;
		}
		
		
		override public function updateTransforms():void 
		{
			if (rootContainer == null)
				return;
			
			rootContainer.x = owner.Transforms.x;
			rootContainer.y = owner.Transforms.y;
			rootContainer.z = owner.Transforms.z;
			
			rootContainer.rotationX = owner.Transforms.rotationX;
			rootContainer.rotationY = owner.Transforms.rotationY;
			rootContainer.rotationZ = owner.Transforms.rotationZ;
		}
		
		override public function applyModel( model:IModel ):void
		{
			super.applyModel(model);
			
			if (model is IBasicAwayModel)
			{
				rootContainer = IBasicAwayModel(model).getRootContainer();
				AwayRootContainer( rootContainer ).owner = owner;
			}
			/*else if (model is IBasicAwayModel)
			{
				rootContainer = IBasicAwayModel(model).getRootContainer();
			}*/
			
			updateTransforms();
			
			SendEvent(new ModelEvent(ModelEvent.APPLIED, model, owner));
		}
		
		override public function remove():void
		{
			if (rootContainer != null && rootContainer.parent != null)
				rootContainer.parent.removeChild(rootContainer);
		}
		
		
		override public function setVisible(state:Boolean):void 
		{
			rootContainer.visible = state;
		}
		
		override public function isVisible():Boolean 
		{
			return rootContainer.visible;
		}
	}

}