package doodad.website.utils;
import doodad.website.MainAPI;
import doodad.website.comps.topics.TopicID;
import doodad.core.data.EventData;
import doodad.core.data.EventType;
import doodad.core.queries.EventQuery;
import doodad.core.utils.DateUtil;
import doodad.core.utils.EventTypeUtil;
import doodad.core.utils.Loca;
import doodad.core.queries.EventQuery.EventFilter_Type;
import doodad.core.utils.TextUtil;
import doodad.core.utils.URLMap;
import haxe.ds.StringMap;
import js.Browser;
import js.JQuery;
import js.html.DivElement;
import js.html.Element;

/**
 * ...
 * @author Yves Scherdin
 */
class NewsGenerator
{
	static private var now:Float;
	static private var today:Float;
	static private var tomorrow:Float;
	static private var dateNote:String;
	
	inline static private var NEWS_DELIMETERS:String = "+++";
	inline static private var DELIMETER_SAME_TYPE:String = ", ";
	inline static private var DELIMETER_CATEGORY:String = " | ";
	
	static public function fromEvents(events:Array<EventData>, api:MainAPI):Array<String>
	{
		var nowDate:Date = Date.now();
		var eq:EventQuery = new EventQuery();
		var element:String = null;
		
		var featuredEvents:Array<EventData> = eq.from(events.copy()).filter(isEventFeatured).getAll();
		
		// try to enlist todays events
		var list:Array<String> = [];
		
		inline function generateElement():Void
		{
			eq.from(events.copy()).filter(isEventToday);
			
			if (eq.getAll().length != 0)
			{
				list.push(fromDaysEvents(eq.getAll()));
			}
		}
		
		// check today
		setTimeFocus(nowDate, Loca.lang.today);
		generateElement();
		
		// check tomorrow
		nowDate = DateUtil.getNextDay(nowDate);
		setTimeFocus(nowDate, Loca.lang.tomorrow);
		generateElement();
		
		// check featured events
		if (featuredEvents.length != 0)
		{
			featuredEvents.sort(compareEventOnPrio);
			var featureElements = fromFeaturedEvents(featuredEvents);
			for (e in featureElements)
				list.push(e);
		}
		
		return list;
	}
	
	static private function describeEventList(list:Array<EventData>):String
	{
		return [for(data in list) data.id].join(", ");
	}
	
	static private function compareEventOnPrio(a:EventData, b:EventData):Int
	{
		var pa = a.featured.prio != null ? a.featured.prio : 0;
		var pb = b.featured.prio != null ? b.featured.prio : 0;
		return pa == pb ? 0 : (pa < pb ? -1 : 1);
	}
	
	static private function setTimeFocus(date:Date, dateString:String):Void
	{
		now		 = date.getTime();
		today	 = DateUtil.roundDayDown(date).getTime();
		tomorrow = DateUtil.roundDayUp  (date).getTime();
		
		dateNote = dateString;
	}
	
	static public function fromFeaturedEvents(events:Array<EventData>):Array<String>
	{
		var list:Array<String> = [];
		var primaryEvents:Array<EventData> = [];
		var newEvents:Array<EventData> = [];
		
		for (evt in events)
		{
			if (evt.featured.isNew)
			{
				newEvents.push(evt);
			}
			else
			{
				primaryEvents.push(evt);
			}
		}
		
		for (evt in primaryEvents)
		{
			list.push(fromSingleFeaturedEvent(evt));
		}
		
		if (newEvents.length != 0)
		{
			var str:String = Loca.lang.newWithUs + ": ";
			str += [for (evt in newEvents) {
				("<a href='" + "#"+URLMap.makeReadableURL(TopicID.events)+"/" + evt.id + "'>" + fromEventShort(evt) + "</a>" );
			}].join(DELIMETER_SAME_TYPE);
			
			list.push(str);
		}
		
		return list;
	}
	
	static private function fromSingleFeaturedEvent(evt:EventData):String
	{
		var str:String = "";
		
		str += "<a href='" + "#"+URLMap.makeReadableURL(TopicID.events)+"/" + evt.id + "'>";
		
		if (evt.featured.bannerText == null)
		{
			str += TextUtil.fromDate(evt.date) + ": " + EventDataUtil.getTitle(evt);
		}
		else
		{
			str += evt.featured.bannerText;
		}
		
		str += "</a>";
		
		return str;
	}
	
