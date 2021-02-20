using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AnKuchen.KuchenList;
using AnKuchen.Map;
using UnityEngine;

namespace AbcConsole.Internal
{
    public class Console : MonoBehaviour
    {
        private Root _root;
        private AbcConsoleUiElements _ui;
        private int _logUpdatedCount;
        private int? _selectingLogId;
        private bool _forceUpdate;

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

            if (_ui == null)
            {
                _ui = new AbcConsoleUiElements(GetComponentInParent<UICache>());
                _ui.EnterButton.onClick.AddListener(() => OnClickEnterButton());
                _ui.PasteButton.onClick.AddListener(() => OnClickPasteButton());
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
        }

        public void Update()
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

        public void OnClickEnterButton()
        {
            var text = _ui.InputField.text.Trim();
            _ui.InputField.text = "";
            if (string.IsNullOrWhiteSpace(text)) return;

            Debug.Log($"> {text}");

            var input = text.Split(' ').Select(x => x.Trim()).ToArray();

            var method = _root.DebugCommands.FirstOrDefault(x => x.MethodInfo.Name == input[0]);
            if (method == null)
            {
                Debug.Log($"{input[0]} is not found");
                return;
            }

            var parameters = new List<object>();
            var args = input.Skip(1).ToList();
            var parameterInfos = method.MethodInfo.GetParameters();
            if (parameterInfos.Length != args.Count)
            {
                Debug.Log($"{method.MethodInfo.Name} needs {parameterInfos.Length} parameters");
                return;
            }

            foreach (var parameterInfo in parameterInfos)
            {
                var type = parameterInfo.ParameterType;
                if (type.IsPrimitive)
                {
                    var value = args.Count > 0
                        ? TypeDescriptor.GetConverter(type).ConvertFromString(args[0])
                        : Activator.CreateInstance(type);
                    parameters.Add(value);
                }
                else if (type == typeof(string))
                {
                    parameters.Add(args.Count > 0 ? args[0] : default);
                }
                else
                {
                    Debug.Log($"parse error: {type}");
                }

                if (args.Count > 0) args.RemoveAt(0);
            }

            method.MethodInfo.Invoke(null, parameters.ToArray());
        }

        public void OnClickPasteButton()
        {
            _ui.InputField.text = GUIUtility.systemCopyBuffer;
        }
    }
}