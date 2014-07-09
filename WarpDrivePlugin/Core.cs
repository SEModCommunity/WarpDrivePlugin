using System;
using System.Collections.Generic;
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
	public class Core : PluginBase, ICubeGridHandler, IBaseEntityHandler
	{
		#region "Attributes"

		private static float m_baseFuel;
		private static float m_fuelRate;
		private static float m_duration;
		private static float m_speedFactor;

		protected Dictionary<ReactorEntity, WarpEngine> m_warpEngineMap;
		protected Timer m_warpEngineMapCleanupTimer;

		#endregion

		#region "Constructors and Initializers"

		public Core()
		{
			m_warpEngineMap = new Dictionary<ReactorEntity, WarpEngine>();

			m_warpEngineMapCleanupTimer = new Timer();
			m_warpEngineMapCleanupTimer.Interval = 5000;
			m_warpEngineMapCleanupTimer.Elapsed += this.CleanUpEngineMap;

			m_baseFuel = 25;
			m_fuelRate = 1;
			m_duration = 2;
			m_speedFactor = 100;

			Console.WriteLine("WarpDrivePlugin '" + Id.ToString() + "' constructed!");
		}

		public override void Init()
		{
			m_warpEngineMapCleanupTimer.Start();

			Console.WriteLine("WarpDrivePlugin '" + Id.ToString() + "' initialized!");
		}

		#endregion

		#region "Properties"

		public static float BaseFuel
		{
			get { return m_baseFuel; }
			set { m_baseFuel = value; }
		}

		public static float FuelRate
		{
			get { return m_fuelRate; }
			set { m_fuelRate = value; }
		}

		public static float Duration
		{
			get { return m_duration; }
			set { m_duration = value; }
		}

		public static float SpeedFactor
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
			}
		}

		public void OnCubeGridMoved(CubeGridEntity cubeGrid)
		{
		}

		public void OnCubeGridCreated(CubeGridEntity cubeGrid)
		{
		}

		public void OnCubeGridDeleted(CubeGridEntity cubeGrid)
		{
			foreach (ReactorEntity key in m_warpEngineMap.Keys)
			{
				if (key.Parent == cubeGrid)
					m_warpEngineMap.Remove(key);
			}
		}

		public void OnBaseEntityMoved(BaseEntity entity)
		{
			if (entity.GetType() == typeof(CubeGridEntity))
			{
				CubeGridEntity cubeGrid = (CubeGridEntity)entity;

				Vector3 velocity = cubeGrid.LinearVelocity;
				float speed = velocity.Length();

				//Ship is at max speed but less than warp speed
				if (speed > 100 && speed < 10000)
				{
					if (cubeGrid.GridSizeEnum == MyCubeSize.Large)
					{
						//Get the list of reactors from the ship
						List<ReactorEntity> reactors = GetCubeGridReactors(cubeGrid);
						if (reactors.Count == 0)
							return;

						//Search for a warp reactor on the ship
						ReactorEntity warpReactor = GetWarpReactor(reactors);
						if (warpReactor == null)
							return;

						ActivateWarpReator(warpReactor);
					}
				}
			}
		}

		public void OnBaseEntityCreated(BaseEntity entity)
		{
		}

		public void OnBaseEntityDeleted(BaseEntity entity)
		{
		}

		#endregion

		protected List<ReactorEntity> GetCubeGridReactors(CubeGridEntity cubeGrid)
		{
			List<CubeBlockEntity> cubeBlocks = cubeGrid.CubeBlocks;

			List<ReactorEntity> reactors = new List<ReactorEntity>();
			foreach (CubeBlockEntity cubeBlock in cubeBlocks)
			{
				if (cubeBlock.GetType() == typeof(ReactorEntity))
				{
					ReactorEntity reactor = (ReactorEntity)cubeBlock;
					reactors.Add(reactor);
				}
			}

			return reactors;
		}

		protected ReactorEntity GetWarpReactor(List<ReactorEntity> reactors)
		{
			//Search for a warp reactor on the ship
			ReactorEntity warpReactor = null;
			foreach (ReactorEntity reactor in reactors)
			{
				if (reactor.Name == "WarpEngine")
				{
					warpReactor = reactor;
					break;
				}
			}

			return warpReactor;
		}

		protected void ActivateWarpReator(ReactorEntity reactor)
		{
			try
			{
				//Do some error checking
				if (reactor == null)
					return;
				if (reactor.Parent == null)
					return;
				if (reactor.Parent.IsDisposed)
					return;

				//Get/Create the warp engine from the reactor
				WarpEngine engine = null;
				if (m_warpEngineMap.ContainsKey(reactor))
				{
					engine = m_warpEngineMap[reactor];
				}
				if (engine == null)
				{
					engine = new WarpEngine(reactor);
					m_warpEngineMap.Add(reactor, engine);
				}

				//Check if the engine is capable of warping
				if (!engine.CanWarp)
					return;

				//Run the warp procedure
				engine.StartWarp();
			}
			catch (Exception ex)
			{
				LogManager.GameLog.WriteLine(ex);
			}
		}

		protected void CleanUpEngineMap(Object source, ElapsedEventArgs e)
		{
			foreach (ReactorEntity key in m_warpEngineMap.Keys)
			{
				if (key.Parent == null || key.Parent.IsDisposed)
					m_warpEngineMap.Remove(key);
			}
		}

		#endregion
	}
}
