using System.Text.Json.Serialization;
using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public static class EnumExtensions<Dest>
        where Dest : struct, Enum
    {
        public static Dest Convert<Src>(Src src)
            where Src : struct, Enum
        {
            string name = Enum.GetName<Src>(src);
            return Enum.Parse<Dest>(name);
        }
    }

    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum Platform
    {
        IOS,
        MAC_OS,
        TV_OS,
    }

    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum AppStoreState
    {
        ACCEPTED,
        DEVELOPER_REMOVED_FROM_SALE,
        DEVELOPER_REJECTED,
        IN_REVIEW,
        INVALID_BINARY,
        METADATA_REJECTED,
        PENDING_APPLE_RELEASE,
        PENDING_CONTRACT,
        PENDING_DEVELOPER_RELEASE,
        PREPARE_FOR_SUBMISSION,
        PREORDER_READY_FOR_SALE,
        PROCESSING_FOR_APP_STORE,
        READY_FOR_REVIEW,
        READY_FOR_SALE,
        REJECTED,
        REMOVED_FROM_SALE,
        WAITING_FOR_EXPORT_COMPLIANCE,
        WAITING_FOR_REVIEW,
        REPLACED_WITH_NEW_VERSION,
    }

    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum ReleaseType
    {
        MANUAL,
        AFTER_APPROVAL,
        SCHEDULED,
    }

    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum InAppPurchaseType
    {
        CONSUMABLE,
        NON_CONSUMABLE,
        NON_RENEWING_SUBSCRIPTION,
    }

    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum InAppPurchaseState
    {
        MISSING_METADATA,
        WAITING_FOR_UPLOAD,
        PROCESSING_CONTENT,
        READY_TO_SUBMIT,
        WAITING_FOR_REVIEW,
        IN_REVIEW,
        DEVELOPER_ACTION_NEEDED,
        PENDING_BINARY_APPROVAL,
        APPROVED,
        DEVELOPER_REMOVED_FROM_SALE,
        REMOVED_FROM_SALE,
        REJECTED,
    }

    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum InAppPurchaseLocaliztionState
    {
        PREPARE_FOR_SUBMISSION,
        WAITING_FOR_REVIEW,
        APPROVED,
        REJECTED,
    }

    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum ScreenshotDisplayType
    {
        APP_IPHONE_67,
        APP_IPHONE_61,
        APP_IPHONE_65,
        APP_IPHONE_58,
        APP_IPHONE_55,
        APP_IPHONE_47,
        APP_IPHONE_40,
        APP_IPHONE_35,
        APP_IPAD_PRO_3GEN_129,
        APP_IPAD_PRO_3GEN_11,
        APP_IPAD_PRO_129,
        APP_IPAD_105,
        APP_IPAD_97,
        APP_DESKTOP,
        APP_WATCH_ULTRA,
        APP_WATCH_SERIES_7,
        APP_WATCH_SERIES_4,
        APP_WATCH_SERIES_3,
        APP_APPLE_TV,
        IMESSAGE_APP_IPHONE_67,
        IMESSAGE_APP_IPHONE_61,
        IMESSAGE_APP_IPHONE_65,
        IMESSAGE_APP_IPHONE_58,
        IMESSAGE_APP_IPHONE_55,
        IMESSAGE_APP_IPHONE_47,
        IMESSAGE_APP_IPHONE_40,
        IMESSAGE_APP_IPAD_PRO_3GEN_129,
        IMESSAGE_APP_IPAD_PRO_3GEN_11,
        IMESSAGE_APP_IPAD_PRO_129,
        IMESSAGE_APP_IPAD_105,
        IMESSAGE_APP_IPAD_97,
    }

    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum PreviewType
    {
        IPHONE_67,
        IPHONE_61,
        IPHONE_65,
        IPHONE_58,
        IPHONE_55,
        IPHONE_47,
        IPHONE_40,
        IPHONE_35,
        IPAD_PRO_3GEN_129,
        IPAD_PRO_3GEN_11,
        IPAD_PRO_129,
        IPAD_105,
        IPAD_97,
        DESKTOP,
        APPLE_TV,
    }

    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum Badge
    {
        LIVE_EVENT,
        PREMIERE,
        CHALLENGE,
        COMPETITION,
        NEW_SEASON,
        MAJOR_UPDATE,
        SPECIAL_EVENT,
    }

    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum EventState
    {
        DRAFT,
        READY_FOR_REVIEW,
        WAITING_FOR_REVIEW,
        IN_REVIEW,
        REJECTED,
        ACCEPTED,
        APPROVED,
        PUBLISHED,
        PAST,
        ARCHIVED,
    }

    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum PurchaseRequirement
    {
        NO_COST_ASSOCIATED,
        IN_APP_PURCHASE,
        SUBSCRIPTION,
        IN_APP_PURCHASE_AND_SUBSCRIPTION,
        IN_APP_PURCHASE_OR_SUBSCRIPTION,
    }

    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum Priority
    {
        HIGH,
        NORMAL,
    }

    [JsonConverter(typeof(JsonStringEnumMemberConverter))]
    public enum Purpose
    {
        APPROPRIATE_FOR_ALL_USERS,
        ATTRACT_NEW_USERS,
        KEEP_ACTIVE_USERS_INFORMED,
        BRING_BACK_LAPSED_USERS,
    }

}