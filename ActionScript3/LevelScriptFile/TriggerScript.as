package yves.scripting.triggers 
{
	import yves.events.EventAdapter;
	import yves.events.IEventAdapterHandler;
	import yves.scripting.BasicScript;
	import yves.scripting.IScriptingAPI;
	import yves.scripting.statements.compound.BlockStatement;
	import yves.scripting.statements.IStatementDomain;
	import yves.scripting.statements.StatementHandler;
	import yves.scripting.StatementThreadController;
	import yves.scripting.variables.VariableStorage;
	/**
	 * A TriggerScript is a special script that consists of many triggers that contain scripting statements.
	 * These statements have to be applied to a Scripting-API to take effect.
	 * 
	 * Multiple triggers can be active at one time. Each running Trigger has its own (pseudo-)thread.
	 * All running (or not running) threads are registered here.
	 * The script can also be paused, which will also pause the threads.
	 * 
	 * Furthermore the TriggerScript has its own variable storage which might be modified during execution of assignment statements.
	 * 
	 * @version 1.0
	 * @author Yves Scherdin
	 */
	public class TriggerScript extends BasicScript implements IEventAdapterHandler, IStatementDomain
	{
		protected var sapi:IScriptingAPI;
		public function setScriptAPI(value:IScriptingAPI):void { sapi = value; }
		
		
		protected var localVars:VariableStorage;
		public function get LocalVars():VariableStorage { return localVars; }
		
		
		protected var triggers:TriggerCollection;
		public function get Triggers():TriggerCollection { return triggers; }
		
		
		protected var threads:StatementThreadController;
		//public function get Threads():StatementThreadController { return threads; }
		
		
		
		public function TriggerScript() 
		{
			localVars = new VariableStorage();
			triggers = new TriggerCollection();
			threads = new StatementThreadController();
		}
		
		/**
		 * Activates all those triggers that shall be initially active.
		 */
		public function setup():void
		{
			for each(var trigger:Trigger in triggers.getAsArray())
			{
				trigger.setParentDomain(this);
				
				if(trigger.Active)
					activateTrigger(trigger);
			}
		}
		
		public function tearDown():void
		{
			for each(var trigger:Trigger in triggers.getAsArray())
			{
				if(trigger.Active)
					deactivateTrigger(trigger);
			}
			
			threads.kill(); // added this on 2014/03/14....
		}
		
		
		/**
		 * Executes a block of statements.
		 * A free thread will be created or picked out of the list of existing ones and cares about executing the block.
		 * An alternate varaible storage can also be passed which will represent the scope of local variables of the block
		 * (mostly the script's localVars is wanted here, which is also the default value; so it can be left blank).
		 * 
		 * @param	statement
		 * @param	storage
		 */
		protected function executeStatement(statement:BlockStatement, storage:VariableStorage=null, domain:IStatementDomain=null):void
		{
			var handler:StatementHandler = getFreeThread();
			handler.setRelatedStorage(storage || localVars);
			handler.handleBlock(statement, domain || this);
			threads.runOrAddToWaitingList(handler);
		}
		
		
		/**
		 * Searches for a free thread (that is a StatementHandler that has currently no statement to execute and is not blocked).
		 * If not a single one is found a new StatementHandler-thread is created.
		 * 
		 * @return
		 */
		protected function getFreeThread():StatementHandler
		{
			var handler:StatementHandler = threads.getNextFree();
			
			if (handler == null)
			{
				handler = new StatementHandler(sapi);
				handler.setThreadController(threads);
			}
			
			return handler;
		}
		
		
		
		// DE-/ACTIVATION
		
		/**
		 * Marks the trigger as 'active' and connects it to event emitter, by which it can be triggered from this point on.
		 * Once the trigger was triggered, its conditions must be passed, before its actions will be executed.
		 * 
		 * @param	trigger
		 */
		protected function activateTrigger(trigger:Trigger):void
		{
			trigger.Active = true;
			
			// TODO: add event listeners to api
			//   TODO: determine how the events shall be stored, since they consist of two string values
			for each(var info:EventAdapter in trigger.Events)
				resolveEvent(info, trigger);
		}
		
		/**
		 * Marks the trigger as 'not active' and disconnects it from event emitter. From then on no incoming events can trigger it.
		 * This has no effect on threads that are currently processing its event block.
		 * 
		 * @param	trigger
		 */
		protected function deactivateTrigger(trigger:Trigger):void
		{
			// Note: running statements of this trigger will not be affected.
			trigger.Active = false;
			
			// TODO: remove event listeners from api
			for each(var info:EventAdapter in trigger.Events)
				info.remove();
		}
		
		
		public function activateTriggerByID(triggerID:String):void
		{
			var trigger:Trigger = triggers.getByID(triggerID);
			
			if (trigger == null)
			{
				throw new Error("Trigger not found: " + triggerID);
			}
			
			activateTrigger(trigger);
		}
		
		public function deactivateTriggerByID(triggerID:String):void
		{
			var trigger:Trigger = triggers.getByID(triggerID);
			
			if (trigger == null)
			{
				throw new Error("Trigger not found: " + triggerID);
			}
			
			if(trigger.Active)
				deactivateTrigger(trigger);
		}
		
		
		// EVENTS
		public function resolveEvent(info:EventAdapter, trigger:Trigger):void
		{
			sapi.findEventDomain(info);
			info.handler = this;
			info.autoRemoval = false;
			info.data = trigger;
			info.add();
		}
		
		/**
		 * !!!Do not call manually!!!
		 * Gets only called by an EventAdapter object when an event of an active trigger was emitted.
		 * There are as much event adapters as events that are listened to by each active script.
		 * 
		 * @param	adapter
		 */
		public function handleEventAdapter(adapter:EventAdapter):void 
		{
			// TODO: should check here if this is deinited already
			var trigger:Trigger = Trigger(adapter.data);
			executeTrigger(trigger, false);
		}
		
		
		// EXECUTING
		
		/**
		 * Executes the block of given trigger, either regarding or ignoring its condition
		 * 
		 * @param	trigger
		 * @param	ignoreCondition
		 */
		protected function executeTrigger(trigger:Trigger, ignoreCondition:Boolean=false):void
		{
			// TODO: might add a 'isAlreadyDeinited()'-check here, for avoiding errors (might occur if this is called via api call)
			var mayTrigger:Boolean = ignoreCondition || !trigger.hasConditions();
			
			if (!mayTrigger)
			{
				// forced to evaluate the condition
				sapi.CurrentScope = localVars;
				mayTrigger = sapi.evalCondition(trigger.Conditions)
			}
			
			if (mayTrigger)
			{
				if (trigger.Once)
					deactivateTrigger(trigger);
				
				executeStatement(trigger.Actions, localVars, trigger);
			}
		}
		
		
		public function executeTriggerByID(triggerID:String, ignoreCondition:Boolean=false):void
		{
			var trigger:Trigger = triggers.getByID(triggerID);
			executeTrigger(trigger, ignoreCondition);
		}
		
		
		
		// PAUSING
		
		public function pause():void
		{
			throw new Error("Not implemented yet!");
		}
		
		public function unpause():void
		{
			throw new Error("Not implemented yet!");
		}
		
		
		/* INTERFACE yves.scripting.statements.IStatementDomain */
		
		public function getParentDomain():IStatementDomain 
		{
			return null;
		}
		
		public function setParentDomain(value:IStatementDomain):void 
		{
			throw new Error("The TriggerScript may not own a parent statement domain.");
		}
		
	}
}