package gtsinf.world 
{
	import flash.display.Sprite;
	import flash.geom.Point;
	import gts.core.Verbose;
	import gts.ents.basic.Entity;
	import gts.system.GameAPI;
	import gts.world.objects.collision.CollisionObject;
	import gts.world.objects.structures.WorldStructure;
	import gts.world.objects.utils.filters.CustomClassObjectFilter;
	import gtsinf.world.utils.filters.SolidObjectFilter;
	import gts.world.objects.utils.filters.WorldObjectFilter;
	import gts.world.serialization.EntityInfo;
	import gts.world.serialization.ISerializeableWorldObject;
	import gts.world.serialization.WorldObjectSZList;
	import gtsinf.ents.EntItem;
	import gtsinf.ents.EntMarker;
	import gtsinf.ents.misc.points.EntPathNode;
	import gtsinf.world.ground.GroundTile;
	import gtsinf.world.ground.GroundTileHeightMap;
	import gtsinf.world.physix.WorldCollisionResult;
	import gtsinf.world.serialization.CollisionInfo;
	import gtsinf.world.serialization.InfEntitySerializer;
	import gtsinf.world.serialization.InfMapCellSetup;
	import gtsinf.world.serialization.StructureInfo;
	import gtsinf.world.serialization.TileInfo;
	import yaway.lights.LightInfo;
	import yaway.topdown.world.AwayCoordSpace;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class InfMapCellBuilder 
	{
		static private const DEFAULT_SIZE:Point = new Point(1000, 1000);
		
		private var entSZ:InfEntitySerializer;
		private var worldAPI:InfWorldAPI;
		private var world:InfWorld;
		private var cell:InfMapCell;
		
		// **************************************
		// DE-/INIT
		// **************************************
		
		public function InfMapCellBuilder(worldAPI:InfWorldAPI) 
		{
			this.worldAPI = worldAPI;
			world = worldAPI.world;
			
			entSZ = new InfEntitySerializer(worldAPI.db);
		}
		
		public function deinit():void
		{
			worldAPI = null;
			world = null;
			entSZ = null;
		}
		
		// **************************************
		// BUILDER
		// **************************************
		
		public function buildNew():InfMapCell
		{
			cell = new InfMapCell();
			world.setCurrCell(cell);
			return cell;
		}
		
		public function buildFromInfo(winf:InfWorldCellCreationInfo):void
		{
			var setup:InfMapCellSetup = winf.setup || new InfMapCellSetup();
			
			cell.configure(worldAPI.settings, winf.setup);
			winf.cell = cell;
			cell.buildLevels(setup.numLayers, setup.size.x, setup.size.y);
			
			cell.CoordSpace = new AwayCoordSpace(cell.GroundLevel.layer);
			
			// coll
			var dcanv:Sprite = new Sprite();
			cell.Physix.setDebugCanvas(dcanv);
			GameAPI.TheOne.getStage().addChild(dcanv);
			
			dcanv.scaleY = -1;
			//GameAPI.TheOne.Layers.FG_1.addChild(dcanv);
			cell.Physix.createScene(worldAPI.db.collisionFactory.CollFlagMap);
			
			var settings:Object = GameAPI.TheOne.mainConfig.settings;
			var lightsEnabled:Boolean = settings.worldEngine.enableLights;
			
			if (lightsEnabled)
			{
				if (settings.worldEngine.useDefaultSceneLight)
				{
					var lightInfo:LightInfo = new LightInfo();
					if(winf.setup.skyLight)
					{
						lightInfo.Deserialize(winf.setup.skyLight);
					}
					
					cell.Scene.Lights.buildSceneLight(lightInfo);
				}
			}

			deserializeList(winf.structures, createStructureFromInfo);
			deserializeList(winf.tiles,      createTileFromInfo     );
			deserializeList(winf.chars,      createCharFromInfo     );
			deserializeList(winf.items,      createItemFromInfo     );
			deserializeList(winf.vehicles,   createVehicleFromInfo  );
			deserializeList(winf.misc,       createMiscFromInfo     );
			deserializeList(winf.lights,     createMiscFromInfo     );
			deserializeList(winf.collision,  createCollFromInfo     );
			
			if (!settings.worldSettings.collision)
			{
				worldAPI.world.CurrCell.setCollisionVisibility(false);
			}
			
			if (!settings.worldSettings.markers)
			{
				worldAPI.world.CurrCell.setMarkerVisibility(false);
			}
			
			if (lightsEnabled)
			{
				cell.Scene.Lights.revalidate();
			}
		}
		
		public function buildForGame():void 
		{
			cell.buildStructureEntities();
			initEntityDefaultStates();
		}
		
		public function buildLinkages():void 
		{
			var nodes:Array = cell.EntMan.getAllByFilter(new CustomClassObjectFilter(EntPathNode));
			for each(var node:EntPathNode in nodes)
			{
				linkNode(node);
			}
		}
		
		private function linkNode(source:EntPathNode):void 
		{
			for each(var id:String in source.Node.ids)
			{
				var otherEnt:Entity = cell.EntMan.getByID(id);
				
				if (otherEnt == null)
				{
					Verbose.logError(new Error("Link target not found: " + id));
					continue;
				}
				
				if (otherEnt is EntPathNode == false)
				{
					Verbose.logError(new Error("Link target invalid: " + id +";" + String(otherEnt)));
					continue;
				}
				
				source.Node.entities.push(otherEnt);
			}
		}
		
		private function initEntityDefaultStates():void 
		{
			var entities:Array;
			var ent:Entity;
			
			var filter:WorldObjectFilter = new SolidObjectFilter();
			var result:WorldCollisionResult = new WorldCollisionResult();
			
			// tell items that are leaning to or lying on some level structure that they shall not be repelled by them
			// (but they would then slip through walls and so on if physical effects were applied)
			entities = cell.EntMan.getAllByClass(EntItem);
			
			for each(ent in entities)
			{
				var itm:EntItem = EntItem(ent);
				cell.Physix.getCollidingObjectsAt(ent.Position, filter, result, itm.CollFlags);
				
				if (result.positive)
				{
					itm.setSolid(false);
				}
			}
		}
		
		
		private function deserializeList(list:WorldObjectSZList, creationMethod:Function):void
		{
			if (!list)
				return;
			
			list.readFromSource();
			while (list.readNextObject())
			{
				creationMethod(list.current);
			}
		}
		
		private function createTileFromInfo(node:ISerializeableWorldObject):void
		{
			var tinf:TileInfo = TileInfo(node);
			var tile:GroundTile = cell.GroundLevel.Tiles.getByIndex(tinf.x, tinf.z);
			
			if(tinf.h != null)
				tile.heightMap = GroundTileHeightMap.deserialize(tile.heightMap, tinf.h);
			
			db.tileFactory.applyModel(tile, tinf.objID, tinf.libID, tinf.r, tinf.fx, tinf.fy);
			cell.Scene.Lights.checkTile(tile);
		}
		
		private function createStructureFromInfo(node:ISerializeableWorldObject):void
		{
			var sinf:StructureInfo = StructureInfo(node);
			
			var structure:WorldStructure = db.structureFactory.create(sinf.objID, sinf.libID, sinf.skin, sinf.properties);
			
			if(sinf.id)
				structure.id = sinf.id;
			
			if (structure != null)
			{
				structure.Transforms = sinf.transforms.clone();
				cell.addStructure(structure);
			}
		}
		
		private function createCollFromInfo(node:ISerializeableWorldObject):void
		{
			var collInfo:CollisionInfo = CollisionInfo(node);
			var coll:CollisionObject = db.collisionFactory.createByInfo(collInfo);
			coll.Transforms = collInfo.transforms.clone();
			cell.addColl(coll);
		}
		
		private function createCharFromInfo(node:EntityInfo):void
		{
			handleEntity(node, db.CharLib.createByInfo(node));
		}
		
		private function createItemFromInfo(node:EntityInfo):void
		{
			handleEntity(node, db.ItemLib.createByInfo(node));
		}
		
		private function createMiscFromInfo(node:EntityInfo):void
		{
			handleEntity(node, db.MiscLib.createByInfo(node));
		}
		
		private function createVehicleFromInfo(node:EntityInfo):void
		{
			handleEntity(node, db.VehicleLib.createByInfo(node));
		}
		/*
		private function createMonsterFromInfo(node:EntityInfo):void
		{
			handleEntity(node, db.MonsterLib.createByInfo(node));
		}
		*/
		
		
		private function handleEntity(info:EntityInfo, ent:Entity):void
		{
			ent.Transforms = info.transforms.clone();
			entSZ.deserilaizeEntity(ent, info);
			world.addEnt(ent);
		}
		
		// OBSOLETE- but still interesting!
		/*
		public function buildFromMC( mc:MovieClip ):MapCell
		{
			var cell:MapCell = new MapCell();
			this.cell = cell;
			
			for (var i:int = 0; i < mc.numChildren; i++ )
			{
				var d:DisplayObject = mc.getChildAt(i);
				
				if (d is Marker_Basic)
				{
					handleMarker(Marker_Basic(d));
				}
				else
				{
					var str:String = d.toString();
					
					if (str.indexOf(IDENTIFIER_COLL) != -1)
					{
						var entColl:EntCollision = getCollisionEntity(str);
						
						var body:b2Body = cell.Physix.handleCollBrush(d, IDENTIFIER_COLL, mc);
						body.SetUserData(entColl);
					}
					else
					{
						// is tile? cache as bitmap
						if (str.indexOf("MovieClip") != -1)
						{
							var mov:MovieClip = MovieClip(d);
							mov.mouseChildren = false;
							mov.mouseEnabled = false;
							if (mov.numChildren == 0 || (mov.numChildren == 1 && mov.getChildAt(0) is Shape))
							{
								if(isTile(d))
									mov.cacheAsBitmap = true;
							}
						}
					}
				}
			}
			
			if (!cell.getSize())
			{
				cell.setSize(DEFAULT_SIZE);
			}
			
			this.cell = null;
			
			cell.Script.init();
			
			return cell;
		}
		*/
		/*
		private function isTile(d:DisplayObject):Boolean
		{
			return d.width > 390 && d.width < 410 && d.height > 390 && d.height < 410;
		}
		*/
		
		
		
		private function handleMarker(marker:EntMarker):void
		{
			//cell.Script.MarkMan.register(marker);
			
			// substitute title with name, because name can change automatically if DOH is changed and is therefore unsafe.
			// DOH = display object hierarchy
			/*
			if(!marker.title && marker.name && marker.name.indexOf("instance") != 0)
			{
				marker.title = marker.name;
			}
			*/
			
			
			/*
			var markerClassName:String = getQualifiedClassName(marker);
			
			switch(markerClassName)
			{
				case "Marker_ExitZone":
				{
					if (Marker_ExitZone(marker).locked)
					{
						trace("exitzone locked: " + marker.name);
						return;
					}
					
					var zone:BasicZone = new BasicZone(ZoneType.EXIT);
					zone.Title = marker.title;
					cell.ZoneMan.addMember(zone);
					initZoneStats(zone, marker);
				}
				break;
				
				case "Marker_MapInfo":
				{
					cell.setSize(Marker_MapInfo(marker).size);
				}
				break;
				
				case "Marker_CellBorder":
				{
					var edge:String = Marker_CellBorder(marker).edge;
					
					applyBorder(edge, marker.getBounds(marker.parent));
				}
				break;
				
				case "Marker_Animal":
				{
					trace(Marker_Animal(marker).chance);
				}
				break;
				
			}
			*/
		}
		
		
		
		/*
		private function applyBorder(edge:String, bounds:Rectangle):void 
		{
			switch(edge)
			{
				case null: // by a bug, "left" is sometimes null
				case "left":	cell.getBorderBounds().left   = bounds.left;	break;
				case "top":		cell.getBorderBounds().top    = bounds.top;		break;
				case "bottom":	cell.getBorderBounds().bottom = bounds.bottom;	break;
				case "right":	cell.getBorderBounds().right  = bounds.right;	break;
			}
		}
		*/
		
		/*
		private function initZoneStats(zone:BasicZone, d:DisplayObject):void
		{
			zone.setWorldBounds(d.getBounds(d.parent));
			var rot:Number = d.rotation;
			zone.setOrientation(rot);
			d.rotation = 0;
			zone.setShapeBounds(d.getBounds(d.parent));
			d.rotation = rot;
			zone.Center = new Point(d.x, d.y);
		}
		*/
		
		
		// **************************************
		// GETTERS AND SETTERS
		// **************************************
		
		private function get db():InfWorldDataBase
		{
			return worldAPI.db;
		}
		
	}

}