using System;
using System.Collections.Generic;
using UnityEngine;
using YARG.Core.Game;
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
        private Vector3 _soloStart;
        private BaseElement _soloStartNote = null;
        private Vector3 _soloEnd;
        private BaseElement _soloEndNote = null;
        private float _noteSpeed;
        private float _soloState;

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

        public void Initialize(float fadePos, float fadeSize, HighwayPreset highwayPreset)
        {
            var soloStart = new Vector3(0.0f, 0.0f, -3.5f);
            var soloEnd = new Vector3(0.0f, 0.0f, 8.0f);
            // Set all fade values
            _material.SetFade(fadePos, fadeSize);
            foreach (var trimMat in _trimMaterials)
            {
                trimMat.SetFade(fadePos, fadeSize);
                // Some sane defaults that cover the entire highway
                // These should probably be calculated properly at some point
                trimMat.SetVector(_soloStartTrimProperty, soloStart);
                trimMat.SetVector(_soloEndTrimProperty, soloEnd);
            }

            _material.SetVector(_soloStartHighwayProperty, soloStart);
            _material.SetVector(_soloEndHighwayProperty, soloEnd);

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

        public void PrepareForSoloStart(BaseElement note)
        {
            // Note has just been spawned above the top of the highway
            _soloProcessingRequired = true;
            _soloStartNote = note;
            _soloStart = new Vector3(0.0f, 0.0f, _soloStartNote.transform.position.z);
            // We can't know the real end yet, so set it even further off the end of the highway
            // If the total length of the solo ends up being less than a highway length
            // PrepareForSoloEnd should have been called before the end note becomes visible
            _soloEnd = new Vector3(0.0f, 0.0f, 8.0f);

            // This has to be called before we turn on SoloMode
            UpdateSoloShader();

            // SoloMode has to be set before the solo actually starts so the visuals can scroll
            // before the start note hits the strike line
            SoloMode = true;
            SoloState = 1.0f;
        }
        public void PrepareForSoloEnd(BaseElement note) {
            _soloEndNote = note;
            // We don't mess with the start value here since it will naturally end up below the viewport anyway
            _soloEnd = new Vector3(0.0f, 0.0f, note.transform.position.z);
            var coords = _soloEndNote.transform.position;
            var logmsg = String.Format("Solo end note found. Coords are {0}, {1}, {2}.", coords.x, coords.y, coords.z);
        }

        public void UpdateSoloShader() {
            if(_soloStartNote)
            {
                _soloStart = new Vector3(0.0f, 0.0f, _soloStartNote.transform.position.z);
                _material.SetVector(_soloStartHighwayProperty, _soloStart);
                foreach(var trimMat in _trimMaterials)
                {
                    trimMat.SetVector(_soloStartTrimProperty, _soloStart);
                }
                // The note has done its job, forget about it
                // Strike line is -2, so note may go away after this
                // We immediately set z to -4 as a temporary expediency
                // in the future, this should scroll at note speed until it hits the bottom of the viewport
                if(_soloStart.z <= 3f)
                {
                    _soloStartNote = null;
                }
                if(_soloStart.z <= -1.8f)
                {
                    _soloStartNote = null;
                }
            } 
            else if(_soloStart.z > -3.5f)
            {
                _soloStart.z -= Time.deltaTime * _noteSpeed;
                _material.SetVector(_soloStartHighwayProperty, _soloStart);
                foreach(var trimMat in _trimMaterials)
                {
                    trimMat.SetVector(_soloStartTrimProperty, _soloStart);
                }
            }
            
            if(_soloEndNote)
            {
                _soloEnd = new Vector3(0.0f, 0.0f, _soloEndNote.transform.position.z);
                _material.SetVector(_soloEndHighwayProperty, _soloEnd);
                foreach(var trimMat in _trimMaterials)
                {
                    trimMat.SetVector(_soloEndTrimProperty, _soloEnd);
                }
            }            
        }

        public void OnSoloEnd() {
            // Solo ended. Make sure solo effects are off and positions are reset
            // FIXME: This will break if a solo end and solo start are too close together
            SoloMode = false;
            // This shouldn't be necessary because Update() should be handling it..
            SoloState = 0.0f;

            _soloProcessingRequired = false;
            _soloStartNote = null;
            _soloEndNote = null;
            _soloStart = new Vector3(0.0f, 0.0f, -3.5f);
            _soloEnd = new Vector3(0.0f, 0.0f, 8.0f);
            _material.SetVector(_soloStartHighwayProperty, _soloStart);
            _material.SetVector(_soloEndHighwayProperty, _soloEnd);
            foreach(var trimMat in _trimMaterials)
            {
                trimMat.SetVector(_soloStartTrimProperty, _soloStart);
                trimMat.SetVector(_soloEndTrimProperty, _soloEnd);
            }

        }
    }
}