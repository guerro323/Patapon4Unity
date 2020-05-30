﻿using System;
using System.Collections.Generic;
using System.Linq;
using Collections.Pooled;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Audio;
using GameHost.Core.Applications;
using GameHost.Core.Audio;
using GameHost.Core.Ecs;
using GameHost.Entities;
using GameHost.HostSerialization;
using GameHost.IO;
using PataNext.Module.RhythmEngine;
using PataNext.Module.RhythmEngine.Data;
using PataponGameHost.Inputs;

namespace PataNext.Module.Presentation.RhythmEngine
{
	[RestrictToApplication(typeof(GameRenderThreadingHost))]
	public class ShoutDrumSystem : AppSystem
	{
		private readonly Query playerQuery = new Query {All = new[] {typeof(PlayerInput)}};

		private readonly PooledDictionary<int, PooledDictionary<int, ResourceHandle<AudioResource>>> audioOnPressureDrum =
			new PooledDictionary<int, PooledDictionary<int, ResourceHandle<AudioResource>>>();

		private readonly PooledDictionary<int, PooledDictionary<int, ResourceHandle<AudioResource>>> audioOnPressureVoice =
			new PooledDictionary<int, PooledDictionary<int, ResourceHandle<AudioResource>>>();

		private LoadAudioResourceSystem loadAudio;
		private CustomModule            module;
		private PresentationWorld   presentation;
		private CurrentRhythmEngineSystem currentRhythmEngineSystem;

		public ShoutDrumSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref loadAudio);
			DependencyResolver.Add(() => ref module);
			DependencyResolver.Add(() => ref presentation);
			DependencyResolver.Add(() => ref currentRhythmEngineSystem);
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			var storage = new StorageCollection {module.DllStorage, module.Storage.Value}
			              .GetOrCreateDirectoryAsync("Sounds/RhythmEngine/Drums")
			              .Result;
			
			// TODO: should we have a configuration file for mapping the audio? (instead of hardcoding the mapping)
			for (var key = 1; key != 5; key++)
			{
				audioOnPressureDrum[key]  = new PooledDictionary<int, ResourceHandle<AudioResource>>();
				audioOnPressureVoice[key] = new PooledDictionary<int, ResourceHandle<AudioResource>>();

				for (var rank = 0; rank != 3; rank++)
				{
					audioOnPressureDrum[key][rank]  = loadAudio.Start($"drum_{key}_{rank}.ogg", storage);
					audioOnPressureVoice[key][rank] = loadAudio.Start($"voice_{key}_{rank}.wav", storage);
				}
			}

			presentation.World.SubscribeComponentChanged<PlayerInput>(OnPlayerInputChanged);
		}

		private void OnPlayerInputChanged(in Entity entity, in PlayerInput prev, in PlayerInput next)
		{
			var score = 0;
			var engineEntity = currentRhythmEngineSystem.Information.Entity;
			if (engineEntity != default
			    && engineEntity.TryGet(out RhythmEngineLocalState state)
			    && engineEntity.TryGet(out RhythmEngineSettings settings))
			{
				if (RhythmEngineUtility.GetScore(state, settings) > FlowPressure.Perfect)
					score++;
			}

			for (var i = 0; i < next.Actions.Length; i++)
			{
				if (!next.Actions[i].WasPressed)
					continue;

				var play = World.Mgr.CreateEntity();
				play.Set(audioOnPressureDrum[i + 1][score].Result);
				play.Set(new AudioVolumeComponent(1));
				play.Set(new FlatAudioPlayerComponent());
			}
		}
	}
}