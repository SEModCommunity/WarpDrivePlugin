using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;

using SEModAPIExtensions.API;

using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;

using VRageMath;

namespace WarpDrivePlugin
{
	public class WarpEngineClassZero : WarpEngine
	{
		#region "Constructors and Initializers"

		public WarpEngineClassZero(CubeGridEntity parent)
			: base(parent)
		{
			if (parent != null && parent.GridSizeEnum != MyCubeSize.Small)
				throw new Exception("Cannot instantiate WarpEngine Class 0 on non-small ship!");
		}

		#endregion

		#region "Properties"

		public override float CoilEfficiency
		{
			get
			{
				return 0.20f;
			}
		}

		public override float EngineEfficiency
		{
			get
			{
				return 0.25f;
			}
		}

		public override string EngineName
		{
			get
			{
				return "Class 0";
			}
		}

		public override float ParentPowerRequirement
		{
			get
			{
				return 7.0f;
			}
		}

		#endregion

		#region "Methods"

		protected override Dictionary<Vector3I, StructureEntry> WarpEngineDefinition()
		{
			Dictionary<Vector3I, StructureEntry> def = new Dictionary<Vector3I, StructureEntry>();
			if (IsDisposed)
				return def;

			StructureEntry beaconCore = new StructureEntry();
			beaconCore.type = typeof(BeaconEntity);

			StructureEntry warpCoilBattery = new StructureEntry();
			warpCoilBattery.type = typeof(BatteryBlockEntity);

			StructureEntry coreLight = new StructureEntry();
			coreLight.type = typeof(ReflectorLightEntity);

			def.Add(new Vector3I(0, 0, 0), beaconCore);

			def.Add(new Vector3I(1, 0, 0), warpCoilBattery);
			def.Add(new Vector3I(-1, 0, 0), warpCoilBattery);
			def.Add(new Vector3I(0, 0, 1), warpCoilBattery);
			def.Add(new Vector3I(0, 0, -1), warpCoilBattery);

			def.Add(new Vector3I(1, 0, 1), coreLight);
			def.Add(new Vector3I(1, 0, -1), coreLight);
			def.Add(new Vector3I(-1, 0, 1), coreLight);
			def.Add(new Vector3I(-1, 0, -1), coreLight);

			return def;
		}

		#endregion
	}
}
