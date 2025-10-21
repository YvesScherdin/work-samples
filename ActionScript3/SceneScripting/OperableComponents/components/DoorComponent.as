package gtsinf.ents.components 
{
	import Box2D.Dynamics.b2Body;
	import flash.geom.Vector3D;
	import gts.ents.components.EntityComponent;
	import gts.events.ents.EntityEvent;
	import gts.world.interfaces.IPhysixDemander;
	import gts.world.objects.basic.BasicWorldObject;
	import gts.world.objects.structures.StructureBasedEntity;
	import gtsinf.ents.components.enums.DoorState;
	import gtsinf.ents.components.info.DoorInfo;
	import gtsinf.ents.EntChar;
	import gtsinf.world.circuits.operatables.IOperatable;
	import gtsinf.world.physix.PhysObjectInfo;
	import yves.utils.Geom;
	
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class DoorComponent extends EntityComponent implements IPhysixDemander, IOperatable
	{
		private var operating:Boolean;
		
		private var doorInfo:DoorInfo;
		
		private var currentState:int;
		//private var targetState:int;
		
		private var currentProgress:Number = 0;
		private var targetProgress:Number = 1;
		
		private var targetObject:BasicWorldObject;
		
		private var initialPosition:Vector3D;
		private var targetPosition:Vector3D;
		
		// **************************************
		// DE-/INIT
		// **************************************
		
		public function DoorComponent(doorInfo:DoorInfo) 
		{
			super(EntityComponent.DOOR);
			this.doorInfo = doorInfo;
			
			needsUpdate = true;
		}
		
		override protected function Init():void 
		{
			super.Init();
			
			if (owner is StructureBasedEntity)
				targetObject = StructureBasedEntity(owner).Structure;
			else
				targetObject = owner;
			
			currentState = DoorState.CLOSED;
			initialPosition = targetObject.Position;
			targetPosition = new Vector3D();
			var dir:Number = doorInfo.direction - targetObject.rotation;
			dir *= Geom.TO_RAD;
			
			targetPosition.x = doorInfo.distance * Math.cos(dir);
			targetPosition.z = doorInfo.distance * Math.sin(dir);
			
			if (doorInfo.openByTouch)
				owner.Events.CreateHandle(EntityEvent.TOUCHED, handleTouch, eventMark);
		}
		
		override protected function Deinit():void 
		{
			owner.Events.ClearHandles(eventMark);
			super.Deinit();
		}
		
		override protected function get eventMark():String 
		{
			return "DoorComponent";
		}
		
		// ACTIONS
		public function operate():void
		{
			//if (isBusy())
				//return;
			
			if (isOpen())
				close();
			else
				open();
		}
		
		public function open():void 
		{
			if (isOpen())
				return;
			
			currentState = DoorState.OPEN;
			operating = true;
			targetProgress = 1;
		}
		
		public function close():void
		{
			if (isClosed())
				return;
			
			currentState = DoorState.CLOSED;
			operating = true;
			targetProgress = 0;
		}
		
		override public function Update():void 
		{
			super.Update();
			
			if (operating)
			{
				// TODO: change transition!
				currentProgress += (targetProgress - currentProgress) * 0.1;
				
				if (Math.abs(Math.abs(currentProgress) - Math.abs(targetProgress)) < 0.01)
					currentProgress = targetProgress;
				
				applyStateToTarget();
				
				if (currentProgress == targetProgress)
				{
					operating = false;
				}
			}
		}
		
		private function applyStateToTarget():void
		{
			//var newPos:Vector3D = initialPosition.clone();
			targetObject.x = initialPosition.x + targetPosition.x * currentProgress;
			targetObject.z = initialPosition.z + targetPosition.z * currentProgress;
		}
		
		
		// EVENT HANDLING
		
		private function handleTouch(evt:EntityEvent):void 
		{
			if (evt.Causer is EntChar == false)
				return;
			
			if (isBusy() || isOpen())
				return;
			
			open();
		}
		
		
		// STATE RETRIEVAL
		public function isOpen():Boolean
		{
			return currentState == DoorState.OPEN;
		}
		
		public function isClosed():Boolean
		{
			return currentState == DoorState.CLOSED;
		}
		
		public function isIdle():Boolean
		{
			return !operating;
		}
		
		public function isBusy():Boolean
		{
			return operating;
		}
		
		
		/* INTERFACE gts.world.interfaces.IPhysixDemander */
		
		public function getPreferredBodyType():uint 
		{
			return b2Body.b2_kinematicBody;
		}
		
		public function getPhysInfo():PhysObjectInfo 
		{
			return null;
		}
	}

}