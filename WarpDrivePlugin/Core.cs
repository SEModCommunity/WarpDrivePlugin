using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Timers;

using Sandbox.Common.ObjectBuilders;

using SEModAPIExtensions.API.Plugin;
using SEModAPIExtensions.API.Plugin.Events;

using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.Support;

using VRageMath;

namespace WarpDrivePlugin
{
	public class Core : PluginBase, ICubeBlockEventHandler
	{
		#region "Attributes"

		private static float m_baseFuel;
		private static float m_fuelRate;
		private static float m_duration;
		private static float m_speedFactor;

		protected Dictionary<CubeGridEntity, WarpEngine> m_warpEngineMap;

		#endregion

		#region "Constructors and Initializers"

		public Core()
		{
			m_warpEngineMap = new Dictionary<CubeGridEntity, WarpEngine>();

			m_baseFuel = 25;
			m_fuelRate = 1;
			m_duration = 2;
			m_speedFactor = 100;

			Console.WriteLine("WarpDrivePlugin '" + Id.ToString() + "' constructed!");
		}

		public override void Init()
		{
			Console.WriteLine("WarpDrivePlugin '" + Id.ToString() + "' initialized!");
		}

		#endregion

		#region "Properties"

		internal static float _BaseFuel
		{
			get { return m_baseFuel; }
			set { m_baseFuel = value; }
		}

		internal static float _FuelRate
		{
			get { return m_fuelRate; }
			set { m_fuelRate = value; }
		}

		internal static float _Duration
		{
			get { return m_duration; }
			set { m_duration = value; }
		}

		internal static float _SpeedFactor
		{
			get { return m_speedFactor; }
			set { m_speedFactor = value; }
		}

		[Category("Warp Drive Plugin")]
		[Browsable(true)]
		[ReadOnly(false)]
		public float BaseFuel
		{
			get { return m_baseFuel; }
			set { m_baseFuel = value; }
		}

		[Category("Warp Drive Plugin")]
		[Browsable(true)]
		[ReadOnly(false)]
		public float FuelRate
		{
			get { return m_fuelRate; }
			set { m_fuelRate = value; }
		}

		[Category("Warp Drive Plugin")]
		[Browsable(true)]
		[ReadOnly(false)]
		public float Duration
		{
			get { return m_duration; }
			set { m_duration = value; }
		}

		[Category("Warp Drive Plugin")]
		[Browsable(true)]
		[ReadOnly(false)]
		public float SpeedFactor
		{
			get { return m_speedFactor; }
			set { m_speedFactor = value; }
		}

		#endregion

		#region "Methods"

		#region "EventHandlers"

		public override void Update()
		{
			foreach (WarpEngine warpEngine in m_warpEngineMap.Values)
			{
				warpEngine.Update();

				//Check if ship is going fast enough to warp
				Vector3 velocity = warpEngine.Parent.LinearVelocity;
				float speed = velocity.Length();
				if (!warpEngine.IsWarping && speed > 100 && speed < 0.95 * m_speedFactor * 100)
				{
					if (warpEngine.CanWarp)
						warpEngine.StartWarp();
				}
			}
		}

		public void OnCubeBlockCreated(CubeBlockEntity cubeBlock)
		{
			if (cubeBlock.Parent.GridSizeEnum != MyCubeSize.Large)
				return;

			WarpEngine dummyEngine = new WarpEngine(null);
			List<Vector3I> matches = dummyEngine.GetDefinitionMatches(cubeBlock.Parent);

			if (matches.Count > 0)
			{
				ProcessWarpEngineMatches(cubeBlock.Parent, matches[0]);
			}
		}

		public void OnCubeBlockDeleted(CubeBlockEntity cubeBlock)
		{
			if (cubeBlock.Parent.GridSizeEnum != MyCubeSize.Large)
				return;

			CleanUpEngineMap(cubeBlock);
		}

		#endregion

		protected void ProcessWarpEngineMatches(CubeGridEntity cubeGrid, Vector3I match)
		{
			if (m_warpEngineMap.ContainsKey(cubeGrid))
				return;

			WarpEngine warpEngine = new WarpEngine(cubeGrid);
			warpEngine.LoadBlocksFromAnchor(match);
			m_warpEngineMap.Add(cubeGrid, warpEngine);
		}

		protected void CleanUpEngineMap(CubeBlockEntity deletedCubeBlock)
		{
			CubeGridEntity parent = deletedCubeBlock.Parent;

			if (!m_warpEngineMap.ContainsKey(parent))
				return;

			WarpEngine warpEngine = m_warpEngineMap[parent];
			bool shouldRemove = false;

			foreach (CubeBlockEntity cubeBlock in warpEngine.Blocks)
			{
				if (cubeBlock == deletedCubeBlock || cubeBlock.IsDisposed)
				{
					shouldRemove = true;
					break;
				}
			}

			if(shouldRemove || parent.IsDisposed)
				m_warpEngineMap.Remove(parent);
		}

		#endregion
	}
}
