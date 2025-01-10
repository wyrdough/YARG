﻿using System;
using System.Collections.Generic;
using YARG.Core;
using YARG.Core.Chart;
using YARG.Core.Logging;

namespace YARG.Gameplay.Visuals
{
    public enum TrackEffectType
    {
        Solo,
        Unison,
        SoloAndUnison,
        DrumFill,
        SoloAndDrumFill,
        DrumFillAndUnison,
    }

    // Small warning, TrackEffects are equal if their Time and TimeEnd values
    // match, regardless of whether or not they have different EffectType values
    public class TrackEffect : IComparable<TrackEffect>, IEquatable<TrackEffect>
    {
        public TrackEffect(double time, double timeEnd, TrackEffectType effectType,
            bool startTransitionEnable = true, bool endTransitionEnable = true)
        {
            Time = time;
            TimeEnd = timeEnd;
            EffectType = effectType;
            StartTransitionEnable = startTransitionEnable;
            EndTransitionEnable = endTransitionEnable;
        }

        // This is the scale of the transition object (currently 0.005) * 100
        // If the scale is changed or the object is changed from a plane this
        // will have to change
        private const float TRANSITION_SCALE = 0.5f;

        public double Time { get; private set; }
        public double TimeEnd { get; private set; }
        public readonly TrackEffectType EffectType;
        public bool StartTransitionEnable { get; private set; }
        public bool EndTransitionEnable { get; private set; }

        public bool Equals(TrackEffect other) => Time.Equals(other.Time) && TimeEnd.Equals(other.TimeEnd);
        public override bool Equals(object obj) => obj is TrackEffect && Equals((TrackEffect)obj);
        public override int GetHashCode() => HashCode.Combine(Time, TimeEnd);
        public int CompareTo(TrackEffect other) => Time.CompareTo(other.Time);

        public static bool operator ==(TrackEffect left, TrackEffect right) => left.Equals(right);
        public static bool operator !=(TrackEffect left, TrackEffect right) => !(left == right);
        public static bool operator <(TrackEffect left, TrackEffect right) => left.CompareTo(right) < 0;
        public static bool operator >(TrackEffect left, TrackEffect right) => left.CompareTo(right) > 0;
        public static bool operator <=(TrackEffect left, TrackEffect right) => left.CompareTo(right) <= 0;
        public static bool operator >=(TrackEffect left, TrackEffect right) => left.CompareTo(right) >= 0;

        public bool Overlaps(TrackEffect other) => !(Time < other.TimeEnd && other.Time >= TimeEnd);
        public bool Contains(TrackEffect other) => Time < other.Time && other.TimeEnd < TimeEnd;