	static private function fromEventShort(evt:EventData):String
	{
		if (evt.type == EventType.exercise)
			return TextUtil.fromEventType(EventType.exercise) + " " + evt.city;
		else
			return EventDataUtil.getTitle(evt);
	}
	
	static public function fromDaysEvents(events:Array<EventData>):String
	{
		var str:String = "";
		str += dateNote + ": ";
		
		var newsSegments:Array<String> = [];
		
		if (events.length == 1)
		{
			newsSegments.push(generateSingularElements(events, true).join(DELIMETER_SAME_TYPE));
		}
		else
		{
			var eventTypeOrder:Array<String> = EventTypeUtil.eventTypeOrder;
			var eq:EventQuery = new EventQuery(events.copy());
			var filter = new EventFilter_Type();
			
			for (type in eventTypeOrder)
			{
				var sel:Array<EventData> = eq.getAllMatching(filter.with(type).contains);
				
				var sub:String = switch(sel.length)
				{
					case 0:	continue;
					case 1: generateSingularElements(sel).join(DELIMETER_SAME_TYPE);
					case _:	generateGroupedElements(type, sel);
				}
				
				newsSegments.push(sub);
			}	
		}
		
		str += newsSegments.join(DELIMETER_CATEGORY);
		
		return str;
	}
	
	static public function wrapInNewsChars(str:String):String
	{
		return NEWS_DELIMETERS + " " + str + " " + NEWS_DELIMETERS;
	}
	
	static public function generateSingularElements(events:Array<EventData>, ?hasMuchSpace:Bool, ?noteToAdd:String):Array<String>
	{
		var newsSegments:Array<String> = [];
		
		for (evt in events)
		{
			newsSegments.push(generateSingleElement(evt, events.length == 1 && hasMuchSpace));
		}
		
		return newsSegments;
	}
	
	static public function generateSingleElement(evt:EventData, useTitle:Bool, ?noteToAdd:String, ?style:String):String
	{
		var sub:String = "<a href='" + "#" + URLMap.makeReadableURL(TopicID.events) + "/" + evt.id + "'" + (style != null ? ' class="$style"' : "") + ">";
		
		if (useTitle)
			sub += EventDataUtil.getTitle(evt);
		else
			sub += TextUtil.fromEventType(evt.type);
		
		if(!TextUtil.isEmpty(evt.city))
			sub += " " + Loca.lang.inPlace + " " + evt.city;
		
		if (noteToAdd != null)
		{
			sub += noteToAdd;
		}
		
		sub += "</a>";
		
		return sub;
	}
	
	static private function generateGroupedElements(type:String, events:Array<EventData>):String
	{
		var newsSegments:Array<String> = [];
		var isNotCityBased:Bool = type == EventType.onlineEvent || type == EventType.webinar;
		
		var i:Int = 0;
		for (evt in events)
		{
			i++;
			var sub:String = "<a href='" + "#" + URLMap.makeReadableURL(TopicID.events) + "/" + evt.id +"'>";
			if (isNotCityBased)
				sub += "#" + i;
			else
				sub += evt.city;
				
			sub += "</a>";
			newsSegments.push(sub);
		}
		
		if(isNotCityBased)
			return TextUtil.fromEventType(type, true) + ": " + newsSegments.join(DELIMETER_SAME_TYPE);
		else
			return TextUtil.fromEventType(type, true) + " " + Loca.lang.inPlace + " " + newsSegments.join(DELIMETER_SAME_TYPE);
	}
	
	static private function isEventToday(event:EventData):Bool
	{
		return event.beginTime >= today && event.beginTime < tomorrow && !event.nextDateIsPlanned;
	}
	
	static private function isEventExercise(event:EventData):Bool
	{
		return event.type == EventType.exercise;
	}
	
	static private function isEventFeatured(event:EventData):Bool
	{
		return event.featured != null && !event.featured.silent;
	}
}