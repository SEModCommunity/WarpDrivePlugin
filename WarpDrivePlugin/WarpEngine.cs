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

		private float m_warpFuelRequired;
		private ReactorEntity m_linkedReactor;
		private Timer m_stopWarpTimer;

		#endregion

		#region "Constructors and Initializers"

		public WarpEngine(ReactorEntity reactor)
		{
			m_linkedReactor = reactor;
			m_warpFuelRequired = 100;
		}

		#endregion

		#region "Properties"
		#endregion

		#region "Methods"

		public bool Warp()
		{
			try
			{
				if (m_linkedReactor.Fuel < m_warpFuelRequired)	//Not enough fuel
					return false;
				if (m_linkedReactor.Enabled == false)	//Reactor is off
					return false;
				if (m_linkedReactor.Parent.MaxLinearVelocity > 150)	//Already warping
					return false;

				Console.WriteLine("WarpDrivePlugin - Ship '" + m_linkedReactor.Parent.Name + "' is warping!");

				//Consume the fuel
				float fuelRequired = m_warpFuelRequired;
				float totalFuelRemoved = 0;
				List<InventoryItemEntity> fuelItems = m_linkedReactor.Inventory.Items;
				foreach (InventoryItemEntity fuelItem in fuelItems)
				{
					if (fuelItem.TotalMass > fuelRequired)
					{
						fuelItem.Amount -= fuelRequired;
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

				//Set the ship's velocity to the 100x speed
				m_linkedReactor.Parent.MaxLinearVelocity = 10000;
				m_linkedReactor.Parent.LinearVelocity = Vector3.Multiply(m_linkedReactor.Parent.LinearVelocity, new Vector3(100, 100, 100));

				//Start the timer to stop warp
				m_stopWarpTimer = new Timer();
				m_stopWarpTimer.Interval = 2000;
				m_stopWarpTimer.Elapsed += StopWarp;
				m_stopWarpTimer.Start();

				return true;
			}
			catch (Exception ex)
			{
				LogManager.APILog.WriteLineAndConsole("Error while going to warp");
				LogManager.GameLog.WriteLine(ex);
				return false;
			}
		}

		public void StopWarp(Object source, ElapsedEventArgs e)
		{
			m_linkedReactor.Parent.LinearVelocity = Vector3.Divide(m_linkedReactor.Parent.LinearVelocity, new Vector3(100, 100, 100));
			m_linkedReactor.Parent.MaxLinearVelocity = (float)104.7;

			m_stopWarpTimer.Stop();

			Console.WriteLine("WarpDrivePlugin - Ship '" + m_linkedReactor.Parent.Name + "' just left warp and is normal again");
		}

		#endregion
	}
}
