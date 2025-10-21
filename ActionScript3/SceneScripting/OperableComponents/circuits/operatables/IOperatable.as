package gtsinf.world.circuits.operatables 
{
	
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public interface IOperatable 
	{
		
		function operate():void;
		
		function isBusy():Boolean;
		
		function isIdle():Boolean;
	}
	
}