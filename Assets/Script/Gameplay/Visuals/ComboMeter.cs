﻿using Cysharp.Text;
using TMPro;
using UnityEngine;
using YARG.Core.Game;

namespace YARG.Gameplay.Visuals
{
    public class ComboMeter : MonoBehaviour
    {
        private static readonly int _spriteIndexProperty = Shader.PropertyToID("_SpriteIndex");
        private static readonly int _multiplierColorProperty = Shader.PropertyToID("_MultiplierColor");
        private static readonly int _customPresetColorProperty = Shader.PropertyToID("_CustomPresetColor");

        [SerializeField]
        private TextMeshPro _multiplierText;
        [SerializeField]
        private MeshRenderer _comboMesh;

        [Header("FC Ring")]
        [SerializeField]
        private MeshRenderer _ringMesh;

        [SerializeField]
        private Material _fcRingMaterial;
        [SerializeField]
        private Material _noFcRingMaterial;

        public void Initialize(EnginePreset preset)
        {
            // Skip if the preset is a default one
            if (EnginePreset.Defaults.Contains(preset)) return;

            var color = _comboMesh.material.GetColor(_customPresetColorProperty);
            _comboMesh.material.SetColor(_multiplierColorProperty, color);
        }

        public void SetCombo(int multiplier, int maxMultiplier, int combo, bool breMode = false)
        {
            // We don't show multiplier once BRE mode has started, per artist team
            if (multiplier != 1 && !breMode)
                _multiplierText.SetTextFormat("{0}<sub>x</sub>", multiplier);
            else
                _multiplierText.text = string.Empty;

            int index = combo % 10;
            if (combo != 0 && index == 0)
            {
                index = 10;
            }
            else if (multiplier == maxMultiplier)
            {
                index = 10;
            }

            _comboMesh.material.SetFloat(_spriteIndexProperty, index);
        }

        public void SetFullCombo(bool isFc)
        {
            _ringMesh.material = isFc ? _fcRingMaterial : _noFcRingMaterial;
        }
    }
}