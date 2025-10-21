package gtsinf.ents.components 
{
	import gts.ents.components.EntityComponent;
	import gts.world.objects.structures.StructureBasedEntity;
	import gtsinf.ents.basic.InfEntity;
	import gtsinf.ents.components.enums.UsableState;
	import gtsinf.ents.components.info.ButtonInfo;
	import yaway.textures.AnimationContentMultiTex;
	import yves.gui.buttons.ButtonState;
	
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public final class ButtonComponent extends EntityComponent 
	{
		private var info:ButtonInfo;
		public function get Info():ButtonInfo { return info; }
		
		private var aniContent:AnimationContentMultiTex;
		
		// **************************************
		// DE-/INIT
		// **************************************
		
		public function ButtonComponent(info:ButtonInfo)
		{
			super(EntityComponent.BUTTON);
			
			this.info = info;
		}
		
		override protected function Init():void 
		{
			super.Init();
			
			if (info.state == null)
			{
				info.state = UsableState.ON;
			}
			
			aniContent = new AnimationContentMultiTex();
			
			var aniComp:InfAnimatableComponent = InfAnimatableComponent(owner.getCompByType(EntityComponent.ANIMATABLE));
			
			if (aniComp == null)
			{
				throw new Error("Bla");
				return;
			}
			
			var libID:String = owner is StructureBasedEntity ? StructureBasedEntity(owner).Structure.sid : InfEntity(owner).libID;
			aniContent.configureSource(aniComp.Info.baseTexID, libID);
			aniComp.initSource(aniContent);
			aniComp.configureTextureAlpha(true, .1, true);
			
			updateState();
		}
		
		override protected function Deinit():void 
		{
			super.Deinit();
		}
		
		// **************************************
		// CUSTOM
		// **************************************
		
		public function operate():void 
		{
			switch(info.state)
			{
				case UsableState.ON:
					info.state = UsableState.OFF;
					updateState();
					break;
					
				case UsableState.OFF:
					info.state = UsableState.ON;
					updateState();
					break;
				
			}
		}
		
		private function updateState():void 
		{
			var aniComp:InfAnimatableComponent = InfAnimatableComponent(owner.getCompByType(EntityComponent.ANIMATABLE));
			
			if (aniComp == null)
			{
				throw new Error("Bla");
				return;
			}
			
			aniContent.setSubID(info.state);
		}
		
	}

}