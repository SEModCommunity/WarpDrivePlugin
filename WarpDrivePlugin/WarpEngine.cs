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
		private bool m_isPowerSetup;

		private float m_energyRequired;
		private float m_accelerationFactor;

		private string m_oldBeaconName;
		private float m_oldBeaconBroadcastRadius;

		private DateTime m_lastPowerCheck;
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
			m_energyRequired = Core._BaseFuel;

			m_lastPowerCheck = DateTime.Now;
			m_lastUpdate = DateTime.Now;

			m_isStartingWarp = false;
			m_isWarping = false;
			m_isAtWarpSpeed = false;
			m_isSpeedingUp = false;
			m_isSlowingDown = false;
			m_isDisposed = false;
			m_isPowerSetup = false;

			m_accelerationFactor = 2;

			if (parent != null)
				m_oldBeaconName = parent.Name;
			else
				m_oldBeaconName = "Beacon";
			m_oldBeaconBroadcastRadius = 10000;
		}

		#endregion

		#region "Properties"

		public bool IsWarping
		{
			get { return m_isWarping; }
		}

		public float PowerRequired
		{
			get
			{
				if (Parent.Mass > 0)
					m_energyRequired = Core._BaseFuel + Core._FuelRate * (Parent.Mass / 100000);
				else
					m_energyRequired = Core._BaseFuel;

				return m_energyRequired;
			}
		}

		public float PowerLevel
		{
			get
			{
				float totalPower = 0;

				foreach (BatteryBlockEntity battery in BatteryBlocks)
				{
					totalPower += battery.CurrentStoredPower;
				}

				return totalPower;
			}
		}

		public bool CanWarp
		{
			get
			{
				if (IsWarping) //Already warping
					return false;
				if (PowerLevel < PowerRequired) //Not enough energy
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

		protected List<BatteryBlockEntity> BatteryBlocks
		{
			get
			{
				List<BatteryBlockEntity> list = new List<BatteryBlockEntity>();
				foreach (CubeBlockEntity cubeBlock in Blocks)
				{
					if (cubeBlock is BatteryBlockEntity)
					{
						BatteryBlockEntity battery = (BatteryBlockEntity)cubeBlock;
						list.Add(battery);
					}
				}

				return list;
			}
		}

		#endregion

		#region "Methods"

		public void Dispose()
		{
			RestoreBeacon();
			RestoreBatteries();

			m_isDisposed = true;
		}

		public override Dictionary<Vector3I, StructureEntry> GetMultiblockDefinition()
		{
			if (IsDisposed)
				return new Dictionary<Vector3I, StructureEntry>();

			if(m_definition.Count == 0)
				m_definition = WarpEngineDefinition();

			return m_definition;
		}

		private Dictionary<Vector3I, StructureEntry> WarpEngineDefinition()
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

			def.Add(new Vector3I(0, 0, 0), beaconCore);

			def.Add(new Vector3I(0, -1, -1), warpCoilBattery);
			def.Add(new Vector3I(1, -1, 0), warpCoilBattery);
			def.Add(new Vector3I(-1, -1, 0), warpCoilBattery);
			def.Add(new Vector3I(0, -1, 1), warpCoilBattery);
			def.Add(new Vector3I(0, 1, -1), warpCoilBattery);
			def.Add(new Vector3I(1, 1, 0), warpCoilBattery);
			def.Add(new Vector3I(-1, 1, 0), warpCoilBattery);
			def.Add(new Vector3I(0, 1, 1), warpCoilBattery);

			def.Add(new Vector3I(0, 0, -1), coreLight);
			def.Add(new Vector3I(1, 0, 0), coreLight);
			def.Add(new Vector3I(-1, 0, 0), coreLight);
			def.Add(new Vector3I(0, 0, 1), coreLight);
			def.Add(new Vector3I(0, -1, 0), coreLight);

			return def;
		}

		private void RestoreBeacon()
		{
			Beacon.CustomName = m_oldBeaconName;
			Beacon.BroadcastRadius = m_oldBeaconBroadcastRadius;
		}

		private void RestoreBatteries()
		{
			if (!m_isPowerSetup)
				return;

			foreach (CubeBlockEntity cubeBlock in Blocks)
			{
				if (cubeBlock.IsDisposed)
					continue;

				if (cubeBlock is BatteryBlockEntity)
				{
					BatteryBlockEntity battery = (BatteryBlockEntity)cubeBlock;
					battery.MaxStoredPower = 1;
					battery.RequiredPowerInput = 4;
					battery.MaxPowerOutput = 4;
					battery.CurrentStoredPower = Math.Min(battery.CurrentStoredPower, battery.MaxStoredPower);
				}
			}

			m_isPowerSetup = false;
		}

		private void SetupBatteries()
		{
			if (m_isPowerSetup)
				return;

			foreach (CubeBlockEntity cubeBlock in Blocks)
			{
				if (cubeBlock is BatteryBlockEntity)
				{
					BatteryBlockEntity battery = (BatteryBlockEntity)cubeBlock;
					battery.MaxStoredPower = 100;
					battery.RequiredPowerInput = 40;
					battery.MaxPowerOutput = 0.001f;
				}
			}

			m_isPowerSetup = true;
		}

		private void DoRadiationDamage()
		{
			List<CharacterEntity> characters = SectorObjectManager.Instance.GetTypedInternalData<CharacterEntity>();

			//TODO - Check if this is accurate at all for calculating the beacon's actual location
			//The parent's position might not be at cubegrid 0,0,0 and might be at center of mass which is going to be hard to calculate
			Vector3I beaconBlockPos = Beacon.Min;
			Matrix matrix = Parent.PositionAndOrientation.GetMatrix();
			Matrix orientation = matrix.GetOrientation();
			Vector3 rotatedBlockPos = Vector3.Transform((Vector3)beaconBlockPos * 2.5f, orientation);
			Vector3 beaconPos = rotatedBlockPos + Parent.Position;

			foreach (CharacterEntity character in characters)
			{
				double distance = Vector3.Distance(character.Position, beaconPos);
				if (distance < 10)
				{
					double damage = m_timeSinceLastUpdate.TotalSeconds * (10.0 - distance);
					character.Health = character.Health - (float)damage;
				}
			}
		}

		public void Update()
		{
			if (IsDisposed)
				return;

			try
			{
				if (!IsFunctional)
					return;

				SetupBatteries();
				DoRadiationDamage();

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

				//if (SandboxGameAssemblyWrapper.IsDebugging)
					LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + Parent.Name + "' is attempting to warp ...");

				if (!CanWarp)
					return;
				if (IsPlayerInCockpit())
					return;

				//if (SandboxGameAssemblyWrapper.IsDebugging)
					LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + Parent.Name + "' is warping!");

				m_isWarping = true;

				//Consume the power
				List<BatteryBlockEntity> warpCoils = BatteryBlocks;
				float dividedPower = PowerRequired / (warpCoils.Count);
				foreach (CubeBlockEntity cubeBlock in Blocks)
				{
					if (cubeBlock is BatteryBlockEntity)
					{
						BatteryBlockEntity battery = (BatteryBlockEntity)cubeBlock;
						battery.CurrentStoredPower -= dividedPower;
					}
				}

				//if (SandboxGameAssemblyWrapper.IsDebugging)
					LogManager.APILog.WriteLineAndConsole("WarpDrivePlugin - Ship '" + Parent.Name + "' consumed " + PowerRequired.ToString() + "MJ of power!");

				//Set the ship's max speed
				Parent.MaxLinearVelocity = 100 * Core._SpeedFactor;

				//Start the acceleration procedure
				m_isSpeedingUp = true;

				m_oldBeaconName = Beacon.CustomName;
				m_oldBeaconBroadcastRadius = Beacon.BroadcastRadius;

				Beacon.CustomName = Core._BeaconText;
				Beacon.BroadcastRadius = Core._BeaconRange;

				//if (SandboxGameAssemblyWrapper.IsDebugging)
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

					Parent.MaxLinearVelocity = (float)104.375;

					RestoreBeacon();
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
							//if (SandboxGameAssemblyWrapper.IsDebugging)
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
