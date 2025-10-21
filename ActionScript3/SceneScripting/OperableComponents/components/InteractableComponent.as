package gtsinf.ents.components 
{
	import Box2D.Dynamics.b2Fixture;
	import gts.ents.components.EntityComponent;
	import gtsinf.world.serialization.CollisionShapeInfo;
	
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class InteractableComponent extends EntityComponent 
	{
		private var shape:CollisionShapeInfo;
		public function setShapeInfo(value:CollisionShapeInfo):void { shape = value; }
		public function getShapeInfo():CollisionShapeInfo { return shape; }
		
		private var inUse:Boolean;
		public function get InUse():Boolean { return inUse; }
		public function set InUse(value:Boolean):void { inUse = value; }
		
		private var sensor:b2Fixture;
		public function get Sensor():b2Fixture { return sensor; }
		public function set Sensor(value:b2Fixture):void { sensor = value; }
		
		private var actionParams:Object;
		public function get ActionParams():Object { return actionParams; }
		public function set ActionParams(value:Object):void { actionParams = value; }
		
		
		public function InteractableComponent() 
		{
			super(EntityComponent.INTERACTABLE);
		}
		
		override protected function Init():void 
		{
			super.Init();
		}
		
		override protected function Deinit():void 
		{
			super.Deinit();
		}
	}

}