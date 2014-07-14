using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

using SEModAPIExtensions.API;

using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.Support;

using VRageMath;

namespace WarpDrivePlugin
{
	public class WarpEngine : MultiblockStructure, IDisposable
	{
		#region "Attributes"

		private bool m_isStartingWarp;
		private bool m_isWarping;
		private bool m_isAtWarpSpeed;
		private bool m_isSpeedingUp;
		private bool m_isSlowingDown;
		private bool m_isDisposed;

		private float m_warpFuelRequired;
		private float m_accelerationFactor;

		private string m_oldBeaconName;
		private float m_oldBeaconBroadcastRadius;

		private DateTime m_lastUpdate;
		private DateTime m_warpRequest;
		private DateTime m_warpStart;
		private TimeSpan m_timeSinceLastUpdate;
		private TimeSpan m_timeSinceWarpRequest;
		private TimeSpan m_timeSinceWarpStart;

		#endregion

		#region "Constructors and Initializers"

		public WarpEngine(CubeGridEntity parent)
			: base(parent)
		{
			m_warpFuelRequired = Core._BaseFuel;

			m_lastUpdate = DateTime.Now;

			m_isStartingWarp = false;
			m_isWarping = false;
			m_isAtWarpSpeed = false;
			m_isSpeedingUp = false;
			m_isSlowingDown = false;
			m_isDisposed = false;

			m_accelerationFactor = 2;

			if (parent != null)
				m_oldBeaconName = parent.Name;
			else
				m_oldBeaconName = "";
			m_oldBeaconBroadcastRadius = 10000;
		}

		#endregion

		#region "Properties"

		public bool IsWarping
		{
			get { return m_isWarping; }
		}

		public float WarpFuel
		{
			get
			{
				if (Parent.Mass > 0)
					m_warpFuelRequired = Core._BaseFuel + Core._FuelRate * (Parent.Mass / 100000);
				else
					m_warpFuelRequired = Core._BaseFuel;

				return m_warpFuelRequired;
			}
		}

		public float FuelLevel
		{
			get
			{
				float fuelCount = 0;

				foreach (InventoryItemEntity item in FuelItems)
				{
					fuelCount += item.Amount;
				}

				return fuelCount;
			}
		}

		public bool CanWarp
		{
			get
			{
				if (IsWarping)		//Already warping
					return false;
				if (!IsFunctional)	//Reactors are off or other damage has been done to the warp engine
					return false;
				if (FuelLevel < WarpFuel)	//Not enough fuel
					return false;

				return true;
			}
		}

		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		protected BeaconEntity Beacon
		{
			get
			{
				BeaconEntity beaconCore = null;

				foreach (CubeBlockEntity cubeBlock in Blocks)
				{
					if (cubeBlock is BeaconEntity)
					{
						beaconCore = (BeaconEntity)cubeBlock;
						break;
					}
				}

				return beaconCore;
			}
		}

		protected List<ReactorEntity> Reactors
		{
			get
			{
				List<ReactorEntity> reactors = new List<ReactorEntity>();
				foreach (CubeBlockEntity cubeBlock in Blocks)
				{
					if (cubeBlock is ReactorEntity)
						reactors.Add((ReactorEntity)cubeBlock);
				}

				return reactors;
			}
		}

		protected List<InventoryItemEntity> FuelItems
		{
			get
			{
				List<InventoryItemEntity> fuelItems = new List<InventoryItemEntity>();
				foreach (ReactorEntity reactor in Reactors)
				{
					fuelItems.AddRange(reactor.Inventory.Items);
				}

				return fuelItems;
			}
		}

		#endregion

		#region "Methods"

		public void Dispose()
		{
			m_isDisposed = true;
		}

