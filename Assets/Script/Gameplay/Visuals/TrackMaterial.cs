using System;
using System.Collections.Generic;
using UnityEngine;
using YARG.Core.Game;
using YARG.Gameplay.Player;
using YARG.Helpers.Extensions;
using YARG.Settings;

namespace YARG.Gameplay.Visuals
{
    public class TrackMaterial : MonoBehaviour
    {
        // TODO: MOST OF THIS CLASS IS TEMPORARY UNTIL THE TRACK TEXTURE SETTINGS ARE IN

        private static readonly int _scrollProperty = Shader.PropertyToID("_Scroll");
        private static readonly int _starpowerStateProperty = Shader.PropertyToID("_Starpower_State");
        private static readonly int _wavinessProperty = Shader.PropertyToID("_Waviness");

        private static readonly int _layer1ColorProperty = Shader.PropertyToID("_Layer_1_Color");
        private static readonly int _layer2ColorProperty = Shader.PropertyToID("_Layer_2_Color");
        private static readonly int _layer3ColorProperty = Shader.PropertyToID("_Layer_3_Color");
        private static readonly int _layer4ColorProperty = Shader.PropertyToID("_Layer_4_Color");

        private static readonly int _soloStateProperty = Shader.PropertyToID("_Solo_State");
        private static readonly int _soloStartTrimProperty = Shader.PropertyToID("_Solo_Start");
        private static readonly int _soloEndTrimProperty = Shader.PropertyToID("_Solo_End");
        private static readonly int _soloStartHighwayProperty = Shader.PropertyToID("_Solo_Start1");
        private static readonly int _soloEndHighwayProperty = Shader.PropertyToID("_Solo_End1");

        private static readonly int _starPowerColorProperty = Shader.PropertyToID("_Starpower_Color");

        public struct Preset
        {
            public Color Layer1;
            public Color Layer2;
            public Color Layer3;
            public Color Layer4;

            public static Preset FromHighwayPreset(HighwayPreset preset, bool groove)
            {
                if (groove)
                {
                    return new Preset
                    {
                        Layer1 = preset.BackgroundGrooveBaseColor1.ToUnityColor(),
                        Layer2 = preset.BackgroundGrooveBaseColor2.ToUnityColor(),
                        Layer3 = preset.BackgroundGrooveBaseColor3.ToUnityColor(),
                        Layer4 = preset.BackgroundGroovePatternColor.ToUnityColor()
                    };
                }

                return new Preset
                {
                    Layer1 = preset.BackgroundBaseColor1.ToUnityColor(),
                    Layer2 = preset.BackgroundBaseColor2.ToUnityColor(),
                    Layer3 = preset.BackgroundBaseColor3.ToUnityColor(),
                    Layer4 = preset.BackgroundPatternColor.ToUnityColor()
                };
            }
        }

        private Preset _normalPreset;
        private Preset _groovePreset;

        private float _grooveState;
        private float GrooveState
        {
            get => _grooveState;
            set
            {
                _grooveState = value;

                _material.SetColor(_layer1ColorProperty,
                    Color.Lerp(_normalPreset.Layer1, _groovePreset.Layer1, value));
                _material.SetColor(_layer2ColorProperty,
                    Color.Lerp(_normalPreset.Layer2, _groovePreset.Layer2, value));
                _material.SetColor(_layer3ColorProperty,
                    Color.Lerp(_normalPreset.Layer3, _groovePreset.Layer3, value));
                _material.SetColor(_layer4ColorProperty,
                    Color.Lerp(_normalPreset.Layer4, _groovePreset.Layer4, value));

                _material.SetFloat(_wavinessProperty, value);
            }
        }

        [HideInInspector]
        public bool GrooveMode;
        [HideInInspector]
        public bool StarpowerMode;
        [HideInInspector]
        public bool SoloMode;

        private bool _soloProcessingRequired = false;
        // The values here are arbitrary, just outside the viewport and with end larger than start
        private Vector4 _soloStart = new Vector4(10.0f, 10.2f, 10.4f, 10.6f);
        private Vector4 _soloEnd = new Vector4(10.1f, 10.3f, 10.5f, 10.7f);
        private int _soloCount = 0;
        private float _noteSpeed;
        private float _soloState;

        private struct Solo
        {
            public Solo(float zStart, float zEnd, int slot)
            {
                // Could have done time here, but I feel like the material
                // doesn't really need to access the engine for RealVisualTime
                // Separation of concerns and all that
                StartZ = zStart;
                EndZ = zEnd;
                Slot = slot; // This should be negative if no slot is available at creation
            }
            public double StartZ { get; set; }
            public double EndZ { get; set; }
            public int Slot { get; set; }
        }

        // I would rather this be a Queue, but elements need to be updated sometimes
        private Queue<Solo> _solos = new();
        private Queue<int> _availableSoloSlots = new (new[] { 3, 2, 1, 0 });

        private GameManager _gameManager;

        public float SoloState
        {
            get => _soloState;
            set
            {
                _soloState = value;

                foreach (var material in _trimMaterials)
                {
                    material.SetFloat(_soloStateProperty, value);
                }

                _material.SetFloat(_soloStateProperty, value);
            }
        }

        public float StarpowerState
        {
            get => _material.GetFloat(_starpowerStateProperty);
            set => _material.SetFloat(_starpowerStateProperty, value);
        }

        [SerializeField]
        private MeshRenderer _trackMesh;

        [SerializeField]
        private MeshRenderer[] _trackTrims;

        private Material _material;
        private readonly List<Material> _trimMaterials = new();

