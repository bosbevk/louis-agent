using LouisAgent.Web.Chats;

namespace LouisAgent.Web.Text;

/// <summary>
/// The words the shared chat components show (TurnView, BlocksView, DelegationCard, CurrencySwitcher), so each app can
/// show them in its own language. English by default; a site registers its own copy.
/// Templates use numbered holes for <see cref="string.Format(string, object?[])"/>.
/// </summary>
public sealed record ChatText
{
    public string Thinking { get; init; } = "Thinking…";
    public string ThoughtFor { get; init; } = "Thought for {0}s";
    public string Input { get; init; } = "Input";
    public string Result { get; init; } = "Result";
    public string Processing { get; init; } = "PROCESSING";

    /// <summary>What a running specialist is doing, per role (F34), in place of processing; none by default.</summary>
    public IReadOnlyDictionary<string, RolePhases> RolePhases { get; init; } = new Dictionary<string, RolePhases>();

    public string Stopped { get; init; } = "Stopped";

    public string DelegatedTo { get; init; } = "Delegated to";

    /// <summary>A minion's card and Werk line (F30): the specialist handed one page to a minion.</summary>
    public string MinionLabel { get; init; } = "Minion";

    /// <summary>Between a minion's role and its model: "web-price-lookup on ollama".</summary>
    public string OnModel { get; init; } = "on";

    /// <summary>The chip on a continued sub-session (F26): a follow-up to a specialist already working.</summary>
    public string FollowUp { get; init; } = "follow-up";

    public string Specialist { get; init; } = "specialist";
    public string Task { get; init; } = "Task";
    public string Working { get; init; } = "Working";
    public string Work { get; init; } = "Work";
    public string Done { get; init; } = "Done";
    public string Failed { get; init; } = "Failed";
    public string Starting { get; init; } = "Starting";
    public string ToolCallOne { get; init; } = "tool call";
    public string ToolCallMany { get; init; } = "tool calls";
    public string ThoughtOne { get; init; } = "thought";
    public string ThoughtMany { get; init; } = "thoughts";

    /// <summary>A chat's title when its first message is empty and no attachment names it (W2: ChatRunner).</summary>
    public string NewChat { get; init; } = "New chat";

    public string AddFavourite { get; init; } = "Add to favourites";
    public string RemoveFavourite { get; init; } = "Remove from favourites";
    public string Share { get; init; } = "Share";
    public string Rename { get; init; } = "Rename";
    public string Archive { get; init; } = "Archive";
    public string Unarchive { get; init; } = "Take out of archive";
    public string Delete { get; init; } = "Delete";
    public string NewName { get; init; } = "New name";

    /// <summary>{0} is the chat's title.</summary>
    public string MoreFor { get; init; } = "More for {0}";

    public string SearchChats { get; init; } = "Search chats";
    public string SearchPlaceholder { get; init; } = "Search chats…";

    /// <summary>{0} is what was typed.</summary>
    public string NoChatsFor { get; init; } = "No chats for \"{0}\".";

    public string Recent { get; init; } = "Recent";
    public string Chats { get; init; } = "Chats";
    public string Archived { get; init; } = "Archived";
    public string SearchChoose { get; init; } = "choose";
    public string SearchOpen { get; init; } = "open";

    public string HideSidebar { get; init; } = "Hide the sidebar";
    public string CloseMenu { get; init; } = "Close the menu";
    public string OpenMenu { get; init; } = "Open the menu";
    public string ResizeSidebar { get; init; } = "Drag to change the width · double-click to reset";
    public string ChatNotSaved { get; init; } = "That chat isn't saved in this browser.";
    public string Copied { get; init; } = "Copied.";
    public string CopyFailed { get; init; } = "Couldn't copy.";

    /// <summary>{0} is what's coming.</summary>
    public string ComingSoon { get; init; } = "{0}: coming soon.";

    public string Favourites { get; init; } = "Favourites";
    public string Projects { get; init; } = "Projects";
    public string NewProject { get; init; } = "New project";
    public string Settings { get; init; } = "Settings";
    public string ApiKey { get; init; } = "API key";
    public string ApiKeyHint { get; init; } = "only if AGENT_API_KEY is set";
    public string Location { get; init; } = "Location";
    public string LocationExample { get; init; } = "e.g. Cape Town";
    public string Locating { get; init; } = "Finding…";
    public string UseMyLocation { get; init; } = "Use my location";

    /// <summary>{0} is the place.</summary>
    public string LocationFound { get; init; } = "Location: {0}";

    public string LocationUnknown { get; init; } = "Couldn't find your location.";
    public string LocationNoBrowser { get; init; } = "This browser can't give your location.";
    public string LocationDenied { get; init; } = "You didn't give permission for your location.";
    public string LocationNotNamed { get; init; } = "OpenStreetMap couldn't name the place.";
    public string LocationUnreachable { get; init; } = "Couldn't reach OpenStreetMap.";

    /// <summary>The languages OpenStreetMap names a place in, as a browser's language list.</summary>
    public string PlaceLanguages { get; init; } = "en";

    public string Names { get; init; } = "Names";
    public string ShowMotion { get; init; } = "Show animations although my system asks for less";
    public string SystemAsksNoLess { get; init; } = " (your system doesn't ask for less)";

    public string ShowSidebar { get; init; } = "Show the sidebar";
    public string MoreForThisChat { get; init; } = "More for this chat";
    public string ToggleWork { get; init; } = "Show or hide the work panel";
    public string ShowWork { get; init; } = "Show the work panel";

    public string Credits { get; init; } = "Credits";

