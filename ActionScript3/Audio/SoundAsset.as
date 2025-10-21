package yves.assets.snd 
{
	import flash.events.Event;
	import flash.events.IOErrorEvent;
	import flash.events.ProgressEvent;
	import flash.media.Sound;
	import flash.net.URLRequest;
	import yves.assets.AssetVerbose;
	import yves.assets.basic.*;
	
	
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class SoundAsset extends Asset 
	{
		private var snd:Sound
		private var groupID:String;
		
		
		public function SoundAsset( id:String, url:String, groupID:String=null ) 
		{
			super( id, url );
			this.groupID = groupID;
			type = AssetType.SOUND;
			snd = new Sound();
		}
		
		
		
		public override function load():void
		{
			snd.load( new URLRequest(url) );
			snd.addEventListener( Event.COMPLETE,        handleEvent );
			snd.addEventListener( IOErrorEvent.IO_ERROR, handleEvent );
			snd.addEventListener( ProgressEvent.PROGRESS,handleProgress );
			state = AssetState.LOADING;
		}
		
		private function handleEvent( e:Event ):void
		{
			var snd:Sound = Sound( e.target );
			
			snd.removeEventListener( Event.COMPLETE,	     handleEvent );
			snd.removeEventListener( IOErrorEvent.IO_ERROR	, handleEvent );
			snd.removeEventListener( ProgressEvent.PROGRESS,handleProgress );
			
			if ( e is IOErrorEvent )
			{
				state = AssetState.FAILED;
				AssetVerbose.NODE.log( "Load Failed:  " + id + " not found.", AssetVerbose.VKEY_MISS );
			}
			else
			{
				state = AssetState.READY;
			}
			
			checkRequests();
		}
		
		public override function get BytesTotal():int
		{
			return bytesTotal;
		}
		
		public override function get BytesLoaded():int
		{
			return bytesLoaded;
		}
		
		private function handleProgress( e:ProgressEvent ):void
		{
			bytesLoaded = e.bytesLoaded;
			bytesTotal  = e.bytesTotal;
		}
		
		private var bytesTotal:int = 1;
		private var bytesLoaded:int;
		
		public function getData():Sound { return snd; }
		public function get GroupID():String { return groupID; }
	}

}