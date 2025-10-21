package yves.display.ani
{
	/**
	 * 
	 * @author Yves Scherdin
	 * @version 2009-08-30
	 */
	public class Animation
	{
		private var name:String;
		private var startFrame:int;
		private var endFrame:int;
		private var looped:Boolean;
		
		
		public function Animation( name:String,sf:int=1,ef:int=-1,looped:Boolean=true ):void
		{
			this.name = name;
			startFrame = sf;
			endFrame = ef;
			this.looped = looped;
			
			if( endFrame == -1 )
				endFrame = startFrame
		}
		
		public function isPose():Boolean	{	return (endFrame == startFrame);	}
		public function isLooped():Boolean	{	return looped;		}
		
		public function get EndFrame():int	{	return endFrame;	}
		public function get StartFrame():int{	return startFrame;	}
		public function get Name():String	{	return name;		}
		
		public function get NumFrames():int {	return endFrame - startFrame; }
	}
}