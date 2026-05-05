using UnityEngine;
using System.Collections.Generic;

namespace MalumMenu;

public class AIUI : MonoBehaviour
{
    public static int windowHeight = 400;
    public static int windowWidth = 500;
    private Rect _windowRect;

    private GUIStyle _messageStyle;
    private GUIStyle _userMessageStyle;
    private GUIStyle _aiMessageStyle;
    private GUIStyle _systemStyle;
    private GUIStyle _suggestionStyle;
    private static Vector2 _scrollPosition = Vector2.zero;
    private static string _inputText = "";
    private static bool _showWindow = false;

    public static bool ShowWindow
    {
        get => _showWindow;
        set => _showWindow = value;
    }

    private void Start()
    {
        _windowRect = new(
            Screen.width - windowWidth - 20,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );
    }

    private void Update()
    {
        // Run auto-analysis on interval
        if (CheatToggles.aiMode && CheatToggles.aiAutoAnalyze)
        {
            AIHandler.AutoAnalysis();
        }
    }

    private void InitStyles()
    {
        if (_messageStyle == null)
        {
            _messageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                richText = true
            };
        }

        if (_userMessageStyle == null)
        {
            _userMessageStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.cyan, background = MakeTex(1, 1, new Color(0f, 0.1f, 0.2f, 0.8f)) },
                padding = new RectOffset(8, 8, 6, 6)
            };
        }

        if (_aiMessageStyle == null)
        {
            _aiMessageStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.green, background = MakeTex(1, 1, new Color(0f, 0.15f, 0f, 0.8f)) },
                padding = new RectOffset(8, 8, 6, 6)
            };
        }

        if (_systemStyle == null)
        {
            _systemStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };
        }

        if (_suggestionStyle == null)
        {
            _suggestionStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(1f, 0.6f, 0f), background = MakeTex(1, 1, new Color(0.2f, 0.1f, 0f, 0.8f)) },
                padding = new RectOffset(8, 8, 6, 6)
            };
        }
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var result = new Texture2D(w, h);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void OnGUI()
    {
        if (!_showWindow || !CheatToggles.aiMode || MalumMenu.isPanicked) return;

        InitStyles();
        UIHelpers.ApplyUIColor();

        _windowRect = GUI.Window((int)WindowId.AIUI, _windowRect, (GUI.WindowFunction)AIWindow, "AI Assistant - " + AIHandler.StatusMessage);
    }

    private void AIWindow(int windowID)
    {
        GUILayout.BeginVertical(GUI.skin.box);

        // Status bar
        GUILayout.Label(AIHandler.StatusMessage, _systemStyle);

        GUILayout.Space(4);

        // Suggestion bar (if there's a suggestion)
        if (!string.IsNullOrEmpty(AIHandler.LastSuggestion))
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Suggested message:", _suggestionStyle, GUILayout.Width(120));
            GUILayout.Label("\"" + AIHandler.LastSuggestion + "\"", _suggestionStyle);
            if (GUILayout.Button("Copy", GUILayout.Width(60)))
            {
                GUIUtility.systemCopyBuffer = AIHandler.LastSuggestion;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(4);

        // Chat history scroll view
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);

        foreach (var msg in AIHandler.ConversationHistory)
        {
            if (msg.role == "user")
            {
                GUILayout.Label($"You: {msg.content}", _userMessageStyle);
            }
            else if (msg.role == "assistant")
            {
                GUILayout.Label($"AI: {msg.content}", _aiMessageStyle);
            }
        }

        // Auto-scroll to bottom
        if (Event.current.type == EventType.Repaint)
        {
            _scrollPosition.y = float.MaxValue;
        }

        GUILayout.EndScrollView();

        GUILayout.Space(4);

        // Input area
        GUILayout.BeginHorizontal();

        _inputText = GUILayout.TextField(_inputText, 200, GUILayout.ExpandWidth(true));

        if (GUILayout.Button("Send", GUILayout.Width(80)))
        {
            if (!string.IsNullOrWhiteSpace(_inputText))
            {
                AIHandler.SendAnalysisRequest(_inputText);
                _inputText = "";
            }
        }

        GUILayout.EndHorizontal();

        // Action buttons
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Analyze Game", GUILayout.Width(120)))
        {
            AIHandler.SendAnalysisRequest();
        }

        if (GUILayout.Button("Analyze Meeting", GUILayout.Width(120)))
        {
            AIHandler.SendAnalysisRequest("Analyze the current meeting. What are players saying, who seems suspicious, and what should I say or vote?");
        }

        if (GUILayout.Button("Clear Chat", GUILayout.Width(100)))
        {
            AIHandler.Reset();
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUI.DragWindow();
    }
}
