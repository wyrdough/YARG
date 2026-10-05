using System;
using YARG.Core;

namespace YARG.Settings.Preview
{
    // Shared preview visual state across all preset type tabs (Camera, Color,
    // Engine, Highway, RockMeter). Bundled into one object (rather than a
    // handful of loose statics) so there is a single named thing to point at
    // with a documented lifetime: process-lifetime, shared by every
    // PresetSubTab<T>, not persisted to disk.
    public sealed class PreviewOptionsState
    {
        private bool _forceStarPowerNotes;
        private bool _forceStarPower;
        private bool _forceGroove;
        private bool _leftyFlip;
        private GameMode _gameMode;

        public bool ForceStarPowerNotes
        {
            get => _forceStarPowerNotes;
            set
            {
                _forceStarPowerNotes = value;
                NotifyChange();
            }
        }

        public bool ForceStarPower
        {
            get => _forceStarPower;
            set
            {
                _forceStarPower = value;
                NotifyChange();
            }
        }

        public bool ForceGroove
        {
            get => _forceGroove;
            set
            {
                _forceGroove = value;
                NotifyChange();
            }
        }

        public bool LeftyFlip
        {
            get => _leftyFlip;
            set
            {
                _leftyFlip = value;
                NotifyChange();
            }
        }

        public GameMode GameMode
        {
            get => _gameMode;
            set
            {
                _gameMode = value;
                NotifyChange();
            }
        }

        public Action OnChange;

        public void NotifyChange()
        {
            OnChange?.Invoke();
        }
    }
}