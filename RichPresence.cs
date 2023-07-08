using System.Linq;
using CDInDeeZ.Extensions;
using JetBrains.Annotations;
using Discord;
using Plugin.Tools;

namespace CDInDeeZ;

public class RichPresence
{
    private static RichPresence _instance;
    public static RichPresence Instance
    {
        get
        {
            return _instance ??= new RichPresence();
        }
    }
    
    public void Init()
    {
        discord = new Discord.Discord(1126599501252132995, (ulong)CreateFlags.Default);
        discord.SetLogHook(
            LogLevel.Debug,
            (level, message) =>
            {
                switch (level)
                {
                    case LogLevel.Error:
                        RichPresenceLogger.LogError(message);
                        break;
                    case LogLevel.Warn:
                        RichPresenceLogger.LogWarning(message);
                        break;
                    case LogLevel.Debug:
                        RichPresenceLogger.LogDebug(message);
                        break;
                    case LogLevel.Info:
                    default:
                        RichPresenceLogger.LogInfo(message);
                        break;
                }
            }
        );

        activityManager = discord.GetActivityManager();
        lobbyManager = discord.GetLobbyManager();
        applicationManager = discord.GetApplicationManager();
        lobbyManager = discord.GetLobbyManager();
        relationshipManager = discord.GetRelationshipManager();

        activityManager.OnActivityJoin += secret =>
        {
            Singleton<MultiplayerLoginManager>.Instance.AuthorizeWithPlayfab();

            var split = secret.Split('_');
            var inviteCode = split.Last();
            
            var gameMode = (GameMode)int.Parse(split.First());
            var gameRequestType = (gameMode == GameMode.CoopChallenge ?
                GameRequestType.CoopChallengeInviteJoin :
                (gameMode == GameMode.EndlessCoop ?
                    GameRequestType.CoopInviteCodeJoin :
                    (gameMode == GameMode.BattleRoyale ?
                        GameRequestType.BattleRoyaleInviteCodeJoin :
                        GameRequestType.DuelInviteCodeJoin)));
            
            RichPresenceLogger.LogWarning($"Invite Code: {inviteCode}");
            RichPresenceLogger.LogWarning($"Game Request Type: {gameRequestType}");

            var gameRequest = new GameRequest()
            {
                InviteCodeToJoin = split.Last(),
                GameType = gameRequestType,
                ActuallyStartServer = true,
            };
            
            Singleton<CustomMatchmakerClientAPI>.Instance.FindMatch(new CustomMatchmakeRequest
            {
                GameRequest = gameRequest
            }, delegate(CustomMatchmakeResult result)
            {
                BoltGlobalEventListenerSingleton<MultiplayerMatchmakingManager>.Instance.ConnectToExternalMatchmakeResult(result, gameRequest);
            }, delegate(CustomMatchmakerError error)
            {
                switch (error.Type)
                {
                    case CustomMatchmakerErrorType.InviteMatchFull:
                        RichPresenceLogger.LogError("Match is full!");
                        break;
                    case CustomMatchmakerErrorType.InviteCodeNotFound:
                        RichPresenceLogger.LogError("Match not found!");
                        break;
                }
            });
        };

        activityManager.OnActivityJoinRequest += (ref User user) =>
        {
            var relationship = relationshipManager.Get(user.Id);
            var reply = ActivityJoinRequestReply.Ignore;
            
            switch (relationship.Type)
            {
                case RelationshipType.Friend:
                case RelationshipType.Implicit:
                case RelationshipType.PendingOutgoing:
                {
                    // accept
                    reply = ActivityJoinRequestReply.Yes;
                    break;
                }
            }

            RichPresenceLogger.LogInfo($"Reply for join request: {reply.ToString()}");
            activityManager.SendRequestReply(user.Id, reply, _=>{});
        };

        activity = new Activity()
        {
            State = "Idle",
            Assets =
            {
                LargeImage = "logo",
                LargeText = "Clone Drone In the Danger Zone"
            }
        };

        Run.Every(5f, 0.1f, Update);
    }

    private void Update()
    {
        var gameMode = Singleton<GameFlowManager>.Instance?.GetCurrentGameMode();
        if (gameMode == null) return;
        var gm = (GameMode)gameMode;
        
        if (!InviteCode.IsNullOrEmpty())
        {
            activity = new Activity
            {
                State = $"Playing {gm.ToString()}",
                Details = "( ͡° ͜ʖ ͡°)",
                Party =
                {
                    Size =
                    {
                        CurrentSize = 1,
                        MaxSize = gm.GetMaxNumberOfPlayers(true)
                    },
                    Id = $"cdeeznuts_{(int)gm}_{InviteCode}"
                },
                Secrets =
                {
                    Join = $"{(int)gm}_{InviteCode}"
                },
                Assets =
                {
                    LargeImage = "logo",
                    LargeText = "Clone Drone In the Danger Zone"
                }
            };
        }
        else
        {
            activity = new Activity
            {
                State = gm == 0 ? "Idle" : $"Playing {gm.ToString()}",
                Details = "( ͡° ͜ʖ ͡°)",
                Assets =
                {
                    LargeImage = "logo",
                    LargeText = "Clone Drone In the Danger Zone"
                }
            };
        }
        
        activityManager.UpdateActivity(activity, (_ =>{}));
        
        discord.RunCallbacks();
    }
    
    private static Discord.Discord discord;
    public static ActivityManager activityManager;
    private static ApplicationManager applicationManager;
    private static LobbyManager lobbyManager;
    public static Activity activity;
    public static RelationshipManager relationshipManager;

    [CanBeNull] public string InviteCode;
}
