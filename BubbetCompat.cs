using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Bootstrap;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using RoR2;

namespace TPDespair.BubbetTweaks
{
	internal static class BubbetCompat
	{
		private static readonly string GUID = "bubbet.bubbetsitems";
		private static BaseUnityPlugin Plugin;
		private static Assembly PluginAssembly;

		private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		private static FieldInfo ItemDefField;
		private static FieldInfo ScalingInfosField;

		private static MethodInfo DescriptionMethod;
		private static MethodInfo ScalingFunctionMethod;
		private static MethodInfo ToStringMethod;

		private static bool FoundScalingInfos = false;
		private static bool FoundScalingFunction = false;
		private static bool FoundToString = false;



		internal static void Init()
		{
			if (!Chainloader.PluginInfos.ContainsKey(GUID)) return;

			Plugin = Chainloader.PluginInfos[GUID].Instance;
			PluginAssembly = Assembly.GetAssembly(Plugin.GetType());

			if (PluginAssembly != null)
			{
				GatherInfos();

				HookMethods();
			}
			else
			{
				BubbetTweaksPlugin.LogWarn("[BubbetCompat] - Could Not Find BubbetsItems Assembly");
			}
		}



		private static void GatherInfos()
		{
			Type type;

			type = Type.GetType("BubbetsItems.ItemBase, " + PluginAssembly.FullName, false);
			if (type != null)
			{
				ItemDefField = type.GetField("ItemDef", Flags);
				if (ItemDefField == null)
				{
					BubbetTweaksPlugin.LogWarn("[BubbetCompat] - Could Not Find Field : ItemBase.ItemDef");
				}

				DescriptionMethod = type.GetMethod("GetFormattedDescription", Flags);
				if (DescriptionMethod == null)
				{
					BubbetTweaksPlugin.LogWarn("[BubbetCompat] - Could Not Find Method : ItemBase.GetFormattedDescription");
				}



				ScalingInfosField = type.GetField("scalingInfos", Flags);
				if (ScalingInfosField == null)
				{
					BubbetTweaksPlugin.LogWarn("[BubbetCompat] - Could Not Find Field : ItemBase.scalingInfos");
				}
				else
				{
					FoundScalingInfos = true;
				}

				Type nestedType = type.GetNestedType("ScalingInfo", Flags);
				if (nestedType != null)
				{
					ScalingFunctionMethod = nestedType.GetMethod("ScalingFunction", Flags, default, new Type[] { typeof(int?) }, default);
					if (ScalingFunctionMethod == null)
					{
						BubbetTweaksPlugin.LogWarn("[BubbetCompat] - Could Not Find Method : ScalingInfo.ScalingFunction");
					}
					else
					{
						FoundScalingFunction = true;
					}

					ToStringMethod = nestedType.GetMethod("ToString", Flags);
					if (ScalingFunctionMethod == null)
					{
						BubbetTweaksPlugin.LogWarn("[BubbetCompat] - Could Not Find Method : ScalingInfo.ToString");
					}
					else
					{
						FoundToString = true;
					}
				}
				else
				{
					BubbetTweaksPlugin.LogWarn("[BubbetCompat] - Could Not Find NestedType : ItemBase.ScalingInfo");
				}
			}
			else
			{
				BubbetTweaksPlugin.LogWarn("[BubbetCompat] - Could Not Find Type : BubbetsItems.ItemBase");
			}
		}

		private static void HookMethods()
		{
			if (ItemDefField != null && DescriptionMethod != null)
			{
				if (FoundScalingInfos && ((BubbetTweaksPlugin.StatsDescriptions.Value && FoundScalingFunction) || (BubbetTweaksPlugin.FunctionDescriptions.Value && FoundToString)))
				{
					BubbetTweaksPlugin.LogWarn("[BubbetCompat] - ComplexDesc");
					HookEndpointManager.Modify(DescriptionMethod, (ILContext.Manipulator)DescriptionOverrideComplexHook);
				}
				else
				{
					BubbetTweaksPlugin.LogWarn("[BubbetCompat] - SimpleDesc");
					HookEndpointManager.Modify(DescriptionMethod, (ILContext.Manipulator)DescriptionOverrideSimpleHook);
				}
			}
			else
			{
				BubbetTweaksPlugin.LogWarn("[BubbetCompat] - Failed to hook GetFormattedDescription");
			}
		}



		private static void DescriptionOverrideSimpleHook(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			ILLabel label = c.MarkLabel();

			c.Index = 0;

			c.Emit(OpCodes.Ldarg, 0);
			c.Emit(OpCodes.Ldfld, ItemDefField);
			c.EmitDelegate<Func<ItemDef, bool>>((itemDef) =>
			{
				if (!itemDef) return false;

				string token = itemDef.descriptionToken;
				//ModTweakerPlugin.LogWarn("[BubbetCompat] - Token : " + token);
				if (BubbetTweaksPlugin.BubbetTokens.ContainsKey(token))
				{
					return BubbetTweaksPlugin.BubbetTokens[token] != "";
				}

				return false;
			});
			c.Emit(OpCodes.Brfalse, label);
			c.Emit(OpCodes.Ldarg, 0);
			c.Emit(OpCodes.Ldfld, ItemDefField);
			c.EmitDelegate<Func<ItemDef, string>>((itemDef) =>
			{
				string token = itemDef.descriptionToken;

				return BubbetTweaksPlugin.BubbetTokens[token];
			});
			c.Emit(OpCodes.Ret);
		}

