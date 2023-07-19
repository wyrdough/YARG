using System;
using System.Collections.Generic;
using PlasticBand.Devices;
using UnityEngine;
using UnityEngine.InputSystem;
using YARG.Core;
using YARG.Data;
using YARG.PlayMode;

namespace YARG.Player.Input
{
    public class RealGuitarInputStrategy : InputStrategy
    {
        [Flags]
        public enum StrumFlag
        {
            NONE = 0,
            STR_0 = 1,
            STR_1 = 2,
            STR_2 = 4,
            STR_3 = 8,
            STR_4 = 16,
            STR_5 = 32
        }

        public static StrumFlag StrumFlagFromInt(int str)
        {
            return (StrumFlag) (1 << str);
        }

        public delegate void FretChangeAction(int str, int fret);

        public delegate void StrumAction(StrumFlag strFlag);

        public event FretChangeAction FretChangeEvent;
        public event StrumAction StrumEvent;

        public delegate void WhammyChangeAction(float delta);
        public event WhammyChangeAction WhammyEvent;

        // private List<NoteInfo> botChart;

        private int[] fretCache = new int[ProGuitar.StringCount];
        private float[] velocityCache = new float[ProGuitar.StringCount];
        private float previousWhammy = 0f;

        private float? stringGroupingTimer = null;
        private StrumFlag stringGroupingFlag = StrumFlag.NONE;

        public RealGuitarInputStrategy(IReadOnlyList<InputDevice> inputDevices) : base(inputDevices)
        {
        }

        public override string GetIconName()
        {
            return "realGuitar";
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Group up strums
            if (stringGroupingTimer != null)
            {
                stringGroupingTimer -= Time.deltaTime;

                if (stringGroupingTimer <= 0f)
                {
                    StrumEvent?.Invoke(stringGroupingFlag);
                    stringGroupingFlag = StrumFlag.NONE;
                    stringGroupingTimer = null;

                    CallGenericCalbirationEvent();
                }
            }
        }

        protected override void UpdatePlayerMode()
        {
            // if (InputDevice is not ProGuitar input)
            // {
            //     return;
            // }
            //
            // // Update frets
            // for (int i = 0; i < ProGuitar.StringCount; i++)
            // {
            //     int fret = input.GetFret(i).ReadValue();
            //     if (fret != fretCache[i])
            //     {
            //         FretChangeEvent?.Invoke(i, fret);
            //         fretCache[i] = fret;
            //     }
            // }
            //
            // // Update strums
            // for (int i = 0; i < ProGuitar.StringCount; i++)
            // {
            //     float vel = input.GetVelocity(i).ReadValue();
            //     if (vel != velocityCache[i])
            //     {
            //         stringGroupingFlag |= StrumFlagFromInt(i);
            //         velocityCache[i] = vel;
            //
            //         // Start grouping if not already
            //         stringGroupingTimer ??= 0.05f;
            //     }
            // }
            //
            // // Constantly activate starpower (for now)
            // CallStarpowerEvent();
        }

        protected override void UpdateNavigationMode()
        {
            // TODO
        }

        public override Instrument[] GetAllowedInstruments()
        {
            return new Instrument[]
            {
                Instrument.ProGuitar_17Fret, Instrument.ProBass_17Fret,
            };
        }

        public override string GetTrackPath()
        {
            return "Tracks/RealGuitar";
        }
    }
}