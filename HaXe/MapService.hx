package doodad.website.utils;
import haxe.ds.StringMap;
import doodad.core.data.LayoutMode;
import doodad.core.data.CityName;
import doodad.core.data.EventData;
import doodad.core.data.EventType;
import doodad.core.utils.Loca;
import doodad.core.queries.EventQuery;
import doodad.core.utils.EventTypeUtil;
import doodad.core.utils.TextUtil;
import doodad.core.utils.URLMap;
import doodad.website.comps.topics.TopicID;
import doodad.website.MainAPI;
import mapboxgldoodad.converters.EventDataToGeoJson;
import mapboxgldoodad.converters.EventDataToGeoJson.ConversionOptions;
import mapboxgldoodad.converters.EventDataToGeoJson.EventFeaturePropertiesData;
import mapboxgl.data.FeatureCollectionData;

/**
 * ...
 * @author Yves Scherdin
 */
class MapService
{
	private var mapi:doodad.website.MainAPI;
	
	public function new() 
	{
		mapi = MainAPI.Instance;
	}
	
	public function generateFeatureList(layerID:String, smallDistanceHandling:Bool):FeatureCollectionData
	{
		var events = new EventQuery(mapi.Events.copy()).getAllMatching(getFilter(layerID));
		events = postProcessEventList(events);
		var options:ConversionOptions = { iconOffsets: smallDistanceHandling };
		var mapData:FeatureCollectionData = mapboxgldoodad.converters.EventDataToGeoJson.convert(events, options);
		
		for (feat in mapData.features)
		{
			var properties:EventFeaturePropertiesData = cast feat.properties;
			//properties.icon = "theatre";
			//properties.icon = "icon_map_vigil_small";
		}
		
		return mapData;
	}
	
	/**
	 * Can only show one event per city.
	 * 
	 * @param	allEvents
	 */
	public function postProcessEventList(allEvents:Array<EventData>):Array<EventData>
	{
		var map:StringMap<EventData> = new StringMap<EventData>();
		var nowTime:Float = Date.now().getTime();
		
		for (bEvent in allEvents)
		{
			if (map.exists(bEvent.city))
			{
				var aEvent:EventData = map.get(bEvent.city);
				var aPrimary:Bool = aEvent.type == EventType.exercise || aEvent.type == EventType.lecture;
				var bPrimary:Bool = bEvent.type == EventType.exercise || bEvent.type == EventType.lecture;
				var aTime:Float = aEvent.sortDate.getTime();
				var bTime:Float = bEvent.sortDate.getTime();
				
				aTime = computeDateSortPriority(aTime, nowTime);
				bTime = computeDateSortPriority(bTime, nowTime);
				
				if (bTime > aTime && !(aPrimary && !bPrimary))
					map.set(bEvent.city, bEvent);
			}
			else
			{
				map.set(bEvent.city, bEvent);
			}
		}
		
		var newList:Array<EventData> = new Array<EventData>();
		var it = map.keys();
		while(it.hasNext())
		{
			newList.push(map.get(it.next()));
		}
		
		return newList;
	}
	
	inline function computeDateSortPriority(time:Float, now:Float):Float
	{
		return (time-now) + (time < now ? 1000 : 0);
	}
	
	function getFilter(layerID:String):EventData->Bool
	{
		return switch(layerID)
		{
			case EventLayerID.layer_special:	shallDisplayOnSpecialLayer;
			case EventLayerID.layer_exercise:	shallDisplayOnExerciseLayer;
			case _:							null;
		}
	}
	
	private function shallDisplayOnExerciseLayer(e:EventData):Bool
	{
		if (e.omitOnMap)
			return false;
		
		return EventTypeUtil.eventTypesOnExerciseLayer.indexOf(e.type) != -1;
	}
	
	private function shallDisplayOnSpecialLayer(e:EventData):Bool
	{
		if (e.omitOnMap)
			return false;
		
		return EventTypeUtil.eventTypesOnSpecialLayer.indexOf(e.type) != -1;
	}
	
	public function useSmallMap():Bool
	{
		return mapi.getLayoutMode() == LayoutMode.SMALL;
	}
	
	public function getToolTipName(p:EventFeaturePropertiesData):String
	{
		var data:EventData = null;
		
		try {
			data = mapi.getEventDataById(p.eventID);
		} catch (e:Dynamic) {
			
		}
		
		if (data == null)
			return p.city;
		
		if (!TextUtil.isEmpty(data.titleOnMap))
		{
			return data.titleOnMap;
		}
		
		var str:String = p.city;
		
		if (p.city == CityName.London)
		{
			var rawDate:String = TextUtil.fromDateShort(data.date);
			str += ", " + rawDate;
		}
		/*else if (data.type == EventType.anythingToSay)
		{
			str = Loca.sub(data.type, Loca.lang.eventType) + "<br>" + p.city;
		}
		*/
		return str;
	}
	
	public function generateMapToolTip(p:EventFeaturePropertiesData):String
	{
		var lang = mapi.Lang;
		var data:EventData = null;
		
		try { 
			data = mapi.getEventDataById(p.eventID);
		} catch (e:Dynamic) {
			return "<div>Error: Strange error occured</div>"
				 + "<br\\>" + "<div>" + e + "</div>";
		}
		
		if (data == null)
			return "<div>Error: Data not found: '" + p.eventID + "'</div>";
		
		var hasChange:Bool = data.changes != null;
		
		var rawDate:String = data.date;
		var rawTime:String = data.time;
		var time:String = null;
		
		if (!data.old)
		{
			if (data.date != null && data.endDate != null)
				time = TextUtil.fromDateRange(rawDate, data.endDate);
			else if(rawDate != null && rawTime != null)
				time = TextUtil.fromDateAndTimeString(rawDate, rawTime, false, false);
		}
		
		if (time == null )
			time = TextUtil.fromRepetition2(data.interval, false, data.dayOfWeek, data.repetition, false);
			
		if (time == null)
			time = "";
		
		var location:String = data.location;
		
		var description:String = "";
		description += "<h6><strong>" + Loca.sub(data.type, Loca.lang.eventType) + " " + p.city + "</strong></h6>";
		description += "<br\\>";
		description += "<div>" + TextUtil.fromLocation(location, false) + "</div>";
		description += "<br\\>";
		description += "<div>" + time + "</div>";
		description += "<br\\>";
		description += "<a href='#"+URLMap.makeReadableURL(TopicID.events)+"/" + data.id + "'>"+ lang.more_info + "</a>";
		
		return description;
	}
	
	public function getAccessToken():String
	{
		return "pk.LONGGENERATEDKEYLONGGENERATEDKEY.LONGGENERATEDKEYLONGGENERATEDKEY";
	}
	
}

class EventLayerID
{
	inline static public var layer_exercise:String = "layer_exercise";
	inline static public var layer_special:String = "layer_special";
}