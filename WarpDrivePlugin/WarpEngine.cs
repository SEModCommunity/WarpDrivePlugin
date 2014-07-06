using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

using SEModAPIInternal.API.Entity;
using SEModAPIInternal.API.Entity.Sector.SectorObject.CubeGrid.CubeBlock;
using SEModAPIInternal.Support;

using VRageMath;

namespace WarpDrivePlugin
{
	public class WarpEngine
	{
		#region "Attributes"

		private ReactorEntity m_linkedReactor;
		private bool m_isWarping;

		private Timer m_startWarpTimer;
		private Timer m_stopWarpTimer;
		private Timer m_speedUpTimer;
		private Timer m_slowDownTimer;

		protected float m_warpFuelRequired;
		protected float m_warpDuration;
		protected float m_warpSpeedFactor;

		#endregion

		#region "Constructors and Initializers"

		public WarpEngine(ReactorEntity reactor)
		{
			m_linkedReactor = reactor;

			m_warpFuelRequired = 100;
			m_warpDuration = 2000;
			m_warpSpeedFactor = 100;

			m_startWarpTimer = new Timer();
			m_startWarpTimer.Interval = 10000;
			m_startWarpTimer.Elapsed += Warp;

			m_stopWarpTimer = new Timer();
			m_stopWarpTimer.Interval = m_warpDuration;
			m_stopWarpTimer.Elapsed += StopWarp;

			m_speedUpTimer = new Timer();
			m_speedUpTimer.Interval = 100;
			m_speedUpTimer.Elapsed += SpeedUp;

			m_slowDownTimer = new Timer();
			m_slowDownTimer.Interval = 100;
			m_slowDownTimer.Elapsed += SlowDown;

			m_isWarping = false;
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
			get { return m_warpFuelRequired; }
		}

		public float WarpDuration
		{
			get { return m_warpDuration; }
		}

		public float WarpMaxSpeed
		{
			get { return 100 * m_warpSpeedFactor; }
		}

		public bool CanWarp
		{
			get
			{
				if (m_isWarping)	//Already warping
					return false;
				if (m_linkedReactor.Enabled == false)	//Reactor is off
					return false;
				if (m_linkedReactor.Fuel < m_warpFuelRequired)	//Not enough fuel
					return false;

				return true;
			}
		}

		#endregion

		#region "Methods"

		public void StartWarp()
		{
			m_startWarpTimer.Start();
		}

		protected void Warp(Object source, ElapsedEventArgs e)
		{
			try
			{
				m_startWarpTimer.Stop();

				if (!CanWarp)
					return;

				LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + m_linkedReactor.Parent.Name + "' is warping!");

				m_isWarping = true;

				//Consume the fuel
				float fuelRequired = m_warpFuelRequired;
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
				m_linkedReactor.Parent.MaxLinearVelocity = WarpMaxSpeed;

				//Start the acceleration procedure
				LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + m_linkedReactor.Parent.Name + "' is accelerating to warp speed!");
				m_speedUpTimer.Start();
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Error while starting warp");
				LogManager.GameLog.WriteLine(ex);
			}
		}

		protected void SpeedUp(Object source, ElapsedEventArgs e)
		{
			try
			{
				m_linkedReactor.Parent.LinearVelocity = Vector3.Multiply(m_linkedReactor.Parent.LinearVelocity, new Vector3(2, 2, 2));

				Vector3 velocity = (Vector3)m_linkedReactor.Parent.LinearVelocity;
				float speed = velocity.Length();
				if (speed > 9000)
				{
					m_speedUpTimer.Stop();

					LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + m_linkedReactor.Parent.Name + "' is at warp speed!");

					//Start the timer to stop warp
					m_stopWarpTimer.Start();
				}
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Error while speeding up to warp");
				LogManager.GameLog.WriteLine(ex);
			}
		}

		protected void SlowDown(Object source, ElapsedEventArgs e)
		{
			try
			{
				m_linkedReactor.Parent.LinearVelocity = Vector3.Divide(m_linkedReactor.Parent.LinearVelocity, new Vector3(2, 2, 2));

				Vector3 velocity = (Vector3)m_linkedReactor.Parent.LinearVelocity;
				float speed = velocity.Length();
				if (speed < 100)
				{
					m_linkedReactor.Parent.MaxLinearVelocity = (float)104.7;

					m_slowDownTimer.Stop();

					m_isWarping = false;

					LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + m_linkedReactor.Parent.Name + "' is back at normal speed!");
				}
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Error while slowing down from warp");
				LogManager.GameLog.WriteLine(ex);
			}
		}

		protected void StopWarp(Object source, ElapsedEventArgs e)
		{
			m_stopWarpTimer.Stop();

			m_slowDownTimer.Start();

			LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + m_linkedReactor.Parent.Name + "' is slowing back down!");
		}

		#endregion
	}
}
