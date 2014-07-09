using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

using SEModAPIInternal.API.Common;
using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.Support;

using VRageMath;

namespace WarpDrivePlugin
{
	public class WarpEngine
	{
		#region "Attributes"

		private ReactorEntity m_linkedReactor;

		private bool m_isStartingWarp;
		private bool m_isWarping;
		private bool m_isSpeedingUp;
		private bool m_isSlowingDown;

		private float m_accelerationFactor;

		private DateTime m_lastUpdate;
		private DateTime m_warpRequest;
		private DateTime m_warpStart;
		private TimeSpan m_timeSinceLastUpdate;
		private TimeSpan m_timeSinceWarpRequest;
		private TimeSpan m_timeSinceWarpStart;

		protected float m_warpFuelRequired;

		#endregion

		#region "Constructors and Initializers"

		public WarpEngine(ReactorEntity reactor)
		{
			m_linkedReactor = reactor;

			if (m_linkedReactor.Parent.Mass > 0)
				m_warpFuelRequired = Core.BaseFuel + Core.FuelRate * (m_linkedReactor.Parent.Mass / 100000);
			else
				m_warpFuelRequired = Core.BaseFuel;

			m_lastUpdate = DateTime.Now;

			m_isStartingWarp = false;
			m_isWarping = false;
			m_isSpeedingUp = false;
			m_isSlowingDown = false;

			m_accelerationFactor = 2;
		}

		#endregion

		#region "Properties"

		public ReactorEntity LinkedReactor
		{
			get { return m_linkedReactor; }
		}

		public bool IsWarping
		{
			get { return m_isWarping; }
		}

		public float WarpFuel
		{
			get	{ return m_warpFuelRequired; }
		}

		public bool CanWarp
		{
			get
			{
				//These checks are arranged in order of least complex to most complex
				//This is to ensure that the checks are going as fast as possible

				if (m_isWarping)	//Already warping
					return false;
				if (m_linkedReactor.Enabled == false)	//Reactor is off
					return false;
				if (m_linkedReactor.Fuel < WarpFuel)	//Not enough fuel
					return false;

				return true;
			}
		}

		#endregion

		#region "Methods"

		public void Update()
		{
			m_timeSinceLastUpdate = DateTime.Now - m_lastUpdate;
			m_lastUpdate = DateTime.Now;

			if (m_isStartingWarp)
			{
				m_timeSinceWarpRequest = DateTime.Now - m_warpRequest;

				if (m_timeSinceWarpRequest.Milliseconds > 10000)
				{
					Warp();
				}
			}

			if (m_isWarping)
			{
				m_timeSinceWarpStart = DateTime.Now - m_warpStart;

				if (m_isSpeedingUp && m_isSlowingDown)
				{
					m_isSpeedingUp = false;
					m_isSlowingDown = true;
				}

				if (m_isSpeedingUp && !m_isSlowingDown)
				{
					SpeedUp();
				}

				if (m_isSlowingDown && !m_isSpeedingUp)
				{
					SlowDown();
				}

				if (!m_isSpeedingUp && !m_isSlowingDown && m_timeSinceWarpStart.Milliseconds > Core.Duration * 1000)
				{
					m_isSlowingDown = true;

					if (SandboxGameAssemblyWrapper.IsDebugging)
						LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + m_linkedReactor.Parent.Name + "' is slowing back down!");
				}
			}
			else
			{
				m_isSpeedingUp = false;
				m_isSlowingDown = false;
			}
		}

		public void StartWarp()
		{
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
					LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + m_linkedReactor.Parent.Name + "' is warping using " + WarpFuel.ToString() + " fuel!");

				m_isWarping = true;

				//Consume the fuel
				float fuelRequired = WarpFuel;
				float totalFuelRemoved = 0;
				List<InventoryItemEntity> fuelItems = m_linkedReactor.Inventory.Items;
				foreach (InventoryItemEntity fuelItem in fuelItems)
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

				//Turn off the reactor
				m_linkedReactor.Enabled = false;

				//Set the ship's max speed
				m_linkedReactor.Parent.MaxLinearVelocity = 100 * Core.SpeedFactor;

				if (SandboxGameAssemblyWrapper.IsDebugging)
					LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + m_linkedReactor.Parent.Name + "' is accelerating to warp speed!");

				//Start the acceleration procedure
				m_isSpeedingUp = true;
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
				Vector3 velocity = (Vector3)m_linkedReactor.Parent.LinearVelocity;
				float speed = velocity.Length();
				if (speed > (0.95 * Core.SpeedFactor * 100))
				{
					if (SandboxGameAssemblyWrapper.IsDebugging)
						LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + m_linkedReactor.Parent.Name + "' is at warp speed!");

					m_isSpeedingUp = false;
					m_warpStart = DateTime.Now;
				}
				else
				{
					float timeScaledAcceleration = m_accelerationFactor * (m_timeSinceLastUpdate.Milliseconds / 100);
					Vector3 acceleration = new Vector3(timeScaledAcceleration, timeScaledAcceleration, timeScaledAcceleration);
					m_linkedReactor.Parent.LinearVelocity = Vector3.Multiply(m_linkedReactor.Parent.LinearVelocity, acceleration);
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
				Vector3 velocity = (Vector3)m_linkedReactor.Parent.LinearVelocity;
				float speed = velocity.Length();
				if (speed < 50)
				{
					m_linkedReactor.Parent.MaxLinearVelocity = (float)104.7;

					m_isSpeedingUp = false;
					m_isSlowingDown = false;
					m_isWarping = false;

					if (SandboxGameAssemblyWrapper.IsDebugging)
						LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + m_linkedReactor.Parent.Name + "' is back at normal speed!");
				}
				else
				{
					float timeScaledAcceleration = m_accelerationFactor * (m_timeSinceLastUpdate.Milliseconds / 100);
					Vector3 acceleration = new Vector3(timeScaledAcceleration, timeScaledAcceleration, timeScaledAcceleration);
					m_linkedReactor.Parent.LinearVelocity = Vector3.Divide(m_linkedReactor.Parent.LinearVelocity, acceleration);
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
				List<CubeBlockEntity> cubeBlocks = m_linkedReactor.Parent.CubeBlocks;
				foreach (var cubeBlock in cubeBlocks)
				{
					if (cubeBlock.GetType() == typeof(CockpitEntity))
					{
						CockpitEntity cockpit = (CockpitEntity)cubeBlock;
						if (cockpit.Pilot != null && !cockpit.IsPassengerSeat)
						{
							if (SandboxGameAssemblyWrapper.IsDebugging)
								LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + m_linkedReactor.Parent.Name + "' cannot warp, player '" + cockpit.Pilot.DisplayName + "' is in a cockpit!");

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
