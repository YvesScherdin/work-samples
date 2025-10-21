package yves.audio 
{
	import flash.media.SoundChannel;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class SoundHandle
	{
		public var file:SoundFile;
		public var channel:SoundChannel;
		public var looped:Boolean;
		public var custom:Object;
		public var range:String;
		
		public function SoundHandle(file:SoundFile, channel:SoundChannel, looped:Boolean=false)
		{
			this.file = file;
			this.channel = channel;
			this.looped = looped;
		}
		
		public function stop():void
		{
			if(channel)
				channel.stop();
		}
		
		public function deinit():void
		{
			file = null;
			channel = null;
			custom = null;
		}
	}

}