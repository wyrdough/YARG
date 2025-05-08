using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YARG.Core;
using YARG.Core.Audio;
using YARG.Core.Chart;
using YARG.Core.Engine;
using YARG.Core.Engine.ProKeys;
using YARG.Core.Engine.ProKeys.Engines;
using YARG.Core.Input;
using YARG.Core.Logging;
using YARG.Core.Replays;
using YARG.Gameplay.Visuals;
using YARG.Helpers.Extensions;

namespace YARG.Gameplay.Player
{
    public class ProKeysPlayer : TrackPlayer<ProKeysEngine, ProKeysNote>
    {
        public struct RangeShift
        {
            public double Time;
            public double TimeLength;

            public uint Tick;
            public uint TickLength;

            public int Key;
        }

        public struct RangeShiftIndicator
        {
            public double Time;
            public bool LeftSide;
        }

        public const int WHITE_KEY_VISIBLE_COUNT = 10;
        public const int TOTAL_KEY_COUNT = 25;

        private const int SHIFT_INDICATOR_MEASURES_BEFORE = 4;

        public override float[] StarMultiplierThresholds { get; protected set; } =
        {
            0.21f, 0.46f, 0.77f, 1.85f, 3.08f, 4.52f
        };

        public override int[] StarScoreThresholds { get; protected set; }

        public ProKeysEngineParameters EngineParams { get; private set; }

        public override bool ShouldUpdateInputsOnResume => true;

        public float RangeShiftOffset => _currentOffset;

        [Header("Pro Keys Specific")]
        [SerializeField]
        private KeysArray _keysArray;
        [SerializeField]
        private ProKeysTrackOverlay _trackOverlay;
        [SerializeField]
        private Pool _shiftIndicatorPool;
        [SerializeField]
        private KeyedPool _chordBarPool;

        private List<RangeShift> _rangeShifts;
        private readonly List<RangeShiftIndicator> _shiftIndicators = new();

        private int _rangeShiftIndex;
        private int _shiftIndicatorIndex;

        private bool _isOffsetChanging;

        private double _offsetStartTime;
        private double _offsetEndTime;

        private float _previousOffset;
        private float _currentOffset;
        private float _targetOffset;

        protected override InstrumentDifficulty<ProKeysNote> GetNotes(SongChart chart)
        {
            var track = chart.ProKeys.Clone();
            return track.GetDifficulty(Player.Profile.CurrentDifficulty);
        }

        protected override ProKeysEngine CreateEngine()
        {
            if (GameManager.ReplayInfo == null)
            {
                // Create the engine params from the engine preset
                EngineParams = Player.EnginePreset.ProKeys.Create(StarMultiplierThresholds);
            }
            else
            {
                // Otherwise, get from the replay
                EngineParams = (ProKeysEngineParameters) Player.EngineParameterOverride;
            }

            var engine = new YargProKeysEngine(NoteTrack, SyncTrack, EngineParams, Player.Profile.IsBot);
            EngineContainer = GameManager.EngineManager.Register(engine, NoteTrack.Instrument, Chart);

            HitWindow = EngineParams.HitWindow;

            YargLogger.LogFormatDebug("Note count: {0}", NoteTrack.Notes.Count);

            engine.OnNoteHit += OnNoteHit;
            engine.OnNoteMissed += OnNoteMissed;
            engine.OnOverhit += OnOverhit;

            engine.OnSustainStart += OnSustainStart;
            engine.OnSustainEnd += OnSustainEnd;

            engine.OnSoloStart += OnSoloStart;
            engine.OnSoloEnd += OnSoloEnd;

            engine.OnStarPowerPhraseHit += OnStarPowerPhraseHit;
            engine.OnStarPowerStatus += OnStarPowerStatus;

            engine.OnKeyStateChange += OnKeyStateChange;

            engine.OnCountdownChange += OnCountdownChange;

            return engine;
        }

