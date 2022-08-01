using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;

using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace TPDespair.BubbetTweaks
{
	[BepInPlugin(ModGuid, ModName, ModVer)]
	[BepInDependency("bubbet.bubbetsitems", BepInDependency.DependencyFlags.HardDependency)]
	[BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]

	public class BubbetTweaksPlugin : BaseUnityPlugin
	{
		public const string ModVer = "1.0.0";
		public const string ModName = "BubbetTweaks";
		public const string ModGuid = "com.TPDespair.BubbetTweaks";

		public static Dictionary<string, string> BubbetTokens = new Dictionary<string, string>();

		public static List<string> ConfigKeys = new List<string>();

		public static ConfigFile configFile;
		public static ManualLogSource logSource;



		public static ConfigEntry<bool> ExcludeExpansion { get; set; }
		public static ConfigEntry<bool> OverrideDescriptions { get; set; }
		public static ConfigEntry<bool> StatsDescriptions { get; set; }
		public static ConfigEntry<bool> FunctionDescriptions { get; set; }

		public static ConfigEntry<bool> VoidJetRefresh { get; set; }
		public static ConfigEntry<int> VoidJetBaseCount { get; set; }
		public static ConfigEntry<int> VoidJetStackCount { get; set; }

		public static ConfigEntry<bool> VoidSlugScalingRegen { get; set; }
		public static ConfigEntry<float> VoidSlugBaseDangerRegen { get; set; }
		public static ConfigEntry<float> VoidSlugStackDangerRegen { get; set; }

		public static ConfigEntry<float> VoidLunarImperfectBaseHealth { get; set; }
		public static ConfigEntry<float> VoidLunarImperfectStackHealth { get; set; }

		public static ConfigEntry<bool> VoidLunarSandScalingRegen { get; set; }
		public static ConfigEntry<float> VoidLunarSandBaseRegen { get; set; }
		public static ConfigEntry<float> VoidLunarSandStackRegen { get; set; }



		public static Action OnLateSetup;

		public static BuffIndex VoidJetBuff;

		public static ItemIndex VoidJetItem;
		public static ItemIndex VoidSlugItem;
		public static ItemIndex VoidLunarImperfectItem;
		public static ItemIndex VoidLunarSandItem;

		public void Awake()
		{
			RoR2Application.isModded = true;
			NetworkModCompatibilityHelper.networkModList = NetworkModCompatibilityHelper.networkModList.Append(ModGuid + ":" + ModVer);

			configFile = Config;
			logSource = Logger;

			SetupConfig();
			SetupRiskOfOptions();

			if (OverrideDescriptions.Value) BubbetCompat.Init();

			RoR2Application.onLoad += LateSetup;

			RegenHook();
			HealthHook();

			if (ExcludeExpansion.Value) OnLateSetup += ExcludeExpansionChoice;

			OnLateSetup += FindIndexes;
			OnLateSetup += InterceptBuffApplication;

			OnLateSetup += GenerateDescriptionConfigs;
			OnLateSetup += GenerateExamples;
		}



		private static void SetupConfig()
		{
			string section = "00_General";

			ExcludeExpansion = ConfigEntry(
				section, "ExcludeExpansion", false,
				"Remove BubbetsItems expansion option from the lobby. BubbetsVoidItems expansion option will still be available. Must restart game for changes to take effect."
			);
			OverrideDescriptions = ConfigEntry(
				section, "OverrideDescriptions", false,
				"Allows overriding item descriptions with custom text. Must restart game for changes to take effect."
			);
			StatsDescriptions = ConfigEntry(
				section, "StatsDescription", false,
				"Show total stats after custom description. Must restart game for changes to take effect if both description additions were disabled."
			);
			FunctionDescriptions = ConfigEntry(
				section, "FunctionDescriptions", false,
				"Show scaling functions after custom description. Must restart game for changes to take effect if both description additions were disabled."
			);



			section = "Tweak_VoidJet";

			VoidJetRefresh = ConfigEntry(
				section, "Refresh", false,
				"Refresh all VoidJet buffs whenever it is applied."
			);
			VoidJetBaseCount = ConfigEntry(
				section, "BaseCount", 0,
				"Maximum buff count. Must set buff as stackable in BubbetsItems config. 0 to disable limit."
			);
			VoidJetStackCount = ConfigEntry(
				section, "StackCount", 0,
				"Maximum buff count per stack."
			);



			section = "Tweak_VoidSlug";

			VoidSlugScalingRegen = ConfigEntry(
				section, "ScalingRegen", true,
				"Whether health regeneration scales with level."
			);
			VoidSlugBaseDangerRegen = ConfigEntry(
				section, "BaseDangerRegen", 0f,
				"Health regeneration while in danger granted by item. 0 to disable."
			);
			VoidSlugBaseDangerRegen.SettingChanged += FixFloat_SettingChanged;
			VoidSlugStackDangerRegen = ConfigEntry(
				section, "StackDangerRegen", 0f,
				"Health regeneration while in danger granted by item per stack."
			);
			VoidSlugStackDangerRegen.SettingChanged += FixFloat_SettingChanged;



			section = "Tweak_VoidLunarSand";

			VoidLunarSandScalingRegen = ConfigEntry(
				section, "ScalingRegen", true,
				"Whether health regeneration scales with level."
			);
			VoidLunarSandBaseRegen = ConfigEntry(
				section, "BaseRegen", 0f,
				"Health regeneration granted by item. 0 to disable."
			);
			VoidLunarSandBaseRegen.SettingChanged += FixFloat_SettingChanged;
			VoidLunarSandStackRegen = ConfigEntry(
				section, "StackRegen", 0f,
				"Health regeneration granted by item per stack."
			);
			VoidLunarSandStackRegen.SettingChanged += FixFloat_SettingChanged;



			section = "Tweak_VoidLunarImperfect";

			VoidLunarImperfectBaseHealth = ConfigEntry(
				section, "BaseHealth", 0f,
				"Health multiplier increase granted by item. 0 to disable. 0.1 = 10% more health."
			);
			VoidLunarImperfectBaseHealth.SettingChanged += FixFloat_SettingChanged;
			VoidLunarImperfectStackHealth = ConfigEntry(
				section, "StackHealth", 0f,
				"Health multiplier increase granted by item per stack."
			);
			VoidLunarImperfectStackHealth.SettingChanged += FixFloat_SettingChanged;
		}

		private static void SetupRiskOfOptions()
		{
			if (RiskOfOptions.Enabled)
			{
				LogInfo("Initializing RiskOfOptions Support!");

				RiskOfOptions.Init();
			}
		}



		internal static ConfigEntry<bool> ConfigEntry(string section, string key, bool defaultValue, string description)
		{
			string fullConfigKey = section + "_" + key;
			ValidateConfigKey(fullConfigKey);
			ConfigEntry<bool> configEntry = configFile.Bind(section, fullConfigKey, defaultValue, description);

			return configEntry;
		}

		internal static ConfigEntry<int> ConfigEntry(string section, string key, int defaultValue, string description)
		{
			string fullConfigKey = section + "_" + key;
			ValidateConfigKey(fullConfigKey);
			ConfigEntry<int> configEntry = configFile.Bind(section, fullConfigKey, defaultValue, description);

			return configEntry;
		}

		internal static ConfigEntry<float> ConfigEntry(string section, string key, float defaultValue, string description)
		{
			string fullConfigKey = section + "_" + key;
			ValidateConfigKey(fullConfigKey);
			ConfigEntry<float> configEntry = configFile.Bind(section, fullConfigKey, defaultValue, description);

			return configEntry;
		}

		internal static ConfigEntry<string> ConfigEntry(string section, string key, string defaultValue, string description)
		{
			string fullConfigKey = section + "_" + key;
			ValidateConfigKey(fullConfigKey);
			ConfigEntry<string> configEntry = configFile.Bind(section, fullConfigKey, defaultValue, description);

			return configEntry;
		}

		private static void ValidateConfigKey(string configKey)
		{
			if (!ConfigKeys.Contains(configKey))
			{
				ConfigKeys.Add(configKey);
			}
			else
			{
				LogWarn("ConfigEntry for " + configKey + " already exists!");
			}
		}

		private static void FixFloat_SettingChanged(object sender, EventArgs e)
		{
			ConfigEntry<float> configEntry = (ConfigEntry<float>)sender;
			configEntry.Value = Mathf.Round(configEntry.Value * 1000f) / 1000f;
		}



		internal static void LogInfo(object data)
		{
			logSource.LogInfo(data);
		}

		internal static void LogWarn(object data)
		{
			logSource.LogWarning(data);
		}

		public static void RegisterBubbetToken(string token, string text)
		{
			if (!BubbetTokens.ContainsKey(token)) BubbetTokens.Add(token, text);
			else BubbetTokens[token] = text;
		}



		private static void LateSetup()
		{
			Action action = OnLateSetup;
			if (action != null)
			{
				LogInfo("LateSetup Initialized!");

				action();
			}
		}



		private static void RegenHook()
		{
			IL.RoR2.CharacterBody.RecalculateStats += (il) =>
			{
				ILCursor c = new ILCursor(il);

				const int lvlScaling = 66;
				const int knurlValue = 67;
				const int multValue = 72;

				bool found = c.TryGotoNext(
					x => x.MatchLdcR4(1f),
					x => x.MatchStloc(multValue)
				);

				if (found)
				{
					// add (affected by lvl regen scaling and ignites)
					c.Emit(OpCodes.Ldarg, 0);
					c.Emit(OpCodes.Ldloc, knurlValue);
					c.Emit(OpCodes.Ldloc, lvlScaling);
					c.EmitDelegate<Func<CharacterBody, float, float, float>>((self, value, scaling) =>
					{
						float scaledAmount = 0f;
						float flatAmount = 0f;

						Inventory inventory = self.inventory;
						if (inventory)
						{
							if (VoidSlugItem != ItemIndex.None && VoidSlugBaseDangerRegen.Value != 0f && !self.outOfDanger)
							{
								int count = inventory.GetItemCount(VoidSlugItem);
								if (count > 0)
								{
									float calcValue = VoidSlugBaseDangerRegen.Value + VoidSlugStackDangerRegen.Value * (count - 1);

									if (VoidSlugScalingRegen.Value)
									{
										scaledAmount += calcValue;
									}
									else
									{
										flatAmount += calcValue;
									}
								}
							}

							if (VoidLunarSandItem != ItemIndex.None && VoidLunarSandBaseRegen.Value != 0f)
							{
								int count = inventory.GetItemCount(VoidLunarSandItem);
								if (count > 0)
								{
									float calcValue = VoidLunarSandBaseRegen.Value + VoidLunarSandStackRegen.Value * (count - 1);

									if (VoidLunarSandScalingRegen.Value)
									{
										scaledAmount += calcValue;
									}
									else
									{
										flatAmount += calcValue;
									}
								}
							}
						}

						if (scaledAmount != 0f)
						{
							value += scaledAmount * scaling;
						}

						if (flatAmount != 0f)
						{
							value += flatAmount;
						}

						return value;
					});
					c.Emit(OpCodes.Stloc, knurlValue);
				}
				else
				{
					LogWarn("RegenHook Failed");
				}
			};
		}

		private static void HealthHook()
		{
			IL.RoR2.CharacterBody.RecalculateStats += (il) =>
			{
				ILCursor c = new ILCursor(il);

				const int baseValue = 62;
				const int multValue = 63;

				bool found = c.TryGotoNext(
					x => x.MatchLdloc(baseValue),
					x => x.MatchLdloc(multValue),
					x => x.MatchMul(),
					x => x.MatchStloc(baseValue)
				);

				if (found)
				{

					c.Index += 4;

					// multiplier
					c.Emit(OpCodes.Ldarg, 0);
					c.Emit(OpCodes.Ldloc, baseValue);
					c.EmitDelegate<Func<CharacterBody, float, float>>((self, value) =>
					{
						Inventory inventory = self.inventory;
						if (inventory)
						{
							if (VoidLunarImperfectItem != ItemIndex.None && VoidLunarImperfectBaseHealth.Value != 0f)
							{
								int count = inventory.GetItemCount(VoidLunarImperfectItem);
								if (count > 0)
								{
									value *= 1f + VoidLunarImperfectBaseHealth.Value + VoidLunarImperfectStackHealth.Value * (count - 1);
								}
							}
						}

						return value;
					});
					c.Emit(OpCodes.Stloc, baseValue);
				}
				else
				{
					LogWarn("HealthHook Failed");
				}
			};
		}



		private static void ExcludeExpansionChoice()
		{
			foreach (RuleDef ruleDef in RuleCatalog.allRuleDefs)
			{
				if (ruleDef.choices != null && ruleDef.choices.Count > 0)
				{
					//LogWarn("RuleGlobalName : " + ruleDef.globalName);

					if (ruleDef.globalName == "Expansions.ExpansionDefBub")
					{
						ruleDef.forceLobbyDisplay = false;

						foreach (RuleChoiceDef ruleDefChoice in ruleDef.choices)
						{
							ruleDefChoice.excludeByDefault = true;
						}

						LogInfo("Hiding RuleCatalog Entries For Expansion : " + ruleDef.globalName);
					}
				}
			}
		}



		private static void FindIndexes()
		{
			VoidJetBuff = BuffCatalog.FindBuffIndex("BuffDefScintillatingJet");

			VoidJetItem = ItemCatalog.FindItemIndex("ItemDefScintillatingJet");
			VoidSlugItem = ItemCatalog.FindItemIndex("ItemDefVoidSlug");
			VoidLunarImperfectItem = ItemCatalog.FindItemIndex("ItemDefImperfect");
			VoidLunarSandItem = ItemCatalog.FindItemIndex("ItemDefClumpedSand");
		}

		private static void InterceptBuffApplication()
		{
			if (VoidJetBuff != BuffIndex.None)
			{
				On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += BuffInterceptHook;
			}
		}

		private static void BuffInterceptHook(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
		{
			if (NetworkServer.active)
			{
				BuffIndex buffIndex = buffDef.buffIndex;
				if (buffIndex != BuffIndex.None && buffIndex == VoidJetBuff)
				{
					if (VoidJetRefresh.Value)
					{
						RefreshTimedBuffStacks(self, buffIndex, duration);
					}

					Inventory inventory = self.inventory;
					if (inventory)
					{
						int baseLimit = VoidJetBaseCount.Value;
						if (baseLimit <= 0 || VoidJetItem == ItemIndex.None)
						{
							orig(self, buffDef, duration);

							return;
						}

						int count = baseLimit + (VoidJetStackCount.Value * (inventory.GetItemCount(VoidJetItem) - 1));
						if (self.GetBuffCount(buffIndex) < count)
						{
							orig(self, buffDef, duration);
						}
					}

					return;
				}
			}

			orig(self, buffDef, duration);
		}

		private static void RefreshTimedBuffStacks(CharacterBody self, BuffIndex buffIndex, float duration)
		{
			if (duration > 0f)
			{
				for (int i = 0; i < self.timedBuffs.Count; i++)
				{
					CharacterBody.TimedBuff timedBuff = self.timedBuffs[i];
					if (timedBuff.buffIndex == buffIndex)
					{
						if (timedBuff.timer > 0.1f && timedBuff.timer < duration) timedBuff.timer = duration;
					}
				}
			}
		}



		private static void GenerateDescriptionConfigs()
		{
			foreach (ItemDef itemDef in ItemCatalog.allItemDefs)
			{
				string descToken = itemDef.descriptionToken;
				if (descToken.StartsWith("BUB_"))
				{
					CreateDescriptionConfigs(descToken);
				}
			}
		}

		private static void CreateDescriptionConfigs(string descToken)
		{
			descToken = descToken.Substring(0, descToken.Length - 5);

			ConfigEntry<string> configEntryDesc = ConfigEntry(
				descToken, "DESC", "",
				"Custom item description. Would recommend editing the config file in a text editor. If using RiskOfOptions, would only recommend simple edits, Press ENTER while editing text to make a new line as \\n is interpreted as text, Style text is hidden and can be difficult to edit."
			);
			configEntryDesc.SettingChanged += ConfigEntryDesc_SettingChanged;
			RegisterBubbetToken(descToken + "_DESC", configEntryDesc.Value);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryDesc);

			ConfigEntry<string> configEntryStat = ConfigEntry(
				descToken, "STAT", "",
				"Custom item stats. Use {x} or {x:0%} to display calculated stat values (x being a value between 0 and 4). Enable FunctionDescriptions to see the displayed order of calculated stat values in the logbook or in-game. Use $StatStack(x,y)! to calculate a value based on item count (x being the baseValue and y being the stackValue)."
			);
			configEntryStat.SettingChanged += ConfigEntryStat_SettingChanged;
			RegisterBubbetToken(descToken + "_STAT", configEntryStat.Value);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryStat);
		}

		private static void ConfigEntryDesc_SettingChanged(object sender, EventArgs e)
		{
			ConfigEntry<string> configEntry = (ConfigEntry<string>)sender;
			RegisterBubbetToken(configEntry.Definition.Section + "_DESC", configEntry.Value);
		}

		private static void ConfigEntryStat_SettingChanged(object sender, EventArgs e)
		{
			ConfigEntry<string> configEntry = (ConfigEntry<string>)sender;
			RegisterBubbetToken(configEntry.Definition.Section + "_STAT", configEntry.Value);
		}

		private static void GenerateExamples()
		{
			ConfigEntry<string> configEntryExampleDesc01 = ConfigEntry(
				"Example", "Desc_01", "Teleporter zones are <style=cIsUtility>50%</style> <style=cStack>(+25% per stack)</style> larger.\nThe area outside the teleporter is filled with <style=cIsVoid>Void Fog</style>.\nStaying in the <style=cIsVoid>Void Fog</style> charges the teleporter <style=cIsUtility>50%</style> <style=cStack>(+25% per stack)</style> faster per player outside.\n<style=cIsVoid>Corrupts all Focused Convergences</style>.",
				"Example Description."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleDesc01);
			ConfigEntry<string> configEntryExampleStat01 = ConfigEntry(
				"Example", "Stat_01", "Teleporter Radius : <style=cIsHealing>{0:0%}</style>\nCharge Rate Increase Per Player: <style=cIsHealing>$StatStack(50,25)!%</style>",
				"Example Stat."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleStat01);

			ConfigEntry<string> configEntryExampleDesc02 = ConfigEntry(
				"Example", "Desc_02", "Reduce <style=cIsUtility>skill cooldowns</style> by <style=cIsUtility>10%</style> <style=cStack>(+10% per stack)</style> of gold gained.\nTaking damage <style=cIsHealth>prevents gold from being earned</style> for <style=cIsHealth>3</style> <style=cStack>(+2 per stack)</style> seconds.\n<style=cIsVoid>Corrupts all Brittle Crowns</style>.",
				"Example Description."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleDesc02);
			ConfigEntry<string> configEntryExampleStat02 = ConfigEntry(
				"Example", "Stat_02", "Cooldown Refund : <style=cIsHealing>{0:0%}</style>\nGold Income Lockout Duration: <style=cIsHealth>{1}s</style>",
				"Example Stat."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleStat02);

			ConfigEntry<string> configEntryExampleDesc03 = ConfigEntry(
				"Example", "Desc_03", "Attacks hit twice, each for <style=cIsDamage>60%</style> <style=cStack>(+15% per stack)</style> TOTAL damage.\nDrains health by <style=cIsHealth>12 hp/s</style> <style=cStack>(+6 hp/s per stack)</style>.\n<style=cIsVoid>Corrupts all Shaped Glass</style>.",
				"Example Description."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleDesc03);
			ConfigEntry<string> configEntryExampleStat03 = ConfigEntry(
				"Example", "Stat_03", "Total Damage : <style=cIsDamage>$StatStack(120,30)!%</style>\nHealth Drain : <style=cIsHealth>$StatStack(12,6)! hp/s</style>",
				"Example Stat."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleStat03);

			ConfigEntry<string> configEntryExampleDesc04 = ConfigEntry(
				"Example", "Desc_04", "Equipment effects trigger <style=cIsUtility>+1</style> <style=cStack>(+1 per stack)</style> additional times.\n<style=cIsHealth>Increase equipment cooldown</style> by <style=cIsHealth>50%</style> <style=cStack>(+50% per stack)</style>.\n<style=cIsVoid>Corrupts all Gestures of the Drowned</style>.",
				"Example Description."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleDesc04);
			ConfigEntry<string> configEntryExampleStat04 = ConfigEntry(
				"Example", "Stat_04", "Equipment Trigger Count : <style=cIsHealing>$StatStack(2,1)!</style>\nEquipment Cooldown Increase: <style=cIsHealth>{1:0%}</style>",
				"Example Stat."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleStat04);

			ConfigEntry<string> configEntryExampleDesc05 = ConfigEntry(
				"Example", "Desc_05", "Reduces barrier decay rate by <style=cIsHealing>10%</style> <style=cStack>(+10% per stack)</style>.",
				"Example Description."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleDesc05);
			ConfigEntry<string> configEntryExampleStat05 = ConfigEntry(
				"Example", "Stat_05", "Barrier Decay Rate : <style=cIsHealing>{0:0.0%}</style>",
				"Example Stat."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleStat05);

			ConfigEntry<string> configEntryExampleDesc06 = ConfigEntry(
				"Example", "Desc_06", "Convert all but <style=cIsHealing>1</style> shield into <style=cIsHealing>health</style>.\nGain <style=cIsHealing>20%</style> <style=cStack>(+10% per stack)</style> <style=cIsHealing>maximum health</style>.\nReduce <style=cIsHealth>armor</style> by <style=cIsHealth>60</style> <style=cStack>(+30 per stack)</style>.\n<style=cIsVoid>Corrupts all Transcendence</style>.",
				"Example Description."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleDesc06);
			ConfigEntry<string> configEntryExampleStat06 = ConfigEntry(
				"Example", "Stat_06", "Health Increase : <style=cIsHealing>$StatStack(20,10)!%</style>\nArmor Reduction : <style=cIsHealth>$StatStack(60,30)!</style>",
				"Example Stat."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleStat06);

			ConfigEntry<string> configEntryExampleDesc07 = ConfigEntry(
				"Example", "Desc_07", "Seems to do nothing... <style=cIsHealth>but...</style>\n<style=cIsVoid>Corrupts all Beads of Fealty</style>.",
				"Example Description."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleDesc07);
			ConfigEntry<string> configEntryExampleStat07 = ConfigEntry(
				"Example", "Stat_07", "Void Seed Spawn Chance Increase : <style=cIsHealing>{0:0%}</style>",
				"Example Stat."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleStat07);

			ConfigEntry<string> configEntryExampleDesc08 = ConfigEntry(
				"Example", "Desc_08", "Taking damage increases <style=cIsHealing>armor</style> by <style=cIsHealing>5</style> for 4 seconds.\nMaximum cap of <style=cIsHealing>20</style> <style=cStack>(+10 per stack)</style> <style=cIsHealing>armor</style>.\n<style=cIsVoid>Corrupts all Oddly Shaped Opals</style>.",
				"Example Description."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleDesc08);
			ConfigEntry<string> configEntryExampleStat08 = ConfigEntry(
				"Example", "Stat_08", "Maximum Armor : <style=cIsHealing>$StatStack(20,10)!</style>",
				"Example Stat."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleStat08);

			ConfigEntry<string> configEntryExampleDesc09 = ConfigEntry(
				"Example", "Desc_09", "Items have a <style=cIsUtility>4%</style> <style=cStack>(+4% per stack)</style> chance to become a <color=#8600CB>Void Lunar</color> item instead.\n<style=cIsVoid>Corrupts all Eulogy Zeros</style>.",
				"Example Description."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleDesc09);
			ConfigEntry<string> configEntryExampleStat09 = ConfigEntry(
				"Example", "Stat_09", "Item Transformation Chance : <style=cIsHealing>{0:0%}</style>",
				"Example Stat."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleStat09);

			ConfigEntry<string> configEntryExampleDesc10 = ConfigEntry(
				"Example", "Desc_10", "Increases <style=cIsHealing>health regeneration</style> by <style=cIsHealing>2%</style> <style=cStack>(+0.5% per stack)</style> <style=cIsDamage>missing hp/s</style> while in danger.\nIncreases <style=cIsHealing>health regeneration</style> by <style=cIsHealing>2.4 hp/s</style> <style=cStack>(+2.4 hp/s per stack)</style> while in danger.\n<style=cIsVoid>Corrupts all Cautious Slugs</style>.",
				"Example Description."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleDesc10);
			ConfigEntry<string> configEntryExampleStat10 = ConfigEntry(
				"Example", "Stat_10", "Missing Health Regen While In Danger : <style=cIsDamage>{0:0.0%} hp/s</style>\nHealth Regen While In Danger : <style=cIsHealing>$StatStack(2.4,2.4)! hp/s</style>",
				"Example Stat."
			);
			if (RiskOfOptions.Enabled) RiskOfOptions.CreateTextOption(configEntryExampleStat10);
		}
	}
}
