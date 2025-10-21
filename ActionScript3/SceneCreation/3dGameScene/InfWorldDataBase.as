package gtsinf.world 
{
	import gts.assets.ConfigDataBase;
	import gts.assets.ContentKit;
	import gts.assets.terrain.TerrainAsset;
	import gts.assets.terrain.TerrainAssetLibrary;
	import gts.world.objects.collision.CollisionFactory;
	import gts.world.objects.structures.StructureEntFactory;
	import gts.world.objects.structures.StructureFactory;
	import gts.world.BasicWorldDataBase;
	import gtsinf.ents.basic.factories.InfCharFactory;
	import gtsinf.ents.basic.factories.EntityComponentFactory;
	import gtsinf.ents.basic.factories.EnvEntityFactory;
	import gtsinf.ents.basic.factories.InfItemFactory;
	import gtsinf.ents.basic.factories.TopDownModelFactory;
	import gtsinf.ents.basic.factories.VehicleFactory;
	import gtsinf.ents.basic.libs.MiscEntityFactory;
	import gtsinf.world.clusters.MapClusterFactory;
	import gtsinf.world.ground.GroundTileFactory;
	import gtsinf.world.physix.PhysixFactory;
	import yaway.models.compound.AwayCompoundModelFactory;
	import yaway.models.cutout.AwayCutoutModelFactory;
	import yaway.models.misc.AwayMiscModelFactory;
	import yaway.textures.AnimationContentMultiTex;
	import yves.data.CascadingDataTree;
	import yves.data.sets.ObjectListDataSet;
	import yves.game.assets.AssetStorage;
	import yves.utils.ArrayUtil;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class InfWorldDataBase extends BasicWorldDataBase
	{
		private var terrainAssets:TerrainAssetLibrary;
		public function get TerrainAssets():TerrainAssetLibrary { return terrainAssets; }
		
		
		public var assetStorage:AssetStorage;
		
		
		// other world objects
		public var tileFactory:GroundTileFactory;
		
		public var collisionFactory:CollisionFactory;
		
		public var structureFactory:StructureFactory;
		
		public var structureEntityFactory:StructureEntFactory;
		
		private var cellComplexFactory:MapClusterFactory;
		public function get CellComplexFactory():MapClusterFactory { return cellComplexFactory; }
		
		
		// entities
		
		private var itemLib:InfItemFactory;
		public function get ItemLib():InfItemFactory { return itemLib; }
		
		private var charLib:InfCharFactory;
		public function get CharLib():InfCharFactory { return charLib; }
		
		private var vehicleLib:VehicleFactory;
		public function get VehicleLib():VehicleFactory { return vehicleLib; }
		
		private var miscLib:MiscEntityFactory;
		public function get MiscLib():MiscEntityFactory { return miscLib; }
		
		private var envLib:EnvEntityFactory;
		public function get EnvLib():EnvEntityFactory { return envLib; }
		
		// components and properties
		private var entCompFactory:EntityComponentFactory;
		public function get EntCompFactory():EntityComponentFactory { return entCompFactory; }
		
		
		// models
		private var cmpMdlFactory:AwayCompoundModelFactory;
		public function get CmpMdlFactory():AwayCompoundModelFactory { return cmpMdlFactory; }
		
		private var cutMdlFactory:AwayCutoutModelFactory;
		
		private var miscMdlFactory:AwayMiscModelFactory;
		
		private var loadedSetIDs:Array = [];
		public function get LoadedSetIDs():Array { return loadedSetIDs; }
		
		private var pendingSetIDs:Array = [];
		
		public function InfWorldDataBase() 
		{
			// init member fields
			entCompFactory = new EntityComponentFactory(this);
			
			terrainAssets = new TerrainAssetLibrary();
			
			collisionFactory = new CollisionFactory();
			
			cutMdlFactory = new AwayCutoutModelFactory();
			
			cmpMdlFactory = new AwayCompoundModelFactory();
			cmpMdlFactory.configure(terrainAssets);
			
			miscMdlFactory = new AwayMiscModelFactory();
			
			structureFactory = new StructureFactory(cmpMdlFactory, collisionFactory);
			
			tileFactory = new GroundTileFactory();
			tileFactory.configureLibs(terrainAssets);
			
			structureEntityFactory = new StructureEntFactory();
			structureEntityFactory.entCompFactory = entCompFactory;
		}
		
		
		public function init(contentKit:ContentKit):void
		{
			var db:ConfigDataBase = contentKit.ConfigDB;
			
			// init sub factories
			miscMdlFactory.contKit = contentKit;
			
			collisionFactory.miscModelFactory = miscMdlFactory;
			collisionFactory.dataSet = db.getConfig("coll");
			
			var data:Object = contentKit.getConfig("models");
			var modelStats:CascadingDataTree = new CascadingDataTree(data.all);
			var modelFactory:TopDownModelFactory = new TopDownModelFactory(contentKit, modelStats);
			
			var physixFactory:PhysixFactory = new PhysixFactory();
			
			// create entity factories
			
			charLib = new InfCharFactory();
			charLib.configureLibs(contentKit, new CascadingDataTree(db.getConfig("chars")));
			charLib.initSubFactories(this, collisionFactory, physixFactory, modelFactory);
			
			vehicleLib = new VehicleFactory();
			vehicleLib.configureLibs(contentKit, new CascadingDataTree(db.getConfig("vehicles")));
			vehicleLib.initSubFactories(this, collisionFactory, physixFactory, modelFactory);
			
			itemLib = new InfItemFactory();
			itemLib.configureLibs(contentKit, new CascadingDataTree(db.getConfig("items")));
			itemLib.miscModelFactory = miscMdlFactory;
			
			envLib = new EnvEntityFactory();
			envLib.configureLibs(contentKit, null);
			envLib.miscModelFactory = miscMdlFactory;
			
			miscLib = new MiscEntityFactory();
			miscLib.miscModelFactory = miscMdlFactory;
			miscLib.cmpMdlFactory = cmpMdlFactory;
			miscLib.dataSet = db.getConfig("misc");
			
			cellComplexFactory = new MapClusterFactory();
			cellComplexFactory.confgiure(db.getConfig("world"));
			cellComplexFactory.init();
			
			AnimationContentMultiTex.textureProvider = terrainAssets;
		}
		
		public function deinit():void
		{
			cellComplexFactory.deinit();
			cellComplexFactory = null;
			
			AnimationContentMultiTex.textureProvider = null;
		}
		
		// TERRAIN ASSETS
		
		public function initTerrainSets():void 
		{
			var setID:String;
			
			for each(setID in pendingSetIDs)
			{
				addTerrainSet( terrainAssets.getTerrainAsset(setID) );
			}
			
			pendingSetIDs = [];
		}
		
		public function removeTerrainSets(idsOfSetsToBeRemoved:Array):void
		{
			for each(var setID:String in idsOfSetsToBeRemoved)
			{
				if (!ArrayUtil.contains(loadedSetIDs, setID))
					continue;
				
				var terrAsset:TerrainAsset = terrainAssets.getTerrainAsset(setID);
				
				if (terrAsset != null)
				{
					removeTerrainSet(terrAsset);
				}
			}
		}
		
		
		public function loadTerrainSet(setID:String):Boolean 
		{
			if (loadedSetIDs.indexOf(setID) != -1 || pendingSetIDs.indexOf(setID) != -1)
			{
				// set is already registered
				return false;
			}
			
			pendingSetIDs.push(setID);
			terrainAssets.registerAsset(new TerrainAsset(setID, "data/terrain/" + setID + "/"), true);
			return true;
		}
		
		private function addTerrainSet(terrainAsset:TerrainAsset):void 
		{
			if (terrainAsset.approved)
				return;
			
			var terrainID:String = terrainAsset.ID;
			var tileSet:Object = terrainAsset.getTileSet();
			
			structureFactory.addConfig(terrainID, terrainAsset.getStructureSet());
			tileFactory.addConfig(terrainID, new ObjectListDataSet(tileSet.tiles as Array));
			
			loadedSetIDs.push(terrainID);
			terrainAsset.approved = true;
		}
		
		public function removeTerrainSet(terrainAsset:TerrainAsset):Boolean
		{
			var terrainID:String = terrainAsset.ID;
			
			if (loadedSetIDs.indexOf(terrainID) == -1)
				return false;
			
			structureFactory.removeConfig(terrainID);
			tileFactory.removeConfig(terrainID);
			
			ArrayUtil.removeElement(terrainID, loadedSetIDs);
			ArrayUtil.removeElement(terrainID, pendingSetIDs);
			//terrainAsset.approved = false;
			
			terrainAssets.clearByID(terrainID);
			
			// TODO: remove all used assets. (freeing memory)
			
			return true;
		}
		
	}

}