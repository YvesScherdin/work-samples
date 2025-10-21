package gtsinf.world.circuits 
{
	import gts.ents.basic.Entity;
	import gts.events.EventMark;
	import gts.world.objects.basic.BasicWorldObject;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class BasicCircuitConnection 
	{
		protected var eventMark:String;
		protected var source:Entity;
		protected var target:Entity;
		
		public function BasicCircuitConnection() 
		{
			eventMark = EventMark.gen("circ_co_");
		}
		
		public function setActors(objA:Entity, objB:Entity):void 
		{
			source = objA;
			target = objB;
		}
		
		public function deinit():void 
		{
			source.Events.clearByMark(eventMark);
			target.Events.clearByMark(eventMark);
			
			source = null;
			target = null;
		}
		
		
		
	}

}