        private void Awake()
        {
            // Get materials
            _material = _trackMesh.material;
            foreach (var trim in _trackTrims)
            {
                _trimMaterials.Add(trim.material);
            }

            _normalPreset = new()
            {
                Layer1 = FromHex("0F0F0F", 1f),
                Layer2 = FromHex("4B4B4B", 0.15f),
                Layer3 = FromHex("FFFFFF", 0f),
                Layer4 = FromHex("575757", 1f)
            };

            _groovePreset = new()
            {
                Layer1 = FromHex("000933", 1f),
                Layer2 = FromHex("23349C", 0.15f),
                Layer3 = FromHex("FFFFFF", 0f),
                Layer4 = FromHex("2C499E", 1f)
            };
        }

        public void Initialize(float fadePos, float fadeSize, HighwayPreset highwayPreset, GameManager gameManager = null)
        {
            _gameManager = gameManager;

            // Set all fade values
            _material.SetFade(fadePos, fadeSize);
            foreach (var trimMat in _trimMaterials)
            {
                trimMat.SetFade(fadePos, fadeSize);
            }

            _material.SetColor(_starPowerColorProperty, highwayPreset.StarPowerColor.ToUnityColor() );
            _normalPreset = Preset.FromHighwayPreset(highwayPreset, false);
            _groovePreset = Preset.FromHighwayPreset(highwayPreset, true);
        }

        private void Update()
        {
            if (GrooveMode)
            {
                GrooveState = Mathf.Lerp(GrooveState, 1f, Time.deltaTime * 5f);
            }
            else
            {
                GrooveState = Mathf.Lerp(GrooveState, 0f, Time.deltaTime * 3f);
            }

            if (StarpowerMode && SettingsManager.Settings.StarPowerHighwayFx.Value is StarPowerHighwayFxMode.On)
            {
                StarpowerState = Mathf.Lerp(StarpowerState, 1f, Time.deltaTime * 2f);
            }
            else
            {
                StarpowerState = Mathf.Lerp(StarpowerState, 0f, Time.deltaTime * 4f);
            }

            if (_soloProcessingRequired)
            {
                UpdateSoloShader();
            }
        }

        private static Color FromHex(string hex, float alpha)
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out var color))
            {
                color.a = alpha;
                return color;
            }

            throw new InvalidOperationException();
        }

        public void SetTrackScroll(double time, float noteSpeed)
        {
            float position = (float) time * noteSpeed / 4f;
            _material.SetFloat(_scrollProperty, position);
            _noteSpeed = noteSpeed;
        }

        // public void PrepareForSoloStart(BaseElement note)
        public void PrepareForSoloStart(float startZ, float endZ)
        {
            // Find an unused shader slot and add the new solo to the list
            // FIXME: We're not doing this yet. Still limiting to a single solo
            // per track length, just without knowing about notes.

            // Note has just been spawned above the top of the highway
            _soloProcessingRequired = true;
            _soloCount += 1;

            if (_availableSoloSlots.TryDequeue(out var slot))
            {
                _solos.Enqueue(new Solo(startZ, endZ, slot));
            }
            else
            {
                // No available slot, we'll assign it later when one frees up
                _solos.Enqueue(new Solo(startZ, endZ, -1));
            }

            if (slot < 0)
            {
                // If the new solo doesn't yet have a slot, best not continue
                return;
            }

            // Happily, Unity's VectorX types accept indexed access
            _soloStart[slot] = startZ;
            _soloEnd[slot] = endZ;

            // This has to be called before we turn on SoloMode
            UpdateSoloShader();

            // SoloMode has to be set before the solo actually starts so the visuals can scroll
            // before the start note hits the strike line
            SoloMode = true;
            // For some reason lerping the SoloState in Update() wasn't working right,
            // so we set it hard on here instead
            SoloState = 1.0f;
        }

        public void UpdateSoloShader()
        {
            var soloDone = false;
            foreach (var solo in _solos)
            {
                if (solo.Slot < 0)
                {
                    // Check to see if there is a free slot now
                    // Actually, this won't work since the z vals haven't been updated
                    // Too much solo density means some will get dropped,
                    // oh well, they're still scored
                    // if (_availableSoloSlots.TryDequeue(out var slot))
                    // {
                    //     solo.Slot = slot;
                    // }
                    continue;
                }
                // FIXME: This should be calculated from the strike line somehow, not a constant
                if(_soloStart[solo.Slot] > -3.5f)
                {
                    _soloStart[solo.Slot] -= Time.deltaTime * _noteSpeed;
                    _material.SetVector(_soloStartHighwayProperty, _soloStart);
                    foreach(var trimMat in _trimMaterials)
                    {
                        trimMat.SetVector(_soloStartTrimProperty, _soloStart);
                    }
                }
                else
                {
                    // Do we actually need to do anything here?
                }
                if (_soloEnd[solo.Slot] > -3.5f)
                {
                    _soloEnd[solo.Slot] -= Time.deltaTime * _noteSpeed;
                    _material.SetVector(_soloEndHighwayProperty, _soloEnd);
                    foreach (var trimMat in _trimMaterials)
                    {
                        trimMat.SetVector(_soloEndTrimProperty, _soloEnd);
                    }
                }
                else
                {
                    // We're done with this one, remove it from the solo queue
                    // and return the slot to the slot queue
                    _availableSoloSlots.Enqueue(solo.Slot);
                    soloDone = true;
                    _soloCount -= 1;
                    if (_soloCount >= 1) continue;
                    // We know of no more solos, so disable processing
                    SoloState = 0.0f;
                    _soloProcessingRequired = false;
                }
            }

            if (soloDone)
            {
                _solos.Dequeue();
            }
        }

        public void OnSoloEnd() {

        }
        public void SetSoloProcessing(bool state)
        {
            _soloProcessingRequired = state;
        }
    }
}