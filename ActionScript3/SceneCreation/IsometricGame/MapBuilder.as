package bbm.world.map 
{
	import bbm.assets.ContentKit;
	import bbm.core.Verbose;
	import bbm.ents.EntChar;
	import bbm.ents.EntItem;
	import bbm.ents.Entity;
	import bbm.ents.EntMarker;
	import bbm.ents.EntProp;
	import bbm.ents.factories.EntityDatabase;
	import bbm.events.world.WorldEvent;
	import bbm.world.cache.MapCache;
	import bbm.world.map.components.view.MapViewComponent;
	import bbm.world.map.decals.DecalFactory;
	import bbm.world.map.decals.DecalManager;
	import bbm.world.map.decals.MapDecal;
	import bbm.world.map.events.MapBuildEvent;
	import bbm.world.map.view.MapViewDecalManager;
	import bbm.world.map.view.ViewMap;
	import bbm.world.map.view.ViewTile;
	import bbm.world.regions.*;
	import bbm.world.World;
	import flash.geom.Point;
	import yves.debug.logger.ErrorLog;
	import yves.events.EventManager;
	import yves.net.StringParser;
	import yves.net.YSON;
	
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class MapBuilder 
	{
		private var eventMan:EventManager;
		public function get EventMan():EventManager { return eventMan; }
		
		private var mapLib:MapDatabase;
		
		private var mapCache:MapCache;
		
		private var entityDB:EntityDatabase;
		
		private var errorLog:ErrorLog;
		
		
		public function MapBuilder(mapLib:MapDatabase, entFactory:EntityDatabase)
		{
			this.mapLib = mapLib;
			this.entityDB = entFactory;
			
			eventMan = new EventManager("MapBuilder");
		}
		
		// WRITE MAP
		// [...]
		
		// LOAD MAP
		
		/**
		 * Creates a raw map object out of String in JSON or YSON-format.
		 */
		static public function fromStringToRawMap(source:String):Object
		{
			var src:Object;
			src = StringParser.parseObjectFromString(source);
			return src;
		}
		
		/**
		 * Creates a map out of given source String and connects it to given world.
		 * The source-String may be in JSON or in YSON format.
		 */
		public function createFromString( str:String, world:World ):Map
		{
			return createFromSource(fromStringToRawMap(str), world);
		}
		
		public function createFromSource( src:Object, world:World ):Map
		{
			errorLog = new ErrorLog();
			
			mapCache = world.getMapCache();
			if (mapCache && mapCache.isEmpty())
				mapCache = null;
			
			if (src.tilesX === undefined)
			{
				errorLog.log(new Error("MapSource' x-Dimensions was invalid."), "resetting tilesX to default (30)");
				src.tilesX = 30;
			}
			
			var map:Map = createMap(src.tilesX, src.tilesY);
			
			// init world
			world.CurrMap = map;
			eventMan.addEvent(new MapBuildEvent(MapBuildEvent.CONNECTED_2WORLD, map));
			
			// init meta data
			map.Title = ( src.title != undefined ) ?  src.title : "untitled map";
			
			// init components    -   THAT are ALL VIEW components!!!!  REFACTOR!!!!
			var view:MapViewComponent = MapViewComponent(map.Comps.add(new MapViewComponent()));
			
			world.EventMan.addEvent(new WorldEvent(WorldEvent.WORLD_MAP_READY));
			
			var decalMan:MapViewDecalManager = MapViewDecalManager(map.Comps.add(new MapViewDecalManager(view.DecalLayer)));
			
			var bg:MapBackground = MapBackground(map.Comps.add(new MapBackground()));
			
			// make sure that all needed components exist
			map.checkComponents();
			map.initManagers();
			
			// apply component data ------------
			
			if ( src.bgID )
			{
				bg.load( src.bgID );
			}
			
			if ( src.bgOffset )
			{
				var offset:Point = new Point( src.bgOffset.split("|")[0], src.bgOffset.split("|")[1] )
				bg.readjustOffset(offset);
			}
			
			// apply tile type matrix
			Verbose.log( "apply tile type matrix", Verbose.Flags.BUILD_MAP );
			// create TileTypeMatrix (prepare to build tiles)
			var rawTiles:Array;
			var presetTiles:Array = mapLib.getTypeMapPreset( src.bgID );
			
			if ( src.tiles != undefined )
			{
				rawTiles = src.tiles;
			}
			else
			{
				rawTiles = presetTiles;
				if ( presetTiles == null )
				{
					errorLog.log(new Error( "No custom tileMatrix exists and there is no preset."), "generated default matrix");
					rawTiles = mapLib.genRawTileMatrix(map.TilesX, map.TilesY, 0);
				}
			}
			
			buildTiles( parseTiles( rawTiles ), map );
			map.VisMan.showAll();
			
			eventMan.addEvent(new MapBuildEvent(MapBuildEvent.TILES_INITED, map));
			
			// now build tile models
			view.buildTiles();
			
			// add decals
			Verbose.log( "add decals", Verbose.Flags.BUILD_MAP );
			if ( src.decals == undefined )
				src.decals = new Array();
			createDecals( src.decals, decalMan );
			
			if(ContentKit.RESOURCE_OVERDRIVE)
				decalMan.mergeWithBackground(bg);
			
			// add entities
			Verbose.log( "add entities", Verbose.Flags.BUILD_MAP );
			createEntities( src.props,   entityDB.Props.create,  map );
			createEntities( src.items,   entityDB.Items.create,   map );
			createEntities( src.chars,   entityDB.Chars.create,   map );
			createEntities( src.markers, entityDB.Misc.createMarker, map );
			
			// add regions
			Verbose.log( "add regions", Verbose.Flags.BUILD_MAP );
			if ( src.regions == undefined )
				src.regions = new Object();
			
			createRegions( src.regions.spawn, map.RegionMan.SpawnRegions, map );
			createRegions( src.regions.other, map.RegionMan.OtherRegions, map );
			
			if ( errorLog.NumEntries > 0 )
			{
				Verbose.logError(new Error("MapCreationError"));
				Verbose.log( errorLog.toString() );
			}
			
			mapCache = null;
			
			return map;
		}
		
		
		private function parseTiles( rawTiles:Array ):Array
		{
			if ( rawTiles == null )	return null;
			
			var i:int;
			var j:int;
			var tilesX:int = String(rawTiles[0]).length;
			var tilesY:int = rawTiles.length;
			var tileType:Array = new Array();
			
			for( i=0; i<tilesX; i++ )
			{
				var column:Array = ( i<rawTiles.length ) ? rawTiles[i].split("") : [];
				
				for(j=0; j<tilesY; j++)
				{
					var strType:String = ( j<column.length ) ? column[j] : "0";
					var type:uint = stringToTileType(strType);
					column[ j ] = type;
				}
				tileType[i] = column;
			}
			
			return tileType;
		}
		
		private function buildTiles( tileData:Array, map:Map ):void
		{
			var i:int;
			var j:int;
			
			var tiles:Vector.<Vector.<Tile>> = map.Tiles;
			var tileList:Vector.<Tile> = map.TileList;
			var tilesX:int = map.TilesX;
			var tilesY:int = map.TilesY;
			var tileWidth:int = Map.tileWidth;
			var tileHeight:int = Map.tileHeight;
			
			// MODEL
			for ( i = 0; i < tilesX; i++ )
			{
				tiles[ i ] = new Vector.<Tile>();
				for( j=0; j<tilesY; j++ )
				{
					var tile:Tile = createTile(i, j);
					
					tileList.push( tile );
					tiles[ i ][ j ] = tile;
					
					if( tileData != null && i < tileData.length && j < tileData[i].length )
					{
						tile.setType( tileData[ i ][ j ] );
					}
				}
			}
			
			// adjust positions
			var X:Number;
			var Y:Number;
			
			X = 0;
			Y = 0;
			
			for ( j = 0; j < tilesY; j++ )
			{
				X =  j * tileWidth  / 2;
				Y = -j * tileHeight / 2;
				
				for ( i = 0; i < tilesX; i++ )
				{
					tiles[ i ][ j ].x = X;
					tiles[ i ][ j ].y = Y;
					
					X += tileWidth/2;
					Y += tileHeight/2;
				}
			}
		}
		
		protected function createMap(tilesX:int, tilesY:int):Map
		{
			return new ViewMap(tilesX, tilesY);
		}
		
		protected function createTile(row:int,column:int):Tile
		{
			return new ViewTile(row, column);
		}
		
		private function createRegions( regList:Array, regMan:RegionManager, map:Map ):void
		{
			if ( regList != null )
			{
				for( var i:int=0; i<regList.length; i++ )
				{
					var regData:Object = regList[i];
					var id     :String = regData.id != undefined ? regData.id : "";
					var loca   :Array  = regData.loc;
					var index  :int    = regData.index;
					
					var region :Region = new RectangularRegion( loca[0], loca[1], loca[2], loca[3] );
					
					region.ID    =    id;
					region.Index = index;
					
					if(regData.data)
						region.data = regData.data;
					
					regMan.addMember( region );
				}
			}
		}
		
		private function createEntities( entList:Object, entCreateMethod:Function, map:Map ):void
		{
			if ( entList == null )
				return;
			
			var mapEntInfo:MapEntityInfo = new MapEntityInfo("dummy");
			var error:Error;
			
			for ( var entLink:String in entList )
			{
				var entData:Array = entList[ entLink ];
				mapEntInfo.entLink = entLink;
				
				for ( var j:int = 0; j < entData.length; j++ )
				{
					try
					{
						var pos:Point  = new Point( entData[j][0], entData[j][1] );
						var subInfo:Object = entData[ j ][ 2 ];
						
						mapEntInfo.frame    = ((subInfo && subInfo.fr != undefined) ? subInfo.fr : -1);
						mapEntInfo.subModel = ((subInfo && subInfo.sm != undefined) ? subInfo.sm :  0);
						var entID:String = subInfo != null ? subInfo.id : null;
						
						if (entID && mapCache && mapCache.isKilled(entID))
							continue;
						
						mapEntInfo.data = subInfo;
						var ent:Entity = entCreateMethod(mapEntInfo);
						entityDB.Serializer.deserialize(ent, mapEntInfo.data);
						
						map.addEntAtCR( ent, pos.x, pos.y );
					}
					catch (err:Error)
					{
						error = err;
						errorLog.log( err, entLink + " " + j + " was not added" );
					}
				}
			}
		}
		
		private function createDecals( decalList:Array, decalMan:DecalManager ):void
		{
			if ( decalList == null )
				return;
			
			var decalFactory:DecalFactory = new DecalFactory();
			
			for ( var i:int = 0; i < decalList.length; i++ )
			{
				var decalData:Array = decalList[i];
				try 
				{
					var link:String = decalData[0];
					var pos:Point = new Point( decalData[1], decalData[2] );
					var subInfo:Object = decalData[3];
					
					var decal:MapDecal = decalFactory.createDecal( 
						link,
						(subInfo && subInfo.id) ? subInfo.id : null,
						(subInfo && subInfo.mx) ? subInfo.mx : false,
						(subInfo && subInfo.my) ? subInfo.my : false
					);
					
					decal.x = pos.x;
					decal.y = pos.y;
					
					decalMan.addMember(decal);
				}
				catch (err:Error)
				{
					errorLog.log( err, link + " " + i + " was not added" );
				}
			}
		}
		
	}
}