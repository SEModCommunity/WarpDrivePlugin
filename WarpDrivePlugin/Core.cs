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
using VRage;

namespace WarpDrivePlugin
{
	public class Core : PluginBase
	{
		#region "Attributes"

		private bool m_isActive;
		private FastResourceLock m_resourceLock;
		private Thread m_mainUpdateLoop;

		private static float m_baseFuel;
		private static float m_fuelRate;
		private static float m_duration;
		private static float m_speedFactor;
		private static float m_beaconRange;
		private static string m_beaconText;
		private static float m_speedThreshold;
		private static float m_warpDelay;

		protected Dictionary<CubeGridEntity, WarpEngine> m_warpEngineMap;

		#endregion

		#region "Constructors and Initializers"

		public Core()
		{
			m_warpEngineMap = new Dictionary<CubeGridEntity, WarpEngine>();

			m_isActive = false;
			m_resourceLock = new FastResourceLock();
			m_mainUpdateLoop = new Thread(MainUpdate);

			m_baseFuel = 25;
			m_fuelRate = 1;
			m_duration = 2;
			m_speedFactor = 100;
			m_beaconRange = 100;
			m_beaconText = "Warp Drive";
			m_speedThreshold = 15;
			m_warpDelay = 10;
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
			m_resourceLock.AcquireExclusive();

			m_warpEngineMap.Clear();
			m_isActive = true;

			m_resourceLock.ReleaseExclusive();

			Console.WriteLine("WarpDrivePlugin '" + Id.ToString() + "' initialized!");
		}

		public override void Update()
		{
			if (!m_mainUpdateLoop.IsAlive)
			{
				m_mainUpdateLoop.Start();
			}
		}

		public override void Shutdown()
		{
			m_mainUpdateLoop.Interrupt();

			m_resourceLock.AcquireExclusive();

			foreach (WarpEngine warpEngine in m_warpEngineMap.Values)
			{
				warpEngine.Dispose();
			}
			m_warpEngineMap.Clear();

			m_isActive = false;

			Console.WriteLine("WarpDrivePlugin '" + Id.ToString() + "' is shut down!");

			m_resourceLock.ReleaseExclusive();
		}

		#endregion

		protected void MainUpdate()
		{
			DateTime lastFullScan = DateTime.Now;
			DateTime lastMainLoop = DateTime.Now;
			TimeSpan timeSinceLastMainLoop = DateTime.Now - lastMainLoop;
			float averageMainLoopInterval = 0;
			float averageMainLoopTime = 0;
			DateTime lastProfilingMessage = DateTime.Now;
			TimeSpan timeSinceLastProfilingMessage = DateTime.Now - lastProfilingMessage;

			while (m_isActive)
			{
				try
				{
					DateTime mainLoopStart = DateTime.Now;

					m_resourceLock.AcquireExclusive();

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

					m_resourceLock.ReleaseExclusive();

					TimeSpan timeSinceLastFullScan = DateTime.Now - lastFullScan;
					if (timeSinceLastFullScan.TotalSeconds > 30)
					{
						lastFullScan = DateTime.Now;

						//Run cleanup
						CleanUpEngineMap();

						//Run a full scan
						FullScan();
					}

					//Performance profiling
					timeSinceLastMainLoop = DateTime.Now - lastMainLoop;
					lastMainLoop = DateTime.Now;
					TimeSpan mainLoopRunTime = DateTime.Now - mainLoopStart;
					averageMainLoopInterval = (averageMainLoopInterval + (float)timeSinceLastMainLoop.TotalMilliseconds) / 2;
					averageMainLoopTime = (averageMainLoopTime + (float)mainLoopRunTime.TotalMilliseconds) / 2;
					timeSinceLastProfilingMessage = DateTime.Now - lastProfilingMessage;
					if (timeSinceLastProfilingMessage.TotalSeconds > 10)
					{
						lastProfilingMessage = DateTime.Now;

						LogManager.APILog.WriteLine("WarpDrivePlugin - Average main loop interval: " + Math.Round(averageMainLoopInterval, 2).ToString() + "ms");
						LogManager.APILog.WriteLine("WarpDrivePlugin - Average main loop time: " + Math.Round(averageMainLoopTime, 2).ToString() + "ms");
					}

					//Pause between loops
					int nextSleepTime = Math.Min(500, Math.Max(100, 200 + (200 - (int)timeSinceLastMainLoop.TotalMilliseconds) / 2));
					Thread.Sleep(nextSleepTime);
				}
				catch (Exception ex)
				{
					LogManager.GameLog.WriteLine(ex);
					Thread.Sleep(5000);
				}
			}
		}