        protected override void FinishInitialization()
        {
            base.FinishInitialization();

            GetRangeShifts();

            _keysArray.Initialize(this, Player.ThemePreset, Player.ColorProfile.ProKeys);
            _trackOverlay.Initialize(this, Player.ColorProfile.ProKeys);

            if (_rangeShifts.Count > 0)
            {
                RangeShiftTo(_rangeShifts[0].Key, 0);
                _rangeShiftIndex++;
            }

            LaneElement.DefineLaneScale(Player.Profile.CurrentInstrument, WHITE_KEY_VISIBLE_COUNT);

            // Everybody else has this in Initialize(), but it should be fine in FinishInitialization()
            // BRELanes = new List<LaneElement>(new LaneElement[WHITE_KEY_VISIBLE_COUNT]);
            // We happen to know this is 3 for the test chart, so use that for now
            // TODO: Figure this out on the fly
            BRELanes = new List<LaneElement>(new LaneElement[3]);
        }

        public override void ResetPracticeSection()
        {
            base.ResetPracticeSection();

            _rangeShiftIndex = 0;
            _shiftIndicatorIndex = 0;

            if (_rangeShifts.Count > 0)
            {
                RangeShiftTo(_rangeShifts[0].Key, 0);
                _rangeShiftIndex++;
            }
        }

        public override void SetPracticeSection(uint start, uint end)
        {
            base.SetPracticeSection(start, end);

            GetRangeShifts();

            // This should never happen unless the chart has no range shifts, which is just bad charting
            if (_rangeShifts.Count == 0)
            {
                YargLogger.LogWarning("No range shifts found in chart. Defaulting to 0.");
                RangeShiftTo(0, 0);
                _rangeShiftIndex++;

                return;
            }

            _rangeShiftIndex = 0;
            _shiftIndicatorIndex = 0;

            int startIndex = _rangeShifts.FindIndex(r => r.Tick >= start);

            // No range shifts were >= start, so get the one prior.
            if(startIndex == -1)
            {
                startIndex = _rangeShifts.FindLastIndex(r => r.Tick < start);
            }

            // If the range shift is not on the starting tick, get the one before it.
            // This is so that the correct range is used at the start of the section.
            if (_rangeShifts[startIndex].Tick > start && startIndex > 0)
            {
                // Only get the previous range shift if there are notes before the current first range shift.
                // If there are no notes, we can just automatically shift to it at the start of the section like in Quickplay.
                if (Notes.Count > 0 && Notes[0].Tick < _rangeShifts[startIndex].Tick)
                {
                    startIndex--;
                }
            }

            int endIndex = _rangeShifts.FindIndex(r => r.Tick >= end);
            if (endIndex == -1)
            {
                endIndex = _rangeShifts.Count;
            }

            _rangeShifts = _rangeShifts.GetRange(startIndex, endIndex - startIndex);

            if (_rangeShifts.Count > 0)
            {
                RangeShiftTo(_rangeShifts[0].Key, 0);
                _rangeShiftIndex++;
            }
        }

        protected override void OnNoteHit(int index, ProKeysNote note)
        {
            base.OnNoteHit(index, note);

            if (GameManager.Paused) return;

            (NotePool.GetByKey(note) as ProKeysNoteElement)?.HitNote();
            _keysArray.PlayHitAnimation(note.Key);

            // Chord bars are spawned based on the parent element
            var parent = note.ParentOrSelf;
            (_chordBarPool.GetByKey(parent) as ProKeysChordBarElement)?.CheckForChordHit();
        }

        protected override void OnNoteMissed(int index, ProKeysNote chordParent)
        {
            base.OnNoteMissed(index, chordParent);

            (NotePool.GetByKey(chordParent) as ProKeysNoteElement)?.MissNote();
        }

        private void OnOverhit(int key)
        {
            base.OnOverhit();

            _keysArray.PlayMissAnimation(key);
        }

        private void OnSustainStart(ProKeysNote parent)
        {

        }

