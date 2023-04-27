package bbm.game.battle.scripting 
{
	import bbm.ents.Entity;
	import bbm.world.map.Tile;
	import bbm.world.regions.Region;
	import bbm.world.view.WorldCamera;
	import flash.geom.Point;
	import yves.utils.ObjectUtil;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class BattleNASP_Camera extends BasicBattleNASP 
	{
		// PROPERTIES
		private function get cam():WorldCamera { return bapi.Control.getCamera(); }
		
		public function BattleNASP_Camera() 
		{
			
		}
		
		// goto  -orders mean: teleport thereto instantly, no transition <=> new shot (film.)
		// moveTo-orders mean: camera really moves thereto, transition   <=> dolly shot (film.) 
		
		public function gotoSpot(target:*):void
		{
			if(target is Entity)
				cam.gotoEnt(Entity(target));
			else if(target is Tile)
				cam.gotoPos(Tile(target).Position);
			else
				gotoPosition(target);	
		}
		
		/** Goes to center of map. */
		public function gotoCenter():void
		{
			cam.gotoCenter();
		}
		
		/** Goes to position of specified entity. */
		public function gotoEntity(ent:Entity):void
		{
			if (ent == null || !ent.isAddedToWorld())
			{
				logError(new Error("Camera cannot goto entity - it does not exist in world."));
				return;
			}
			
			cam.gotoEnt(ent);
		}
		
		/** Goes to passed tile coordinates. */
		public function gotoXY(x:int, y:int):void
		{
			cam.gotoPosXY(x, y);
		}
		
		/** Goes to passed position holding tile coordinates. */
		public function gotoPosition(posParam:Object):void
		{
			var pos:Point = convToPos(posParam);
			cam.gotoPos(pos);
		}
		
		
		// MOVEMENT
		
		/**
		 * 
		 * @param	place		an entity / tile / position
		 * @param	timeInMS
		 */
		public function moveTo(target:*, timeInMS:int=-1):void
		{
			if(target is Entity)
				cam.moveToEnt(Entity(target), timeInMS);
			else if(target is Tile)
				cam.moveToPos(Tile(target).Position, timeInMS);
			else
				moveToPos(target, timeInMS);	
		}
		
		public function moveToEnt(ent:Entity, timeInMS:int=-1):void
		{
			cam.moveToEnt(ent, timeInMS);
		}
		
		public function moveToPos(posParam:Object, timeInMS:int=-1):void
		{
			var pos:Point = convToPos(posParam);
			cam.moveToPos(pos, timeInMS);
		}
		
		public function moveToXY(x:int, y:int, timeInMS:int = -1):void
		{
			var pos:Point = new Point(x,y);
			cam.moveToPos(pos, timeInMS);
		}
		
		// AUX
		
		static private function convToPos(posParam:Object):Point
		{
			if (ObjectUtil.isObject(posParam))
				return new Point(posParam.x, posParam.y);
			else if (posParam is Point)
				return Point(posParam);
			else if(posParam is Region)
				return Region(posParam).Center;
			else if (posParam is String)
				return new Point(String(posParam).split("|")[0], String(posParam).split("|")[1]);
			else
				return new Point();
		}
	}
}