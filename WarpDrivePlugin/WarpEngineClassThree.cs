using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;

using SEModAPIExtensions.API;

using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.Support;

using VRageMath;

namespace WarpDrivePlugin
{
	public class WarpEngineClassThree : WarpEngine
	{
		#region "Constructors and Initializers"

		public WarpEngineClassThree(CubeGridEntity parent)
			: base(parent)
		{
			if (parent != null && parent.GridSizeEnum != MyCubeSize.Large)
				throw new Exception("Cannot instantiate WarpEngine Class 3 on non-large ship!");
		}

		#endregion

		#region "Properties"

		public override float CoilEfficiency
		{
			get
			{
				return 1.25f;
			}
		}

		public override float EngineEfficiency
		{
			get
			{
				return 4.0f;
			}
		}

		public override string EngineName
		{
			get
			{
				return "Class III";
			}
		}

		public override float ParentPowerRequirement
		{
			get
			{
				return 1000.0f;
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

			StructureEntry refinery = new StructureEntry();
			refinery.type = typeof(RefineryEntity);
			refinery.useSubTypeName = true;
			refinery.subTypeName = "LargeRefinery";

			StructureEntry arcFurnace = new StructureEntry();
			arcFurnace.type = typeof(RefineryEntity);
			arcFurnace.useSubTypeName = true;
			arcFurnace.subTypeName = "Blast Furnace";

			try
			{
				def.Add(new Vector3I(0, 0, 0), beaconCore);

				def.Add(new Vector3I(0, -2, 4), warpCoilBattery);
				def.Add(new Vector3I(0, -1, 4), warpCoilBattery);
				def.Add(new Vector3I(0, 0, 4), warpCoilBattery);
				def.Add(new Vector3I(0, 1, 4), warpCoilBattery);
				def.Add(new Vector3I(0, -2, -4), warpCoilBattery);
				def.Add(new Vector3I(0, -1, -4), warpCoilBattery);
				def.Add(new Vector3I(0, 0, -4), warpCoilBattery);
				def.Add(new Vector3I(0, 1, -4), warpCoilBattery);
				def.Add(new Vector3I(4, -2, 0), warpCoilBattery);
				def.Add(new Vector3I(4, -1, 0), warpCoilBattery);
				def.Add(new Vector3I(4, 0, 0), warpCoilBattery);
				def.Add(new Vector3I(4, 1, 0), warpCoilBattery);
				def.Add(new Vector3I(-4, -2, 0), warpCoilBattery);
				def.Add(new Vector3I(-4, -1, 0), warpCoilBattery);
				def.Add(new Vector3I(-4, 0, 0), warpCoilBattery);
				def.Add(new Vector3I(-4, 1, 0), warpCoilBattery);
				
				def.Add(new Vector3I(1, -2, 1), coreLight);
				def.Add(new Vector3I(1, -2, 2), coreLight);
				def.Add(new Vector3I(1, -2, -1), coreLight);
				def.Add(new Vector3I(1, -2, -2), coreLight);
				def.Add(new Vector3I(2, -2, 1), coreLight);
				def.Add(new Vector3I(2, -2, 2), coreLight);
				def.Add(new Vector3I(2, -2, -1), coreLight);
				def.Add(new Vector3I(2, -2, -2), coreLight);
				def.Add(new Vector3I(-1, -2, 1), coreLight);
				def.Add(new Vector3I(-1, -2, 2), coreLight);
				def.Add(new Vector3I(-1, -2, -1), coreLight);
				def.Add(new Vector3I(-1, -2, -2), coreLight);
				def.Add(new Vector3I(-2, -2, 1), coreLight);
				def.Add(new Vector3I(-2, -2, 2), coreLight);
				def.Add(new Vector3I(-2, -2, -1), coreLight);
				def.Add(new Vector3I(-2, -2, -2), coreLight);
				
				def.Add(new Vector3I(1, -2, -5), refinery);
				def.Add(new Vector3I(-2, -2, -5), refinery);
				def.Add(new Vector3I(-5, -2, 1), refinery);
				def.Add(new Vector3I(-5, -2, -2), refinery);
				def.Add(new Vector3I(1, -2, 4), refinery);
				def.Add(new Vector3I(-2, -2, 4), refinery);
				def.Add(new Vector3I(4, -2, 1), refinery);
				def.Add(new Vector3I(4, -2, -2), refinery);
				
				def.Add(new Vector3I(0, -2, 2), arcFurnace);
				def.Add(new Vector3I(0, -2, -3), arcFurnace);
				def.Add(new Vector3I(0, -1, 1), arcFurnace);
				def.Add(new Vector3I(0, -1, -2), arcFurnace);
				def.Add(new Vector3I(2, -2, 0), arcFurnace);
				def.Add(new Vector3I(-3, -2, 0), arcFurnace);
				def.Add(new Vector3I(1, -1, 0), arcFurnace);
				def.Add(new Vector3I(-2, -1, 0), arcFurnace);
				
				/*
				def.Add(new Vector3I(-1, -2, 0), mergeBlock);
				def.Add(new Vector3I(0, -2, 1), mergeBlock);
				def.Add(new Vector3I(0, -2, -1), mergeBlock);
				def.Add(new Vector3I(1, -2, 0), mergeBlock);
				def.Add(new Vector3I(-1, 0, 0), mergeBlock);
				def.Add(new Vector3I(0, 0, 1), mergeBlock);
				def.Add(new Vector3I(0, 0, -1), mergeBlock);
				def.Add(new Vector3I(1, 0, 0), mergeBlock);
				*/
			}
			catch (Exception ex)
			{
				LogManager.ErrorLog.WriteLine(ex);
			}
			return def;
		}

		#endregion
	}
}
