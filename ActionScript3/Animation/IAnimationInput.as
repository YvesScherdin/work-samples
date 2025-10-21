package yves.display.ani 
{
	
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public interface IAnimationInput 
	{
		function hasAnimation(aniName:String=null):Boolean;
		function animate(newAniName:String, dir:int = -1, forceBegin:Boolean = false):void;
		
		function randomizeCurrAni():void;
		
		function freezeAnimation():void;
		function unfreezeAnimation():void;
		
		function get CurrAni():Animation;
		function get CurrAniName():String;
	}
	
}