package gtsinf.world 
{
	import away3d.containers.ObjectContainer3D;
	import away3d.containers.View3D;
	import flash.geom.Point;
	import flash.geom.Rectangle;
	import flash.geom.Vector3D;
	import gts.ents.basic.Entity;
	import gts.ents.basic.EntityFinder;
	import gts.ents.basic.EntityManager;
	import gts.world.BasicMapCell;
	import gts.world.interfaces.ICollidableWorldObject;
	import gts.world.objects.collision.ColliderList;
	import gts.world.objects.collision.CollisionObject;
	import gts.world.objects.structures.StructureBasedEntity;
	import gts.world.objects.structures.StructureList;
	import gts.world.objects.structures.WorldStructure;
	import gts.world.utils.CoordinateSpace;
	import gtsinf.ents.EntMarker;
	import gtsinf.ents.components.AwayModelComponent;
	import gtsinf.ents.misc.lights.EntLight;
	import gtsinf.ents.misc.lights.EntLightSpot;
	import gtsinf.ents.misc.zones.EntZone;
	import gtsinf.world.ground.WorldGround;
	import gtsinf.world.physix.WorldPhysixComponent;
	import gtsinf.world.physix.WorldPhysixStats;
	import gtsinf.world.scripts.InfMapCellScript;
	import gtsinf.world.serialization.InfMapCellSetup;
	import yaway.lights.ILightSource;
	import yaway.managers.ContainerManager;
	import yaway.models.basic.IBasicAwayModel;
	import yaway.topdown.TopDownAwayScene;
	import yves.events.EventManager;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class InfMapCell extends BasicMapCell
	{
		public var coordSpace:CoordinateSpace;
		public function set CoordSpace(value:CoordinateSpace):void { coordSpace = value; }
		public function get CoordSpace():CoordinateSpace { return coordSpace; }
		
		private var interior:Boolean;
		public function isInterior():Boolean { return interior; }
		
		private var size:Point;
		public function getSize():Point { return size; }
		public function setSize(value:Point):void { size = value; }
		
		private var script:InfMapCellScript;
		public function get Script():InfMapCellScript { return script; }
		
		private var physix:WorldPhysixComponent;
		public function get Physix():WorldPhysixComponent { return physix; }
		
		private var world:InfWorld;
		public function getWorld():InfWorld { return world; }
		public function setWorld(value:InfWorld):void { world = value; }
		
		private var borderBounds:Rectangle;
		public function getBorderBounds():Rectangle { return borderBounds; }
		
		private var awayScene:TopDownAwayScene;
		public function get Scene():TopDownAwayScene { return awayScene; }
		
		
		private var layMan:ContainerManager;
		public function get LayMan():ContainerManager { return layMan; }
		
		private var structureList:StructureList;
		public function get Structures():StructureList { return structureList; }
		
		private var colliderList:ColliderList;
		public function get Colliders():ColliderList { return colliderList; }
		
		private var groundLevel:WorldGround;
		public function get GroundLevel():WorldGround { return groundLevel; }
		
		private var settings:InfWorldSettings;
		public function get Settings():InfWorldSettings { return settings; }
		
		private var setup:InfMapCellSetup;
		public function get Setup():InfMapCellSetup { return setup; }
		
		// vis-flags
		private var collVisible:Boolean = true;
		private var markerVisible:Boolean = true;
		private var structuresVisible:Boolean = true;
		private var lightsVisible:Boolean = true;
		
		
		// **************************************
		// DE-/INIT
		// **************************************
		
		public function InfMapCell() 
		{
			awayScene = new TopDownAwayScene();
			awayScene.setup();
			
			var cont:ObjectContainer3D = new ObjectContainer3D();
			awayScene.scene.addChild(cont);
			layMan = new ContainerManager(cont);
			
			entMan = new EntityManager();
			script = new InfMapCellScript(this);
			physix = new WorldPhysixComponent();
			
			eventMan = new EventManager();
			entFinder = new EntityFinder();
			entFinder.init(entMan);
			
			borderBounds = new Rectangle(1000, 1000);
			
			structureList = new StructureList();
			colliderList = new ColliderList();
		}
		
		// INITIALIZATION
		
		public function configure(settings:InfWorldSettings, setup:InfMapCellSetup):void
		{
			this.settings = settings;
			this.setup = setup;
			
			var stats:WorldPhysixStats = new WorldPhysixStats();
			
			physix.configure(stats, settings.physScale, settings.worldScale);
		}
		
		override public function init():void
		{
			super.init();
		}
		
		override public function deinit():void
		{
			super.deinit();
			
			removeAllEntities();
			removeAllStructures();
			removeAllColliders();
			
			awayScene.deinit();
			awayScene = null;
			
			groundLevel.deinit();
			groundLevel = null;
			
			layMan.deinit();
			layMan = null;
			
			physix.deinit();
			physix = null;
			
			script.deinit();
			script = null;
			
			entFinder.deinit();
			entFinder = null;
			
			entMan.clear();
			entMan = null;
			
			colliderList.clear();
			colliderList = null;
			
			structureList.clear();
			structureList = null;
		}
		
		
		public function buildLevels(numLevels:int, tilesX:int, tilesY:int):void
		{
			groundLevel = new WorldGround();
			layMan.buildLayers(numLevels);
			
			groundLevel.init(layMan.getLayerAt(0));
			groundLevel.buildTileMatrix(tilesX, tilesY, settings.tileSize);
		}
		
		/**
		 * Only called once, and not for Editor!
		 * structure based entities are created out of structures that own properties.
		 * This is needed for ingame happenings because only entities are refferable to, structures not.
		 */
		public function buildStructureEntities():void
		{
			var allStructures:Array = structureList.getAsArray();
			
			for each(var structure:WorldStructure in allStructures)
			{
				if (structure.Properties != null)
				{
					var ent:StructureBasedEntity = convertStructureToEntity(structure);
				}
			}
		}
		
		public function convertStructureToEntity(structure:WorldStructure):StructureBasedEntity
		{
			var ent:StructureBasedEntity = world.DB.structureEntityFactory.buildEntFromStructure(structure);
			
			if (ent != null)
			{
				world.addEnt(ent);
			}
			
			return ent;
		}
		
		public function bake():void 
		{
			physix.bakeCollisions(colliderList);
			physix.bakeStructureCollisions(structureList);
			physix.bakeZoneColliders(world.EntFinder.getAllEntsByClass(EntZone));
			
			//baked = true;
		}
		 
		// ELEMENT REGISTRATION
		
		override public function addEnt(ent:Entity):void
		{
			entMan.addMember(ent);
			
			if (ent.View == null)
				ent.CompPack.Add(new AwayModelComponent());
			
			awayScene.addModel(IBasicAwayModel(ent.View.getModel()));
			
			if (ent is EntLight)
			{
				var light:EntLight = EntLight(ent);
				
				if (light.getLight() == null)
				{
					EntLight(ent).setLight(awayScene.Lights.createLight(EntLight(ent).getLightInfo()));
					EntLight(ent).applyTransforms();
					
					if (ent is EntLightSpot && ent.View.getModel() != null)
					{
						awayScene.Lights.linkModel(IBasicAwayModel(ent.View.getModel()));
					}
				}
			}
			
			if (ent is ILightSource)
				awayScene.Lights.addLight(ILightSource(ent).getLight());
			
			//if (baked)
				//physix.bakeCollisionsOf(ent);
			
			physix.createBodyOfEnt(ent);
			
			if(ent.View.getModel() != null)
				groundLevel.layer.addChild(IBasicAwayModel(ent.View.getModel()).getRootContainer());
			
			eventAddedObject.object = ent;
			eventMan.addEvent(eventAddedObject);
		}
		
		override public function removeEnt(ent:Entity):void
		{
			entMan.removeMember(ent, !engineRunning);
			awayScene.removeModel(IBasicAwayModel(ent.View.getModel()));
			
			if (ent is ILightSource)
			{
				awayScene.Lights.removeLight(ILightSource(ent).getLight());
				
				// TODO: remove spot light model
			}
			
			physix.destroyBodyOfEnt(ent);
			
			eventRemovedObject.object = ent;
			eventMan.addEvent(eventRemovedObject);
		}
		
		
		public function addStructure(structure:WorldStructure):void
		{
			structureList.add(structure);
			
			awayScene.addModel(IBasicAwayModel(structure.View.getModel()));
			groundLevel.layer.addChild(IBasicAwayModel(structure.View.getModel()).getRootContainer());
			
			eventAddedObject.object = structure;
			eventMan.addEvent(eventAddedObject);
			
			if(structure.Collider)
				structure.Collider.View.setVisible(collVisible);
		}
		
		public function removeStructure(structure:WorldStructure):void
		{
			structureList.remove(structure);
			
			awayScene.removeModel( IBasicAwayModel(structure.View.getModel()));
			
			eventRemovedObject.object = structure;
			eventMan.addEvent(eventRemovedObject);
		}
		
		
		public function addColl(object:CollisionObject):void
		{
			colliderList.add(object);
			
			if (object.View.getModel())
			{
				awayScene.addModel( IBasicAwayModel(object.View.getModel()));
				groundLevel.layer.addChild(IBasicAwayModel(object.View.getModel()).getRootContainer());
			}
			
			eventAddedObject.object = object;
			eventMan.addEvent(eventAddedObject);
			
			object.View.setVisible(collVisible);
		}
		
		public function removeColl(object:CollisionObject):void
		{
			colliderList.remove(object);
			
			if (object.View.getModel())
			{
				awayScene.removeModel( IBasicAwayModel(object.View.getModel()));
			}
			
			eventRemovedObject.object = object;
			eventMan.addEvent(eventRemovedObject);
		}
		
		// total removal
		
		private function removeAllColliders():void 
		{
			var allWorldObjects:Array = colliderList.getAsArray().concat();
			for each(var worldObject:CollisionObject in allWorldObjects)
			{
				// has no own world reference, remove manually
				removeColl(worldObject);
				worldObject.kill();
			}
		}
		
		private function removeAllStructures():void 
		{
			var allWorldObjects:Array = structureList.getAsArray().concat();
			for each(var worldObject:WorldStructure in allWorldObjects)
			{
				// has no own world reference, remove manually
				removeStructure(worldObject);
				
				//var mdl:IModel = worldObject.View.getModel();
				
				//if(mdl != null)
					//mdl.deinit();
				
				worldObject.kill();
			}
		}
		
		
		
		// element visibility
		
		public function toggleCollisionVis():void 
		{
			setCollisionVisibility( !collVisible );
		}
		
		public function setCollisionVisibility(value:Boolean):void 
		{
			collVisible = value;
			
			var matchingEnts:Array = colliderList.getAsArray();
			
			for each (var coll:CollisionObject in matchingEnts)
			{
				coll.View.setVisible(collVisible);
			}
			
			matchingEnts = structureList.getAsArray();
			for each (var relColl:ICollidableWorldObject in matchingEnts)
			{
				if(relColl.Collider)
					relColl.Collider.View.setVisible(collVisible);
				
				if(relColl is WorldStructure && WorldStructure(relColl).InteractionCollider != null)
					WorldStructure(relColl).InteractionCollider.View.setVisible(collVisible);
			}
		}
		
		
		public function toggleMarkerVis():void 
		{
			setMarkerVisibility( !markerVisible );
		}
		
		public function setMarkerVisibility(value:Boolean):void
		{
			markerVisible = value;
			
			var matchingEnts:Array = [];
			
			entFinder.getAllEntsByClass(EntMarker, matchingEnts);
			entFinder.getAllEntsByClass(EntLight,  matchingEnts);
			
			for each (var ent:Entity in matchingEnts)
			{
				ent.View.setVisible(markerVisible);
			}
		}
		
		public function toggleStructureVis():void 
		{
			setStructureVisibility( !structuresVisible );
		}
		
		public function setStructureVisibility(value:Boolean):void
		{
			structuresVisible = value;
			
			var matching:Array = structureList.getAsArray();
			
			for each (var structure:WorldStructure in matching)
			{
				structure.View.setVisible(structuresVisible);
			}
		}
		
		
		
		public function toggleLightVis():void 
		{
			setLightVisibility( !lightsVisible );
		}
		
		public function setLightVisibility(value:Boolean):void
		{
			lightsVisible = value;
			Scene.Lights.Enabled = value;
			
			/*
			var matching:Array = structureList.getAsArray();
			
			for each (var structure:WorldStructure in matching)
			{
				structure.View.setVisible(structuresVisible);
			}*/
		}
		
		
		
		public function get MainLayer():View3D { return awayScene.view; }
		
		public function get Width():int  { return size.x; }
		
		public function get Height():int { return size.y; }
		
		public function get TileSize():int { return settings.tileSize; }
		
		public function get TileCenter():Vector3D { return new Vector3D(groundLevel.Tiles.TilesX / 2, 0, -groundLevel.Tiles.TilesY / 2); }
		
		public function get MetricCenter():Vector3D { return new Vector3D(groundLevel.Tiles.TilesX / 2 * TileSize,0, -groundLevel.Tiles.TilesY / 2 * TileSize); }
	}

}