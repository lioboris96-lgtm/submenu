using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using AmongUs.GameOptions;

namespace MalumMenu;

public static class AIHandler
{
    private const string GROQ_API_URL = "https://api.groq.com/openai/v1/chat/completions";
    private static readonly List<ChatMessage> conversationHistory = new();
    private static readonly List<string> meetingChatLog = new();
    private static volatile bool isProcessing = false;
    private static string lastResponse = "";
    private static string lastSuggestion = "";
    private static string statusMessage = "AI Mode: Idle";
    private static float lastAutoAnalysisTime = 0f;
    private static readonly float autoAnalysisInterval = 30f;

    public static string StatusMessage => statusMessage;
    public static string LastResponse => lastResponse;
    public static string LastSuggestion => lastSuggestion;
    public static bool IsProcessing => isProcessing;
    public static List<ChatMessage> ConversationHistory => conversationHistory;
    public static List<string> MeetingChatLog => meetingChatLog;

    public class ChatMessage
    {
        public string role;
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
        sb.AppendLine("You are an AI assistant embedded in MalumMenu, a cheat mod for Among Us. Your job is to analyze game state and help the player win.");
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

                if (player == PlayerControl.LocalPlayer || CheatToggles.seeRoles)
                {
                    sb.AppendLine($"  {name}: Role={role} ({team}), Alive={alive}, Disconnected={disconnected}, InVent={inVent}");
                }
                else
                {
                    sb.AppendLine($"  {name}: Alive={alive}, Disconnected={disconnected}, InVent={inVent}");
                }
            }

            // Game options - use try/catch for each property since they vary by game mode
            try
            {
                if (GameOptionsManager.Instance.CurrentGameOptions != null)
                {
                    var opts = GameOptionsManager.Instance.CurrentGameOptions;
                    sb.AppendLine();
                    sb.AppendLine($"Game Mode Type: {opts.GameMode}");
                    sb.AppendLine($"# Impostors: {opts.NumImpostors}");

                    // Try to get NormalGameOptions-specific properties
                    try
                    {
                        var normalOpts = opts.Cast<NormalGameOptions>();
                        if (normalOpts != null)
                        {
                            sb.AppendLine($"Player Speed: {normalOpts.PlayerSpeedMod}");
                            sb.AppendLine($"Kill Cooldown: {normalOpts.KillCooldown}");
                            sb.AppendLine($"Emergency Cooldown: {normalOpts.EmergencyCooldown}");
                            sb.AppendLine($"# Common Tasks: {normalOpts.NumCommonTasks}");
                            sb.AppendLine($"# Long Tasks: {normalOpts.NumLongTasks}");
                            sb.AppendLine($"# Short Tasks: {normalOpts.NumShortTasks}");
                        }
                    }
                    catch { /* Not NormalGameOptions, skip */ }
                }
            }
            catch { /* Game options not available */ }

            // Sabotage state
            if (Utils.isShip)
            {
                sb.AppendLine();
                sb.AppendLine($"Sabotage Active: {Utils.isAnySabotageActive}");
            }

            // Active cheats
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

        string systemPrompt = BuildSystemPrompt();

        if (!string.IsNullOrWhiteSpace(userMessage))
        {
            conversationHistory.Add(new ChatMessage("user", userMessage));
        }
        else
        {
            conversationHistory.Add(new ChatMessage("user", "Analyze the current game situation and give me strategic advice. What should I do next?"));
        }

        // Run API call on background thread to avoid blocking game
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                SendGroqRequestSync(apiKey, systemPrompt);
            }
            catch (Exception ex)
            {
                lastResponse = $"Error: {ex.Message}";
                statusMessage = "AI Mode: Error";
                isProcessing = false;
            }
        });
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

    private static void SendGroqRequestSync(string apiKey, string systemPrompt)
    {
        var model = MalumMenu.aiModel.Value;
        if (string.IsNullOrWhiteSpace(model)) model = "llama-3.3-70b-versatile";

        // Build JSON payload
        var messagesArray = new StringBuilder();
        messagesArray.Append($"{{\"role\":\"system\",\"content\":{EscapeJson(systemPrompt)}}}");

        int maxHistory = 10;
        int startIndex = Mathf.Max(0, conversationHistory.Count - maxHistory);
        for (int i = startIndex; i < conversationHistory.Count; i++)
        {
            if (i > 0 || startIndex > 0) messagesArray.Append(",");
            messagesArray.Append($"{{\"role\":\"{conversationHistory[i].role}\",\"content\":{EscapeJson(conversationHistory[i].content)}}}");
        }

        string payload = $"{{\"model\":\"{model}\",\"messages\":[{messagesArray}],\"max_tokens\":512,\"temperature\":0.7}}";

        try
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = client.PostAsync(GROQ_API_URL, content).Result;
            response.EnsureSuccessStatusCode();

            string responseText = response.Content.ReadAsStringAsync().Result;
            string aiContent = ParseGroqResponse(responseText);

            if (!string.IsNullOrEmpty(aiContent))
            {
                lastResponse = aiContent;
                conversationHistory.Add(new ChatMessage("assistant", aiContent));
                statusMessage = "AI Mode: Ready";

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

            client.Dispose();
        }
        catch (Exception ex)
        {
            string errorMsg = ex.Message;
            lastResponse = $"API Error: {errorMsg}";
            statusMessage = "AI Mode: API Error";
            MalumMenu.Log.LogWarning($"AI Groq API error: {errorMsg}");
        }
        finally
        {
            isProcessing = false;
        }
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
            int choicesIdx = jsonResponse.IndexOf("\"choices\"");
            if (choicesIdx < 0) return null;

            int contentIdx = jsonResponse.IndexOf("\"content\"", choicesIdx);
            if (contentIdx < 0) return null;

            int colonIdx = jsonResponse.IndexOf(':', contentIdx);
            if (colonIdx < 0) return null;

            int openQuote = jsonResponse.IndexOf('"', colonIdx + 1);
            if (openQuote < 0) return null;

            int i = openQuote + 1;
            while (i < jsonResponse.Length)
            {
                if (jsonResponse[i] == '\\' && i + 1 < jsonResponse.Length)
                {
                    i += 2;
                    continue;
                }
                if (jsonResponse[i] == '"') break;
                i++;
            }

            if (i >= jsonResponse.Length) return null;

            string rawContent = jsonResponse.Substring(openQuote + 1, i - openQuote - 1);

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
        int sayIdx = aiResponse.IndexOf("SAY:");
        if (sayIdx < 0) return null;

        int quoteStart = aiResponse.IndexOf('"', sayIdx);
        if (quoteStart < 0) return null;

        int quoteEnd = aiResponse.IndexOf('"', quoteStart + 1);
        if (quoteEnd < 0) return null;

        return aiResponse.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
    }
}
