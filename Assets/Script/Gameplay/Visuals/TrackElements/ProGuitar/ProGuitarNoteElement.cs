﻿using Cysharp.Text;
using TMPro;
using UnityEngine;
using YARG.Core.Chart;
using YARG.Gameplay.Player;

namespace YARG.Gameplay.Visuals
{
    public sealed class ProGuitarNoteElement : TrackElement<ProGuitarPlayer>
    {
        public ProGuitarNote ChordRef { get; set; }

        public override double ElementTime => ChordRef.Time;

        [SerializeField]
        private TextMeshPro[] _textObjects;
        [SerializeField]
        private GameObject _chordMeshParent;

        protected override void InitializeElement()
        {
            _chordMeshParent.SetActive(true);

            foreach (var note in ChordRef.AllNotes)
            {
                _textObjects[note.String].gameObject.SetActive(true);
                _textObjects[note.String].text = ZString.Format("{0}", note.Fret);
            }
        }

        protected override void UpdateElement()
        {
        }

        protected override void HideElement()
        {
            _chordMeshParent.SetActive(false);

            foreach (var text in _textObjects)
            {
                text.gameObject.SetActive(false);
            }
        }

        public void HitNote()
        {
            HideElement();
        }
    }
}