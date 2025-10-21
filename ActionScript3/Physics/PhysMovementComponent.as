package gtsinf.ents.components 
{
	import Box2D.Common.Math.b2Vec2;
	import gts.ents.components.MovementComponent;
	import gtsinf.ents.components.PhysixComponent;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class PhysMovementComponent extends MovementComponent 
	{
		private var physix:PhysixComponent;
		public function get Physix():PhysixComponent { return physix; }
		
		
		private var tempOffset:b2Vec2;
		private var vecOrigin:b2Vec2;
		
		
		public function PhysMovementComponent() 
		{
			super();
		}
		
		
		protected override function Init():void
		{
			super.Init();
			
			physix = PhysixComponent(owner.getCompByType(PHYSIX));
			tempOffset = new b2Vec2();
			vecOrigin = new b2Vec2();
		}
		
		override public function moveIntoDir(dir:Number):void 
		{
			if (physix != null && physix.Body != null)
			{
				tempOffset.x =  speed * Math.cos(dir * toRad);
				tempOffset.y = -speed * Math.sin(dir * toRad);
				physix.Body.ApplyImpulse(tempOffset, vecOrigin);
			}
			else
			{
				owner.x += speed * Math.cos(dir * toRad);
				owner.z -= speed * Math.sin(dir * toRad);
			}
			
		}
		
		public function applyThrust(dir:Number, pos:b2Vec2, strength:Number = Number.NaN):void 
		{
			if (isNaN(strength))
				strength = speed;
			
			if (physix != null && physix.Body != null)
			{
				tempOffset.x =  speed * Math.cos(dir * toRad);
				tempOffset.y = -speed * Math.sin(dir * toRad);
				physix.Body.ApplyImpulse(tempOffset, pos);
			}
		}
	}

}