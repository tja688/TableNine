public static class TableNineUIKeys
{
    public const string GameplayHud = "gameplay.hud";
    public const string PopupMessage = "popup.message";
    public const string AttributeChoice = "choice.attribute";
    public const string RoomChoice = "choice.room";
    public const string HelpReward = "reward.help";
    public const string ChestReward = "reward.chest";
    public const string ShopMain = "shop.main";
    public const string TutorSkillChoice = "choice.tutor_skill";
    public const string NextNodePrompt = "prompt.next_node";
    public const string DeleteHelpCardConfirm = "confirm.delete_help_card";
    public const string CardDetail = "card.detail";

    public const string GeneratedFallbackPanelPrefix = "UIFallbackPanel.";
}

public enum TableNineUIType
{
    Hud,
    Popup,
    ChoiceOverlay,
    RoomChoice,
    RewardChoice,
    Shop,
    Confirmation,
    Detail,
    StatusPrompt
}

public enum TableNineUIFallbackStrategy
{
    None,
    AtomicPopup,
    AtomicChoiceList,
    AtomicRoomChoice,
    AtomicShopList,
    AtomicStatusPrompt
}
