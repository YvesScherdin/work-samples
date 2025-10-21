package gts.display.mdl 
{
	import yves.display.ani.Animation;
	import yves.display.ani.AnimationOptions;
	import yves.display.ani.AnimationSet;
	import yves.display.ani.controllers.BasicAnimationController;
	
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public interface IAnimatedModel extends IModel
	{
		function animate( animation:Animation, options:AnimationOptions=null ):void;
		function getAnimationController():BasicAnimationController;
		function getAnimationSet():AnimationSet;
		
		function stopAnimation(options:AnimationOptions = null):void;
	}
	
}