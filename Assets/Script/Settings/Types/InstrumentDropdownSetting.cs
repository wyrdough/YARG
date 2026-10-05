using System;
using YARG.Core;
using YARG.Localization;

namespace YARG.Settings.Types
{
    /// <summary>
    /// Instrument-selector dropdown. The value is the unlocalized label (kept
    /// stable for the OnChange match); the displayed text is the localized
    /// instrument name from Enum.Instrument.*, resolved through the sub-section
    /// name that backs each label.
    /// </summary>
    public class InstrumentDropdownSetting : DropdownSetting<string>
    {
        private readonly (string SubSection, string Label, GameMode Mode)[] _instrumentModes;

        public InstrumentDropdownSetting(string currentLabel,
            (string SubSection, string Label, GameMode Mode)[] instrumentModes,
            Action<string> onChange)
            : base(currentLabel, onChange, localizable: false)
        {
            _instrumentModes = instrumentModes;
        }

        public override string ValueToString(string value)
        {
            // value is the unlocalized label; find its sub-section so we can
            // resolve the matching Enum.Instrument localization key.
            foreach (var (subSection, label, _) in _instrumentModes)
            {
                if (label == value)
                {
                    return Localize.Key($"Enum.GameMode.{subSection}");
                }
            }

            return value;
        }
    }
}