        private void OnSustainEnd(ProKeysNote parent, double timeEnded, bool finished)
        {
            (NotePool.GetByKey(parent) as ProKeysNoteElement)?.SustainEnd(finished);

            // Mute the stem if you let go of the sustain too early.
            // Leniency is handled by the engine's sustain burst threshold.
            if (!finished)
            {
                // Do we want to check if its part of a chord, and if so, if all sustains were dropped to mute?
                SetStemMuteState(true);
            }
        }

        private void OnKeyStateChange(int key, bool isPressed)
        {
            _trackOverlay.SetKeyHeld(key, isPressed);
            _keysArray.SetPressed(key, isPressed);
        }

        private void RangeShiftTo(int noteIndex, double timeLength)
        {
            _isOffsetChanging = true;

            _offsetStartTime = GameManager.RealVisualTime;
            _offsetEndTime = GameManager.RealVisualTime + timeLength;

            _previousOffset = _currentOffset;

            // We need to get the offset relative to the 0th key (as that's the base)
            _targetOffset = _keysArray.GetKeyX(0) - _keysArray.GetKeyX(noteIndex);
        }

        public float GetNoteX(int index)
        {
            return _keysArray.GetKeyX(index) + _currentOffset;
        }

        protected override void UpdateVisuals(double songTime)
        {
            UpdateBaseVisuals(Engine.EngineStats, EngineParams, songTime);
            UpdatePhrases(songTime);

            if (_isOffsetChanging)
            {
                float changePercent = (float) YargMath.InverseLerpD(_offsetStartTime, _offsetEndTime,
                    GameManager.RealVisualTime);

                // Because the range shift is called when resetting practice mode, the start time
                // will be that of the previous section causing the real time to be less than the start time.
                // In that case, just complete the range shift immediately.
                if (GameManager.RealVisualTime < _offsetStartTime)
                {
                    changePercent = 1f;
                }

                if (changePercent >= 1f)
                {
                    // If the change has finished, stop!
                    _isOffsetChanging = false;
                    _currentOffset = _targetOffset;
                }
                else
                {
                    _currentOffset = Mathf.Lerp(_previousOffset, _targetOffset, changePercent);
                }

                // Update the visuals with the new offsets

                var keysTransform = _keysArray.transform;
                keysTransform.localPosition = keysTransform.localPosition.WithX(_currentOffset);

                var overlayTransform = _trackOverlay.transform;
                overlayTransform.localPosition = overlayTransform.localPosition.WithX(_currentOffset);

                foreach (var note in NotePool.AllSpawned)
                {
                    (note as ProKeysNoteElement)?.UpdateXPosition();
                }

                foreach (var lane in LanePool.AllSpawned)
                {
                    (lane as LaneElement)?.OffsetXPosition(_currentOffset);
                }

                foreach (var bar in _chordBarPool.AllSpawned)
                {
                    (bar as ProKeysChordBarElement)?.UpdateXPosition();
                }
            }
        }

        protected override void ResetVisuals()
        {
            base.ResetVisuals();

            _chordBarPool.ReturnAllObjects();
        }

        private void UpdatePhrases(double songTime)
        {
            while (_rangeShiftIndex < _rangeShifts.Count && _rangeShifts[_rangeShiftIndex].Time <= songTime)
            {
                var rangeShift = _rangeShifts[_rangeShiftIndex];

                const double rangeShiftTime = 0.25;
                RangeShiftTo(rangeShift.Key, rangeShiftTime);

                _rangeShiftIndex++;
            }

            while (_shiftIndicatorIndex < _shiftIndicators.Count
                && _shiftIndicators[_shiftIndicatorIndex].Time <= songTime + SpawnTimeOffset)
            {
                var shiftIndicator = _shiftIndicators[_shiftIndicatorIndex];

                // Skip this frame if the pool is full
                if (!_shiftIndicatorPool.CanSpawnAmount(1))
                {
                    break;
                }

                var poolable = _shiftIndicatorPool.TakeWithoutEnabling();
                if (poolable == null)
                {
                    YargLogger.LogWarning("Attempted to spawn shift indicator, but it's at its cap!");
                    break;
                }

                YargLogger.LogDebug("Shift indicator spawned!");

                ((ProKeysShiftIndicatorElement) poolable).RangeShiftIndicator = shiftIndicator;
                poolable.EnableFromPool();

                _shiftIndicatorIndex++;
            }
        }

