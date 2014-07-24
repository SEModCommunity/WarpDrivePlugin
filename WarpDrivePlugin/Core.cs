using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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

		private bool m_isActive;
		private bool m_isEngineMapLocked;

		private static float m_baseFuel;
		private static float m_fuelRate;
		private static float m_duration;
		private static float m_speedFactor;
		private static float m_beaconRange;
		private static string m_beaconText;
		private static float m_speedThreshold;
		private static float m_warpDelay;

		protected Dictionary<CubeGridEntity, WarpEngine> m_warpEngineMap;
		protected DateTime m_lastFullScan;
		protected DateTime m_lastCleanUp;

		#endregion

		#region "Constructors and Initializers"

		public Core()
		{
			m_warpEngineMap = new Dictionary<CubeGridEntity, WarpEngine>();

			m_isActive = false;
			m_isEngineMapLocked = false;

			m_baseFuel = 25;
			m_fuelRate = 1;
			m_duration = 2;
			m_speedFactor = 100;
			m_beaconRange = 100;
			m_beaconText = "Warp Drive";
			m_speedThreshold = 15;
			m_warpDelay = 10;

			m_lastFullScan = DateTime.Now;
			m_lastCleanUp = DateTime.Now;

			Console.WriteLine("WarpDrivePlugin '" + Id.ToString() + "' constructed!");
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

		internal static float _BeaconRange
		{
			get { return m_beaconRange; }
			set { m_beaconRange = value; }
		}

		internal static string _BeaconText
		{
			get { return m_beaconText; }
			set { m_beaconText = value; }
		}

		internal static float _SpeedThreshold
		{
			get { return m_speedThreshold; }
			set { m_speedThreshold = value; }
		}

		internal static float _WarpDelay
		{
			get { return m_warpDelay; }
			set { m_warpDelay = value; }
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

		[Category("Warp Drive Plugin")]
		[Browsable(true)]
		[ReadOnly(false)]
		public float BeaconRange
		{
			get { return m_beaconRange; }
			set { m_beaconRange = value; }
		}

		[Category("Warp Drive Plugin")]
		[Browsable(true)]
		[ReadOnly(false)]
		public string BeaconText
		{
			get { return m_beaconText; }
			set { m_beaconText = value; }
		}

		[Category("Warp Drive Plugin")]
		[Browsable(true)]
		[ReadOnly(false)]
		public float SpeedThreshold
		{
			get { return m_speedThreshold; }
			set { m_speedThreshold = value; }
		}

		[Category("Warp Drive Plugin")]
		[Browsable(true)]
		[ReadOnly(false)]
		public float WarpDelay
		{
			get { return m_warpDelay; }
			set { m_warpDelay = value; }
		}

		[Category("Warp Drive Plugin")]
		[Browsable(true)]
		[ReadOnly(true)]
		public List<string> WarpEngines
		{
			get
			{
				List<string> cubeGridNameList = new List<string>();
				foreach (var key in m_warpEngineMap.Keys)
				{
					cubeGridNameList.Add(key.Name);
				}

				return cubeGridNameList;
			}
		}

		#endregion

		#region "Methods"

		#region "EventHandlers"

		public override void Init()
		{
			m_warpEngineMap.Clear();
			m_lastFullScan = DateTime.Now;
			m_lastCleanUp = DateTime.Now;
			m_isActive = true;

			Console.WriteLine("WarpDrivePlugin '" + Id.ToString() + "' initialized!");
		}

		public override void Update()
		{
			if (!m_isActive)
				return;

			AcquireMapLock();
			if (!m_isEngineMapLocked)
				return;

			foreach (WarpEngine warpEngine in m_warpEngineMap.Values)
			{
				warpEngine.Update();

				//Check if ship is going fast enough to warp
				Vector3 velocity = warpEngine.Parent.LinearVelocity;
				float speed = velocity.Length();
				if (!warpEngine.IsWarping && speed > m_speedThreshold && speed < 0.95 * m_speedFactor * 100)
				{
					if (warpEngine.CanWarp)
						warpEngine.StartWarp();
				}
			}
			ReleaseMapLock();

			TimeSpan timeSinceLastFullScan = DateTime.Now - m_lastFullScan;
			if (timeSinceLastFullScan.TotalMilliseconds > 30000)
			{
				m_lastFullScan = DateTime.Now;

				//Run cleanup
				CleanUpEngineMap();

				//Queue up a full scan
				Action action = FullScan;
				SandboxGameAssemblyWrapper.Instance.EnqueueMainGameAction(action);
			}

		}

		public override void Shutdown()
		{
			AcquireMapLock();

			foreach (WarpEngine warpEngine in m_warpEngineMap.Values)
			{
				warpEngine.Dispose();
			}
			m_warpEngineMap.Clear();

			m_isActive = false;

			Console.WriteLine("WarpDrivePlugin '" + Id.ToString() + "' is shut down!");

			ReleaseMapLock();
		}

		public void OnCubeBlockCreated(CubeBlockEntity cubeBlock)
		{
			if (!m_isActive)
				return;

			if (cubeBlock == null || cubeBlock.IsDisposed)
				return;

			CubeGridEntity cubeGrid = cubeBlock.Parent;

			//Do some data validating
			if (cubeGrid == null)
				return;
			if (cubeGrid.IsDisposed)
				return;
			if (cubeGrid.GridSizeEnum != MyCubeSize.Large)
				return;

			//Check if the map already has an entry for this cube grid
			if (m_warpEngineMap.ContainsKey(cubeGrid))
			{
				CleanUpEngineMap();

				if (m_warpEngineMap.ContainsKey(cubeGrid))
				{
					ReleaseMapLock();
					return;
				}
			}

			//Search for multiblock matches
			WarpEngine dummyEngine = new WarpEngine(null);
			if (dummyEngine.IsDefinitionMatch(cubeBlock))
			{
				CreateWarpEngine(cubeBlock);
			}
		}

		public void OnCubeBlockDeleted(CubeBlockEntity cubeBlock)
		{
			if (!m_isActive)
				return;

			if (cubeBlock == null)
				return;

			CleanUpEngineMap();
		}

		#endregion

		private void AcquireMapLock()
		{
			DateTime timeOfRequest = DateTime.Now;
			while (m_isEngineMapLocked)
			{
				Thread.Sleep(15);

				TimeSpan timeSinceRequest = DateTime.Now - timeOfRequest;
				if (timeSinceRequest.TotalMilliseconds > 100)
					return;
			}

			m_isEngineMapLocked = true;
		}

		private void ReleaseMapLock()
		{
			m_isEngineMapLocked = false;
		}

		protected void RemoveWarpEngine(CubeGridEntity cubeGrid)
		{
			AcquireMapLock();

			if (!m_warpEngineMap.ContainsKey(cubeGrid))
				return;

			WarpEngine engine = m_warpEngineMap[cubeGrid];
			LogManager.APILog.WriteLineAndConsole("Removing warp engine on cube grid '" + engine.Parent.Name + "'");
			m_warpEngineMap.Remove(engine.Parent);

			engine.Dispose();

			ReleaseMapLock();
		}

		protected void CreateWarpEngine(CubeBlockEntity cubeBlock)
		{
			AcquireMapLock();

			CleanUpEngineMap();

			CubeGridEntity cubeGrid = cubeBlock.Parent;

			WarpEngine warpEngine = new WarpEngine(cubeGrid);
			warpEngine.LoadBlocksFromAnchor(cubeBlock.Min);
			if (warpEngine.Blocks.Count == 0)
			{
				ReleaseMapLock();
				//LogManager.APILog.WriteLineAndConsole("Failed to create warp engine on cube grid '" + cubeGrid.Name + "'");
				return;
			}

			m_warpEngineMap.Add(cubeGrid, warpEngine);

			LogManager.APILog.WriteLineAndConsole("Created warp engine on cube grid '" + cubeGrid.Name + "'");

			ReleaseMapLock();
		}

		protected bool CheckEngineForRemoval(WarpEngine engine)
		{
			bool shouldBeRemoved = false;
			List<CubeBlockEntity> blocks = engine.Blocks;
			foreach (var block in blocks)
			{
				if (block == null || block.IsDisposed || block.Parent == null || block.Parent.IsDisposed)
				{
					shouldBeRemoved = true;
					break;
				}
			}

			return shouldBeRemoved;
		}

		protected void CleanUpEngineMap()
		{
			try
			{
				//Allow cleanup to only run once every 10 seconds
				TimeSpan timeSinceLastCleanup = DateTime.Now - m_lastCleanUp;
				if (timeSinceLastCleanup.TotalSeconds < 10)
					return;
				m_lastCleanUp = DateTime.Now;

				AcquireMapLock();
				foreach (WarpEngine engine in m_warpEngineMap.Values)
				{
					if (CheckEngineForRemoval(engine))
					{
						RemoveWarpEngine(engine.Parent);
					}
				}
				ReleaseMapLock();
			}
			catch (Exception ex)
			{
				LogManager.GameLog.WriteLine(ex);
			}
		}

		protected void FullScan()
		{
			AcquireMapLock();

			if(SandboxGameAssemblyWrapper.IsDebugging)
				LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Scanning all entities for valid warp drives ...");
			else
				LogManager.APILog.WriteLine("WarpDrivePlugin - Scanning all entities for valid warp drives ...");

			foreach (BaseEntity entity in SectorObjectManager.Instance.GetTypedInternalData<BaseEntity>())
			{
				try
				{
					//Skip if not cube grid
					if (!(entity is CubeGridEntity))
						continue;

					CubeGridEntity cubeGrid = (CubeGridEntity)entity;

					//Skip if not large cube grid
					if (cubeGrid.GridSizeEnum != MyCubeSize.Large)
						continue;

					//Skip if cube grid already has engine
					if (m_warpEngineMap.ContainsKey(cubeGrid))
						continue;

					//Force a cube block refresh
					List<CubeBlockEntity> cubeBlocks = cubeGrid.CubeBlocks;

					//Scan cube grid for engines
					WarpEngine dummyEngine = new WarpEngine(null);
					List<Vector3I> matches = dummyEngine.GetDefinitionMatches(cubeGrid);
					if (matches.Count > 0)
					{
						//If there was a match, create a new engine
						CubeBlockEntity cubeBlock = cubeGrid.GetCubeBlock(matches[0]);
						CreateWarpEngine(cubeBlock);
					}
				}
				catch (Exception ex)
				{
					LogManager.GameLog.WriteLine(ex);
				}
			}

			ReleaseMapLock();
		}

		#endregion
	}
}