		public override Dictionary<Vector3I, Type> GetMultiblockDefinition()
		{
			Dictionary<Vector3I, Type> def = new Dictionary<Vector3I, Type>();
			if (IsDisposed)
				return def;

			def.Add(new Vector3I(0, 0, 0), typeof(ReactorEntity));
			def.Add(new Vector3I(1, 0, 1), typeof(ReactorEntity));
			def.Add(new Vector3I(-1, 0, 1), typeof(ReactorEntity));
			def.Add(new Vector3I(0, 0, 2), typeof(ReactorEntity));
			def.Add(new Vector3I(0, 2, 0), typeof(ReactorEntity));
			def.Add(new Vector3I(1, 2, 1), typeof(ReactorEntity));
			def.Add(new Vector3I(-1, 2, 1), typeof(ReactorEntity));
			def.Add(new Vector3I(0, 2, 2), typeof(ReactorEntity));

			//def.Add(new Vector3I(0, 1, 0), typeof(CubeBlockEntity));	//ConveyorTubeEntity
			//def.Add(new Vector3I(1, 1, 1), typeof(CubeBlockEntity));	//ConveyorTubeEntity
			//def.Add(new Vector3I(-1, 1, 1), typeof(CubeBlockEntity));	//ConveyorTubeEntity
			//def.Add(new Vector3I(0, 1, 2), typeof(CubeBlockEntity));	//ConveyorTubeEntity

			//def.Add(new Vector3I(0, 0, 1), typeof(CubeBlockEntity));	//ConveyorEntity

			def.Add(new Vector3I(0, 1, 1), typeof(BeaconEntity));

			return def;
		}

		public void Update()
		{
			if (IsDisposed)
				return;

			try
			{
				try
				{
					if (!this.IsFunctional)
						return;
				}
				catch
				{
					return;
				}

				m_timeSinceLastUpdate = DateTime.Now - m_lastUpdate;
				m_lastUpdate = DateTime.Now;

				Vector3 velocity = (Vector3)Parent.LinearVelocity;
				float speed = velocity.Length();

				if (m_isStartingWarp && speed > Core._SpeedThreshold)
				{
					m_timeSinceWarpRequest = DateTime.Now - m_warpRequest;

					if (m_timeSinceWarpRequest.TotalMilliseconds > Core._WarpDelay * 1000)
					{
						Warp();
					}
				}

				if (m_isWarping)
				{
					if (m_isAtWarpSpeed)
					{
						m_timeSinceWarpStart = DateTime.Now - m_warpStart;

						if (speed > (0.95 * Core._SpeedFactor * 100) && m_timeSinceWarpStart.TotalMilliseconds > Core._Duration * 1000)
						{
							m_isSpeedingUp = false;
							m_isAtWarpSpeed = false;
							m_isSlowingDown = true;

							if (SandboxGameAssemblyWrapper.IsDebugging)
								LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + Parent.Name + "' is slowing back down!");
						}
					}

					if (m_isSpeedingUp)
					{
						m_isSlowingDown = false;
						m_isAtWarpSpeed = false;
						SpeedUp();
					}

					if (m_isSlowingDown)
					{
						m_isSpeedingUp = false;
						m_isAtWarpSpeed = false;
						SlowDown();
					}

				}
				else
				{
					m_isAtWarpSpeed = false;
					m_isSpeedingUp = false;
					m_isSlowingDown = false;
				}
			}
			catch (Exception ex)
			{
				LogManager.GameLog.WriteLine(ex);
			}
		}

		public void StartWarp()
		{
			if (IsDisposed)
				return;

			if (m_isStartingWarp)
				return;

			m_isStartingWarp = true;
			m_warpRequest = DateTime.Now;
		}