		private static void DescriptionOverrideComplexHook(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			ILLabel label = c.MarkLabel();

			c.Index = 0;

			c.Emit(OpCodes.Ldarg, 0);
			c.Emit(OpCodes.Ldfld, ItemDefField);
			c.EmitDelegate<Func<ItemDef, bool>>((itemDef) =>
			{
				if (!itemDef) return false;

				string token = itemDef.descriptionToken;
				//ModTweakerPlugin.LogWarn("[BubbetCompat] - Token : " + token);
				if (BubbetTweaksPlugin.BubbetTokens.ContainsKey(token))
				{
					return BubbetTweaksPlugin.BubbetTokens[token] != "";
				}

				return false;
			});
			c.Emit(OpCodes.Brfalse, label);
			c.Emit(OpCodes.Ldarg, 1);
			c.Emit(OpCodes.Ldarg, 0);
			c.Emit(OpCodes.Ldfld, ItemDefField);
			c.Emit(OpCodes.Ldarg, 0);
			c.Emit(OpCodes.Ldfld, ScalingInfosField);
			c.Emit(OpCodes.Ldarg, 3);
			c.EmitDelegate<Func<Inventory, ItemDef, object, bool, string>>((inventory, itemDef, scalingData, forceHide) =>
			{
				string token = itemDef.descriptionToken;
				string output = BubbetTweaksPlugin.BubbetTokens[token];

				if (!forceHide && BubbetTweaksPlugin.StatsDescriptions.Value && FoundScalingFunction && inventory)
				{
					string outputStat = token.Substring(0, token.Length - 5) + "_STAT";
					if (BubbetTweaksPlugin.BubbetTokens.ContainsKey(outputStat))
					{
						outputStat = BubbetTweaksPlugin.BubbetTokens[outputStat];
						if (outputStat != "")
						{
							output += "\n\n" + GetStatTotals(outputStat, inventory.GetItemCount(itemDef), scalingData);
						}
					}
				}

				if (!forceHide && BubbetTweaksPlugin.FunctionDescriptions.Value && FoundToString)
				{
					output += "\n\n" + GetScalingFunctions(scalingData);

					string customFunc = GetCustomScalingFunction(itemDef);
					if (customFunc != "")
					{
						output += "\n" + customFunc;
					}
				}

				return output;
			});
			c.Emit(OpCodes.Ret);
		}

		private static string GetStatTotals(string token, int itemCount, object scalingData)
		{
			List<object> formatArgsList = new List<object>();
			IEnumerable<object> scalingInfos = (IEnumerable<object>)scalingData;

			foreach (object scalingInfo in scalingInfos)
			{
				formatArgsList.Add((object)ScalingFunctionMethod.Invoke(scalingInfo, new object[] { itemCount }));
			}

			while (formatArgsList.Count < 5)
			{
				formatArgsList.Add((object)0f);
			}

			object[] formatArgs = formatArgsList.ToArray();

			itemCount = Mathf.Max(1, itemCount);

			while (true)
			{
				int matchedIndex = token.IndexOf("$Stat");
				int terminateIndex = token.IndexOf(")!") + 2;

				if (matchedIndex == -1 || terminateIndex == 1) break;

				float baseValue = float.Parse(GetTextBetween(token, matchedIndex, "(", ","));
				float stackValue = float.Parse(GetTextBetween(token, matchedIndex, ",", ")"));

				float value = baseValue + stackValue * (itemCount - 1);

				int endLength = token.Length - terminateIndex;
				token = token.Substring(0, matchedIndex) + value + ((endLength > 0) ? token.Substring(terminateIndex, endLength) : "");
			}

			return String.Format(token, formatArgs);
		}

		private static string GetTextBetween(string text, int index, string start, string end)
		{
			int p1 = text.IndexOf(start, index) + start.Length;
			int p2 = text.IndexOf(end, p1);

			return text.Substring(p1, p2 - p1);
		}

		private static string GetScalingFunctions(object scalingData)
		{
			string output = "";
			IEnumerable<object> scalingInfos = (IEnumerable<object>)scalingData;

			foreach (object scalingInfo in scalingInfos)
			{
				if (output != "") output += "\n";
				output += (string)ToStringMethod.Invoke(scalingInfo, null);
			}

			return output;
		}

		private static string GetCustomScalingFunction(ItemDef itemDef)
		{
			ItemIndex itemIndex = itemDef.itemIndex;

			if (itemIndex == ItemIndex.None) return "";

			if (itemIndex == BubbetTweaksPlugin.VoidJetItem)
			{
				return CustomScalingFunction("Max Buff Count", BubbetTweaksPlugin.VoidJetBaseCount.Value, BubbetTweaksPlugin.VoidJetStackCount.Value);
			}
			if (itemIndex == BubbetTweaksPlugin.VoidSlugItem)
			{
				return CustomScalingFunction("Regen", BubbetTweaksPlugin.VoidSlugBaseDangerRegen.Value, BubbetTweaksPlugin.VoidSlugStackDangerRegen.Value);
			}
			if (itemIndex == BubbetTweaksPlugin.VoidLunarImperfectItem)
			{
				return CustomScalingFunction("Health Increase", BubbetTweaksPlugin.VoidLunarImperfectBaseHealth.Value, BubbetTweaksPlugin.VoidLunarImperfectStackHealth.Value);
			}
			if (itemIndex == BubbetTweaksPlugin.VoidLunarSandItem)
			{
				return CustomScalingFunction("Regen", BubbetTweaksPlugin.VoidLunarSandBaseRegen.Value, BubbetTweaksPlugin.VoidLunarSandStackRegen.Value);
			}

			return "";
		}

		private static string CustomScalingFunction(string desc, float baseValue, float stackValue)
		{
			if (baseValue == 0f) return "";

			if (stackValue != 0f)
			{
				return baseValue + " + " + stackValue + " * ([a] - 1) " + "\n(" + desc + ": [a] = item count)";
			}
			else
			{
				return baseValue + "\n(" + desc + ": [a] = item count)";
			}
		}
	}
}
