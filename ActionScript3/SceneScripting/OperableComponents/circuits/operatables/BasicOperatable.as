package gtsinf.world.circuits.operatables 
{
	import gts.ents.basic.Entity;
	import gts.events.EventMark;
	import gts.events.ents.OperationEvent;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class BasicOperatable 
	{
		protected var eventMark:String;
		protected var operatable:Boolean;
		protected var entity:Entity;
		
		// **************************************
		// DE-/INIT
		// **************************************
		
		public function BasicOperatable() 
		{
			eventMark = EventMark.gen("op");
		}
		
		public function applyEntity(value:Entity):void
		{
			if (entity != null)
				deinit();
			
			entity = value;
			
			if (entity != null)
				init();
		}
		
		public function init():void
		{
			entity.Events.CreateHandle(OperationEvent.OPERATED, handleOperated, eventMark);
		}
		
		public function deinit():void
		{
			entity.Events.ClearHandles(eventMark);
		}
		
		// **************************************
		// CUSTOM
		// **************************************
		
		public function startOperation():void
		{
			
		}
		
		public function updateOperation():void
		{
			
		}
		
		public function stopOperation():void
		{
			
		}
		
		// **************************************
		// EVENT LISTENERS
		// **************************************
		
		private function handleOperated(event:OperationEvent):void 
		{
			
		}
		
		
	}

}