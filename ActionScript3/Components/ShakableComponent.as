package gtsinf.ents.components 
{
	import away3d.core.math.Quaternion;
	import away3d.core.math.Vector3DUtils;
	import flash.geom.Matrix3D;
	import flash.geom.Orientation3D;
	import flash.geom.Vector3D;
	import gts.ents.basic.Entity;
	import gts.ents.components.EntityComponent;
	import gts.events.ents.EntityEvent;
	import gts.system.combat.CombatHit;
	import gts.system.physics.Oscillation;
	import gtsinf.ents.components.info.ShakableInfo;
	import yves.utils.Geom;
	import yves.utils.VectorUtil;
	
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public final class ShakableComponent extends EntityComponent 
	{
		static private const DEFAULT_POS:Vector3D = new Vector3D(0, 0, 0);
		static private const DEFAULT_SCL:Vector3D = new Vector3D(1, 1, 1);
		
		private var info:ShakableInfo;
		private var osc:Oscillation;
		private var oscSide:Oscillation;
		private var shaking:Boolean;
		
		private var baseOrientation:Vector3D;
		private var currOrientation:Vector3D;
		
		private var matrix:Matrix3D;
		private var matrixComps:Vector.<Vector3D>;
		private var axis:Vector3D;
		
		
		public function ShakableComponent(info:ShakableInfo) 
		{
			super(EntityComponent.SHAKY);
			
			this.info = info;
			
			needsUpdate = true;
		}
		
		// **************************************
		// DE-/INIT
		// **************************************
		
		override protected function Init():void 
		{
			super.Init();
			
			currOrientation = new Vector3D();
			
			matrix = new Matrix3D();
			matrixComps = new Vector.<Vector3D>(3, true);
			matrixComps[0] = DEFAULT_POS;
			matrixComps[1] = currOrientation;
			matrixComps[2] = DEFAULT_SCL;
			
			if(info.onTouch)
				owner.Events.CreateHandle(EntityEvent.TOUCHED, handleTouched, eventMark);
			
			if(info.onHit)
				owner.Events.CreateHandle(EntityEvent.GOT_HIT, handleHit, eventMark);
		}
		
		override protected function Deinit():void 
		{
			super.Deinit();
		}
		
		// **************************************
		// CUSTOM
		// **************************************
		
		public function shake(impactDir:Vector3D, intensity:Number=1.0):void 
		{
			if (intensity < 0 || isNaN(intensity))
				return;
			
			if (baseOrientation == null)
			{
				baseOrientation = new Vector3D(
					owner.Transforms.rotationX,
					owner.Transforms.rotationY,
					owner.Transforms.rotationZ
				);
			}
			
			stop(false);
			
			axis = impactDir;
			axis.y = 0;
			axis.normalize();
			
			VectorUtil.rotateXZ(axis, -90 + baseOrientation.y, false, null, 1);
			
			osc = new Oscillation(info.tilt, info.speed * intensity, 0, info.damping);
			
			shaking = true;
		}
		
		public function stop(resetToDefault:Boolean=true):void
		{
			shaking = false;
			
			if (resetToDefault)
			{
				owner.Transforms.rotationX = baseOrientation.x;
				owner.Transforms.rotationY = baseOrientation.y;
				owner.Transforms.rotationZ = baseOrientation.z;
				owner.updateTransforms();
			}
		}
		
		override public function Update():void 
		{
			if (shaking)
			{
				applyShakeState();
			}
		}
		
		private function applyShakeState():void 
		{
			osc.update(1); // TODO: change to real time delta
			
			var tilt:Number = osc.getValue();
			
			currOrientation.copyFrom(baseOrientation);
			currOrientation.scaleBy(Geom.TO_RAD);
			matrix.recompose(matrixComps);
			matrix.prependRotation(tilt, axis);
			
			Vector3DUtils.matrix2euler(matrix, currOrientation);
			currOrientation.scaleBy(Geom.TO_DEG);
			currOrientation.x *= -1;
			
			owner.Transforms.rotationX = currOrientation.x;
			owner.Transforms.rotationY = currOrientation.y;
			owner.Transforms.rotationZ = currOrientation.z;
			
			owner.updateTransforms();
			
			if (osc.isOver())
			{
				stop(true);
			}
		}
		
		// **************************************
		// EVENT LISTENERS
		// **************************************
		
		private function handleTouched(event:EntityEvent):void 
		{
			var causer:Entity = event.Causer;
			var target:Entity = owner;
			
			var impactDir:Vector3D = target.Position.clone();
			impactDir.decrementBy(causer.Position);
			
			shake(impactDir);
		}
		
		private function handleHit(event:EntityEvent):void 
		{
			if (event.custom is CombatHit == false)
				return;
			
			var hit:CombatHit = CombatHit(event.custom);
			if (hit.conductor == null)
				return;
			
			var causer:Entity = Entity(hit.conductor);
			var target:Entity = owner;
			var impactDir:Vector3D = target.Position.clone();
			
			impactDir.decrementBy(causer.Position);
			shake(impactDir);
		}
		
	}

}