        // Takes a list of track effects, sorts, slices, and dices, chops,
        // and blends in order to create just the right combination
        // of non-overlapping effects to delight and surprise users
        public static List<TrackEffect> SliceEffects(float noteSpeed, params List<TrackEffect>[] trackEffects)
        {
            // NOTE: This breaks if the size of the effect transition is changed
            // Multiplied by two since both effect objects have a transition
            var minTime = TRANSITION_SCALE * 2 / noteSpeed;
            // Combine all the lists we were given, then sort
            var fullEffectsList = new List<TrackEffect>();
            foreach (var effectList in trackEffects)
            {
                fullEffectsList.AddRange(effectList);
            }
            fullEffectsList.Sort();
            var slicedEffects = new List<TrackEffect>();
            var q = new Queue<TrackEffect>(fullEffectsList);

            while (q.TryDequeue(out var effect))
            {
                if (!q.TryPeek(out var nextEffect))
                {
                    // There is no next effect, so overlap is impossible
                    slicedEffects.Add(effect);
                    continue;
                }

                if (!effect.Overlaps(nextEffect))
                {
                    // There is no overlap, so no need to slice
                    // We do need to check for adjacency, though!
                    if (effect.TimeEnd == nextEffect.Time)
                    {
                        // There is adjacency, so disable the transitions
                        effect.EndTransitionEnable = false;
                        nextEffect.StartTransitionEnable = false;
                    }

                    if (nextEffect.Time - effect.TimeEnd < minTime)
                    {
                        // Too close, adjust them to be adjacent
                        var adjustedTime = ((nextEffect.Time - effect.TimeEnd) / 2) + effect.TimeEnd;
                        nextEffect.Time = adjustedTime;
                        effect.TimeEnd = adjustedTime;
                        effect.EndTransitionEnable = false;
                        nextEffect.StartTransitionEnable = false;
                    }
                    slicedEffects.Add(effect);
                    continue;
                }

                // We reached this point, so there is overlap
                if (effect.Contains(nextEffect))
                {
                    // Add segment of outer effect lasting until beginning of inner effect
                    // Disabled end transition is required
                    slicedEffects.Add(new TrackEffect(effect.Time, nextEffect.Time,
                        effect.EffectType, effect.StartTransitionEnable, false));
                    var newEffect = new TrackEffect(nextEffect.Time, nextEffect.TimeEnd,
                        GetEffectCombination(effect, nextEffect), false, nextEffect.EndTransitionEnable);
                    if (!(q.TryPeek(out var nextNextEffect) && newEffect.Contains(nextNextEffect)))
                    {
                        // Remainder of outer effect contains no more inner effects
                        slicedEffects.Add(newEffect);
                        slicedEffects.Add(new TrackEffect(newEffect.TimeEnd, effect.TimeEnd, effect.EffectType, false, effect.EndTransitionEnable));
                        q.Dequeue();
                        continue;
                    }

                    // There are more inner effects, so process them until we run out
                    while (q.TryPeek(out nextNextEffect) && effect.Contains(nextNextEffect))
                    {
                        if (newEffect.TimeEnd < nextNextEffect.Time)
                        {
                            // There is a gap, so fill it with outer effect
                            slicedEffects.Add(new TrackEffect(newEffect.TimeEnd, nextNextEffect.Time,
                                effect.EffectType, false, false));
                        }

                        // now the inner effect
                        newEffect = new TrackEffect(nextNextEffect.Time, nextNextEffect.TimeEnd,
                            GetEffectCombination(effect, nextNextEffect), false, false);
                        slicedEffects.Add(newEffect);
                        q.Dequeue();
                    }

                    continue;
                }
                // There is overlap, but next is not contained in current
                // Create three sections, current alone, combination, next alone
                slicedEffects.Add(new TrackEffect(effect.Time, nextEffect.Time, effect.EffectType,
                    effect.StartTransitionEnable, false));
                slicedEffects.Add(new TrackEffect(nextEffect.Time, effect.TimeEnd,
                    GetEffectCombination(effect, nextEffect), false, false));
                slicedEffects.Add(new TrackEffect(effect.TimeEnd, nextEffect.TimeEnd, nextEffect.EffectType, false,
                    nextEffect.EndTransitionEnable));
                q.Dequeue();
            }

            return slicedEffects;
        }

        static TrackEffectType GetEffectCombination(TrackEffect outer, TrackEffect inner)
        {
            TrackEffectType? combo = null;
            if (outer.EffectType == TrackEffectType.Solo)
            {
                combo = inner.EffectType switch
                {
                    TrackEffectType.Unison   => TrackEffectType.SoloAndUnison,
                    TrackEffectType.DrumFill => TrackEffectType.SoloAndDrumFill,
                    // If we don't know what else to do, just use the outer type
                    _ => null
                };
            }
            // I'm not sure if this is even chartable
            if (outer.EffectType == TrackEffectType.DrumFill)
            {
                combo = inner.EffectType switch
                {
                    TrackEffectType.Unison => TrackEffectType.DrumFillAndUnison,
                    _                      => null
                };
            }

            if (combo != null)
            {
                return (TrackEffectType) combo;
            }
            YargLogger.LogFormatWarning("Someone charted a {0} in a {1} and TrackEffect doesn't know how to combine that business",
                outer.EffectType, inner.EffectType);
            return outer.EffectType;
        }

        // Give me a list of chart phrases, I give you a list of corresponding track effects
        // I only ask that the phrases you give me are all the same kind
        public static List<TrackEffect> PhrasesToEffects(params List<Phrase>[] arrayOfPhraseLists)
        {
            var effects = new List<TrackEffect>();
            foreach (var phrases in arrayOfPhraseLists)
            {
                for (var i = 0; i < phrases.Count; i++)
                {
                    var type = phrases[i].Type;
                    TrackEffectType? kind = type switch
                    {
                        PhraseType.Solo      => TrackEffectType.Solo,
                        PhraseType.DrumFill  => TrackEffectType.DrumFill,
                        PhraseType.StarPower => TrackEffectType.Unison,
                        _                    => null
                    };
                    if (kind == null)
                    {
                        YargLogger.LogFormatWarning(
                            "TrackEffects given phrase type {0}, which has no corresponding effect", phrases[i].Type);
                        continue;
                    }

                    effects.Add(
                        new TrackEffect(phrases[i].Time, phrases[i].TimeEnd, (TrackEffectType) kind, true, true));
                }
            }
            return effects;
        }
        // A struct to make phrase comparison more readable
        public struct StarPowerSection : IEquatable<StarPowerSection>
        {
            public double Time;
            public double TimeEnd;
            public Phrase PhraseRef;