		protected void RemoveWarpEngine(CubeGridEntity cubeGrid)
		{
			if (!m_warpEngineMap.ContainsKey(cubeGrid))
				return;

			WarpEngine engine = m_warpEngineMap[cubeGrid];
			LogManager.APILog.WriteLineAndConsole("Removing warp engine on cube grid '" + engine.Parent.Name + "'");
			m_warpEngineMap.Remove(engine.Parent);

			engine.Dispose();
		}

		protected void CreateWarpEngine(CubeBlockEntity cubeBlock)
		{
			if (cubeBlock == null || cubeBlock.IsDisposed)
				return;

			CubeGridEntity cubeGrid = cubeBlock.Parent;

			if (m_warpEngineMap.ContainsKey(cubeGrid))
				return;

			WarpEngine warpEngine = new WarpEngine(cubeGrid);
			if (!warpEngine.IsDefinitionMatch(cubeBlock))
				return;
			warpEngine.LoadBlocksFromAnchor(cubeBlock.Min);
			if (warpEngine.Blocks.Count == 0)
			{
				LogManager.APILog.WriteLineAndConsole("Failed to create warp engine on cube grid '" + cubeGrid.Name + "' with anchor at " + ((Vector3I)cubeBlock.Min).ToString());
				return;
			}

			m_warpEngineMap.Add(cubeGrid, warpEngine);

			LogManager.APILog.WriteLineAndConsole("Created warp engine on cube grid '" + cubeGrid.Name + "' with anchor at " + ((Vector3I)cubeBlock.Min).ToString());
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
			m_resourceLock.AcquireExclusive();

			try
			{
				foreach (WarpEngine engine in m_warpEngineMap.Values)
				{
					if (CheckEngineForRemoval(engine))
					{
						RemoveWarpEngine(engine.Parent);
					}
				}
			}
			catch (Exception ex)
			{
				LogManager.GameLog.WriteLine(ex);
			}

			m_resourceLock.ReleaseExclusive();
		}

		protected void FullScan()
		{
			m_resourceLock.AcquireExclusive();

			if (SandboxGameAssemblyWrapper.IsDebugging)
				LogManager.APILog.WriteLine("WarpDrivePlugin - Scanning all cube grids for valid warp drives ...");

			foreach (CubeGridEntity cubeGrid in SectorObjectManager.Instance.GetTypedInternalData<CubeGridEntity>())
			{
				try
				{
					//Skip if not large cube grid
					if (cubeGrid.GridSizeEnum != MyCubeSize.Large)
						continue;

					//Skip if cube grid already has engine
					if (m_warpEngineMap.ContainsKey(cubeGrid))
						continue;

					//Skip if cube grid is still loading
					if (cubeGrid.IsLoading)
						continue;

					//Force a cube block refresh
					List<CubeBlockEntity> cubeBlocks = cubeGrid.CubeBlocks;

					//Scan cube grid for engines
					WarpEngine dummyEngine = new WarpEngine(null);
					List<Vector3I> matches = dummyEngine.GetDefinitionMatches(cubeGrid);
					if (matches.Count > 0)
					{
						CreateWarpEngine(dummyEngine.AnchorBlock);
					}
				}
				catch (Exception ex)
				{
					LogManager.GameLog.WriteLine(ex);
				}
			}

			m_resourceLock.ReleaseExclusive();
		}

		#endregion
	}
}
