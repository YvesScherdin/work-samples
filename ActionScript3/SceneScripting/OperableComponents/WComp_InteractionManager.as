package gtsinf.world.components 
{
	import Box2D.Dynamics.b2Fixture;
	import gts.ents.basic.Entity;
	import gts.ents.components.EntityComponent;
	import gts.events.ents.EntityEvent;
	import gts.events.ents.InteractionEvent;
	import gts.events.ents.OperationEvent;
	import gts.events.world.WorldEvent;
	import gts.system.GameAPI;
	import gts.world.components.WorldComponent;
	import gtsinf.world.utils.filters.InteractableFilter;
	import gtsconv.scene.ConversationScene;
	import gtsconv.scene.ConversationSceneInfo;
	import gtsinf.ents.actions.Action_Sit;
	import gtsinf.ents.actions.interaction.InfInteractionType;
	import gtsinf.ents.chars.misc.CharRole;
	import gtsinf.ents.components.ButtonComponent;
	import gtsinf.ents.components.InteractableComponent;
	import gtsinf.ents.EntChar;
	import gtsinf.ents.EntVehicle;
	import gtsinf.world.serialization.CollisionShapeInfo;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public class WComp_InteractionManager extends InfWorldComponent
	{
		private var gameAPI:GameAPI;
		
		
		// INITIALIZATION
		
		public function WComp_InteractionManager() 
		{
			super();
			
			type = TYPE__InteractionManager;
			eventMark = "wc_im" + (NUMERATOR++);
		}
		
		override public function init():void 
		{
			super.init();
			
			world.EventMan.createListener(WorldEvent.SCENE_READY, handleSceneReady, eventMark, true);
			world.EventMan.createListener(InteractionEvent.USED,  handleEntityUsed, eventMark);
			
			// TODO: add listeners:  OBJECT_ADDED, OBJECT_REMOVED, (INVALIDATED?)
		}
		
		override public function deinit():void 
		{
			world.EventMan.clearByMark(eventMark);
			super.deinit();
		}
		
		
		public function configure2(gameAPI:GameAPI):void 
		{
			this.gameAPI = gameAPI;
		}
		
		
		// EVENT HANDLERS
		
		private function handleSceneReady(event:WorldEvent):void 
		{
			// traverse objects, check for interactive ones
			
			var allInteractables:Array = world.EntFinder.getAllEntsInFilter(new InteractableFilter());
			
			for each(var ent:Entity in allInteractables)
			{
				if (ent is EntChar)
				{
					var shallBe:Boolean = EntChar(ent).Role == CharRole.BARTENDER || EntChar(ent).Role == CharRole.MERCHANT;
					if (!shallBe)
					{
						var comp:InteractableComponent = resolveComponent(ent, false);
						if (comp != null && comp.ActionParams)
						{
							shallBe = true;
						}
					}
					
					if (shallBe)
						activate(ent);
				}
				else
					activate(ent);
			}
		}
		
		private function handleEntityUsed(event:InteractionEvent):void 
		{
			var target:Entity = event.Target;
			var comp:InteractableComponent = InteractableComponent(target.getCompByType(EntityComponent.INTERACTABLE));
			
			switch(event.InteractionType)
			{
				case InfInteractionType.TALK:
					if (comp.ActionParams)
					{
						var info:ConversationSceneInfo = ConversationSceneInfo.fromData([comp.ActionParams.conversation, comp.ActionParams.state]);
						gameAPI.StateMan.enter(new ConversationScene(info));
					}
					
					break;
				
				case InfInteractionType.EMBARK:
					// TODO: encapsulate into Action_Embark
					EntVehicle(event.Target).embark(event.Causer);
					world.EventMan.addEvent(new EntityEvent(EntityEvent.CONTROL_GAINED, target, target));
					break;
				
				//case InteractionType.OPEN:
					
					//break;
				
				case InfInteractionType.SIT:
					event.Causer.Actions.Apply(new Action_Sit(target));
					break;
					
				case InfInteractionType.USE:
				default:
					useEnt(event.Causer, event.Target);
					break;
			}
		}
		
		private function useEnt(causer:Entity, target:Entity):void 
		{
			target.Events.Send(new OperationEvent(OperationEvent.OPERATED, causer, target));
			
			//var buttonComp:ButtonComponent = ButtonComponent(target.getCompByType(EntityComponent.BUTTON));
			
			//if(buttonComp)
				//buttonComp.operate();
		}
		
		
		// STUFF
		
		public function toggleInteractive(ent:Entity, state:Boolean):void
		{
			if (state)
				activate(ent);
			else
				deactivate(ent);
		}
		
		public function activate(ent:Entity):void
		{
			var comp:InteractableComponent = resolveComponent(ent);
			
			if (comp.InUse)
				return;
			
			var shapeInfo:CollisionShapeInfo = comp.getShapeInfo();
			
			var sensor:b2Fixture = infWorld.CurrCell.Physix.addSensorShapeTo(ent, shapeInfo);
			sensor.SetUserData("sensor");
			
			comp.Sensor = sensor;
			comp.InUse = true;
		}
		
		public function deactivate(ent:Entity):void
		{
			var comp:InteractableComponent = resolveComponent(ent);
			
			if (!comp.InUse)
				return;
			
			if (comp.Sensor != null)
			{
				infWorld.CurrCell.Physix.removeSensorShapeFrom(ent, comp.Sensor);
			}
			
			comp.InUse = false;
		}
		
		private function resolveComponent(ent:Entity, createIfNotExists:Boolean=true):InteractableComponent
		{
			var comp:InteractableComponent = InteractableComponent(ent.CompPack.GetByType(EntityComponent.INTERACTABLE));
			
			if(comp == null && createIfNotExists)
				comp = InteractableComponent(ent.CompPack.Add(new InteractableComponent()));
			
			return comp;
		}
		
		
	}

}