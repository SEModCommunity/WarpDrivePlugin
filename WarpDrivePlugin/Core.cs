using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;

using SEModAPIExtensions.API.Plugin;
using SEModAPIExtensions.API.Plugin.Events;

using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;

using VRageMath;

namespace WarpDrivePlugin
{
	public class Core : PluginBase, ICubeGridHandler, IBaseEntityHandler
	{
		#region "Attributes"

		private DateTime m_lastMovementTime;
		private TimeSpan m_timeSinceLastMovement;

		#endregion

		#region "Constructors and Initializers"

		public Core()
		{
			m_timeSinceLastMovement = new TimeSpan();

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
		}

		public void OnCubeGridMoved(CubeGridEntity cubeGrid)
		{
		}

		public void OnCubeGridCreated(CubeGridEntity cubeGrid)
		{
		}

		public void OnCubeGridDeleted(CubeGridEntity cubeGrid)
		{
		}

		public void OnBaseEntityMoved(BaseEntity entity)
		{
			if (entity.GetType() == typeof(CubeGridEntity))
			{
				CubeGridEntity cubeGrid = (CubeGridEntity)entity;

				Vector3 velocity = cubeGrid.LinearVelocity;
				float speed = velocity.Length();

				if (speed > 1)
				{
					if (speed > 100)
					{
						//if(SandboxGameAssemblyWrapper.IsDebugging)
							//Console.WriteLine("WarpDrivePlugin - Ship '" + cubeGrid.Name + "' is moving very fast!");

						m_timeSinceLastMovement = DateTime.Now - m_lastMovementTime;

						List<CubeBlockEntity> cubeBlocks = cubeGrid.CubeBlocks;
						if (cubeGrid.GridSizeEnum == MyCubeSize.Large)
						{
							//Search for a warp reactor on the ship
							ReactorEntity warpReactor = null;
							foreach (CubeBlockEntity cubeBlock in cubeBlocks)
							{
								if (cubeBlock.GetType() == typeof(ReactorEntity))
								{
									ReactorEntity reactor = (ReactorEntity)cubeBlock;
									if (reactor.Name == "WarpEngine")
									{
										warpReactor = reactor;
										break;
									}
								}
							}

							//If we found a warp reactor then run the warp procedure
							if (warpReactor != null)
							{
								WarpEngine engine = new WarpEngine(warpReactor);
								bool result = engine.Warp();
							}
						}
					}
					else
					{
						//if (SandboxGameAssemblyWrapper.IsDebugging)
							//Console.WriteLine("WarpDrivePlugin - Ship '" + cubeGrid.Name + "' is moving!");
					}
				}
			}

			m_lastMovementTime = DateTime.Now;
		}

		#endregion
	}
}
