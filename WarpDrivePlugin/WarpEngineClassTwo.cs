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
	public class WarpEngineClassTwo : WarpEngine
	{
		#region "Constructors and Initializers"

		public WarpEngineClassTwo(CubeGridEntity parent)
			: base(parent)
		{
			if (parent != null && parent.GridSizeEnum != MyCubeSize.Large)
				throw new Exception("Cannot instantiate WarpEngine Class 2 on non-large ship!");
		}

		#endregion

		#region "Properties"

		public override float CoilEfficiency
		{
			get
			{
				return 0.85f;
			}
		}

		public override float EngineEfficiency
		{
			get
			{
				return 2.0f;
			}
		}

		public override string EngineName
		{
			get
			{
				return "Class II";
			}
		}

		public override float ParentPowerRequirement
		{
			get
			{
				return 500.0f;
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

			StructureEntry conveyorTube = new StructureEntry();
			conveyorTube.type = typeof(ConveyorTubeEntity);

			StructureEntry conveyorBlock = new StructureEntry();
			conveyorBlock.type = typeof(ConveyorBlockEntity);

			StructureEntry mergeBlock = new StructureEntry();
			mergeBlock.type = typeof(MergeBlockEntity);

			def.Add(new Vector3I(0, 0, 0), beaconCore);

			def.Add(new Vector3I(3, 1, 0), warpCoilBattery);
			def.Add(new Vector3I(-3, 1, 0), warpCoilBattery);
			def.Add(new Vector3I(0, 1, 3), warpCoilBattery);
			def.Add(new Vector3I(0, 1, -3), warpCoilBattery);

			//Lower lights
			def.Add(new Vector3I(1, 0, 1), coreLight);
			def.Add(new Vector3I(1, 0, -1), coreLight);
			def.Add(new Vector3I(-1, 0, 1), coreLight);
			def.Add(new Vector3I(-1, 0, -1), coreLight);

			//Upper lights
			def.Add(new Vector3I(0, 2, 1), coreLight);
			def.Add(new Vector3I(0, 2, 0), coreLight);
			def.Add(new Vector3I(0, 2, -1), coreLight);
			def.Add(new Vector3I(1, 2, 1), coreLight);
			def.Add(new Vector3I(1, 2, 0), coreLight);
			def.Add(new Vector3I(1, 2, -1), coreLight);
			def.Add(new Vector3I(-1, 2, 1), coreLight);
			def.Add(new Vector3I(-1, 2, 0), coreLight);
			def.Add(new Vector3I(-1, 2, -1), coreLight);

			return def;
		}

		#endregion
	}
}
