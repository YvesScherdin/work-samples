package yves.audio 
{
	
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public interface IAudioOutput 
	{
		function playSound( id:String, volume:Number = 1.0, looped:Boolean=false ):SoundHandle;
		function playSoundByGroup( groupID:String, volume:Number = 1, looped:Boolean=false ):SoundHandle;
		
		
	}
	
}