using System;
using System.IO;
using System.Collections.Generic;

namespace IConvertJson
{
	public static class Json
	{
		public static IEnumerable<char> ReadToEnd(this TextReader reader)
		{
			while(true)
			{
				var c	= reader.Read();
				if(c < 0)
					yield break;
				yield return (char)c;
			}
		}

		public static IConvertible ReadJson(this TextReader reader)
		{
			return JavaScriptReader.Read(reader);
		}

		public static string ToJson<T>(this T value) where T : IConvertible
		{
			return value.ToJson(null);
		}

		readonly static Dictionary<string,string> escapeReplace	= new Dictionary<string,string>() {
			{ "\\","\\\\" },{ "\"","\\\"" },{ "/","\\/" },{ "\x8","\\b" }
			,{ "\f","\\f" },{ "\r","\\r" },{ "\n","\\n" },{ "\t","\\t" },
		};

		public static string ToJson<T>(this T value,IFormatProvider format) where T : IConvertible
		{
			if(value == null)
				return "null";

			if(value is string)
			{
				string val	= value.ToString(format);
				foreach(var pair in escapeReplace)
					return val.Replace(pair.Key,pair.Value);

				return '"' + val + '"';
			}

			return value.ToString(format);
		}
	}
}