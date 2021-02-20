using System.Collections;
using System.Collections.Generic;
using AnKuchen.KuchenList;
using AnKuchen.Map;
using UnityEngine;
using UnityEngine.UI;

namespace AbcConsole.Internal
{
    public class Console : MonoBehaviour
    {
        private Root _root;
        private RectTransform _canvasRect;
        private AbcConsoleUiElements _ui;
        private int _logUpdatedCount;
        private int? _selectingLogId;
        private bool _forceUpdate;
        private float _updatedKeyboardHeight;

        private static readonly Color LogColor = new Color32(0, 0, 0, 0);
        private static readonly Color WarningColor = new Color32(255, 255, 0, 32);
        private static readonly Color ErrorColor = new Color32(255, 0, 0, 32);
        private static readonly Color ExceptionColor = new Color32(255, 0, 0, 32);
        private static readonly Color AssertColor = new Color32(255, 0, 0, 32);

        public void OnEnable()
        {
            Debug.Log("Console.OnEnable");

            if (_root == null)
            {
                _root = GetComponentInParent<Root>();
            }

            if (_canvasRect == null)
            {
                _canvasRect = GetComponentInParent<CanvasScaler>().GetComponent<RectTransform>();
            }

            if (_ui == null)
            {
                _ui = new AbcConsoleUiElements(GetComponentInParent<UICache>());
                _ui.EnterButton.onClick.AddListener(OnClickEnterButton);
                _ui.PasteButton.onClick.AddListener(OnClickPasteButton);
                _ui.InputField.onEndEdit.AddListener(_ => OnInputFieldEndEdit());
                _ui.InputField.onValidateInput += (text, index, addedChar) =>
                {
                    if (addedChar == '`') return '\0';
                    return addedChar;
                };
            }

            /*
            using (var editor = _ui.Autocomplete.Edit())
            {
                for (var i = 0; i < 5; ++i)
                {
                    var a = editor.Create();
                    a.Text.text = $"Autocomplete {i}";
                    a.Button.onClick.AddListener(() =>
                    {
                        _ui.InputField.text = a.Text.text;
                    });
                }
            }
            */

            _ui.InputField.Focus(this);
        }

        public void Update()
        {
            UpdateViewArea();
            UpdateLogs();
        }

        private void UpdateViewArea()
        {
            var keyboardHeight = KeyboardRect.GetHeight();
            if (Mathf.Abs(keyboardHeight - _updatedKeyboardHeight) < 0.0001f) return;

            _updatedKeyboardHeight = keyboardHeight;
            Debug.Log($"Update Height {_updatedKeyboardHeight}");

            var resolutionHeight = _canvasRect.sizeDelta.y;
            var rate = resolutionHeight / Screen.height;
            var margin = keyboardHeight * rate;
            _ui.ViewArea.sizeDelta = new Vector2(0, -margin);
        }

        private void UpdateLogs()
        {
            if (!_ui.LogRoot.activeSelf) return;
            if (!_forceUpdate && _logUpdatedCount == _root.LogCount) return;
            _logUpdatedCount = _root.LogCount;
            _forceUpdate = false;

            using (var editor = _ui.Log.Edit())
            {
                foreach (var log in _root.Logs)
                {
                    editor.Contents.Add(new UIFactory<AbcConsoleUiElements.LogLineUiElements, AbcConsoleUiElements.LogDetailUiElements>(x =>
                    {
                        var imageColor = Color.clear;
                        if (log.Type == LogType.Log) imageColor = LogColor;
                        else if (log.Type == LogType.Warning) imageColor = WarningColor;
                        else if (log.Type == LogType.Error) imageColor = ErrorColor;
                        else if (log.Type == LogType.Exception) imageColor = ExceptionColor;
                        else if (log.Type == LogType.Assert) imageColor = AssertColor;

                        x.Text.text = log.Condition;
                        x.Image.color = imageColor;

                        if (log.Condition.StartsWith("> "))
                        {
                            x.Button.onClick.AddListener(() =>
                            {
                                _ui.InputField.text = log.Condition.TrimStart('>').Trim();
                            });
                        }
                        else if (_selectingLogId == log.Id)
                        {
                            x.Button.onClick.AddListener(() =>
                            {
                                _selectingLogId = null;
                                _forceUpdate = true;
                            });
                        }
                        else
                        {
                            x.Button.onClick.AddListener(() =>
                            {
                                _selectingLogId = log.Id;
                                _forceUpdate = true;
                            });
                        }
                    }));

                    if (log.Id == _selectingLogId)
                    {
                        editor.Contents.Add(new UIFactory<AbcConsoleUiElements.LogLineUiElements, AbcConsoleUiElements.LogDetailUiElements>(x =>
                        {
                            x.CopyButton.onClick.AddListener(() =>
                            {
                                GUIUtility.systemCopyBuffer = $"{log.Condition}\n---\n{log.StackTrace}";
                                Debug.Log("DebugLog Copied!");
                                _selectingLogId = null;
                            });
                        }));
                    }
                }
            }
        }

        private void OnInputFieldEndEdit()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                OnClickEnterButton();
                _ui.InputField.Focus(this);
            }

            if (_ui.InputField.touchScreenKeyboard?.status == TouchScreenKeyboard.Status.Done)
            {
                OnClickEnterButton();
                _ui.InputField.Focus(this);
            }
        }

        public void OnClickEnterButton()
        {
            var text = _ui.InputField.text.Trim();
            _ui.InputField.text = "";
            if (string.IsNullOrWhiteSpace(text)) return;

            Debug.Log($"> {text}");
            _root.Executor.ExecuteMethod(text);
        }

        public void OnClickPasteButton()
        {
            _ui.InputField.text = GUIUtility.systemCopyBuffer;
        }
    }
}