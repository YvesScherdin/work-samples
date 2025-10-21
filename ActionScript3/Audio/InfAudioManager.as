package gtsinf.world.components 
{
	import flash.events.Event;
	import flash.geom.Vector3D;
	import flash.media.SoundChannel;
	import flash.media.SoundTransform;
	import flash.utils.Dictionary;
	import gts.audio.AudioDistanceLevel;
	import gts.audio.IAudioSourceController;
	import gts.ents.basic.Entity;
	import gts.ents.misc.EntCamera;
	import gts.events.world.WorldEvent;
	import gts.world.objects.structures.StructureBasedEntity;
	import gtsinf.ents.EntBullet;
	import gtsinf.ents.EntChar;
	import gtsinf.ents.misc.env.EntGib;
	import yves.audio.SoundHandle;
	import yves.audio.SoundRange;
	import yves.system.IUpdatable;
	import yves.utils.ArrayUtil;
	import yves.utils.DictionaryUtil;
	import yves.utils.NumberUtil;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public final class InfAudioManager extends InfWorldComponent implements IAudioSourceController, IUpdatable
	{
		private var dict:Dictionary;
		private var handles:Array;
		
		private var cam:EntCamera;
		private var focus:Vector3D;
		private var reference:Vector3D;
		private var listeningRange:Number;
		private var listeningRangeInv:Number;
		
		private var tempST:SoundTransform;
		
		private var currDistLevel:AudioDistanceLevel;
		private var distanceLevels:Dictionary;
		
		// **************************************
		// DE-/INIT
		// **************************************
		
		public function InfAudioManager() 
		{
			super();
			type = TYPE__AudioManager;
			eventMark = type;
		}
		
		override public function init():void 
		{
			super.init();
			
			distanceLevels = new Dictionary(); // <String><Number>
			distanceLevels[SoundRange.near]   = new AudioDistanceLevel(400);
			distanceLevels[SoundRange.medium] = new AudioDistanceLevel(800);
			distanceLevels[SoundRange.far]    = new AudioDistanceLevel(1600);
			distanceLevels[SoundRange.global] = null;
			
			world.EventMan.createListener(WorldEvent.SCENE_READY,  handleSceneReady,  eventMark);
			world.EventMan.createListener(WorldEvent.OBJECT_ADDED, handleEntityAdded, eventMark);
			
			focus = new Vector3D();
			reference = new Vector3D();
			
			dict = new Dictionary();
			handles = [];
		}
		
		override public function deinit():void 
		{
			world.EventMan.clearByMark(eventMark);
			
			while (handles.length != 0)
			{
				var handle:SoundHandle = handles.pop();
				
				handle.channel.removeEventListener(Event.SOUND_COMPLETE, handleSoundOver);
				handle.deinit();
			}
			handles = null;
			
			
			DictionaryUtil.dispose(dict);
			dict = null;
			
			super.deinit();
		}
		
		// **************************************
		// LIFETIME
		// **************************************
		
		public function update():void
		{
			if (cam == null)
				return;
			
			focus.x = cam.x;
			focus.z = cam.z;
			
			for each(var handle:SoundHandle in handles)
			{
				updateHandle(handle);
			}
		}
		
		// **************************************
		// HANDLE MANAGEMENT
		// **************************************
		
		public function registerHandle(handle:SoundHandle):void
		{
			if (handle.channel == null) // TODO: find out why this can happen
				return;
			
			handles.push(handle);
			updateHandle(handle);
			
			dict[handle.channel] = handle;
			
			if(!handle.looped)
				handle.channel.addEventListener(Event.SOUND_COMPLETE, handleSoundOver, false, 0, true);
		}
		
		public function unregisterHandle(handle:SoundHandle):void
		{
			if(handle.looped && handle.channel!= null)
				handle.channel.removeEventListener(Event.SOUND_COMPLETE, handleSoundOver);
			
			ArrayUtil.removeElement(handle, handles);
			//handle.stop(); // ?
			
			dict[handle.channel] = null;
			delete dict[handle.channel];
		}
		
		private function updateHandle(handle:SoundHandle):void 
		{
			var ent:Entity = Entity(handle.custom);
			
			if (handle.channel == null)
				return; // TODO: find out why that happens and prevent it
			
			var range:String = handle.range || SoundRange.medium;
			
			if (range == null || range == SoundRange.global)
				return;
			
			currDistLevel = distanceLevels[handle.range];
			if (currDistLevel == null)
				return;
			
			if(tempST == null)
				tempST = handle.channel.soundTransform;
			
			reference.x = ent.x - focus.x;
			reference.z = ent.z - focus.z;
			
			var sign:int = NumberUtil.getSign(reference.x);
			
			reference.x *= reference.x;
			reference.z *= reference.z;
			
			var dist:Number = reference.x + reference.z;
			var distModX:Number = reference.x * currDistLevel.inversed * sign;
			
			distModX = NumberUtil.clamp(distModX, -1, 1);
			tempST.pan = distModX;
			
			dist *= currDistLevel.inversed;
			dist = NumberUtil.clamp(dist, 0, 1);
			dist = 1 - dist;
			
			tempST.volume = dist * handle.file.TotalVolume;
			handle.channel.soundTransform = tempST;
		}
		
		// **************************************
		// ENTITY SELECTION
		// **************************************
		
		/**
		 * Polling for all entites currently existing
		 */
		private function checkForEntities():void 
		{
			var entities:Array = world.EntMan.getAll();
			
			for each(var ent:Entity in entities)
			{
				checkEntity(ent);
			}
		}
		
		private function checkEntity(ent:Entity):void
		{
			if (cam == null)
			{
				if (ent is EntCamera)
				{
					cam = EntCamera(ent);
					return;
				}
			}
			
			if (ent is EntChar || ent is EntBullet || ent is EntGib || ent is StructureBasedEntity)
			{
				ent.Audio.setController(this);
			}
		}
		
		// **************************************
		// EVENT HANDLERS
		
		private function handleSoundOver(e:Event):void 
		{
			var handle:SoundHandle = dict[SoundChannel(e.target)];
			unregisterHandle(handle);
		}
		// **************************************
		
		private function handleSceneReady(event:WorldEvent):void 
		{
			checkForEntities();
		}
		
		private function handleEntityAdded(event:WorldEvent):void 
		{
			checkEntity(Entity(event.object));
		}
		
	}

}