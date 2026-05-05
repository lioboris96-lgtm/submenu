using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using UnityEngine;
using AmongUs.GameOptions;

namespace MalumMenu;

public static class AIHandler
{
    private const string GROQ_API_URL = "https://api.groq.com/openai/v1/chat/completions";
    private static readonly List<ChatMessage> conversationHistory = new();
    private static readonly List<string> meetingChatLog = new();
    private static bool isProcessing = false;
    private static string lastResponse = "";
    private static string lastSuggestion = "";
    private static string statusMessage = "AI Mode: Idle";
    private static float lastAutoAnalysisTime = 0f;
    private static readonly float autoAnalysisInterval = 30f; // seconds between auto-analyses

    public static string StatusMessage => statusMessage;
    public static string LastResponse => lastResponse;
    public static string LastSuggestion => lastSuggestion;
    public static bool IsProcessing => isProcessing;
    public static List<ChatMessage> ConversationHistory => conversationHistory;
    public static List<string> MeetingChatLog => meetingChatLog;

    public class ChatMessage
    {
        public string role; // "system", "user", "assistant"
        public string content;
        public float timestamp;

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
            this.timestamp = Time.time;
        }
    }

    public static void Reset()
    {
        conversationHistory.Clear();
        meetingChatLog.Clear();
        lastResponse = "";
        lastSuggestion = "";
        statusMessage = "AI Mode: Idle";
        isProcessing = false;
    }

    public static void AddMeetingChat(string playerName, string message)
    {
        meetingChatLog.Add($"[{playerName}]: {message}");
    }

    private static string BuildSystemPrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an AI assistant embedded in MalumMenu, a cheat mod for Among Us. Your job is to analyze the game state and help the player win.");
        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- Be concise. Keep responses under 200 words unless asked for detail.");
        sb.AppendLine("- When suggesting what to say in meetings, put the exact message in quotes like: SAY: \"your message here\"");
        sb.AppendLine("- The player can copy suggested messages to clipboard automatically.");
        sb.AppendLine("- Analyze roles, behavior, and voting patterns to detect impostors or identify threats.");
        sb.AppendLine("- If the player is impostor, help them blend in and deceive.");
        sb.AppendLine("- If the player is crewmate, help them identify impostors and survive.");
        sb.AppendLine();
        sb.AppendLine("Current game state:");
        sb.AppendLine(CollectGameState());
        sb.AppendLine();

        if (meetingChatLog.Count > 0)
        {
            sb.AppendLine("Recent meeting chat:");
            foreach (var msg in meetingChatLog)
            {
                sb.AppendLine(msg);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string CollectGameState()
    {
        var sb = new StringBuilder();

        try
        {
            if (!Utils.isPlayer || !Utils.isClient)
            {
                return "No active game.";
            }

            // Game info
            var gameMode = Utils.isNormalGame ? "Classic" : Utils.isHideNSeek ? "Hide & Seek" : "Unknown";
            sb.AppendLine($"Game Mode: {gameMode}");
            sb.AppendLine($"Map: {GetCurrentMapName()}");
            sb.AppendLine($"In Meeting: {Utils.isMeeting}");
            sb.AppendLine($"Is Host: {Utils.isHost}");
            sb.AppendLine($"Ping: {Utils.GetPing()}ms");
            sb.AppendLine();

            // Local player info
            var localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer != null && localPlayer.Data != null)
            {
                var myRole = Utils.GetRoleName(localPlayer.Data);
                var myTeam = localPlayer.Data.Role.TeamType.ToString();
                sb.AppendLine($"You are: {localPlayer.Data.PlayerName} ({myRole}, {myTeam})");
                sb.AppendLine($"You are alive: {!localPlayer.Data.IsDead}");
                sb.AppendLine();
            }

            // All players
            sb.AppendLine("Players:");
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null) continue;

                var name = player.Data.PlayerName;
                var role = Utils.GetRoleName(player.Data);
                var team = player.Data.Role.TeamType.ToString();
                var alive = !player.Data.IsDead;
                var disconnected = player.Data.Disconnected;
                var inVent = player.inVent;

                // Only show roles if seeRoles cheat is on or it's the local player
                if (player == PlayerControl.LocalPlayer || CheatToggles.seeRoles)
                {
                    sb.AppendLine($"  {name}: Role={role} ({team}), Alive={alive}, Disconnected={disconnected}, InVent={inVent}");
                }
                else
                {
                    sb.AppendLine($"  {name}: Alive={alive}, Disconnected={disconnected}, InVent={inVent}");
                }
            }

            // Game options
            if (GameOptionsManager.Instance.CurrentGameOptions != null)
            {
                var opts = GameOptionsManager.Instance.CurrentGameOptions;
                sb.AppendLine();
                sb.AppendLine($"Player Speed: {opts.PlayerSpeedMod}");
                sb.AppendLine($"Kill Cooldown: {opts.KillCooldown}");
                sb.AppendLine($"Emergency Cooldown: {opts.EmergencyCooldown}");
                sb.AppendLine($"# Impostors: {opts.NumImpostors}");
                sb.AppendLine($"# Common Tasks: {opts.NumCommonTasks}");
                sb.AppendLine($"# Long Tasks: {opts.NumLongTasks}");
                sb.AppendLine($"# Short Tasks: {opts.NumShortTasks}");
            }

            // Sabotage state
            if (Utils.isShip)
            {
                sb.AppendLine();
                sb.AppendLine($"Sabotage Active: {Utils.isAnySabotageActive}");
            }

            // Active cheats that affect gameplay
            sb.AppendLine();
            sb.AppendLine("Active Cheats:");
            if (CheatToggles.noClip) sb.AppendLine("  NoClip");
            if (CheatToggles.seeRoles) sb.AppendLine("  See Roles");
            if (CheatToggles.seeGhosts) sb.AppendLine("  See Ghosts");
            if (CheatToggles.noKillCd) sb.AppendLine("  No Kill Cooldown");
            if (CheatToggles.killReach) sb.AppendLine("  Kill Reach");
            if (CheatToggles.killAnyone) sb.AppendLine("  Kill Anyone");
            if (CheatToggles.teleportCursor) sb.AppendLine("  Teleport to Cursor");
            if (CheatToggles.unlockVents) sb.AppendLine("  Use Vents");
            if (CheatToggles.walkInVents) sb.AppendLine("  Walk in Vents");
            if (CheatToggles.noShadows) sb.AppendLine("  No Shadows");
            if (CheatToggles.revealVotes) sb.AppendLine("  Reveal Votes");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"Error collecting game state: {ex.Message}");
        }

        return sb.ToString();
    }

    private static string GetCurrentMapName()
    {
        if (Utils.isSkeldMap) return "The Skeld";
        if (Utils.isMiraHQMap) return "MIRA HQ";
        if (Utils.isPolusMap) return "Polus";
        if (Utils.isDleksMap) return "dlekS ehT";
        if (Utils.isAirshipMap) return "Airship";
        if (Utils.isFungleMap) return "Fungle";
        return "Unknown";
    }

    public static void SendAnalysisRequest(string userMessage = null)
    {
        if (isProcessing) return;

        var apiKey = MalumMenu.aiApiKey.Value;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            statusMessage = "AI Mode: No API key set! Configure in AI tab.";
            return;
        }

        if (!CheatToggles.aiMode) return;

        isProcessing = true;
        statusMessage = "AI Mode: Thinking...";

        // Build system prompt with current game state
        string systemPrompt = BuildSystemPrompt();

        // Add user message or default analysis request
        if (!string.IsNullOrWhiteSpace(userMessage))
        {
            conversationHistory.Add(new ChatMessage("user", userMessage));
        }
        else
        {
            conversationHistory.Add(new ChatMessage("user", "Analyze the current game situation and give me strategic advice. What should I do next?"));
        }

        // Start async API call
        MalumMenu.Plugin.StartCoroutine(SendGroqRequest(apiKey, systemPrompt));
    }

    public static void AutoAnalysis()
    {
        if (!CheatToggles.aiAutoAnalyze || !CheatToggles.aiMode) return;
        if (!Utils.isInGame && !Utils.isMeeting) return;
        if (isProcessing) return;
        if (Time.time - lastAutoAnalysisTime < autoAnalysisInterval) return;

        lastAutoAnalysisTime = Time.time;
        SendAnalysisRequest("Quick update: What's the current situation? Any new suggestions?");
    }

    private static IEnumerator SendGroqRequest(string apiKey, string systemPrompt)
    {
        var model = MalumMenu.aiModel.Value;
        if (string.IsNullOrWhiteSpace(model)) model = "llama-3.3-70b-versatile";

        // Build JSON payload
        var messagesArray = new StringBuilder();
        messagesArray.Append($"{{\"role\":\"system\",\"content\":{EscapeJson(systemPrompt)}}}");

        // Include last N messages from conversation to keep context manageable
        int maxHistory = 10;
        int startIndex = Mathf.Max(0, conversationHistory.Count - maxHistory);
        for (int i = startIndex; i < conversationHistory.Count; i++)
        {
            if (i > 0 || startIndex > 0) messagesArray.Append(",");
            messagesArray.Append($"{{\"role\":\"{conversationHistory[i].role}\",\"content\":{EscapeJson(conversationHistory[i].content)}}}");
        }

        string payload = $"{{\"model\":\"{model}\",\"messages\":[{messagesArray}],\"max_tokens\":512,\"temperature\":0.7}}";

        byte[] body = Encoding.UTF8.GetBytes(payload);

        var request = (HttpWebRequest)WebRequest.Create(GROQ_API_URL);
        request.Method = "POST";
        request.ContentType = "application/json";
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.ContentLength = body.Length;
        request.Timeout = 30000; // 30 second timeout

        try
        {
            using (var stream = request.GetRequestStream())
            {
                stream.Write(body, 0, body.Length);
            }

            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string responseText = reader.ReadToEnd();
                string aiContent = ParseGroqResponse(responseText);

                if (!string.IsNullOrEmpty(aiContent))
                {
                    lastResponse = aiContent;
                    conversationHistory.Add(new ChatMessage("assistant", aiContent));
                    statusMessage = "AI Mode: Ready";

                    // Extract SAY: suggestions for clipboard
                    string suggestion = ExtractSuggestion(aiContent);
                    if (!string.IsNullOrEmpty(suggestion))
                    {
                        lastSuggestion = suggestion;
                        if (CheatToggles.aiAutoClipboard)
                        {
                            GUIUtility.systemCopyBuffer = suggestion;
                        }
                    }
                }
                else
                {
                    lastResponse = "(No response from AI)";
                    statusMessage = "AI Mode: Empty response";
                }
            }
        }
        catch (WebException ex)
        {
            string errorMsg = ex.Message;
            if (ex.Response != null)
            {
                using (var errReader = new StreamReader(ex.Response.GetResponseStream()))
                {
                    errorMsg = errReader.ReadToEnd();
                }
            }
            lastResponse = $"API Error: {errorMsg}";
            statusMessage = "AI Mode: API Error";
            MalumMenu.Log.LogWarning($"AI Groq API error: {errorMsg}");
        }
        catch (Exception ex)
        {
            lastResponse = $"Error: {ex.Message}";
            statusMessage = "AI Mode: Error";
            MalumMenu.Log.LogWarning($"AI Handler error: {ex.Message}");
        }
        finally
        {
            isProcessing = false;
        }

        yield break;
    }

    private static string EscapeJson(string text)
    {
        if (text == null) return "\"\"";
        var sb = new StringBuilder();
        sb.Append('"');
        foreach (char c in text)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 32)
                        sb.AppendFormat("\\u{0:X4}", (int)c);
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }

    private static string ParseGroqResponse(string jsonResponse)
    {
        try
        {
            // Simple JSON parsing without dependency - find "content" field in choices
            // Format: {"choices":[{"message":{"content":"..."}}]}
            int choicesIdx = jsonResponse.IndexOf("\"choices\");");
            if (choicesIdx < 0) return null;

            int contentIdx = jsonResponse.IndexOf("\"content\"", choicesIdx);
            if (contentIdx < 0) return null;

            // Find the colon after "content"
            int colonIdx = jsonResponse.IndexOf(':', contentIdx);
            if (colonIdx < 0) return null;

            // Find the opening quote
            int openQuote = jsonResponse.IndexOf('"', colonIdx + 1);
            if (openQuote < 0) return null;

            // Find closing quote (handle escaped quotes)
            int i = openQuote + 1;
            while (i < jsonResponse.Length)
            {
                if (jsonResponse[i] == '\\' && i + 1 < jsonResponse.Length)
                {
                    i += 2; // Skip escaped character
                    continue;
                }
                if (jsonResponse[i] == '"')
                {
                    break;
                }
                i++;
            }

            if (i >= jsonResponse.Length) return null;

            string rawContent = jsonResponse.Substring(openQuote + 1, i - openQuote - 1);

            // Unescape JSON string
            rawContent = rawContent.Replace("\\n", "\n")
                                   .Replace("\\\"", "\"")
                                   .Replace("\\\\", "\\")
                                   .Replace("\\t", "\t")
                                   .Replace("\\r", "\r");

            return rawContent;
        }
        catch (Exception ex)
        {
            MalumMenu.Log.LogWarning($"Failed to parse Groq response: {ex.Message}");
            return null;
        }
    }

    private static string ExtractSuggestion(string aiResponse)
    {
        // Look for SAY: "message" pattern
        int sayIdx = aiResponse.IndexOf("SAY:");
        if (sayIdx < 0) return null;

        int quoteStart = aiResponse.IndexOf('"', sayIdx);
        if (quoteStart < 0) return null;

        int quoteEnd = aiResponse.IndexOf('"', quoteStart + 1);
        if (quoteEnd < 0) return null;

        return aiResponse.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
    }
}
