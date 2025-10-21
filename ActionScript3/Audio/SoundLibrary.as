package yves.assets.snd 
{
	import flash.media.Sound;
	import flash.utils.Dictionary;
	import yves.assets.basic.*;
	import yves.assets.basic.AssetRequest;
	/**
	 * ...
	 * @author Yves Scherdin
	 */
	public final class SoundLibrary extends AssetLibrary 
	{
		private var groups:Dictionary;
		
		
		public function SoundLibrary( name:String="" ) 
		{
			super( name );
			groups = new Dictionary();
		}
		
		
		public function loadData( url:String, id:String, groupID:String=null, callBack:Function=null ):SoundAsset
		{
			var asset:SoundAsset = new SoundAsset( id, url, groupID );
			addAsset(asset);
			if ( callBack != null )
				asset.addAssetRequest( new AssetRequest( id, callBack ) );
			
			registerAsset( asset );
			return asset;
		}
		
		
		
		public function findSoundGroup( id:String ):Vector.<String>
		{
			if ( !id || id == "-" ) return null;
			
			if ( groups[id] == undefined )
			{
				groups[id] = new Vector.<String>();
			}
			return groups[id];
		}
		
		
		
		override public function isAssetAllowed(asset:Asset):Boolean 
		{
			return asset is SoundAsset;
		}
		
		override public function addAsset(asset:Asset):void 
		{
			super.addAsset(asset);
			
			var soundGroup:Vector.<String> = findSoundGroup( SoundAsset(asset).GroupID );
			if ( soundGroup && soundGroup.indexOf( asset.ID ) == -1 )
				soundGroup.push( asset.ID );
		}
		
		public function getSoundByGroup( id:String ):Sound
		{
			var soundGroup:Vector.<String> = findSoundGroup( id );
			switch( soundGroup.length )
			{
				case 0:  return null;
				case 1:  return getSoundByID( soundGroup[0] );
				default: return getSoundByID( soundGroup[int(Math.random() * soundGroup.length)] );
			}
		}
		
		public function getSoundIDByGroup( id:String ):String
		{
			var soundGroup:Vector.<String> = findSoundGroup( id );
			switch( soundGroup.length )
			{
				case 0:  return null;
				case 1:  return soundGroup[0];
				default: return soundGroup[int(Math.random() * soundGroup.length)];
			}
		}
		
		
		public function getSoundByID( id:String ):Sound
		{
			for each( var asset:SoundAsset in assets )
			{
				if ( asset.ID == id )
					return asset.getData();
			}
			return null;
		}
		
		public function getAssetByID( id:String ):SoundAsset
		{
			for each( var asset:SoundAsset in assets )
			{
				if ( asset.ID == id )
					return asset;
			}
			return null;
		}
		
		override public function getAssetStatus( id:String ):int
		{
			var asset:Asset = getAssetByID( id );
			return (asset == null) ? -1 : asset.getStatus();
		}
		
		override public function hasAsset( id:String ):Boolean
		{
			return getAssetByID( id ) != null;
		}
		
	}

}