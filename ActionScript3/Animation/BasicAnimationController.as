package yves.display.ani.controllers 
{
	import flash.events.Event;
	import flash.events.EventDispatcher;
	import yves.display.ani.Animation;
	import yves.display.ani.AnimationEvent;
	import yves.display.ani.AnimationSIgnalInputNode;
	import yves.display.ani.AnimationSignalMap;
	import yves.display.ani.IAnimationSignalInput;
	import yves.display.ani.SignalAnimation;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class BasicAnimationController extends EventDispatcher
	{
		protected var currFrame:int;
		protected var numFrames:int;
		
		protected var stopped:Boolean;
		protected var stopOnEnd:Boolean;
		protected var startFrame:int;
		protected var endFrame:int;
		protected var count:int = 0;
		
		protected var aniSpeed:int = 1;
		public function getAniSpeed():int { return aniSpeed; }
		public function setAniSpeed(value:int):void { aniSpeed = value; }
		
		
		protected var currAni:Animation;
		protected var freezed:Boolean;
		
		
		protected var currSignalMap:AnimationSignalMap;
		protected var signalsUsed:Boolean;
		protected var currSignal:String;
		protected var signalInput:AnimationSIgnalInputNode;
		
		
		
		
		public function BasicAnimationController() 
		{
			
		}
		
		
		
		public function animate(animation:Animation):void 
		{
			currAni = animation;
			
			if (animation is SignalAnimation)
				currSignalMap = SignalAnimation(animation).getSignals();
			else
				currSignalMap = null;
			
			signalsUsed = currSignalMap && signalInput;
			
			play( currAni.StartFrame, currAni.EndFrame, !currAni.isLooped() );
		}
		
		public function forgetCurrentAni():void
		{
			currAni = null;
		}
		
		public function update():Boolean
		{
			if ( freezed )
				return false;
			
			if( !stopped )
			{
				if ( count++ % aniSpeed == 0 )
				{
					nextFrame();
					return true;
				}
			}
			return false;
		}
		
		
		
		public function gotoAndStop( frame:int ):void
		{
			setCurrFrame( frame-1 );
			stop();
		}
		
		public function gotoAndPlay( frame:int ):void
		{
			setCurrFrame( frame-1 );
			play();
		}
		
		public function stop():void
		{
			stopped = true;
		}
		
		public function play( startFrame:int=-1, endFrame:int=-1, stopOnEnd:Boolean=false ):void
		{
			stopped = false;
			
			if( startFrame != -1 )
				this.startFrame = startFrame;
			
			if( endFrame != -1 )
				this.endFrame = endFrame;
			
			this.stopOnEnd = stopOnEnd;
			
			currFrame = startFrame;
			updateCurrentFrame();
			
			if (signalsUsed)
			{
				checkSignal();
			}
		}
		
		
		public function nextFrame():void
		{
			setCurrFrame( currFrame + 1 );
		}
		
		private function setCurrFrame( newFrame:int ):void
		{
			if ( newFrame > endFrame )
			{
				if( !stopOnEnd )// goto startFrame if numFrames are exceeded.
					newFrame = startFrame;
				else
				{
					dispatchEvent( new Event(AnimationEvent.END) );
					stop();
					return;
				}
			}
			else if( newFrame < 1 )
				newFrame = 1;
			
			currFrame = newFrame;
			updateCurrentFrame();
			
			if (signalsUsed)
			{
				checkSignal();
			}
		}
		
		// signals
		private function checkSignal():void
		{
			currSignal = currSignalMap.getTypeAt(currFrame);
			
			if (currSignal)
			{
				signalInput.handleAnimationSignal(currSignal);
			}
		}
		
		public function addSignalInput(value:IAnimationSignalInput):void
		{
			if (!signalInput)
				signalInput = new AnimationSIgnalInputNode(value);
			else
				signalInput.addNode(new AnimationSIgnalInputNode(value));
		}
		
		public function removeSignalInput(value:IAnimationSignalInput):void
		{
			if (!signalInput)
				return;
			
			if (signalInput.remove(value) && signalInput.input == null)
			{
				signalInput = null;
			}
		}
		
		
		
		protected function updateCurrentFrame():void
		{
			
		}
		
		public function refreshCurrentFrame():void
		{
			updateCurrentFrame();
		}
		
		
		public function get CurrentFrame():int
		{
			return currFrame+1;
		}
		
		public function get NumFrames():int
		{
			return numFrames;
		}
		
		
		public function isFreezed():Boolean { return freezed; }
		public function freeze():void { freezed = true; }
		public function unfreeze():void { freezed = false; }
		
		
		public function set StartFrame( value:int ):void {   startFrame = value;   }
		public function get StartFrame():int             {   return startFrame;    }
		
		public function set EndFrame( value:int ):void   {   endFrame = value;     }
		public function get EndFrame():int               {   return endFrame;      }
	}

}