            public StarPowerSection(double time, double timeEnd, Phrase phrase)
            {
                Time = time;
                TimeEnd = timeEnd;
                PhraseRef = phrase;
            }

            public bool Equals(StarPowerSection other) => Time.Equals(other.Time) && TimeEnd.Equals(other.TimeEnd);

            public override bool Equals(object obj) => obj is StarPowerSection other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(Time, TimeEnd);
        }

        // TODO: Move this where it belongs, as this is just a proof of concept stuffed somewhere to see if it can work
        // This scans all instruments of a chart and builds unison phrases from the resulting star power phrases
        // Since unisons can be missing instruments, we also require the proposed instrument so we don't
        // return unison sections where the instrument isn't participating
        /// <summary>
        /// Builds unison phrases for a combination of instrument and chart
        /// </summary>
        /// <param name="instrument">YARG.Core.Instrument</param>
        /// <param name="chart">YARG.Core.Chart.SongChart</param>
        /// <returns>List of Phrase objects with Type == PhraseType.StarPower
        /// <br />These Phrases have corresponding StarPower Phrases in other tracks,
        /// <br />which is what makes them unison phrases.
        /// </returns>
        public static List<Phrase> GetUnisonPhrases(Instrument instrument, SongChart chart)
        {
            var phrases = new List<Phrase>();
            // the list we will compare against to find unisons
            var sourceSpSections = new List<StarPowerSection>();
            var othersSpSections = new List<StarPowerSection>();
            var outSpSections = new List<StarPowerSection>();
            // This list should have the instruments with duplicate SP phrases removed
            var acceptedSpSections = new List<List<StarPowerSection>>();

            var foundSelf = false;

            if(TryFindTrackForInstrument(instrument, chart.FiveFretTracks, out var fiveFretTrack))
            {
                var selfInstrumentDifficulty = GetAnyInstrumentDifficulty(fiveFretTrack);
                sourceSpSections = GetSpSectionsFromDifficulty(selfInstrumentDifficulty);
                foundSelf = true;
            }

            if (!foundSelf && TryFindTrackForInstrument(instrument, chart.DrumsTracks, out var drumsTrack))
            {
                var selfInstrumentDifficulty = GetAnyInstrumentDifficulty(drumsTrack);
                sourceSpSections = GetSpSectionsFromDifficulty(selfInstrumentDifficulty);
                foundSelf = true;
            }

            if (!foundSelf && TryFindTrackForInstrument(instrument, chart.SixFretTracks, out var sixFretTrack))
            {
                var selfInstrumentDifficulty = GetAnyInstrumentDifficulty(sixFretTrack);
                sourceSpSections = GetSpSectionsFromDifficulty(selfInstrumentDifficulty);
                foundSelf = true;
            }

            if (!foundSelf && TryFindTrackForInstrument(instrument, chart.ProGuitarTracks, out var proGuitarTrack))
            {
                var selfInstrumentDifficulty = GetAnyInstrumentDifficulty(proGuitarTrack);
                sourceSpSections = GetSpSectionsFromDifficulty(selfInstrumentDifficulty);
                foundSelf = true;
            }

            if (!foundSelf && chart.ProKeys.Instrument == instrument)
            {
                var selfInstrumentDifficulty = GetAnyInstrumentDifficulty(chart.ProKeys);
                sourceSpSections = GetSpSectionsFromDifficulty(selfInstrumentDifficulty);
                foundSelf = true;
            }

            if (!foundSelf)
            {
                YargLogger.LogFormatError("Could not find any instrument difficulty for {0}", instrument);
                return phrases;
            }

            // Add ourselves to the beginning of the accepted list so any dupes with us will be filtered
            acceptedSpSections.Add(sourceSpSections);

            GetSpSectionsFromCharts(chart.FiveFretTracks, ref acceptedSpSections, instrument);
            GetSpSectionsFromCharts(chart.SixFretTracks, ref acceptedSpSections, instrument);
            GetSpSectionsFromCharts(chart.DrumsTracks, ref acceptedSpSections, instrument);
            GetSpSectionsFromCharts(chart.ProGuitarTracks, ref acceptedSpSections, instrument);

            if (chart.ProKeys.Instrument != instrument)
            {
                var proKeysDifficulty = GetAnyInstrumentDifficulty(chart.ProKeys);
                var candidateSpSections = GetSpSectionsFromDifficulty(proKeysDifficulty);
                if (!SpListIsDuplicate(candidateSpSections, acceptedSpSections))
                {
                    acceptedSpSections.Add(candidateSpSections);
                }
            }

            // Now we delete self from the accepted list to ensure we don't match against self
            acceptedSpSections.Remove(sourceSpSections);

            // Unpack all the accepted sp sections into othersSpSections
            foreach (var section in acceptedSpSections)
            {
                othersSpSections.AddRange(section);
            }

            // Now that we have all the SP sections, compare them
            foreach (var section in othersSpSections)
            {
                if (sourceSpSections.Contains(section) && !outSpSections.Contains(section))
                {
                    outSpSections.Add(section);
                }
            }

            // Build the phrase list we actually want to return
            foreach (var section in outSpSections)
            {
                phrases.Add(section.PhraseRef);
            }

            return phrases;

            // Thus begins a parade of helper functions

            static bool TryFindTrackForInstrument<TNote>(Instrument instrument,
                IEnumerable<InstrumentTrack<TNote>> trackEnumerable, out InstrumentTrack<TNote> instrumentTrack) where TNote : Note<TNote>
            {
                foreach (var track in trackEnumerable)
                {
                    if (track.Instrument == instrument)
                    {
                        instrumentTrack = track;
                        return true;
                    }
                }

                instrumentTrack = null;
                return false;
            }

            // Gets the StarPower sections from a list of charts, excluding a specific instrument
            static void GetSpSectionsFromCharts<TNote>(IEnumerable<InstrumentTrack<TNote>> tracks,
                ref List<List<StarPowerSection>> acceptedSpSections,
                Instrument instrument) where TNote : Note<TNote>
            {
                foreach (var track in tracks)
                {
                    if (track.Instrument == instrument)
                    {
                        continue;
                    }
                    var instrumentDifficulty = GetAnyInstrumentDifficulty(track);
                    var candidateSpSections = GetSpSectionsFromDifficulty(instrumentDifficulty);
                    if (!SpListIsDuplicate(candidateSpSections, acceptedSpSections))
                    {
                        acceptedSpSections.Add(candidateSpSections);
                    }
                }
            }

            static List<StarPowerSection> GetSpSectionsFromDifficulty<TNote>(InstrumentDifficulty<TNote> difficulty) where TNote : Note<TNote>
            {
                var spSections = new List<StarPowerSection>();
                foreach (var phrase in difficulty.Phrases)
                {
                    if (phrase.Type == PhraseType.StarPower)
                    {
                        spSections.Add(new StarPowerSection(phrase.Time, phrase.TimeEnd, phrase));
                    }
                }
                return spSections;
            }

            static bool SpListIsDuplicate(List<StarPowerSection> proposed, List<List<StarPowerSection>> accepted)
            {
                foreach (var sections in accepted)
                {
                    if (proposed.Count != sections.Count)
                    {
                        continue;
                    }

                    // Count is the same, so it could be a dupe
                    var dupeCount = 0;
                    for (var i = 0; i < sections.Count; i++)
                    {
                        if (!proposed[i].Equals(sections[i]))
                        {
                            break;
                        }
                        dupeCount++;
                    }

                    if (dupeCount == sections.Count)
                    {
                        YargLogger.LogDebug("Found duplicate star power list");
                        return true;
                    }
                }
                return false;
            }

            static InstrumentDifficulty<TNote> GetAnyInstrumentDifficulty<TNote>(InstrumentTrack<TNote> instrumentTrack) where TNote : Note<TNote>
            {
                // We don't care what difficulty, so we return the first one we find
                foreach (var difficulty in Enum.GetValues(typeof(Difficulty)))
                {
                    if (instrumentTrack.TryGetDifficulty((Difficulty) difficulty, out var instrumentDifficulty))
                    {
                        return instrumentDifficulty;
                    }
                }

                return null;
            }
        }
    }
}