		protected void Warp()
		{
			try
			{
				m_isStartingWarp = false;

				if (!CanWarp)
					return;
				if (IsPlayerInCockpit())
					return;

				if (SandboxGameAssemblyWrapper.IsDebugging)
					LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + Parent.Name + "' is warping using " + WarpFuel.ToString() + " fuel!");

				m_isWarping = true;

				//Consume the fuel
				float fuelRequired = WarpFuel;
				float totalFuelRemoved = 0;
				foreach (InventoryItemEntity fuelItem in FuelItems)
				{
					if (fuelItem.TotalMass > (fuelRequired - totalFuelRemoved))
					{
						fuelItem.Amount -= (fuelRequired - totalFuelRemoved);
						totalFuelRemoved = fuelRequired;
					}
					else
					{
						totalFuelRemoved += fuelItem.TotalMass;
						fuelItem.Amount = 0;
					}

					if (totalFuelRemoved >= fuelRequired)
						break;
				}

				//Set the ship's max speed
				Parent.MaxLinearVelocity = 100 * Core._SpeedFactor;

				//Start the acceleration procedure
				m_isSpeedingUp = true;

				m_oldBeaconName = Beacon.CustomName;
				m_oldBeaconBroadcastRadius = Beacon.BroadcastRadius;

				Beacon.CustomName = Core._BeaconText;
				Beacon.BroadcastRadius = Core._BeaconRange;

				if (SandboxGameAssemblyWrapper.IsDebugging)
					LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + Parent.Name + "' is accelerating to warp speed!");
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Error while starting warp");
				LogManager.GameLog.WriteLine(ex);
			}
		}

		protected void SpeedUp()
		{
			try
			{
				if (!m_isSpeedingUp)
					return;

				Vector3 velocity = (Vector3)Parent.LinearVelocity;
				float speed = velocity.Length();
				if (speed > (0.95 * Core._SpeedFactor * 100))
				{
					if (SandboxGameAssemblyWrapper.IsDebugging)
						LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + Parent.Name + "' is at warp speed!");

					m_isAtWarpSpeed = true;
					m_isSpeedingUp = false;
					m_isSlowingDown = false;
					m_warpStart = DateTime.Now;
				}
				else
				{
					float timeScaledAcceleration = 1 + ((float)m_timeSinceLastUpdate.TotalMilliseconds / 100);
					Vector3 acceleration = new Vector3(timeScaledAcceleration, timeScaledAcceleration, timeScaledAcceleration);
					Parent.LinearVelocity = Vector3.Multiply(Parent.LinearVelocity, acceleration);
				}
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Error while speeding up to warp");
				LogManager.GameLog.WriteLine(ex);
			}
		}

		protected void SlowDown()
		{
			try
			{
				if (!m_isSlowingDown)
					return;

				Vector3 velocity = (Vector3)Parent.LinearVelocity;
				float speed = velocity.Length();
				if (speed < 10)
				{
					if (SandboxGameAssemblyWrapper.IsDebugging)
						LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + Parent.Name + "' is back at normal speed!");

					m_isWarping = false;
					m_isAtWarpSpeed = false;
					m_isSpeedingUp = false;
					m_isSlowingDown = false;

					Parent.MaxLinearVelocity = (float)104.7;

					Beacon.CustomName = m_oldBeaconName;
					Beacon.BroadcastRadius = m_oldBeaconBroadcastRadius;
				}
				else
				{
					float timeScaledAcceleration = 1 + ((float)m_timeSinceLastUpdate.TotalMilliseconds / 100);
					Vector3 acceleration = new Vector3(timeScaledAcceleration, timeScaledAcceleration, timeScaledAcceleration);
					Parent.LinearVelocity = Vector3.Divide(Parent.LinearVelocity, acceleration);
				}
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Error while slowing down from warp");
				LogManager.GameLog.WriteLine(ex);
			}
		}

		protected bool IsPlayerInCockpit()
		{
			bool isPlayerInCockpit = false;
			try
			{
				List<CubeBlockEntity> cubeBlocks = Parent.CubeBlocks;
				foreach (var cubeBlock in cubeBlocks)
				{
					if (cubeBlock.GetType() == typeof(CockpitEntity))
					{
						CockpitEntity cockpit = (CockpitEntity)cubeBlock;
						if (cockpit.Pilot != null && !cockpit.IsPassengerSeat)
						{
							if (SandboxGameAssemblyWrapper.IsDebugging)
								LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + Parent.Name + "' cannot warp, player '" + cockpit.Pilot.DisplayName + "' is in a cockpit!");

							isPlayerInCockpit = true;
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				LogManager.GameLog.WriteLine(ex);
			}

			return isPlayerInCockpit;
		}

		#endregion
	}
}
