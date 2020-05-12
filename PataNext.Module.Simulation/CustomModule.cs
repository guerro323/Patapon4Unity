﻿using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Modding;
using GameHost.Entities;
using GameHost.Injection;
using PataNext.Module.Simulation;
using PataNext.Module.Simulation.RhythmEngine;
using PataNext.Module.Simulation.RhythmEngine.Data;
using PataponGameHost.RhythmEngine.Components;
using SoLoud;

[assembly: ModuleDescription("PataNext Simulation", "guerro", typeof(CustomModule))]

namespace PataNext.Module.Simulation
{
	public class CustomModule : CModule
	{
		public CustomModule(Entity source, Context ctxParent, SModuleInfo original) : base(source, ctxParent, original)
		{
			Console.WriteLine("My custom module has been loaded!");

			var simulationClient = new GameSimulationThreadingClient();
			simulationClient.Connect();

			simulationClient.InjectAssembly(GetType().Assembly);

			var inputClient = new GameInputThreadingClient();
			inputClient.Connect();

			inputClient.InjectAssembly(GetType().Assembly);
		}
	}

	[RestrictToApplication(typeof(GameSimulationThreadingHost))]
	public class TestModSystem : AppSystem
	{
		private IManagedWorldTime worldTime;
		private Soloud soloud;
		private SoLoud.Wav wav;

		private CModule module;

		public TestModSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref worldTime);
			DependencyResolver.Add(() => ref module);
			soloud = new Soloud();
			soloud.init();
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			wav = new Wav();
			Console.WriteLine(module.Storage.Value.CurrentPath + "/on_new_beat.ogg");
			wav.load(module.Storage.Value.CurrentPath + "/on_new_beat.ogg");
			
			for (var i = 0; i != 1; i++)
			{
				var ent = World.Mgr.CreateEntity();
				ent.Set(new RhythmEngineController {State      = RhythmEngineState.Playing, StartTime = worldTime.Total.Add(TimeSpan.FromSeconds(2))});
				ent.Set(new RhythmEngineSettings {BeatInterval = TimeSpan.FromSeconds(0.5), MaxBeat   = 4});
				ent.Set(new RhythmEngineLocalState());
				ent.Set(new RhythmEngineExecutingCommand());
				ent.Set(new GameComboState());
				ent.Set(new GameCommandState());
				ent.Set(new RhythmEngineLocalCommandBuffer());
				ent.Set(new RhythmEnginePredictedCommandBuffer());
			}

			var march = World.Mgr.CreateEntity();
			march.Set(new RhythmCommandDefinition("march", stackalloc[]
			{
				new RhythmCommandAction(0, 1),
				new RhythmCommandAction(1, 1),
				new RhythmCommandAction(2, 1),
				new RhythmCommandAction(3, 2),
			}));
			Console.WriteLine(march.Get<RhythmCommandDefinition>());

			var results = new List<bool>();
			results.Add(RhythmCommandUtility.SameAsSequence(march.Get<RhythmCommandDefinition>().Actions, new[]
			{
				new FlowPressure {FlowBeat = 4, KeyId = 1}
			}));
			results.Add(RhythmCommandUtility.CanBePredicted(march.Get<RhythmCommandDefinition>().Actions, new[]
			{
				new FlowPressure {FlowBeat = 4, KeyId = 1}
			}));
			results.Add(RhythmCommandUtility.SameAsSequence(march.Get<RhythmCommandDefinition>().Actions, new[]
			{
				new FlowPressure {FlowBeat = 8, KeyId = 1},
				new FlowPressure {FlowBeat = 9, KeyId = 1},
				new FlowPressure {FlowBeat = 10, KeyId = 1},
				new FlowPressure {FlowBeat = 11, KeyId = 2},
			}));
			results.Add(new Func<bool>(() =>
			{
				var set = World.Mgr.GetEntities().With<RhythmCommandDefinition>().AsSet();
				var returnCommands = new List<Entity>();
				
				RhythmCommandUtility.GetCommand(set, new[]
				{
					new FlowPressure {FlowBeat = 8, KeyId  = 1},
					new FlowPressure {FlowBeat = 9, KeyId  = 1},
					new FlowPressure {FlowBeat = 10, KeyId = 1},
					new FlowPressure {FlowBeat = 11, KeyId = 2},
				}, returnCommands, false);
				
				return returnCommands.Count > 0;
			})());
			
			foreach (var r in results)
				Console.WriteLine(r);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			if (!World.Mgr.Get<RhythmEngineOnNewBeat>().IsEmpty && wav != null)
			{
				soloud.play(wav, 1);
			}
		}
	}
}