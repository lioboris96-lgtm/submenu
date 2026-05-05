using UnityEngine;

namespace MalumMenu;

public class AITab : ITab
{
    public string name => "AI";

    private static string _apiKeyInput = "";
    private static string _modelInput = "";
    private static bool _apiKeyVisible = false;

    public void Draw()
    {
        GUILayout.BeginVertical(GUILayout.Width(MenuUI.windowWidth * 0.425f));

        DrawMainToggle();

        GUILayout.Space(10);

        DrawConfiguration();

        GUILayout.Space(10);

        DrawFeatures();

        GUILayout.Space(10);

        DrawActions();

        GUILayout.EndVertical();
    }

    private void DrawMainToggle()
    {
        GUILayout.Label("AI Mode", GUIStylePreset.TabSubtitle);

        CheatToggles.aiMode = GUILayout.Toggle(CheatToggles.aiMode, " Enable AI Mode");

        if (CheatToggles.aiMode)
        {
            AIUI.ShowWindow = true;
        }
        else
        {
            AIUI.ShowWindow = false;
        }
    }

    private void DrawConfiguration()
    {
        GUILayout.Label("Configuration", GUIStylePreset.TabSubtitle);

        // API Key
        GUILayout.BeginHorizontal();
        GUILayout.Label("API Key:", GUILayout.Width(80));
        _apiKeyInput = _apiKeyVisible ? _apiKeyInput : MaskString(MalumMenu.aiApiKey.Value);
        if (_apiKeyVisible)
        {
            _apiKeyInput = GUILayout.TextField(MalumMenu.aiApiKey.Value, 128, GUILayout.ExpandWidth(true));
        }
        else
        {
            GUILayout.Label(MaskString(MalumMenu.aiApiKey.Value), GUILayout.ExpandWidth(true));
        }

        if (GUILayout.Button(_apiKeyVisible ? "Hide" : "Show", GUILayout.Width(60)))
        {
            _apiKeyVisible = !_apiKeyVisible;
        }
        GUILayout.EndHorizontal();

        // Save API key on change
        if (_apiKeyVisible && _apiKeyInput != MalumMenu.aiApiKey.Value)
        {
            MalumMenu.aiApiKey.Value = _apiKeyInput;
        }

        // Model selection
        GUILayout.BeginHorizontal();
        GUILayout.Label("Model:", GUILayout.Width(80));
        _modelInput = GUILayout.TextField(MalumMenu.aiModel.Value, 64, GUILayout.ExpandWidth(true));
        if (_modelInput != MalumMenu.aiModel.Value)
        {
            MalumMenu.aiModel.Value = _modelInput;
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("Models: llama-3.3-70b-versatile, llama-3.1-8b-instant, mixtral-8x7b-32768, gemma2-9b-it", _labelHintStyle ?? new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = Color.gray } });
    }

    private GUIStyle _labelHintStyle;

    private void DrawFeatures()
    {
        GUILayout.Label("Features", GUIStylePreset.TabSubtitle);

        CheatToggles.aiAutoAnalyze = GUILayout.Toggle(CheatToggles.aiAutoAnalyze, " Auto-Analyze Game");

        CheatToggles.aiAutoClipboard = GUILayout.Toggle(CheatToggles.aiAutoClipboard, " Auto-Copy Suggestions to Clipboard");

        CheatToggles.aiReadChat = GUILayout.Toggle(CheatToggles.aiReadChat, " Read Meeting Chat");
    }

    private void DrawActions()
    {
        GUILayout.Label("Actions", GUIStylePreset.TabSubtitle);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Analyze Now", GUILayout.Width(120)))
        {
            if (CheatToggles.aiMode)
            {
                AIHandler.SendAnalysisRequest();
                AIUI.ShowWindow = true;
            }
        }

        if (GUILayout.Button("Open AI Chat", GUILayout.Width(120)))
        {
            if (CheatToggles.aiMode)
            {
                AIUI.ShowWindow = true;
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Reset Conversation", GUILayout.Width(120)))
        {
            AIHandler.Reset();
        }

        if (GUILayout.Button("Test API Key", GUILayout.Width(120)))
        {
            if (!string.IsNullOrWhiteSpace(MalumMenu.aiApiKey.Value))
            {
                AIHandler.SendAnalysisRequest("Hello, respond with 'API key works!' if you can read this.");
                AIUI.ShowWindow = true;
            }
        }

        GUILayout.EndHorizontal();
    }

    private static string MaskString(string input)
    {
        if (string.IsNullOrEmpty(input)) return "(not set)";
        if (input.Length <= 8) return new string('*', input.Length);
        return input.Substring(0, 4) + new string('*', input.Length - 8) + input.Substring(input.Length - 4);
    }
}
