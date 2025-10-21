package gtsinf.world.circuits 
{
	import gts.ents.basic.Entity;
	import gts.ents.components.EntityComponent;
	import gts.events.ents.OperationEvent;
	import gtsinf.ents.components.ButtonComponent;
	import gtsinf.ents.components.DoorComponent;
	import gtsinf.ents.misc.lights.EntLight;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class MonoConnection extends BasicCircuitConnection
	{
		
		public function MonoConnection() 
		{
			super();
			
		}
		
		override public function setActors(objA:Entity, objB:Entity):void 
		{
			super.setActors(objA, objB);
			
			source.Events.CreateHandle(OperationEvent.OPERATED, handleOperated, eventMark);
		}
		
		private function handleOperated(event:OperationEvent):void 
		{
			var buttonComp:ButtonComponent = ButtonComponent(source.getCompByType(EntityComponent.BUTTON));
			
			if (buttonComp)
			{
				buttonComp.operate();
			}
			
			var doorComp:DoorComponent = DoorComponent(target.getCompByType(EntityComponent.DOOR));
			
			if (doorComp != null)
			{
				doorComp.operate();
			}
			
			if (target is EntLight)
			{
				EntLight(target).getLight().visible = !EntLight(target).getLight().visible;
			}
		}
		
	}

}