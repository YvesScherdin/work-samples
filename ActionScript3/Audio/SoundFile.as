package yves.audio 
{
	import flash.media.Sound;
	import flash.media.SoundChannel;
	import flash.media.SoundTransform;
	import gts.core.Verbose;
	import yves.system.timers.TimeRelay;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class SoundFile 
	{
		static public const MAX_VOLUME_MODIFIER:Number = 1.5;
		
		private var id:String;
		public function get ID():String { return id; }
		
		private var sound  :Sound;
		private var control:IVolumeControl;
		
		private var channel:SoundChannel;
		public function getCurrentChannel():SoundChannel { return channel; }
		
		
		private var volume :Number;
		public function getVolume():Number { return volume; }
		public function setVolume(value:Number):void { volume = value; checkVolume(); }
		
		private var music:Boolean;
		public function isMusic():Boolean { return music; }
		
		private var stopped:Boolean;
		public function wasStopped():Boolean { return stopped; }
		
		private var baseVolume:Number;
		public function getBaseVolume():Number { return baseVolume; }
		
		public function isReady():Boolean { return sound.bytesLoaded == sound.bytesTotal; }
		
		
		private var fadeTimer:TimeRelay;
		
		
		public function SoundFile( id:String, sound:Sound, volume:Number, control:IVolumeControl, music:Boolean=false, baseVolume:Number=1.0 ) 
		{
			this.id      = id;
			this.sound   = sound;
			this.volume  = volume;
			this.control = control;
			this.music   = music;
			
			this.baseVolume = !isNaN(baseVolume) ? baseVolume : 1.0;
		}
		
		public function deinit():void
		{
			this.id      = null;
			this.sound   = null;
			this.control = null;
		}
		
		private function genST():SoundTransform
		{
			return new SoundTransform( TotalVolume );
		}
		
		
		/**
		 * This may cause handled errors, if file was not loaded
		 * @param	volume
		 * @return
		 */
		public function play( volume:Number=-1, loopedExplicitely:Boolean = false ):SoundChannel
		{
			if ( volume != -1 )
			{
				this.volume = volume;
			}
			else if (this.volume == 0)
			{
				this.volume = 1; // cope with fade out
			}
			
			try
			{
				channel = sound.play( 0, getNumOfLoops(loopedExplicitely), genST() );
			}
			catch (e:Error)
			{
				Verbose.logError(e, 0, true);
			}
			
			stopped = false;
			return channel;
		}
		
		public function stop():void
		{
			if ( channel != null )
			{
				channel.stop();
				stopped = true;
			}
		}
		
		
		public function checkVolume():void
		{
			if( channel )
				channel.soundTransform = genST();
		}
		
		public function get TotalVolume():Number
		{
			var vol:Number = baseVolume * volume * ( isMusic() ? control.getMusicVolume() : control.getSoundVolume() );
			if ( vol > MAX_VOLUME_MODIFIER )
				vol = MAX_VOLUME_MODIFIER;
			
			return vol;
		}
		
		private function getNumOfLoops(loopedExplicitely:Boolean):int
		{
			return (loopedExplicitely || isMusic()) ? 4242 : 0;
		}
		
		
		static private var fadeInterval:int = 33;
		
		private var fading:Boolean;
		private var fadeSpeed:Number;
		private var fadeProgress:int;
		private var fadeTarget:Number;
		
		
		public function fadeIn(timeInMS:int):void
		{
			if (fadeTimer)
			{
				stopFading();
			}
			
			fade(timeInMS, 0, volume);
		}
		
		public function fadeOut(timeInMS:int):void
		{
			fade(timeInMS, volume, 0);
		}
		
		public function fade(timeInMS:int, start:Number, end:Number):void
		{
			if (timeInMS < fadeInterval)
				timeInMS = fadeInterval;
			
			fadeTarget = end;
			if (volume != start)
			{
				volume = start;
				checkVolume();
			}
			
			var fadeRange:Number = end - start;
			var numFadeTicks:int = (timeInMS / fadeInterval);
			fadeSpeed = fadeRange / numFadeTicks;
			fadeTimer = TimeRelay.doPeriodic(checkFading, fadeInterval, numFadeTicks);
		}
		
		public function stopFading():void
		{
			if (!fadeTimer)
				return;
			
			fadeTimer.stop();
			fadeTimer = null;
			fading = false;
		}
		
		private function checkFading():void
		{
			volume += fadeSpeed;
			if (volume < 0)
				volume = 0;
			
			if(Math.abs(volume - fadeTarget) < Math.abs(fadeSpeed))
			{
				volume = fadeTarget;
				stopFading();
				
				if (volume == 0)
				{
					stop();
				}
			}
			checkVolume();
		}
		
	}

}