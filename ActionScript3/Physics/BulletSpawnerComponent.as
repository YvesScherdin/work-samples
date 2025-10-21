package gtsinf.ents.components 
{
	import flash.geom.Vector3D;
	import gts.display.mdl.away.TopDownModel;
	import gts.ents.basic.Entity;
	import gts.ents.components.EntityComponent;
	import gts.events.ents.EntityEvent;
	import gts.system.combat.CombatHit;
	import gts.system.combat.DamageInflictionInfo;
	import gts.system.combat.IDamageInflictor;
	import gts.system.stats.RangedStat;
	import gts.world.utils.WorldTransforms;
	import gtsinf.ents.EntBullet;
	import gtsinf.ents.items.RangedWeapon;
	import gtsinf.ents.items.Weapon;
	import gtsinf.ents.items.weapons.FiringType;
	import gtsinf.ents.misc.env.EntBeam;
	import gtsinf.ents.misc.env.EnvEntityType;
	import gtsinf.ents.misc.muzzles.MuzzleInfo;
	import gtsinf.ents.misc.muzzles.MuzzleLocation;
	import gtsinf.world.InfWorldDataBase;
	import gtsinf.world.InfWorld;
	import yaway.topdown.world.AwayCoordSpace;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class BulletSpawnerComponent extends EntityComponent
	{
		static private const RAD_TO_DEG:Number = 180 / Math.PI;
		static private const DEG_TO_RAD:Number = Math.PI / 180;
		static private const TICK_PER_SECONDS:int = 30;
		
		private var damageLimits:RangedStat;
		private var muzzles:Array;
		private var initialDelay:int;
		
		private var frequency:int;
		public function get Frequency():int { return frequency; }
		
		private var blockingTimer:int;
		private var ready:Boolean;
		
		private var spawnLink:String;
		
		private var weaponPos:Vector3D;
		private var firing:Boolean;
		
		public var ammoType:String;
		
		public var entityType:String;
		
		public var firingType:String;
		
		private var numOfUsedAmmo:int;
		public function get NumOfUsedAmmo():int { return numOfUsedAmmo; }
		
		private var dmgInfo:DamageInflictionInfo;
		
		private var ammoReduceTicker:int;
		private var ammoReduceTickerMax:int = 0;
		private var ammoDecrease:int;
		private var ammoDecreaseFix:Boolean;
		
		
		private var directionalOffset:int;
		public function get DirectionalOffset():int { return directionalOffset; }
		public function set DirectionalOffset(value:int):void { directionalOffset = value; }
		
		
		
		public function BulletSpawnerComponent(damageLimits:RangedStat) 
		{
			super(EntityComponent.BULLETS);
			
			this.damageLimits = damageLimits;
			this.ammoType = ammoType;
			
			muzzles = [];
			ready = true;
		}
		
		
		override protected function Init():void 
		{
			super.Init();
			owner.Events.CreateHandle(EntityEvent.WORLD_SCOPE_CHANGED, handleWorldScopeChange, type);
		}
		
		override protected function Deinit():void 
		{
			owner.Events.ClearHandles(type);
			super.Deinit();
		}
		
		
		public function build(bulletLink:String, initialDelay:int, frequency:int):void
		{
			this.spawnLink    = bulletLink;
			this.initialDelay = initialDelay;
			this.frequency    = frequency;
		}
		
		public function initAmmo(source:Object):void
		{
			ammoReduceTickerMax = (source.ammoFreq !== undefined) ? int(source.ammoFreq) : 4;
			ammoDecrease = (source.ammoDecrease !== undefined) ? int(source.ammoDecrease) : 1;
			ammoDecreaseFix = source.ammoDecreaseFix;
		}
		
		
		
		override public function Update():void 
		{
			numOfUsedAmmo = 0;
			
			if (--blockingTimer <= 0)
				ready = true;
			
			if (firing)
			{
				if (firingType == FiringType.CONSTANT)
				{
					for each(var m:MuzzleInfo in muzzles)
					{
						if (m.locator)
							m.locator.update();
					}
				}
			}
		}
		
		
		public function warmUp():void
		{
			blockingTimer = initialDelay;
			ready = false;
		}
		
		public function startFiring():void
		{
			firing = true;
			ammoReduceTicker = 0;
		}
		
		public function stopFiring():void
		{
			firing = false;
			
			if (entityType == EnvEntityType.BEAM)
			{
				for each(var m:MuzzleInfo in muzzles)
				{
					if (m.firedEntity)
					{
						m.firedEntity.die();
						m.firedEntity = null;
					}
					
					if (m.muzzleFire)
					{
						m.muzzleFire.die();
						m.muzzleFire = null;
					}
				}
			}
		}
		
		public function fire():Boolean
		{
			numOfUsedAmmo = 0;
			
			if (firingType == FiringType.CONSTANT)
			{
				if (++ammoReduceTicker % ammoReduceTickerMax == 0)
				{
					numOfUsedAmmo = ammoDecrease;
					ammoReduceTicker = 0;
				}
			}
			
			if (!ready)
				return false;
			
			weaponPos = TopDownModel(owner.View.getModel()).getRootContainer().scenePosition;
			
			var fired:Boolean;
			
			for each(var m:MuzzleInfo in muzzles)
			{
				if (!m.firedEntity || firingType != FiringType.CONSTANT)
				{
					fireBullet(m);
					fired = true;
					
					if (ammoDecrease > 0)
						numOfUsedAmmo = ammoDecrease;
					else
						numOfUsedAmmo++;
				}
				else
				{
					EntBeam(m.firedEntity).HitID = CombatHit.genNextID(); 
				}
			}
			
			blockingTimer = frequency;
			ready = false;
			
			return fired;
		}
		
		public function isFiring():Boolean
		{
			return firing;
		}
		
		private function fireBullet(muzzle:MuzzleInfo):void
		{
			if (owner.RefWorld.Reference == null)
			{
				trace("Cannot fire, world is null! (related owner: " + owner + ")");
				return;
			}
			
			// create
			var firedEnt:Entity;
			
			switch(entityType)
			{
				case EnvEntityType.BULLET:
					firedEnt = gdb.EnvLib.createBullet(spawnLink);
					break;
					
				case EnvEntityType.BEAM:
					firedEnt = gdb.EnvLib.createBeam(spawnLink);
					muzzle.firedEntity = firedEnt;
					break;
			}
			
			if (firedEnt is IDamageInflictor)
			{
				if (!dmgInfo)
				{
					dmgInfo = new DamageInflictionInfo();
					dmgInfo.damageLimits = damageLimits;
					dmgInfo.ammoType = ammoType;
					
					if(firingType == FiringType.CONSTANT)
						dmgInfo.damageMod = 1 / TICK_PER_SECONDS * frequency;
					
					if(owner is RangedWeapon && RangedWeapon(owner).Equipper)
						dmgInfo.causer = RangedWeapon(owner).Equipper;
				}
				IDamageInflictor(firedEnt).initDamage(dmgInfo);
			}
			
			// bring into position
			var transforms:WorldTransforms
			var firingWeapon:Weapon = Weapon(owner);
			var firingEntity:Entity = firingWeapon.Equipper;
			var weaponModel:TopDownModel = TopDownModel(firingWeapon.View.getModel());
			
			var loc:MuzzleLocation;
			if (!muzzle.locator)
			{
				loc = new MuzzleLocation();
				muzzle.locator = loc;
				
				loc.muzzle = muzzle;
				updateMuzzleLocator(loc, muzzle);
				loc.transforms = new WorldTransforms();
			}
			else
			{
				loc = muzzle.locator;
			}
			
			loc.dirOffset = directionalOffset;
			loc.update();
			
			
			firedEnt.x = loc.x;
			firedEnt.y = loc.y;
			firedEnt.z = loc.z;
			firedEnt.rotation = loc.rotation;
			
			// add to world
			Weapon(owner).Equipper.RefWorld.Reference.addEnt(firedEnt);
			
			if (firedEnt is EntBullet)
			{
				EntBullet(firedEnt).getFired(0, loc.rotation);
			}
			else
			{
				firedEnt.Move.faceTo(firedEnt.rotation);
				firedEnt.Ani.play("idle");
				
				if (firedEnt is EntBeam)
				{
					var beam:EntBeam = EntBeam(firedEnt);
					beam.setStartNode(muzzle.locator);
					beam.HitID = CombatHit.genNextID(); 
					beam.start();
					
					beam.createEffects();
					
					
					/*
					if (beam.StartFX)
					{
						var fxModel:AwayMovieClipModel = AwayMovieClipModel(beam.StartFX.View.getModel());
						fxModel.getSprite().rotationX = -90;
						loc.muzzleContainer.addChild(fxModel.getRootContainer());
					}*/
				}
			}
		}
		
		
		// muzzle management
		
		public function addMuzzle(info:MuzzleInfo):void
		{
			muzzles.push(info);
		}
		
		private function updateMuzzleLocator(loc:MuzzleLocation, muzzle:MuzzleInfo):void 
		{
			loc.muzzleContainer = TopDownModel(owner.View.getModel()).getPartByID(muzzle.id).mesh;
			
			// NOTE: have to reset this when item is dropped or disposed!
			loc.sourceSpace = new AwayCoordSpace(loc.muzzleContainer);
			loc.targetSpace = InfWorld(getUser().RefWorld.Reference).CurrCell.CoordSpace;
		}
		
		public function updateAllMuzzleLocators():void
		{
			for each(var m:MuzzleInfo in muzzles)
			{
				if(m.locator)
					updateMuzzleLocator(m.locator, m);
			}
		}
		
		private function handleWorldScopeChange(evt:EntityEvent):void 
		{
			updateAllMuzzleLocators();
		}
		
		
		
		private function getUser():Entity
		{
			var firingWeapon:Weapon = Weapon(owner);
			var firingEntity:Entity = firingWeapon.Equipper;
			return firingEntity;
		}
		
		private function get gdb():InfWorldDataBase
		{
			return InfWorld(owner.RefWorld.Reference).DB;
		}
		
	}

}