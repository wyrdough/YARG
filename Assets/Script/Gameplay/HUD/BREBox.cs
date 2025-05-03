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
        private Sprite _breSpriteSuccess;
        [SerializeField]
        private Sprite _breSpriteFail;

        [SerializeField]
        private TMP_ColorGradient _breGradientNormal;
        [SerializeField]
        private TMP_ColorGradient _breGradientSuccess;
        [SerializeField]
        private TMP_ColorGradient _breGradientFail;

        private bool _breEnded;
        private CodaSection _coda;

        private bool _showingForPreview;

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

        public void EndCoda(int breBonus, Action endCallback)
        {
            StopCurrentCoroutine();

            _currentCoroutine = StartCoroutine(HideCoroutine(breBonus, endCallback));
        }

        public void ForceReset()
        {
            StopCurrentCoroutine();
            _breEnded = true;

            _breBox.gameObject.SetActive(false);
            _currentCoroutine = null;
            _coda = null;
        }

        private IEnumerator HideCoroutine(int breBonus, Action endCallback)
        {
            _breEnded = true;

            // Hide the top and bottom text
            _breTopText.text = string.Empty;
            _breBottomText.text = string.Empty;

            var (sprite, gradient) = _coda.Success switch
            {
                true  => (_breSpriteSuccess, _breGradientSuccess),
                false => (_breSpriteFail, _breGradientFail),
            };

            _breBox.sprite = sprite;
            _breFullText.colorGradientPreset = gradient;

            _breFullText.text = Localize.KeyFormat("Gameplay.Solo.PointsResult", breBonus);

            // Move the box so we aren't obscuring strong finish/full combo text
            _breBoxCanvasGroup.transform.DOMoveY(Screen.height / 2, 0.25f);

            // Go away sadly if BRE failed or triumphantly engorge if successful
            if (!_coda.Success)
            {
                _breBoxCanvasGroup.transform.DOScale(0.01f, 0.25f);
                _breBoxCanvasGroup.DOFade(0f, 0.25f).WaitForCompletion();
            }
            else
            {
                _breBoxCanvasGroup.transform.DOScale(1.5f, 0.25f);
                yield return new WaitForSeconds(2f);
                // Fade out the box
                yield return _breBoxCanvasGroup
                    .DOFade(0f, 0.25f)
                    .WaitForCompletion();
            }

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
                _breFullText.text = Localize.KeyFormat("Gameplay.Solo.PointsResult", 6969);
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