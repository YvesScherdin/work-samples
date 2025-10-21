package gtsinf.world 
{
	import gts.game.data.persons.People;
	import gts.game.data.persons.PeopleDebug;
	import gtsinf.world.serialization.InfMapCellSetup;
	import gtsinf.world.serialization.InfMapParser;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class InfWorldFactory 
	{
		private var worldAPI:InfWorldAPI;
		
		public function InfWorldFactory(worldAPI:InfWorldAPI) 
		{
			this.worldAPI = worldAPI;
		}
		
		
		public function createNew(mapID:String, editorMode:Boolean, mapReadyCallback:Function=null):void 
		{
			var parser:InfMapParser = new InfMapParser();
			var mapInfo:InfWorldCellCreationInfo = parser.read(worldAPI.db.assetStorage.getConfigData(mapID));
			
			mapInfo.mapID = mapID;
			create(mapInfo, editorMode, mapReadyCallback);
		}
		
		public function createCustom(setup:InfMapCellSetup, editorMode:Boolean, mapReadyCallback:Function=null):void
		{
			var mapInfo:InfWorldCellCreationInfo = new InfWorldCellCreationInfo();
			mapInfo.setup = setup;
			
			create(mapInfo, editorMode, mapReadyCallback);
		}
		
		
		private function create(mapInfo:InfWorldCellCreationInfo, editorMode:Boolean, mapReadyCallback:Function):void 
		{
			var world:InfWorld = new InfWorld(worldAPI.db);
			worldAPI.world = world;
			world.Context.inhabitants = PeopleDebug.generate(100);
			
			//worldAPI.settings.tileSize = editorAPI.config.start.tileSize;
			var cellBuilder:InfMapCellBuilder = new InfMapCellBuilder(worldAPI);
			var cell:InfMapCell = cellBuilder.buildNew();
			
			cellBuilder.buildFromInfo(mapInfo);
			
			cell.MapID = mapInfo.mapID;
			
			if (!editorMode)
				cellBuilder.buildForGame();
			else
				cell.buildStructureEntities();
			
			cellBuilder.buildLinkages();
			
			if(mapReadyCallback != null)
				mapReadyCallback();
		}
		
	}

}