    /// <summary>{0} is this month's cost.</summary>
    public string CreditsTitle { get; init; } = "Credits and usage: {0} this month";

    public string CreditsHeading { get; init; } = "Credits · pay as you go";
    public string ThisChat { get; init; } = "This chat";
    public string ThisMonth { get; init; } = "This month";
    public string ChatOne { get; init; } = "chat";
    public string ChatMany { get; init; } = "chats";

    public string CreditsNote { get; init; } =
        "No subscription: you pay only for the questions you ask, specialists included. From the chats in this browser.";

    public string BuyCredits { get; init; } = "Buy credits";

    public string Language { get; init; } = "Language";

    public string Profile { get; init; } = "Profile";
    public string GuestInitial { get; init; } = "G";
    public string Guest { get; init; } = "Guest";
    public string LocalOnly { get; init; } = "Local to this browser";
    public string Theme { get; init; } = "Theme";
    public string ThemeDark { get; init; } = "Dark";
    public string ThemeLight { get; init; } = "Light";
    public string ThemeLightSoon { get; init; } = "The light theme";
    public string ThemeAuto { get; init; } = "Automatic (system)";
    public string ThemeAutoSoon { get; init; } = "An automatic theme";
    public string SignOut { get; init; } = "Sign out";

    public string ChatEnded { get; init; } = "This chat ended when the API restarted - start a new chat";
    public string AskAnything { get; init; } = "Ask anything…";
    public string SendTitle { get; init; } = "Send (Enter)";
    public string StopTitle { get; init; } = "Stop";

    /// <summary>A chat window folds to its head (F127.2).</summary>
    public string Minimise { get; init; } = "Minimise";

    /// <summary>A chat window spreads wide (F127.2).</summary>
    public string Maximise { get; init; } = "Maximise";

    /// <summary>A minimised or maximised chat window goes back as it came (F127.2).</summary>
    public string Restore { get; init; } = "Restore";

    public string AddToMessage { get; init; } = "Add";
    public string UploadFile { get; init; } = "Upload a file";
    public string AddScreenshot { get; init; } = "Add a screenshot";
    public string ScreenshotsSoon { get; init; } = "Screenshots";
    public string Tools { get; init; } = "Tools";
    public string SearchWeb { get; init; } = "Search the web";
    public string SearchWebDraft { get; init; } = "Search online: ";
    public string ReadFile { get; init; } = "Read a file";
    public string ReadFileDraft { get; init; } = "Read the file ";
    public string CreateImage { get; init; } = "Create an image";
    public string DeepResearch { get; init; } = "Deep research";
    public string ThinkLonger { get; init; } = "Think longer";
    public string Model { get; init; } = "Model";
    public string ServerModel { get; init; } = "The server's model";
    public string OtherModels { get; init; } = "Other models…";
    public string OtherModelsSoon { get; init; } = "Other models";
    public string VoiceMessage { get; init; } = "Voice message";
    public string VoiceMessagesSoon { get; init; } = "Voice messages";

    public string Copy { get; init; } = "Copy";
    public string EditMessage { get; init; } = "Edit the message";
    public string EditSoon { get; init; } = "Editing";
    public string CopyAnswer { get; init; } = "Copy the answer";
    public string TryAgain { get; init; } = "Try again";
    public string GoodAnswer { get; init; } = "Good answer";
    public string BadAnswer { get; init; } = "Bad answer";
    public string FeedbackSoon { get; init; } = "Feedback";
    public string ShowSources { get; init; } = "Show the sources in the work panel";
    public string SourceOne { get; init; } = "source";
    public string SourceMany { get; init; } = "sources";

    public string HideWork { get; init; } = "Hide the work panel";
    public string CloseWork { get; init; } = "Close the work panel";
    public string NoWorkYet { get; init; } = "No work yet";

    public string WorkEmpty { get; init; } =
        "When a task is handed over, you see the specialist's steps, tools and cost here, as it happens.";

    public string StepsLabel { get; init; } = "Steps";
    public string Requests { get; init; } = "requests";
    public string Cost { get; init; } = "Cost";
    public string Sources { get; init; } = "Sources";
    public string OwnCost { get; init; } = "This chat";
    public string Specialists { get; init; } = "Specialists";


    /// <summary>The page's notices and a turn's errors, as the pages wrote them. {0} is the file's name.</summary>
    public string AttachmentTooLarge { get; init; } = "{0} is larger than 1 MB and was not attached.";

    public string SessionGone { get; init; } =
        "This chat's session no longer exists on the server (the API was restarted). Start a new chat to continue.";

    /// <summary>A refused turn (F61.2): on a site with accounts a person signs in; anywhere, a key in the sidebar works.</summary>
    public string NeedsKey { get; init; } =
        "The API said no: sign in, or enter AGENT_API_KEY under API key in the sidebar.";

    public string StillAnswering { get; init; } = "This chat is still answering; wait for it or press Stop.";
    public string ApiUnreachable { get; init; } = "Can't reach the agent API. Is the api container running?";

    public string Currency { get; init; } = "Currency";
    public string AsCharged { get; init; } = "As charged";

    /// <summary>{0} the base currency, {1} the rate, {2} the shown currency, {3} the rates' date.</summary>
    public string ConvertedAt { get; init; } = "1 {0} = {1} {2} on {3}; the ledger keeps what was charged";

    /// <summary>"3 tool calls, 1 thought" in this language.</summary>
    public string Steps(int tools, int thoughts) =>
        $"{tools} {(tools == 1 ? ToolCallOne : ToolCallMany)}, {thoughts} {(thoughts == 1 ? ThoughtOne : ThoughtMany)}";
}
