package gtsinf.world 
{
	import away3d.core.data.EntityListItem;
	import gts.ents.basic.Entity;
	import gts.events.ents.EntityEvent;
	import gts.system.timing.InfWorldTimeStep;
	import gts.world.BasicWorld;
	import gts.world.components.WorldComponentPack;
	import gts.world.objects.basic.BasicWorldObject;
	import gts.world.objects.collision.CollisionObject;
	import gts.world.objects.structures.WorldStructure;
	import gtsinf.scene.infos.InfWorldContext;
	import gtsinf.world.clusters.InfMapCluster;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class InfWorld extends BasicWorld
	{
		private var currCell:InfMapCell;
		public function get CurrCell():InfMapCell { return currCell; }
		
		private var db:InfWorldDataBase;
		public function get DB():InfWorldDataBase { return db; }
		
		private var timeStep:InfWorldTimeStep;
		public function get TimeStep():InfWorldTimeStep { return timeStep; }
		
		private var context:InfWorldContext;
		public function get Context():InfWorldContext { return context; }
		
		private var complex:InfMapCluster;
		public function get Complex():InfMapCluster { return complex; }
		public function set Complex(value:InfMapCluster):void { complex = value; }
		
		// util
		private var activationEvent:EntityEvent;
		
		// **************************************
		// DE-/INIT
		// **************************************
		
		/**
		 * A world is the connection between game and some of its most important elements: map, entities, events.
		 * The world can work independently from game. Mostly world is controlled by game and if smthg. happens in world, the game can respond.
		 */
		public function InfWorld(db:InfWorldDataBase) 
		{
			super();
			this.db = db;
			
			allComponents = new WorldComponentPack(this);
			timeStep = new InfWorldTimeStep();
			context = new InfWorldContext();
			
			InfWorldTimeStep.current = timeStep;
		}
		
		override public function init():void 
		{
			super.init();
		}
		
		override public function deinit():void 
		{
			super.deinit();
			
			entMan.clearGraveyard();
			currCell.deinit();
			eventMan.clear();
			
			allComponents.deinit();
			
			activationEvent = null;
		}
		
		public function setCurrCell(currCell:InfMapCell):void
		{
			this.currCell = currCell;
			currCell.setWorld(this);
			
			this.entMan    = currCell.EntMan;
			this.entFinder = currCell.EntFinder;
			this.eventMan  = currCell.EventMan;
		}
		
		public function activate():void
		{
			active = true;
			
			activationEvent = new EntityEvent(EntityEvent.ACTIVATED);
			
			for each(var ent:Entity in entMan.getAll())
			{
				ent.Events.SendInternally(activationEvent);
			}
		}
		
		// **************************************
		// ELEMENT MANAGEMENT
		// **************************************
		
		/**
		 * TODO: Make this only add entities, when update cycle is over, elsewise store it in temporary list of entities to add.
		 * @param	ent
		 */
		override public function addEnt(ent:Entity):void 
		{
			if (ent == null)
				return;
			
			super.addEnt(ent);
			
			currCell.addEnt(ent);
			ent.RefWorld.register(this);
			
			ent.ParentSpace = currCell.CoordSpace;
			
			if (active)
				ent.Events.SendInternally(activationEvent);
		}
		
		override public function removeEnt(ent:Entity):void 
		{
			super.removeEnt(ent);
			
			currCell.removeEnt(ent);
			ent.RefWorld.unregister();
			ent.View.remove();
		}
		
		
		public function addStructure(structure:WorldStructure):void
		{
			currCell.addStructure(structure);
		}
		
		public function removeStructure(structure:WorldStructure):void
		{
			currCell.removeStructure(structure);
		}
		
		
		public function addObject(object:BasicWorldObject):void
		{
			if (object is WorldStructure)
				addStructure(WorldStructure(object));
			else if (object is Entity)
				addEnt(Entity(object));
			else if (object is CollisionObject)
				currCell.addColl(CollisionObject(object));
			else
				throw new Error("cannot add unknown world object type");
		}
		
		public function removeObject(object:BasicWorldObject):void
		{
			if (object is WorldStructure)
				removeStructure(WorldStructure(object));
			else if (object is Entity)
				removeEnt(Entity(object));
			else if (object is CollisionObject)
				currCell.removeColl(CollisionObject(object));
			else
				throw new Error("cannot remove unknown world object type");
		}
		
		// **************************************
		// LIFE CYCLE
		// **************************************
		
		override public function update():void 
		{
			timeStep.update();
			InfWorldTimeStep.dt = timeStep.delta;
			InfWorldTimeStep.dtms = timeStep.deltaMS;
			
			currCell.Physix.update();
			currCell.Physix.checkPostPhysix(this.entMan.getAll());
			
			super.update();
		}
	}

}