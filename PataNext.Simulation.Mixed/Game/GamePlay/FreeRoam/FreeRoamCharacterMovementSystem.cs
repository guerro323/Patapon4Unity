﻿using System;
using System.Numerics;
using System.Threading;
using GameHost.Core;
using GameHost.Core.Ecs;
using GameHost.Revolution.NetCode.Components;
using GameHost.Revolution.Snapshot.Systems.Components;
using GameHost.Simulation.Utility.EntityQuery;
using GameHost.Worlds.Components;
using PataNext.Module.Simulation.Components;
using PataNext.Module.Simulation.Components.GamePlay.Units;
using PataNext.Module.Simulation.Components.Roles;
using PataNext.Module.Simulation.Components.Units;
using PataNext.Module.Simulation.Game.GamePlay.Units;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Network;
using StormiumTeam.GameBase.Network.Authorities;
using StormiumTeam.GameBase.Physics.Components;
using StormiumTeam.GameBase.Roles.Components;
using StormiumTeam.GameBase.Roles.Descriptions;
using StormiumTeam.GameBase.SystemBase;

namespace PataNext.Module.Simulation.Game.GamePlay.FreeRoam
{
	[UpdateAfter(typeof(UnitPhysicsSystem))]
	[UpdateBefore(typeof(UnitCollisionSystem))]
	public class FreeRoamCharacterMovementSystem : GameAppSystem, IPostUpdateSimulationPass
	{
		private IManagedWorldTime   worldTime;
		private NetReportTimeSystem reportTimeSystem;

		public FreeRoamCharacterMovementSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref worldTime);
			DependencyResolver.Add(() => ref reportTimeSystem);
		}

		private EntityQuery characterQuery;

		public void OnAfterSimulationUpdate()
		{
			var dt = (float) worldTime.Delta.TotalSeconds;

			var relativePlayerAccessor = GetAccessor<Relative<PlayerDescription>>();
			var inputAccessor          = GetAccessor<FreeRoamInputComponent>();
			var controllerAccessor     = GetAccessor<UnitControllerState>();
			var velocityAccessor       = GetAccessor<Velocity>();

			foreach (var entityHandle in characterQuery ??= CreateEntityQuery(new[]
			{
				typeof(UnitDescription),
				typeof(UnitPlayState),
				typeof(UnitFreeRoamMovement),
				typeof(UnitControllerState),
				typeof(Velocity),
				typeof(Relative<PlayerDescription>),
				typeof(SimulationAuthority)
			}))
			{
				var report = reportTimeSystem.Get(entityHandle, out _);

				ref var controller = ref controllerAccessor[entityHandle];
				ref var velocity   = ref velocityAccessor[entityHandle].Value;

				var              playerHandle = relativePlayerAccessor[entityHandle].Handle;
				ref readonly var input        = ref inputAccessor[playerHandle];

				controller.ControlOverVelocityX = true;
				controller.PassThroughEnemies   = true;

				var inputXY   = new Vector2(input.HorizontalMovement, 0);
				var inputXYZ  = new Vector3(inputXY, 0);
				var direction = new Vector3(Math.Sign(input.HorizontalMovement), 0, 0);


				var isGrounded = TryGetComponentData(entityHandle, out GroundState gs)
				                 && gs.Value;

				if (isGrounded)
				{
					var d = SrtGroundSettings.NewBase();
					d.BaseSpeed   = 5;
					d.SprintSpeed = 6;

					var n = SrtMovement.GroundMove(velocity, inputXY, direction, d, dt, controller.PreviousPosition);
					velocity.X = n.X;

					if (input.Up.Pressed > input.Up.Released)
					{
						Console.WriteLine($"jump!");
						velocity.Y = 12;
					}

					if (HasComponent<UnitDirection>(entityHandle))
					{
						if (Math.Abs(input.HorizontalMovement) > 0.1f)
							GetComponentData<UnitDirection>(entityHandle) = Math.Sign(input.HorizontalMovement) < 0 ? UnitDirection.Left : UnitDirection.Right;
						else if (GetComponentData<UnitDirection>(entityHandle).Invalid)
							GetComponentData<UnitDirection>(entityHandle) = UnitDirection.Right;
					}
				}
				else
				{
					var d = SrtAerialSettings.NewBase();
					d.BaseSpeed = 4;

					var n = SrtMovement.AerialMove(velocity, inputXYZ, d, dt);
					velocity.X = n.X;
					
					/*if (HasComponent<UnitDirection>(entityHandle))
					{
						GetComponentData<UnitDirection>(entityHandle) = Math.Sign(input.HorizontalMovement) < 0 ? UnitDirection.Left : UnitDirection.Right;
					}*/
				}
			}
		}
	}
}