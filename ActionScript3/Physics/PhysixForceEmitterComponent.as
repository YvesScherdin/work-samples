package gtsinf.ents.components 
{
	import gts.ents.basic.Entity;
	import gts.ents.basic.InfEntityUtil;
	import gts.ents.components.EntityComponent;
	import gts.events.ents.EntityEvent;
	import gts.system.timing.InfWorldTimeStep;
	import gts.world.objects.collision.CollisionGroup;
	import gts.world.objects.collision.CollisionShapeType;
	import gtsinf.world.utils.filters.MovableEntityFilter;
	import gts.world.objects.utils.filters.WorldObjectFilter;
	import gts.world.objects.utils.filters.WorldObjectTypeMapFilter;
	import gts.world.utils.WorldObjectLocation;
	import gts.world.zones.ZoneType;
	import gtsinf.ents.basic.InfEntity;
	import gtsinf.ents.components.info.ForceEmitterInfo;
	import gtsinf.ents.misc.env.EntBlastWave;
	import gtsinf.ents.misc.MiscEntType;
	import gtsinf.ents.misc.zones.EntZoneForce;
	import gtsinf.world.InfWorldDataBase;
	import gtsinf.world.physix.PhysixForce;
	import gtsinf.world.physix.WorldCollisionResult;
	import gts.world.serialization.EntityInfo;
	import gtsinf.world.serialization.MarkerInfo;
	import gtsinf.world.utils.WorldDistanceUtil2D;
	import gtsinf.world.InfWorld;
	import yves.utils.Geom;
	
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class PhysixForceEmitterComponent extends EntityComponent 
	{
		private var info:ForceEmitterInfo;
		private var allowedTargets:WorldObjectFilter;
		private var collisionResult:WorldCollisionResult;
		
		
		public function PhysixForceEmitterComponent(info:ForceEmitterInfo) 
		{
			super(EntityComponent.FORCE_EMITTER);
			this.info = info;
			
			needsUpdate = true;
		}
		
		override protected function Init():void 
		{
			super.Init();
			
			//owner.Events.CreateHandle(EntityEvent.ADDED_TO_WORLD,     handleAdding,  eventMark);
			//owner.Events.CreateHandle(EntityEvent.REMOVED_FROM_WORLD, handleRemoval, eventMark);
			
			//owner.Events.CreateHandle(EntityEvent.TOUCHED,  handleTouch,   "EC_ForceEmitter");
			//owner.Events.CreateHandle(EntityEvent.RELEASED, handleRelease, "EC_ForceEmitter");
			
			for each(var force:PhysixForce in info.forces)
			{
				force.origin = new WorldObjectLocation(owner);
			}
			
			//allowedTargets = new MovableEntityFilter(); // TODO: customize
			allowedTargets = new WorldObjectTypeMapFilter(
				CollisionGroup.FORCE_TARGETS
			);
			collisionResult = new WorldCollisionResult();
		}
		
		
		override protected function Deinit():void 
		{
			super.Deinit();
			
			owner.Events.ClearHandles(eventMark);
			
			for each(var force:PhysixForce in info.forces)
			{
				force.deinit();
			}
		}
		
		
		override public function Update():void 
		{
			super.Update();
			
			if (owner is EntBlastWave)
			{
				//trace(info.forces[0]);
			}
			
			if (owner.RefWorld.Reference == null)
				return;
			
			for each(var force:PhysixForce in info.forces)
			{
				//if(!force.local)
				force.update(InfWorldTimeStep.current.delta);
				
				//force.origin
				/*
				if (force.zoneNeeded)
				{
					force.zone
				}*/
				
				if(!force.isOver())
					checkForce(force);
			}
		}
		
		
		
		private function checkForce(force:PhysixForce):void 
		{
			switch(force.type)
			{
				case PhysixForce.TYPE_CENTROID:
				{
					InfWorld(owner.RefWorld.Reference).CurrCell.Physix.getCollidingObjectsInCircle(
						owner.Position, force.distance, allowedTargets, collisionResult
					);
					
					break;
				}
				
				case PhysixForce.TYPE_DIRECTIONAL:
				{
					// copied from ventroid
					InfWorld(owner.RefWorld.Reference).CurrCell.Physix.getCollidingObjectsInCircle(
						owner.Position, force.distance, allowedTargets, collisionResult
					);
					
					break;
				}
				
				default:
				{
					if(collisionResult.objects != null && collisionResult.objects.length != 0)
						collisionResult.objects = [];
				}
			}
			
			for each(var ent:Entity in collisionResult.objects)
			{
				applyForceTo(ent, force);
			}
		}
		
		private function applyForceTo(target:Entity, force:PhysixForce):void
		{
			var strength:Number;
			var direction:Number;
			
			if (force.type == PhysixForce.TYPE_CENTROID)
			{
				// determine direction
				direction = Geom.getDir2(owner.x, -owner.z, target.x, -target.z);
				direction += force.direction;
				//trace(direction);
				
				strength  = force.calcDistanceModifier(WorldDistanceUtil2D.objectToObject(target, owner));
				strength *= force.totalStrength;
			}
			else
			{
				direction = force.direction;
				strength  = force.totalStrength;
			}
			
			if(strength > 0)
				InfEntityUtil.getPhysix(target).applyDirectionalForce(direction, strength);
		}
		/*
		private function fallOff(raw:Number):Number
		{
			return raw * raw;
		}
		*/
		
		/*
		private function handleAdding(evt:EntityEvent):void
		{
			for each(var force:PhysixForce in info.forces)
			{
				if (force.zoneNeeded)
				{
					//createAddZoneForForce(force);
				}
			}
		}
		
		private function handleRemoval(evt:EntityEvent):void
		{
			for each(var force:PhysixForce in info.forces)
			{
				if (force.zoneNeeded && force.zone != null)
				{
					force.zone.kill();
					force.zone = null;
				}
			}
		}
		
		private function createAddZoneForForce(force:PhysixForce):void 
		{
			var entInfo:MarkerInfo = new MarkerInfo();
			entInfo.libID = MiscEntType.ZONE;
			entInfo.objID = ZoneType.FORCE;
			
			var zone:EntZoneForce = InfWorldDataBase(World(owner.RefWorld.Reference).GDB).MiscLib.createByInfo(entInfo) as EntZoneForce;
			zone.Dimension.shape.shape = CollisionShapeType.CIRCLE;
			zone.Dimension.shape.length = force.distance;
			
			zone.Force = force;
			force.zone = zone;
			zone.Transforms = owner.Transforms;
			
			owner.RefWorld.Reference.addEnt(zone);
		}
		*/
		
		/*
		private function handleTouch(evt:EntityEvent):void
		{
			var ent:Entity = evt.Causer;
			
			var physix:PhysixComponent = PhysixComponent(ent.getCompByType(EntityComponent.PHYSIX));
			
			if (physix == null)
				return;
			
			for each(var force:PhysixForce in info.forces)
			{
				physix.addForce(force);
			}
		}
		
		private function handleRelease(evt:EntityEvent):void
		{
			var ent:Entity = evt.Causer;
			
			var physix:PhysixComponent = PhysixComponent(ent.getCompByType(EntityComponent.PHYSIX));
			
			if (physix == null)
				return;
			
			for each(var force:PhysixForce in info.forces)
			{
				physix.removeForce(force);
			}
		}
		*/
		
	}

}