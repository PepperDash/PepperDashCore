using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PepperDash.Core.JsonToSimpl
{
	public class JsonToSimplArrayLookupChild : JsonToSimplChildObjectBase
	{
		public string SearchPropertyName { get; set; }
		public string SearchPropertyValue { get; set; }

		int ArrayIndex;

		public void Initialize(string file, string key, string pathPrefix, string pathSuffix,
			string searchPropertyName, string searchPropertyValue)
		{
			base.Initialize(file, key, pathPrefix, pathSuffix);
			SearchPropertyName = searchPropertyName;
			SearchPropertyValue = searchPropertyValue;
		}


		//PathPrefix+ArrayName+[x]+path+PathSuffix
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		protected override string GetFullPath(string path)
		{
			return string.Format("{0}[{1}].{2}{3}", 
				PathPrefix == null ? "" : PathPrefix, 
				ArrayIndex, path, 
				PathSuffix == null ? "" : PathSuffix);
		}

		public override void ProcessAll()
		{
			if(FindInArray())
				base.ProcessAll();
		}

		bool FindInArray()
		{
			if (Master == null)
				throw new InvalidOperationException("Cannot do operations before master is linked");
			if (Master.JsonObject == null)
				throw new InvalidOperationException("Cannot do operations before master JSON has read");
			if (PathPrefix == null)
				throw new InvalidOperationException("Cannot do operations before PathPrefix is set");

			var token = Master.JsonObject.SelectToken(PathPrefix);
			if (token is JArray)
			{
				var array = token as JArray;
				try
				{
					var item = array.FirstOrDefault(o => 
					{
						var prop = o[SearchPropertyName];
						return prop != null && prop.Value<string>()
						.Equals(SearchPropertyValue, StringComparison.OrdinalIgnoreCase);
					});
					if (item == null)
					{
						Debug.Console(0, "Child[{0}] Array '{1}' '{2}={3}' not found: ", Key,
							PathPrefix, SearchPropertyName, SearchPropertyValue);
						return false;
					}

					ArrayIndex = array.IndexOf(item);
					Debug.Console(0, "Child[{0}] Found array match at index {1}", Key, ArrayIndex);
					return true;
				}
				catch (Exception e)
				{
					Debug.Console(0, "Child[{0}] Array '{1}' lookup error: '{2}={3}'\r{4}", Key, 
						PathPrefix, SearchPropertyName, SearchPropertyValue, e);
				}
			}
			else
				Debug.Console(0, "Child[{0}] Path '{1}' is not an array", Key, PathPrefix);

			return false;
		}
	}
}