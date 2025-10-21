package gtsinf.scene 
{
	import away3d.containers.View3D;
	import flash.events.Event;
	import gts.assets.cache.MainAssetCache;
	import gts.core.Verbose;
	import gts.ents.basic.Entity;
	import gts.events.GameSceneEvent;
	import gts.events.ents.EntityEvent;
	import gts.events.world.WorldEvent;
	import gts.gui.loading.GtsLoadingScreenA;
	import gts.scene.BasicGameScene;
	import gts.system.GameAPI;
	import gts.world.components.WComp_PlayerManager;
	import gtsinf.ai.controllers.InfAIController;
	import gtsinf.ents.EntChar;
	import gtsinf.ents.components.info.markers.ExitInfo;
	import gtsinf.ents.misc.InfEntCamera;
	import gtsinf.ents.misc.zones.EntZoneExit;
	import gtsinf.events.ExitZoneEvent;
	import gtsinf.scene.infos.InfGameSceneProperties;
	import gtsinf.scene.infos.InfSceneInfo;
	import gtsinf.scene.infos.SceneLocationInfo;
	import gtsinf.ui.GameSceneEventNotifier;
	import gtsinf.ui.WorldDebugControl;
	import gtsinf.world.InfMapCell;
	import gtsinf.world.InfWorld;
	import gtsinf.world.InfWorldAPI;
	import gtsinf.world.InfWorldDataBase;
	import gtsinf.world.clusters.EntryMark;
	import gtsinf.world.clusters.InfMapCluster;
	import gtsinf.world.components.InfAIManager;
	import gtsinf.world.components.InfAudioManager;
	import gtsinf.world.components.WComp_ControllerManager;
	import gtsinf.world.components.WComp_InteractionManager;
	import gtsinf.world.components.Wcomp_ObjectScripts;
	import gtsinf.world.scripts.CamScript;
	import gtsinf.world.scripts.env.InfEnvScript;
	import gtsinf.world.serialization.CharInfo;
	import gtsinf.world.serialization.InfEntitySerializer;
	import gtsinf.world.states.WorldLoadingState;
	import yves.display.Screen;
	import yves.system.timers.EnterFrameProcess;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class InfTopDownScene extends BasicGameScene
	{
		private var sceneProperties:InfGameSceneProperties;
		private var sceneInfo:InfSceneInfo;
		
		private var world:InfWorld;
		private var cam:InfEntCamera;
		private var camScript:CamScript;
		private var envScript:InfEnvScript;
		
		private var plrChar:EntChar;
		
		private var worldControl:WorldDebugControl;
		private var eventNotifier:GameSceneEventNotifier;
		
		private var entSZ:InfEntitySerializer;
		private var worldAPI:InfWorldAPI;
		private var gdb:InfWorldDataBase;
		
		private var interrupted:Boolean;
		private var newLocation:SceneLocationInfo;
		
		private var controlMan:WComp_ControllerManager;
		
		// TODO: for transition
		//    bundle scene components (gui, .., ?)
		//    don't re-create what was created before.
		
		
		// **************************************
		// DE-/INIT
		// **************************************
		
		public function InfTopDownScene(worldAPI:InfWorldAPI, sceneInfo:InfSceneInfo=null) 
		{
			this.worldAPI = worldAPI;
			this.sceneInfo = sceneInfo;
		}
		
		override public function init():void 
		{
			super.init();
			
			gdb = worldAPI.db;
			
			entSZ = new InfEntitySerializer(gdb);
			
			// create components
			
			sendEvent(new GameSceneEvent(GameSceneEvent.ENTERING));
			
			enterCell();
			start();
			
			sendEvent(new GameSceneEvent(GameSceneEvent.ENTERED));
			
			components.init();
		}
		
		override public function deinit():void 
		{
			sendEvent(new GameSceneEvent(GameSceneEvent.LEAVING));
			
			// leaveCell ? (if not null) ?
			stop();
			
			sendEvent(new GameSceneEvent(GameSceneEvent.LEFT));
			
			// destroy components
			
			entSZ.deinit();
			entSZ = null;
			
			components.deinit();
			
			super.deinit();
		}
		
		override public function pause():void 
		{
			super.pause();
			
			stop();
		}
		
		override public function resume():void 
		{
			super.resume();
			
			if (newLocation)
			{
				if (world == worldAPI.world) // has not been built yet
					return;
				
				sceneInfo.location = newLocation;
				newLocation = null;
				enterCell();
			}
			
			start();
		}
		
		// **************************************
		// CELL MANAGEMENT
		// **************************************
		
		private function enterCell():void
		{
			world = worldAPI.world;
			world.CurrCell.bake();
			
			
			//trace(world.allComponents.allComponents);
			
			
			sceneProperties = new InfGameSceneProperties();
			
			var playMan:WComp_PlayerManager = new WComp_PlayerManager();
			world.addComponent(playMan);
			
			var aiMan:InfAIManager = new InfAIManager();
			world.addComponent(aiMan);
			
			var interMan:WComp_InteractionManager = new WComp_InteractionManager();
			interMan.configure2(api);
			world.addComponent(interMan);
			
			world.addComponent(new InfAudioManager());
			world.addComponent(new Wcomp_ObjectScripts());
			
			controlMan = new WComp_ControllerManager();
			controlMan.configure2(api, worldAPI, sceneProperties);
			world.addComponent(controlMan);
			
			components.add(worldControl = new WorldDebugControl(world));
			
			world.getAllComponents().init();
			
			initCam();
			controlMan.setCamera(cam);
			
			initAvatar();
			
			initSpawning(aiMan);
			
			initScene();
			
			initEntries();
			
			world.EventMan.addEvent(new EntityEvent(EntityEvent.CONTROL_GAINED, plrChar, plrChar));
			
			world.EventMan.createListener(ExitZoneEvent.ENTERED, handleExitZone);
			
			eventNotifier = new GameSceneEventNotifier(worldAPI);
			eventNotifier.init();
			
			world.EventMan.addEvent(new WorldEvent(WorldEvent.SCENE_READY));
			world.EventMan.addEvent(new WorldEvent(WorldEvent.ENTERED));
			world.activate();
		}
		
		private function initEntries():void 
		{
			var complex:InfMapCluster = worldAPI.complex;
			
			if (complex != null)
			{
				var cell:InfMapCell = world.CurrCell;
				var cellID:String = complex.getCellIDByMapID(cell.MapID);
				cell.CellID = cellID;
				
				var exits:Array = worldAPI.world.EntMan.getAllByClass(EntZoneExit);
				
				for each(var exitZone:EntZoneExit in exits)
				{
					var info:ExitInfo = exitZone.Exit;
					
					if (!info.cell)
					{
						var entryID:String = exitZone.ID.split("_")[1];
						var mark:EntryMark = worldAPI.complex.getCellLink(cellID, entryID);
						
						if (mark == null)
						{
							Verbose.logError(new Error("Cell link not found: " + cellID + "." + entryID));
							continue;
						}
						
						info.cell = mark.cellID;
						info.entry = "entry_" + mark.entryID;
					}
				}
			}
		}
		
		private function initSpawning(aiMan:InfAIManager):void 
		{
			world.CurrCell.Script.initOnEnter();
			world.CurrCell.Script.spawnEntityAtTarget(plrChar, sceneInfo.location.start);
			
			if (!plrChar.RefWorld.isRegistered())
			{
				world.CurrCell.Script.spawnEntity(plrChar, 0, 0, 0, 0);
			}
			
			aiMan.checkFor(world.EntFinder.getAllEntsByClass(EntChar));
			
			if(GameAPI.TheOne.mainConfig.debug.riotMode)
				InfAIController(aiMan.DefaultController).causeRiot();
		}
		
		private function initAvatar():void 
		{
			if (sceneInfo.avatar != null)
			{
				plrChar = sceneInfo.avatar as EntChar;
			}
			else if(sceneInfo.avatarInfo != null)
			{
				var cinf:CharInfo = sceneInfo.avatarInfo as CharInfo;
				cinf.aiDisabled = true;
				
				plrChar = gdb.CharLib.createByInfo(cinf) as EntChar;
				plrChar.Player.HumanControl = true;
				
				entSZ.deserilaizeEntity(plrChar, cinf);
				
				sceneInfo.avatar = plrChar;
				sceneInfo.avatarInfo = null;
			}
			else
				throw new Error("No avatar!");
		}
		
		private function initScene():void 
		{
			sceneProperties.cam = cam;
			sceneProperties.cell = world.CurrCell;
			sceneProperties.world = world;
			sceneProperties.player = plrChar;
			worldControl.setGameScene(sceneProperties);
		}
		
		private function initCam():void 
		{
			var camSettings:Object = gameAPI.mainConfig.settings.cam;
			
			cam = new InfEntCamera();
			world.addEnt( cam );
			cam.applyFocalPoint(api.ScreenMan.getCenter());
			cam.y = camSettings.initialHeight;
			cam.FollowerDamping = camSettings.followSpeedMod;
			
			if (camSettings.transforms)
			{
				cam.Roll = int(camSettings.transforms.roll);
				cam.Tilt = int(camSettings.transforms.tilt);
				cam.Pan  = int(camSettings.transforms.pan);
			}
			else
			{
				cam.Roll = 0;
				cam.Tilt = 90;
				cam.Pan  = 0;
			}
			
			envScript = new InfEnvScript();
			envScript.setReferences(world.CurrCell);
			envScript.init();
			
			cam.Script = new CamScript();
			cam.Script.heightAcceleration = camSettings.acceleration;
			cam.Script.setReferences( world.CurrCell, cam, api.ScreenMan.getResolution() );
			
			cam.Script.update();
		}
		
		/**
		 * Leave the current cell.
		 */
		private function leaveCell():void
		{
			interrupted = false;
			
			if (world.CurrCell == null)
				return;
			
			components.remove(worldControl);
			worldControl = null;
			
			// now remove all entities that shall be cached
			if (steeredEntity != null)
			{
				if (steeredEntity.Actions.isSomeActive())
					steeredEntity.Actions.AbortCurrent();
				world.removeEnt(steeredEntity);
			}
			
			world.EventMan.addEvent(new WorldEvent(WorldEvent.LEFT));
			
			world.deinit()
			eventNotifier.deinit();
		}
		
		// **************************************
		// LIFE TIME
		// **************************************
		
		protected function start():void
		{
			if(worldAPI.process == null)
				worldAPI.process = new EnterFrameProcess();
			
			worldAPI.process.Routine = update;
			worldAPI.process.start();
			
			var monitor:Screen = api.GuiMan.getScreen();
			monitor.addEventListener(Event.CHANGE, handleScreenResize);
			
			world.EventMan.addEvent(new WorldEvent(WorldEvent.SCENE_ACTIVATED));
		}
		
		protected function stop():void
		{
			var isStoppedAlready:Boolean = worldAPI.process == null;
			
			if (isStoppedAlready)
				return;
			
			worldAPI.process.stop();
			worldAPI.process.Routine = null;
			worldAPI.process = null;
			
			var monitor:Screen = api.GuiMan.getScreen();
			monitor.removeEventListener(Event.CHANGE, handleScreenResize);
			
			world.EventMan.addEvent(new WorldEvent(WorldEvent.SCENE_DEACTIVATED));
		}
		
		public function update():void
		{
			controlMan.update();
			
			if ( worldControl )	
				worldControl.update(); 
			
			world.update();
			
			cam.Script.update();
			
			world.CurrCell.Physix.updateDebug(cam, api.GuiMan.getScreen().Resolution);
			
			// render
			worldAPI.world.CurrCell.Scene.update();
			
			eventNotifier.update();
			
			if (interrupted)
			{
				handleInterrupt();
			}
		}
		
		private function loadNextCell():void
		{
			var mapID:String = newLocation.cell;
			enterSubState(new WorldLoadingState(worldAPI).loadingThisMap(mapID, true));
			api.GuiMan.openScreen(new GtsLoadingScreenA());
		}
		
		private function letEntityLeaveCell(entity:Entity, exit:ExitInfo):void
		{
			if (entity == steeredEntity)
			{
				if (!exit.cell)
				{
					Verbose.logError(new Error("Invalid exit: " + exit.cell + "." + exit.entry));
					return;
				}
				
				newLocation = new SceneLocationInfo();
				newLocation.cell    = exit.cell;
				newLocation.complex = exit.complex;
				newLocation.start   = exit.entry;
				
				interrupted = true;
				MainAssetCache.screenShotData = sceneProperties.cell.Scene.renderToBitmapData();
			}
		}
		
		// **************************************
		// EVENT HANDLERS
		// **************************************
		
		private function handleInterrupt():void 
		{
			if (newLocation)
			{
				stop();
				
				var mapID:String = newLocation.cell;
			
				if (worldAPI.complex != null)
				{
					// inner-complex transition
					mapID = worldAPI.complex.getMapIDByCellID(newLocation.cell);
					newLocation.cell = mapID;
				}
				
				
				leaveCell();
				loadNextCell();
			}
		}
		
		private function handleExitZone(evt:ExitZoneEvent):void
		{
			letEntityLeaveCell(evt.Causer, evt.Exit);
		}
		
		private function handleScreenResize(e:Event):void 
		{
			var sceneView:View3D = world.CurrCell.Scene.view;
			var monitor:Screen = api.GuiMan.getScreen();
			
			sceneView.width  = monitor.Resolution.x;
			sceneView.height = monitor.Resolution.y;
		}
		
		// **************************************
		// GETTERS AND SETTERS
		// **************************************
		
		[Inline]
		private function get steeredEntity():Entity { return plrChar; }
		
		[Inline]
		private function get api():GameAPI { return gameAPI; }
		
	}
}