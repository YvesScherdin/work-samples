package yves.audio 
{
	import flash.geom.Transform;
	import flash.utils.Dictionary;
	import gts.core.Verbose;
	import yves.system.serialization.ISerializable;
	/**
	 * This is table of key value pairs that represent the linkage between event ids and sounds.
	 * It can be used anywhere for everything; buttons, entities and so on.
	 * 
	 * @author Yves Scherdin
	 */
	public class SoundProfile implements ISerializable
	{
		private var all:Dictionary;
		
		private var id:String;
		public function get ID():String { return id; }
		
		private var mute:Boolean;
		public function get Mute():Boolean { return mute; }
		public function set Mute(value:Boolean):void
		{
			mute = value;
		}
		
		
		public function SoundProfile(id:String=null)
		{
			this.id = id;
			all = new Dictionary();
		}
		
		public function register( eventID:String, soundID:String, isGroup:Boolean = false, vol:Number=1.0, looped:Boolean=false ):Object
		{
			//if ( all[eventID] === undefined )
			{
				// simply overrides
				all[eventID] = { id:eventID, soundID:soundID, isGroup:isGroup, volume:vol, looped:looped };
			}
			return all[eventID];
		}
		
		public function play(id:String):SoundHandle
		{
			if ( mute || !id )
				return null;
			
			var info:Object = all[id];
			
			if ( !info )
			{
				Verbose.log("SoundProfile::soundID not found: '" + id + "'", Verbose.Flags.MISSING_CONTENT, 1);
				info = register(id, id);
			}
			
			if ( !AudioManager.TheOne )
				return null;
			
			var looped:Boolean = info.looped;
			var handle:SoundHandle;
			
			if( !(info.isGroup) )
				handle = AudioManager.TheOne.playSound( info.soundID, info.volume, looped );
			else
				handle = AudioManager.TheOne.playSoundByGroup( info.soundID, info.volume, looped );
			
			if (handle != null)
			{
				if(info.range)
					handle.range = info.range;
			}
			
			return handle;
		}
		
		public function playSoundBySet( id:String, soundSet:String ):void
		{
			if ( mute || !id )
				return;
			
			AudioManager.TheOne.playSoundBySet(soundSet, id);
		}
		
		/* INTERFACE cmsg.system.serialisazion.ISerializable */
		
		public function Serialize(src:Object = null):Object 
		{
			return null;
		}
		
		public function Deserialize(src:Object):void 
		{
			for ( var key:String in src )
			{
				var data:Object = src[key];
				
				var entry:Object;
				
				if (data is String)
				{
					entry = register( key, String(data), true, 1.0, false );
					entry.isGroup = AudioManager.TheOne.hasSoundGroup(entry.soundID);
				}
				else
				{
					entry = register( key, data.soundID || data.id, data.isGroup, data.vol === undefined ? 1.0 : data.vol, data.looped );
					
					if (data.isGroup === undefined)
						entry.isGroup = AudioManager.TheOne.hasSoundGroup(entry.soundID);
					
					if (data.range != null)
						entry.range = data.range;
				}
				
			}
		}
		
		
		/**
		 * Not ready yet...
		 * @return
		 */
		public function clone():SoundProfile
		{
			var p:SoundProfile = new SoundProfile(this.id);
			throw new Error("Not supported yet.");
			return p;
		}
		
		
		
	}
}