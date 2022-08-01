using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;

namespace TPDespair.BubbetTweaks
{
	public static class RiskOfOptions
	{
		private static int state = -1;

		internal static bool Enabled
		{
			get
			{
				if (state == -1)
				{
					if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions")) state = 1;
					else state = 0;
				}

				return state == 1;
			}
		}



		public static void Init()
		{
			ModSettingsManager.SetModDescription("Additional configuration for BubbetsItems.");

			ModSettingsManager.AddOption(new CheckBoxOption(BubbetTweaksPlugin.ExcludeExpansion));
			ModSettingsManager.AddOption(new CheckBoxOption(BubbetTweaksPlugin.OverrideDescriptions));

			ModSettingsManager.AddOption(new CheckBoxOption(BubbetTweaksPlugin.StatsDescriptions));
			ModSettingsManager.AddOption(new CheckBoxOption(BubbetTweaksPlugin.FunctionDescriptions));

			ModSettingsManager.AddOption(new CheckBoxOption(BubbetTweaksPlugin.VoidJetRefresh));
			ModSettingsManager.AddOption(new IntSliderOption(BubbetTweaksPlugin.VoidJetBaseCount, new IntSliderConfig { min = 0, max = 12 }));
			ModSettingsManager.AddOption(new IntSliderOption(BubbetTweaksPlugin.VoidJetStackCount, new IntSliderConfig { min = 0, max = 12 }));

			ModSettingsManager.AddOption(new CheckBoxOption(BubbetTweaksPlugin.VoidSlugScalingRegen));
			ModSettingsManager.AddOption(new StepSliderOption(BubbetTweaksPlugin.VoidSlugBaseDangerRegen, new StepSliderConfig { min = 0f, max = 12f, increment = 0.2f }));
			ModSettingsManager.AddOption(new StepSliderOption(BubbetTweaksPlugin.VoidSlugStackDangerRegen, new StepSliderConfig { min = 0f, max = 12f, increment = 0.2f }));

			ModSettingsManager.AddOption(new CheckBoxOption(BubbetTweaksPlugin.VoidLunarSandScalingRegen));
			ModSettingsManager.AddOption(new StepSliderOption(BubbetTweaksPlugin.VoidLunarSandBaseRegen, new StepSliderConfig { min = -36f, max = 0f, increment = 3f }));
			ModSettingsManager.AddOption(new StepSliderOption(BubbetTweaksPlugin.VoidLunarSandStackRegen, new StepSliderConfig { min = -36f, max = 0f, increment = 3f }));

			ModSettingsManager.AddOption(new StepSliderOption(BubbetTweaksPlugin.VoidLunarImperfectBaseHealth, new StepSliderConfig { min = 0f, max = 0.5f, increment = 0.01f }));
			ModSettingsManager.AddOption(new StepSliderOption(BubbetTweaksPlugin.VoidLunarImperfectStackHealth, new StepSliderConfig { min = 0f, max = 0.5f, increment = 0.01f }));
		}



		internal static void CreateTextOption(ConfigEntry<string> configEntry)
		{
			ModSettingsManager.AddOption(new StringInputFieldOption(configEntry));
		}
	}
}
