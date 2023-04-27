package frea.website.macros;

/**
 * ...
 * @author Yves Scherdin
 */
class CacheBusterGenerator
{
	public static macro function bustTheCache(infoPath:String)
	{
		var path:String = "bin/index.html";
		var pathJS:String = "bin/__JS_FILE_NAME__.js";
		var pathCSS:String = "bin/style/style.min.css";
		
		var lastComp = sys.FileSystem.exists(infoPath) ? sys.io.File.getContent(infoPath) : null;
		var datesOld:Array<String> = lastComp != null ? lastComp.split("|") : ["", ""];
		var datesNew:Array<String> = [
			 sys.FileSystem.exists(pathCSS) ? sys.FileSystem.stat(pathCSS).mtime.toString() : ""
			,sys.FileSystem.exists(pathJS)  ? sys.FileSystem.stat(pathJS ).mtime.toString() : ""
		];
		 
		if (sys.FileSystem.exists(path))
		{
			var content = sys.io.File.getContent(path);
			var changes:Bool = false;
			 
			inline function updateCacheBuster(startText:String):Void
			{
				var i:Int = content.indexOf(startText);
				var i2:Int = content.indexOf("\"", i);
				
				if (i != -1 && i2 != -1)
				{ 
					var ver:Int = Std.parseInt(content.substring(i + startText.length, i2)) + 1;
					content = content.substring(0, i + startText.length) + ver + content.substring(i2);
					changes = true;
				}
			}
			
			try
			{
				var updateCSS:Bool = datesOld[0] != datesNew[0];
				var updateJS :Bool = datesOld[1] != datesNew[1];
				
				if (updateCSS)	updateCacheBuster("css?v=");
				if (updateJS)	updateCacheBuster("js?v=" );
				
				if (changes)	sys.io.File.saveContent(path, content);
				
				sys.io.File.saveContent(infoPath, datesNew.join("|"));
			}
			catch (error:String)
			{
				// create position inside the json, FlashDevelop handles this very nice.
				var position = Std.parseInt(error.split("position").pop());
				var pos = haxe.macro.Context.makePosition({
						min:position,
						max:position + 1,
						file:path
				});
				haxe.macro.Context.error(path + " is not valid Json. " + error, pos);
			}
		}
		else
		{
			haxe.macro.Context.warning(path + " does not exist", haxe.macro.Context.currentPos());
		}
		
		return macro null;
	}
	
}