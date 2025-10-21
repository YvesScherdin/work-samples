package gts.display.mdl.away 
{
	import away3d.containers.ObjectContainer3D;
	import flash.display.MovieClip;
	import flash.utils.Dictionary;
	import gts.core.Verbose;
	import gts.display.mdl.stats.CustomModelStats;
	import gts.display.mdl.IAnimatedModel;
	import gts.display.mdl.IModel;
	import gts.world.properties.skins.ISkinnable;
	import gts.world.properties.skins.ModelSkin;
	import gts.world.properties.skins.SkinCollection;
	import gts.world.properties.skins.SkinMap;
	import gtsinf.ents.basic.factories.TopDownModelFactory;
	import yaway.models.basic.AwayRootContainer;
	import yaway.models.cutout.AwayMcCutoutModel;
	import yaway.models.cutout.AwaySubClip;
	import yves.display.ani.Animation;
	import yves.display.ani.AnimationOptions;
	import yves.display.ani.AnimationSet;
	import yves.display.ani.controllers.BasicAnimationController;
	import yves.display.ani.controllers.MovieClipController;
	
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class TopDownModel extends AwayMcCutoutModel implements IAnimatedModel, ISkinnable
	{
		private var modelFactory:TopDownModelFactory;
		public function setModelFactory(modelFactory:TopDownModelFactory):void 
		{
			this.modelFactory = modelFactory;
		}
		
		private var clipController:MovieClipController;
		public function getAnimationController():BasicAnimationController { return clipController; }
		
		/** A second animation controller, for movement only. */
		private var movementAniController:MovieClipController;
		
		private var aniSet:AnimationSet;
		public function getAnimationSet():AnimationSet { return aniSet; }
		public function setAnimationSet(value:AnimationSet):void { aniSet = value; }
		
		private var allSkins:SkinCollection;
		public function get AllSkins():SkinCollection { return allSkins; }
		public function get NumSkins():int { return allSkins == null ? 0 : allSkins.NumSkins; }
		
		private var currentSkin:ModelSkin;
		public function get CurrentSkin():ModelSkin { return currentSkin; }
		public function set CurrentSkin(value:ModelSkin):void
		{
			currentSkin = value;
			setSkinByIndex(currentSkin.index);
		}
		
		private var skinIndex:int;
		public function getSkinIndex():int { return skinIndex; }
		public function setSkinByIndex(value:int):void
		{
			skinIndex = value;
			
			if (awaySubClips)
			{
				invalidate();
				
				awaySubClips.clear();
				buildSubClips();
				
				revalidate();
			}
		}
		
		public function getSkinID():String { return hasSkin(skinIndex) ? allSkins.getAt(skinIndex).id : null; }
		
		private var subSkins:SkinMap;
		public function get SubSkins():SkinMap { return subSkins; }
		
		private var clipNodeMap:Dictionary;
		
		private var customStats:CustomModelStats;
		public function get CustomStats():CustomModelStats { return customStats; }
		
		
		private var movingParts:Array;
		
		private var moveChannelActive:Boolean;
		
		private var height:Number;
		public function get Height():Number { return height; }
		//public function set Height(value:Number):void { height = value; }
		
		private var heightMap:Object;
		private var heightMapModifier:Number;
		
		private var interactive:Boolean;
		
		
		// **************************************
		// DE-/INIT
		// **************************************
		
		public function TopDownModel( clip:MovieClip=null ) 
		{
			heightMapModifier = 1;
			clipNodeMap = new Dictionary();
			
			if ( clip )
			{
				init( clip );
			}
		}
		
		public function initCustomStats(customStats:CustomModelStats):void 
		{
			this.customStats = customStats;
		}
		
		override public function init(mainClip:MovieClip):void 
		{
			super.init(mainClip);
			clipController = new MovieClipController(mainClip);
		}
		
		override protected function buildSubClips():void 
		{
			super.buildSubClips();
			
			// TODO: readjust sub nodes
		}
		
		// **************************************
		// LIFE TIME
		// **************************************
		
		override public function update():void
		{
			if (clipController.update())
			{
				awaySubClips.update();
			}
			else if (moveChannelActive)
			{
				clipController.refreshCurrentFrame();
				awaySubClips.update();
			}
			
			if (moveChannelActive)
			{
				updateMovement();
				clipController.refreshCurrentFrame();
			}
		}
		
		override public function revalidate():void 
		{
			super.revalidate();
			
			if (interactive)
				setMouseInteractivity(true);
		}
		
		// **************************************
		// MOVEMENT
		// **************************************
		
		public function configureMovement(movingParts:Array):void
		{
			this.movingParts = movingParts;
			
			movementAniController = new MovieClipController(mainClip);
		}
		
		public function startMovement(aniID:String):void
		{
			moveChannelActive = true;
			var ani:Animation = aniSet.getAni(aniID);
			
			if(ani && movementAniController)
				movementAniController.animate(ani);
		}
		
		public function updateMovement():void
		{
			if (!movementAniController)
				return;
			
			if (movementAniController.update())
			{
				//trace("--mov up");
				awaySubClips.updateSelective(movingParts);
			}
		}
		
		public function stopMovement():void
		{
			if (movementAniController)
			{
				movementAniController.stop();
				movementAniController.forgetCurrentAni();
			}
			
			moveChannelActive = false;
			
			clipController.refreshCurrentFrame();
			awaySubClips.update();
		}
		
		// **************************************
		// ANIMATION
		// **************************************
		
		public function animate( animation:Animation, options:AnimationOptions=null ):void
		{
			if(!options || options.channel == 0)
				clipController.animate(animation);
			else
				startMovement(animation.Name);
		}
		
		public function stopAnimation(options:AnimationOptions = null):void
		{
			if(!options || options.channel == 0)
				clipController.stop();
			else
				stopMovement();
		}
		
		// **************************************
		// CONTROLLER
		// **************************************
		
		public function removeModelFromController(model:IModel, containerID:String, ctrlID:String):void
		{
			var mdl:TopDownModel = TopDownModel(model);
			mdl.getRootContainer().parent.removeChild(mdl.getRootContainer());
			mdl.ignoreScale(false);
		}
		
		public function applyModelToController(model:IModel, containerID:String, ctrlID:String, rotationOffset:Number=0):void
		{
			// get sub clip in MovieClip
			var mc:MovieClip = MovieClip(clipController.getClip().getChildByName(containerID));
			
			if (!mc)
				return;
			
			// get controller
			mc = mc[ctrlID];
			
			if (!mc)
				return;
			
			// prepare model to apply
			var mdl:TopDownModel = TopDownModel(model);
			mdl.ignoreScale(true);
			
			var subClip:AwaySubClip = mdl.awaySubClips.getAt(0);
			
			//trace(subClip.origin);
			// the origin has to be readjusted, because it is not there where it shall be in the moment it gets added
			var cont:AwayRootContainer = mdl.getRootContainer();
			cont.x =  mc.x;
			cont.y = -mc.y;
			cont.z = -1;
			
			cont.rotationX = -90;
			cont.rotationY = 0;
			cont.rotationZ = -mc.rotation + rotationOffset;
			
			// get sub mesh that is related to to movieclip
			var clip:AwaySubClip = awaySubClips.subClipMap[containerID];
			clip.mesh.addChild(cont);
		}
		
		// **************************************
		// CLIP NODE
		// **************************************
		
		private function buildClipNode(bpID:String, nodeID:String):ObjectContainer3D
		{
			var nodeName:String = bpID + "." + nodeID;
			var clipNode:ObjectContainer3D = new ObjectContainer3D();
			clipNode.name = nodeName;
			var mc:MovieClip = MovieClip(clipController.getClip().getChildByName(bpID));
			
			if (!mc)
				return null;
			
			// get controller
			mc = MovieClip(mc.getChildByName(nodeID));
			
			if (!mc)
				return null;
			
			clipNode.x =  mc.x;
			clipNode.y = -mc.y;
			clipNode.z = -1;
			
			clipNode.rotationX = -90;
			clipNode.rotationY = 0;
			clipNode.rotationZ = -mc.rotation;
			
			clipNodeMap[nodeName] = clipNode;
			
			var clip:AwaySubClip = awaySubClips.subClipMap[bpID];
			clip.mesh.addChild(clipNode);
			return clipNode;
		}
		
		public function getClipNode(bpID:String, nodeID:String):ObjectContainer3D
		{
			return clipNodeMap[bpID+"."+nodeID] || buildClipNode(bpID, nodeID);
		}
		
		public function getPartByID(id:String):AwaySubClip
		{
			return awaySubClips.getByID(id);
		}
		
		// **************************************
		// SKINS
		// **************************************
		
		public function initSkins(allSkins:SkinCollection):void
		{
			this.allSkins = allSkins;
			
			if (allSkins.SubSkinMap)
			{
				subSkins = new SkinMap();
			}
		}
		
		public function hasSkin(def:*):Boolean
		{
			return allSkins != null && allSkins.contains(def);
		}
		
		public function changeSkin(def:*):Boolean
		{
			if (!allSkins || !allSkins.contains(def))
			{
				throw new Error("Skin '" + def + "' invalid for model '" + modelID + "'");
				return false;
			}
			
			return modelFactory.changeSkin(this, def);
		}
		
		public function changeBodyPartSkin(bpID:String, def:*):void
		{
			modelFactory.applySkinToBodyPart(this, bpID, def);
		}
		
		public function setSubSkin(bpID:String, skin:ModelSkin):void 
		{
			invalidate();
			subSkins.reg(bpID, skin);
			awaySubClips.refreshSubClip(bpID);
			revalidate();
		}
		
		public function getSubSkin(bpID:String):ModelSkin
		{
			var skin:ModelSkin = subSkins != null ? subSkins.retrieve(bpID) : null;
			return skin;
		}
		
		// **************************************
		// HEIGHT MAP
		// **************************************
		
		public function modifyHeightMap(multiplicator:Number):void
		{
			heightMapModifier = multiplicator;
			/*
			for each(var pb:AwaySubClip  in awaySubClips.All)
			{
				pb.mesh.z *= multiplicator;
			}
			*/
			applyHeightMap();
		}
		
		public function setHeightMap(value:Object):void 
		{
			this.heightMap = value;
			applyHeightMap();
		}
		
		private function applyHeightMap():void 
		{
			if (true)
			{
				var modifier:Number = heightMap["_modifier"];
				
				if (isNaN(modifier) || modifier <= 0)
					modifier = 1;
				
				modifier *= heightMapModifier;
				
				var maxHeight:int = 0;
				
				for (var key:String in heightMap)
				{
					if (key.charAt(0) == "_")
						continue;
					
					var pb:AwaySubClip = getPartByID(key);
					if (pb == null)
					{
						Verbose.logError(new Error("no bodypart '" + key + "' for model '" + modelID + "'"));
						continue;
					}
					
					var ht:int = -heightMap[key] * modifier;
					pb.mesh.z = ht;
					
					if (Math.abs(ht) > maxHeight)
						maxHeight = Math.abs(ht);
				}
				
				height = maxHeight;
			}
		}
		
	}

}