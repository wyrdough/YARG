using System;
using System.Collections;
using Cysharp.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YARG.Core.Engine;
using YARG.Localization;

namespace YARG.Gameplay.HUD
{
    public class BREBox : MonoBehaviour
    {
        [SerializeField]
        private Image           _breBox;
        [SerializeField]
        private TextMeshProUGUI _breTopText;
        [SerializeField]
        private TextMeshProUGUI _breBottomText;
        [SerializeField]
        private TextMeshProUGUI _breFullText;
        [SerializeField]
        private CanvasGroup     _breBoxCanvasGroup;

        [Space]
        [SerializeField]
        private Sprite _breSpriteNormal;

        [SerializeField]
        private TMP_ColorGradient _breGradientNormal;

        private bool _breEnded;
        private CodaSection _coda;

        private bool _showingForPreview;

        // private int HitPercent => Mathf.FloorToInt((float) _solo.NotesHit / _solo.NoteCount * 100f);

        private Coroutine _currentCoroutine;

        public void StartCoda(CodaSection coda)
        {
            // Don't even bother if the solo has no points
            // if (coda.NoteCount == 0) return;

            _coda = coda;
            _breEnded = false;
            gameObject.SetActive(true);

            StopCurrentCoroutine();

            _currentCoroutine = StartCoroutine(ShowCoroutine());
        }

        private IEnumerator ShowCoroutine()
        {
            _breFullText.text = string.Empty;
            _breBox.sprite = _breSpriteNormal;

            // Set some dummy text
            _breTopText.text = string.Empty;
            _breBottomText.text = string.Empty;

            _breFullText.text = Localize.KeyFormat("Gameplay.Solo.PointsResult", _coda.TotalCodaBonus);

            // Fade in the box
            yield return _breBoxCanvasGroup
                .DOFade(1f, 0.25f)
                .WaitForCompletion();
        }

        private void Update()
        {
            if (_breEnded || _showingForPreview) return;

            _breFullText.text = Localize.KeyFormat("Gameplay.Solo.PointsResult", _coda.TotalCodaBonus);
            // _breTopText.SetTextFormat("{0}", _coda.TotalCodaBonus);
            // _breBottomText.SetTextFormat("{0}/{1}", _coda.NotesHit, _coda.NoteCount);
        }

        public void EndCoda(int soloBonus, Action endCallback)
        {
            StopCurrentCoroutine();

            _currentCoroutine = StartCoroutine(HideCoroutine(soloBonus, endCallback));
        }

        public void ForceReset()
        {
            StopCurrentCoroutine();
            _breEnded = true;

            _breBox.gameObject.SetActive(false);
            _currentCoroutine = null;
            _coda = null;
        }

        private IEnumerator HideCoroutine(int soloBonus, Action endCallback)
        {
            _breEnded = true;

            // Hide the top and bottom text
            _breTopText.text = string.Empty;
            _breBottomText.text = string.Empty;

            // Get the correct gradient and color
            // var (sprite, gradient) = HitPercent switch
            // {
            //     >= 100 => (_soloSpritePerfect, _soloGradientPerfect),
            //     >= 60  => (_soloSpriteNormal, _soloGradientNormal),
            //     _      => (_soloSpriteMessy, _soloGradientMessy),
            // };
            // _breBox.sprite = sprite;
            // _soloFullText.colorGradientPreset = gradient;

            // Display final hit percentage
            // _breFullText.SetTextFormat("{0}", _coda.TotalCodaBonus);
            //
            // yield return new WaitForSeconds(1f);

            // Show performance text
            // string performanceKey = HitPercent switch
            // {
            //     > 100 => "How",
            //       100 => "Perfect",
            //     >= 95 => "Awesome",
            //     >= 90 => "Great",
            //     >= 80 => "Good",
            //     >= 70 => "Solid",
            //        69 => "Nice",
            //     >= 60 => "Okay",
            //     >= 0  => "Messy",
            //     <  0  => "How",
            // };
            //
            // _breFullText.text = Localize.Key("Gameplay.Solo.Performance", performanceKey);

            yield return new WaitForSeconds(1f);

            // Show point bonus
            // TODO: Fix this to use BRE text
            _breFullText.text = Localize.KeyFormat("Gameplay.Solo.PointsResult", _coda.TotalCodaBonus);

            yield return new WaitForSeconds(1f);

            // Fade out the box
            yield return _breBoxCanvasGroup
                .DOFade(0f, 0.25f)
                .WaitForCompletion();

            _breBox.gameObject.SetActive(false);
            _currentCoroutine = null;
            _coda = null;

            endCallback?.Invoke();
        }

        private void StopCurrentCoroutine()
        {
            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
                _currentCoroutine = null;
            }
        }

        public void PreviewForEditMode(bool on)
        {
            if (on && !_breBox.gameObject.activeSelf)
            {
                _breBox.gameObject.SetActive(true);

                // Set preview solo box properties
                // TODO: Fix this to use BRE text instead of solo text
                _breFullText.text = string.Empty;
                _breBox.sprite = _breSpriteNormal;
                _breTopText.text = "50%";
                _breBottomText.text = "50/100";
                _breBoxCanvasGroup.alpha = 1f;

                _showingForPreview = true;
            }
            else if (!on && _showingForPreview)
            {
                _breBox.gameObject.SetActive(false);
                _showingForPreview = false;
            }
        }
    }
}