        public override void SetStemMuteState(bool muted)
        {
            if (IsStemMuted != muted)
            {
                GameManager.ChangeStemMuteState(SongStem.Keys, muted);
                IsStemMuted = muted;
            }
        }

        protected override void InitializeSpawnedNote(IPoolable poolable, ProKeysNote note)
        {
            ((ProKeysNoteElement) poolable).NoteRef = note;
        }

        private enum ColorGroup
        {
            Red,
            Yellow,
            Blue,
            Green,
            Orange
        }

        private struct Lanes
        {
            public int Count;
            public int Key;
        }

        protected override void InitializeBRELane(LaneElement lane, int laneIndex)
        {
            int[] redGroup =
            {
                0,
                // 1,
                2,
                // 3,
                4
            };

            int[] yellowGroup =
            {
                5,
                // 6,
                7,
                // 8,
                9,
                // 10,
                11
            };

            int[] blueGroup =
            {
                12,
                // 13,
                14,
                // 15,
                16
            };

            int[] greenGroup =
            {
                17,
                // 18,
                19,
                // 20,
                21,
                // 22,
                23
            };

            int[] orangeGroup =
            {
                24
            };

            // This takes an actual key, not a lane index
            int[] GetColorGroup(int key)
            {
                int noteIndex = key % 12;
                int octaveIndex = key / 12;
                int group = octaveIndex * 2 + (ProKeysUtilities.IsLowerHalfKey(noteIndex) ? 0 : 1);

                return group switch
                {
                    0 => redGroup,
                    1 => yellowGroup,
                    2 => blueGroup,
                    3 => greenGroup,
                    4 => orangeGroup,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            // All of the possible white keys
            int[] whiteKeys = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19, 21, 23, 24 };

            // We need to adjust "key" here to ensure that it is a value that is
            // on the currently displayed part of the track
            // This will have something to do with _currentOffset, I think?

            laneIndex -= 1; // We need to be zero index, but because all the other players are 1 indexed we have to make this adjustment here
            int totalKeys = 18; // There are 18 keys shown at any given time

            int currentRange = laneIndex; // An absurd default, but we need something

            // Get the current range
            if (_rangeShifts.Count > 0)
            {
                currentRange = _rangeShifts[Math.Min(_rangeShiftIndex, _rangeShifts.Count - 1)].Key;
            }

            var lowColorGroup = GetColorGroup(currentRange);
            var lowColorFraction = (lowColorGroup.Length - Array.IndexOf(lowColorGroup, currentRange)) / (float) lowColorGroup.Length;
            int lowColorIndex = Array.IndexOf(lowColorGroup, currentRange);
            int lowColorCount = lowColorGroup.Length - lowColorIndex - 1;

            // This is not the way, but it should work for now
            // Of course, this means BRELanes will need to be a list instead of an array since Pro Keys could have
            // either 3 or 4 groups rather than a fixed number of lanes
            // TODO: We really shouldn't be calculating this for every lane index, but whatever


            List<Lanes> laneCounts = new();
            // int keys = 0;
            int keys = Array.IndexOf(whiteKeys, currentRange);
            int keyCount = 0;
            for (int i = 0; i < BRELanes.Count; i++)
            {
                var colorGroup = GetColorGroup(whiteKeys[keys]);
                int colorIndex = Array.IndexOf(colorGroup, whiteKeys[keys]);
                int colorCount = colorGroup.Length - colorIndex;

                Lanes newLane = new();
                newLane.Count = (keyCount + colorCount) <= WHITE_KEY_VISIBLE_COUNT ? colorCount : WHITE_KEY_VISIBLE_COUNT - keyCount;
                newLane.Key = colorGroup[0];
                laneCounts.Add(newLane);

                // This should handle the case where the rightmost group isn't fully visible
                keyCount += colorCount;
                keys = (keys + colorCount) <= WHITE_KEY_VISIBLE_COUNT ? keys + colorCount : WHITE_KEY_VISIBLE_COUNT - keys;
            }

            // So now we just need to figure out the correct lane scale for a single key and do the obvious multiplication
            // It turns out that as long as DefineLaneScale was called with 10, we can just set the x scale to n for n white keys of required width
            // There's all kinds of weirdness in the child object scales, but it doesn't matter for our purposes,
            // we can just set the scale of the parent and it's fine

            // Not actually fine, because the endcaps are not scaled properly

            var whiteKeyWidth = _keysArray.GetKeyX(24) - _keysArray.GetKeyX(23);
            float rightX = -1;
            for (int i = 0; i < laneCounts.Count; i++)
            {
                rightX += laneCounts[i].Count * whiteKeyWidth;
                if (i == laneIndex)
                {
                    break;
                }
            }

            // rightX now has the x position of the rightmost visible key in the group
            float laneX = rightX - ((laneCounts[laneIndex].Count * whiteKeyWidth) / 2);

            // laneX now has the x position of the center of the lane

            int laneScale = laneCounts[laneIndex].Count;

            // laneScale now has the number of keys in the lane, which is the X scale we need to set on the parent object

            // The problem we now have is figuring out which color this laneIndex should take...
            // I guess laneCounts actually needs to hold a count and a color (actually a group index, 0 = red, 1 = yellow, etc)

            var anOctave = laneCounts[laneIndex].Key / 12;
            var aGroup = anOctave * 2 + (ProKeysUtilities.IsLowerHalfKey(laneCounts[laneIndex].Key % 12) ? 0 : 1);
            var laneColor = Player.ColorProfile.ProKeys.GetOverlayColor(aGroup);

            // Maybe this will work?
            LaneElement.DefineLaneScale(Player.Profile.CurrentInstrument, (int) Math.Floor((double) WHITE_KEY_VISIBLE_COUNT / laneScale), true);
            lane.SetAppearance(Player.Profile.CurrentInstrument, laneCounts[laneIndex].Key, laneX, laneColor.ToUnityColor());
            // Shrink the scale slightly to prevent clipping
            lane.MultiplyScale(0.9f);
            // lane.transform.localScale.WithX(laneScale);

            return;

            // Just as a test
            int GetWhiteKeyIndex(int index)
            {
                // Now we need to scale based on BRELanes.Length. When at its maximum of 10, we're creating one
                // lane per white key, but if it were 5, we'd want to distribute them evenly across the keys,
                // so (for example, if currentRange was 5), we would want lanes on keys 7, 11, 14, 17, and 19
                // (not really, but that's the best we can do)
                // At range 5 the keys on screen are 5, 7, 9, 11, 12, 14, 16, 17, 19, 21, so if there were 3 lanes,
                // we would want them at 12 or 14 for the middle one, then 11 and 17 or 19 (really we'd want them
                // on the black keys 10, 15, and 20 in this case, I guess? (21 - 5) / 3 * index

                // This would be so much easier if we could go backwards from a track relative x pos to the closest
                // white key

                // Ideally, they'd actually be dynamically sized based on the visible portion of each group, so let's
                // try to work that out...

                // We are guaranteed that 23 and 24 will always be positive X, so we'll use that to get the distance between keys
                // float whiteKeyDistance = _keysArray.GetKeyX(24) - _keysArray.GetKeyX(23);

                // Sadly, there could be 3 or 4 groups visible depending on position
                // We do, however, know that either blue or yellow will always be on the left since range shifts
                // only go up to 9.

                int idx;

                // There is probably a way to do this without a loop, but we'll have to live with it for now
                for (idx = 0; idx < whiteKeys.Length; idx++)
                {
                    if (whiteKeys[idx] == currentRange)
                    {
                        break;
                    }
                }

                // idx now contains the index of the start of the current range
                // Therefore, idx + index should return the white key for the given index
                return whiteKeys[index + idx];
            }

            // I believe that currentRange should contain the index of the lowest key in the current range
            // We need to put the lanes on each of the white keys in the range, but for testing what we'll do
            // is just have n lanes spread across the track and use the group color of the key that corresponds to
            // the center of the lane

            // what we really want is for the lanes to cover whatever groups are currently visible

            // Limit the number of spawned visual lanes to 12 in any case
            var laneCount = Math.Min(BRELanes.Count, WHITE_KEY_VISIBLE_COUNT);

            int adjustedKey = GetWhiteKeyIndex(laneIndex); // + currentRange;

            int noteIndex = adjustedKey % 12;
            int octaveIndex = adjustedKey / 12;

            int group = octaveIndex * 2 + (ProKeysUtilities.IsLowerHalfKey(noteIndex) ? 0 : 1);

            // I believe x position for the visible part of the track ranges from -1 to +1
            // so if there were 3 lanes, we would have a total of 6 units to cover, with the middle lane being at 0
            // and the first and last lanes being at -2/3 and + 2/3 respectively.
            laneIndex %= laneCount;
            float laneXPosition = -1f + (1f / laneCount) + (2f / laneCount) * laneIndex;

            var keyX = _keysArray.GetKeyX(adjustedKey);
            var color = Player.ColorProfile.ProKeys.GetOverlayColor(group).ToUnityColor();
            var realX = _keysArray.GetKeyX(adjustedKey) + _currentOffset;

            // The overlay color here is wrong, but whatever, this will suffice for testing
            lane.SetAppearance(Player.Profile.CurrentInstrument, adjustedKey, realX, Player.ColorProfile.ProKeys.GetOverlayColor(group).ToUnityColor());
        }

        protected override void InitializeSpawnedLane(LaneElement lane, int key)
        {
            int noteIndex = key % 12;
            int octaveIndex = key / 12;

            // Get the group index (two groups per octave)
            int group = octaveIndex * 2 + (ProKeysUtilities.IsLowerHalfKey(noteIndex) ? 0 : 1);

            lane.SetAppearance(Player.Profile.CurrentInstrument, key, _keysArray.GetKeyX(key), Player.ColorProfile.ProKeys.GetOverlayColor(group).ToUnityColor());
            lane.OffsetXPosition(_currentOffset);
        }

        protected override void ModifyLaneFromNote(LaneElement lane, ProKeysNote note)
        {
            if (note.IsTrill && note.NextNote != null)
            {
                // Trills between adjacent white and black keys should have a single, wider lane
                int leftKey = Math.Min(note.Key, note.NextNote.Key);
                int rightKey = Math.Max(note.Key, note.NextNote.Key);

                bool keysAreSameType = ProKeysUtilities.IsBlackKey(leftKey % 12) == ProKeysUtilities.IsBlackKey(rightKey % 12);

                if (!keysAreSameType && rightKey - leftKey == 1)
                {
                    lane.SetIndexRange(leftKey, rightKey);

                    float leftKeyPosition = GetNoteX(leftKey);
                    lane.SetXPosition(leftKeyPosition + (GetNoteX(rightKey) - leftKeyPosition)/2);
                    lane.MultiplyScale(1.75f);

                    return;
                }
                else if (keysAreSameType && rightKey - leftKey <= 2)
                {
                    // Lanes have enough space to be separate, but are still touching, adjust size to prevent clipping
                    lane.MultiplyScale(0.9f);
                }
            }

            if (ProKeysUtilities.IsWhiteKey(note.Key % 12))
            {
                // White notes are slightly wider than the lane
                lane.MultiplyScale(1.25f);
            }
        }

        protected override void OnNoteSpawned(ProKeysNote parentNote)
        {
            base.OnNoteSpawned(parentNote);

            if (parentNote.WasHit || parentNote.ChildNotes.Count <= 0)
            {
                return;
            }

            if (!_chordBarPool.CanSpawnAmount(1))
            {
                return;
            }

            var poolable = _chordBarPool.KeyedTakeWithoutEnabling(parentNote);
            if (poolable == null)
            {
                YargLogger.LogWarning("Attempted to spawn shift indicator, but it's at its cap!");
                return;
            }

            ((ProKeysChordBarElement) poolable).NoteRef = parentNote;
            poolable.EnableFromPool();
        }

        protected override void RescaleLanesForBRE()
        {
            // LaneElement.DefineLaneScale(Player.Profile.CurrentInstrument, BRELanes.Length, true);
            // LaneElement.DefineLaneScale(Player.Profile.CurrentInstrument, WHITE_KEY_VISIBLE_COUNT, true);
        }

        protected override bool InterceptInput(ref GameInput input)
        {
            var action = input.GetAction<ProKeysAction>();

            // Ignore SP in practice mode
            if (action == ProKeysAction.StarPower && GameManager.IsPractice) return true;

            return false;
        }

        private void GetRangeShifts()
        {
            // Get the range shifts from the phrases

            _rangeShifts = NoteTrack.Phrases
                .Where(phrase => phrase.Type is >= PhraseType.ProKeys_RangeShift0 and <= PhraseType.ProKeys_RangeShift5)
                .Select(phrase =>
                {
                    return new RangeShift
                    {
                        Time = phrase.Time,
                        TimeLength = phrase.TimeLength,

                        Tick = phrase.Tick,
                        TickLength = phrase.TickLength,

                        Key = phrase.Type switch
                        {
                            PhraseType.ProKeys_RangeShift0 => 0,
                            PhraseType.ProKeys_RangeShift1 => 2,
                            PhraseType.ProKeys_RangeShift2 => 4,
                            PhraseType.ProKeys_RangeShift3 => 5,
                            PhraseType.ProKeys_RangeShift4 => 7,
                            PhraseType.ProKeys_RangeShift5 => 9,
                            _                              => throw new Exception("Unreachable")
                        }
                    };
                })
                .ToList();

            // Get the range shift change indicator times based on the strong beatlines

            var beatlines = Beatlines
                .Where(i => i.Type is BeatlineType.Measure or BeatlineType.Strong)
                .ToList();

            _shiftIndicators.Clear();
            int lastShiftKey = 0;
            int beatlineIndex = 0;

            foreach (var shift in _rangeShifts)
            {
                if (shift.Key == lastShiftKey)
                {
                    continue;
                }

                var shiftLeft = shift.Key > lastShiftKey;
                lastShiftKey = shift.Key;

                // Look for the closest beatline index. Since the range shifts are
                // in order, we can just continuously look for the correct beatline
                for (; beatlineIndex < beatlines.Count; beatlineIndex++)
                {
                    if (beatlines[beatlineIndex].Time > shift.Time)
                    {
                        break;
                    }
                }

                // Add the indicators before the range shift
                for (int i = SHIFT_INDICATOR_MEASURES_BEFORE; i >= 1; i--)
                {
                    var realIndex = beatlineIndex - i;

                    // If the indicator is before any measures, skip
                    if (realIndex < 0)
                    {
                        break;
                    }

                    _shiftIndicators.Add(new RangeShiftIndicator
                    {
                        Time = beatlines[realIndex].Time,
                        LeftSide = shiftLeft
                    });
                }
            }
        }

        public override (ReplayFrame Frame, ReplayStats Stats) ConstructReplayData()
        {
            var frame = new ReplayFrame(Player.Profile, EngineParams, Engine.EngineStats, ReplayInputs.ToArray());
            return (frame, Engine.EngineStats.ConstructReplayStats(Player.Profile.Name));
        }
    }
}