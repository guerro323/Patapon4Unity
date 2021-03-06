﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using DefaultEcs;
using GameHost.Core.IO;

namespace PataNext.Game.GameItems
{
	public class EquipmentItemMetadataStorage : BaseGameItemMetadataStorage
	{
		public EquipmentItemMetadataStorage(IStorage parent) : base(parent)
		{
		}
	}
	
	public class EquipmentItemMetadataFile : GameItemMetadataFile
	{
		public EquipmentItemMetadataFile(IFile file) : base(file)
		{
		}

		protected override void FillDescriptionChildClass(Entity target, JsonDocument document)
		{
			static int tryGetInt(JsonElement element, string propertyName, int defaultValue = 0)
			{
				return element.TryGetProperty(propertyName, out var prop) ? prop.GetInt32() : defaultValue;
			}

			static float tryGetFloat(JsonElement element, string propertyName, float defaultValue = 0)
			{
				return element.TryGetProperty(propertyName, out var prop) ? prop.GetSingle() : defaultValue;
			}

			base.FillDescriptionChildClass(target, document);

			var additive       = default(EquipmentItemStatisticsAdditive);
			var multiplicative = EquipmentItemStatisticsMultiplicative.Default;

			var root = document.RootElement;
			root = root.GetProperty("stat");
			
			if (root.TryGetProperty("+", out var addProp))
			{
				additive.Health  = tryGetInt(addProp, "health");
				additive.Attack  = tryGetInt(addProp, "attack");
				additive.Defense = tryGetInt(addProp, "defense");

				additive.Weight              = tryGetFloat(addProp, "weight");
				additive.BaseWalkSpeed       = tryGetFloat(addProp, "base_walk_speed");
				additive.FeverWalkSpeed      = tryGetFloat(addProp, "fever_walk_speed");
				additive.MovementAttackSpeed = tryGetFloat(addProp, "movement_attack_speed");
			}

			if (root.TryGetProperty("*", out var mulProp))
			{
				multiplicative.AttackSpeed         = tryGetFloat(mulProp, "attack_speed");
				multiplicative.MovementSpeed       = tryGetFloat(mulProp, "movement_speed");
				multiplicative.MovementAttackSpeed = tryGetFloat(mulProp, "movement_attack_speed");
			}

			target.Set(new EquipmentItemDescription
			{
				Additive = additive,
				Multiplicative = multiplicative
			});
		}
	}

	public struct EquipmentItemDescription
	{
		public string                                ItemType;
		public EquipmentItemStatisticsAdditive       Additive;
		public EquipmentItemStatisticsMultiplicative Multiplicative;
	}

	public struct EquipmentItemStatisticsAdditive
	{
		public int Health;
		public int Attack;
		public int Defense;

		public float Weight;
		public float BaseWalkSpeed;
		public float FeverWalkSpeed;
		public float MovementAttackSpeed;
	}

	public struct EquipmentItemStatisticsMultiplicative
	{
		public static readonly EquipmentItemStatisticsMultiplicative Default = new()
		{
			AttackSpeed = 1.0f
		};
		
		public float AttackSpeed;
		public float MovementSpeed;
		public float MovementAttackSpeed;
	}
}