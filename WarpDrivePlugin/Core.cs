using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SEModAPIExtensions.API.Plugin.Events;

using SEModAPIInternal.API.Entity.Sector.SectorObject;

namespace SEModAPIExtensions.API.Plugin
{
	public class Core : PluginBase
	{
		#region "Constructors and Initializers"

		public Core()
		{
			Console.WriteLine("WarpDrivePlugin '" + Id.ToString() + "' constructed!");
		}

		public override void Init()
		{
			Console.WriteLine("WarpDrivePlugin '" + Id.ToString() + "' initialized!");
		}

		#endregion

		#region "Methods"

		public override void Update()
		{
			//Console.WriteLine("WarpDrivePlugin '" + Id.ToString() + "' updated!");
		}

		#endregion
	}
}
