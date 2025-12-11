using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models;

public class Building
{
    public int Id { get; set; }

    // General information
    [Column("FIdId", TypeName = "integer")]
    [Display(Name = "ID נכס לצורך מערכת זו בלבד")]
    [FieldSpec(Category = "מידע כללי", FieldName = "ID נכס לצורך מערכת זו בלבד")]
    // Category: מידע כללי | Field Name: ID נכס לצורך מערכת זו בלבד | Foreign Field ID: — | Event Log: No
    public int? FldId { get; set; }

    [Column("StreetName", TypeName = "text")]
    [Display(Name = "שם רחוב")]
    [FieldSpec(Category = "מידע כללי", FieldName = "שם רחוב")]
    // Category: מידע כללי | Field Name: שם רחוב | Foreign Field ID: — | Event Log: No
    public string StreetName { get; set; } = string.Empty;

    [Column("BldNum", TypeName = "text")]
    [Display(Name = "מספר בית")]
    [FieldSpec(Category = "מידע כללי", FieldName = "מספר בית")]
    // Category: מידע כללי | Field Name: מספר בית | Foreign Field ID: — | Event Log: No
    public string HouseNumber { get; set; } = string.Empty;

    [Column("BldName", TypeName = "text")]
    [Display(Name = "כינוי הבניין")]
    [FieldSpec(Category = "מידע כללי", FieldName = "כינוי הבניין")]
    // Category: מידע כללי | Field Name: כינוי הבניין | Foreign Field ID: — | Event Log: No
    public string BuildingName { get; set; } = string.Empty;

    [Column("BldSivug", TypeName = "integer")]
    [Display(Name = "סיווג")]
    [FieldSpec(Category = "מידע כללי", FieldName = "סיווג", ForeignFieldId = "Tbl_Sivug", IncludeInEventLog = true)]
    // Category: מידע כללי | Field Name: סיווג | Foreign Field ID: Tbl_Sivug | Event Log: Yes
    public int? BldSivug { get; set; }

    [Column("ShikumStatus", TypeName = "integer")]
    [Display(Name = "סטטוס שיקום")]
    [FieldSpec(Category = "מידע כללי", FieldName = "סטטוס שיקום", ForeignFieldId = "Tbl_StatusShikum", IncludeInEventLog = true)]
    // Category: מידע כללי | Field Name: סטטוס שיקום | Foreign Field ID: Tbl_StatusShikum | Event Log: Yes
    public BuildingStatus ShikumStatus { get; set; } = BuildingStatus.Unknown;

    [Column("StatusSummary", TypeName = "text")]
    [Display(Name = "תמצית מצב")]
    [FieldSpec(Category = "מידע כללי", FieldName = "תמצית מצב", IncludeInEventLog = true)]
    // Category: מידע כללי | Field Name: תמצית מצב | Foreign Field ID: — | Event Log: Yes
    public string StatusSummary { get; set; } = string.Empty;

    [Column("StatusSummary_Update_Dt", TypeName = "date")]
    [Display(Name = "תאריך עדכון תמצית מצב")]
    [FieldSpec(Category = "מידע כללי", FieldName = "תאריך עדכון תמצית מצב")]
    // Category: מידע כללי | Field Name: תאריך עדכון תמצית מצב | Foreign Field ID: — | Event Log: No
    public DateTime? StatusSummaryUpdatedAt { get; set; }

    [Column("complaints", TypeName = "text")]
    [Display(Name = "תלונות תושבים")]
    [FieldSpec(Category = "מידע כללי", FieldName = "תלונות תושבים", IncludeInEventLog = true)]
    // Category: מידע כללי | Field Name: תלונות תושבים | Foreign Field ID: — | Event Log: Yes
    public string Complaints { get; set; } = string.Empty;

    // Identifiers
    [Column(TypeName = "double precision")]
    [Display(Name = "קוארדינטות אורך")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "קוארדינטות אורך")]
    // Category: פרטים מזהים | Field Name: קוארדינטות אורך | Foreign Field ID: — | Event Log: No
    public double? Longitude { get; set; }

    [Column(TypeName = "double precision")]
    [Display(Name = "קוארדינטות רוחב")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "קוארדינטות רוחב")]
    // Category: פרטים מזהים | Field Name: קוארדינטות רוחב | Foreign Field ID: — | Event Log: No
    public double? Latitude { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "גוש מוסדר")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "גוש מוסדר")]
    // Category: פרטים מזהים | Field Name: גוש מוסדר | Foreign Field ID: — | Event Log: No
    public int? GushM { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "חלקה מוסדר")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "חלקה מוסדר")]
    // Category: פרטים מזהים | Field Name: חלקה מוסדר | Foreign Field ID: — | Event Log: No
    public int? ParcelM { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "גוש שומה")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "גוש שומה")]
    // Category: פרטים מזהים | Field Name: גוש שומה | Foreign Field ID: — | Event Log: No
    public int? GushS { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "חלקה שומה")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "חלקה שומה")]
    // Category: פרטים מזהים | Field Name: חלקה שומה | Foreign Field ID: — | Event Log: No
    public int? ParcelS { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "תת חלקה")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "תת חלקה")]
    // Category: פרטים מזהים | Field Name: תת חלקה | Foreign Field ID: — | Event Log: No
    public string? ParcelTat { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "קוד רחוב")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "קוד רחוב")]
    // Category: פרטים מזהים | Field Name: קוד רחוב | Foreign Field ID: — | Event Log: No
    public int? StreetCode { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "מספר תיק בניין (הנדסה)")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "מספר תיק בניין (הנדסה)")]
    // Category: פרטים מזהים | Field Name: מספר תיק בניין (הנדסה) | Foreign Field ID: — | Event Log: No
    public string? TikBinyanNum { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "מספר מבנה (ארנונה)")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "מספר מבנה (ארנונה)")]
    // Category: פרטים מזהים | Field Name: מספר מבנה (ארנונה) | Foreign Field ID: — | Event Log: No
    public string? MivneNum { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "מספר פיזי (ארנונה)")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "מספר פיזי (ארנונה)")]
    // Category: פרטים מזהים | Field Name: מספר פיזי (ארנונה) | Foreign Field ID: — | Event Log: No
    public int? FiziNum { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "סוג המבנה")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "סוג המבנה", ForeignFieldId = "Tbl_SugMivne")]
    // Category: פרטים מזהים | Field Name: סוג המבנה | Foreign Field ID: Tbl_SugMivne | Event Log: No
    public int? SugMivne { get; set; }

    [Column("IsUnitInEmptyBldg", TypeName = "integer")]
    [Display(Name = "האם מדובר ביחידה בתוך בניין שכולו ריק")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "האם מדובר ביחידה בתוך בניין שכולו ריק", ForeignFieldId = "Tbl_Y_N_Maybe")]
    // Category: פרטים מזהים | Field Name: האם מדובר ביחידה בתוך בניין שכולו ריק | Foreign Field ID: Tbl_Y_N_Maybe | Event Log: No
    public int? IsUnitInEmptyBuilding { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "אחוז המבנה שמוגדר ניזוק")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "אחוז המבנה שמוגדר ניזוק")]
    // Category: פרטים מזהים | Field Name: אחוז המבנה שמוגדר ניזוק | Foreign Field ID: — | Event Log: No
    public int? DamagePercentage { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "הקומה בה נמצא החלק הנטוש")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "הקומה בה נמצא החלק הנטוש")]
    // Category: פרטים מזהים | Field Name: הקומה בה נמצא החלק הנטוש | Foreign Field ID: — | Event Log: No
    public int? FloorNum { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "פרטי בעלים")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "פרטי בעלים", IncludeInEventLog = true)]
    // Category: פרטים מזהים | Field Name: פרטי בעלים | Foreign Field ID: — | Event Log: Yes
    public string? OwnerDetails { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "פרטי מחזיקים")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "פרטי מחזיקים", IncludeInEventLog = true)]
    // Category: פרטים מזהים | Field Name: פרטי מחזיקים | Foreign Field ID: — | Event Log: Yes
    public string? HolderDetails { get; set; }

    [Column("PropNum", TypeName = "integer")]
    [Display(Name = "מספר הנכס (אם נכס עירוני)")]
    [FieldSpec(Category = "פרטים מזהים", FieldName = "מספר הנכס (אם נכס עירוני)")]
    // Category: פרטים מזהים | Field Name: מספר הנכס (אם נכס עירוני) | Foreign Field ID: — | Event Log: No
    public int? PropNum { get; set; }

    // Usage
    [Column(TypeName = "integer")]
    [Display(Name = "האם הייתה צריכת מים ב־6 החודשים האחרונים")]
    [FieldSpec(Category = "מאפייני שימוש", FieldName = "האם הייתה צריכת מים ב־6 החודשים האחרונים", ForeignFieldId = "Tbl_Y_N_Maybe")]
    // Category: מאפייני שימוש | Field Name: האם הייתה צריכת מים ב־6 החודשים האחרונים | Foreign Field ID: Tbl_Y_N_Maybe | Event Log: No
    public int? WaterConsumption { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "משך הזמן מאז צריכת המים האחרונה")]
    [FieldSpec(Category = "מאפייני שימוש", FieldName = "משך הזמן מאז צריכת המים האחרונה")]
    // Category: מאפייני שימוש | Field Name: משך הזמן מאז צריכת המים האחרונה | Foreign Field ID: — | Event Log: No
    public string? TimeFromLastWaterConsumption { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "האם הייתה צריכת חשמל ב־6 החודשים האחרונים")]
    [FieldSpec(Category = "מאפייני שימוש", FieldName = "האם הייתה צריכת חשמל ב־6 החודשים האחרונים", ForeignFieldId = "Tbl_Y_N_Maybe")]
    // Category: מאפייני שימוש | Field Name: האם הייתה צריכת חשמל ב־6 החודשים האחרונים | Foreign Field ID: Tbl_Y_N_Maybe | Event Log: No
    public int? ElectricityConsumption { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "משך הזמן מאז השימוש האחרון בחשמל")]
    [FieldSpec(Category = "מאפייני שימוש", FieldName = "משך הזמן מאז השימוש האחרון בחשמל")]
    // Category: מאפייני שימוש | Field Name: משך הזמן מאז השימוש האחרון בחשמל | Foreign Field ID: — | Event Log: No
    public string? TimeFromLastElectricityConsumption { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "הסיבה לאי-שימוש")]
    [FieldSpec(Category = "מאפייני שימוש", FieldName = "הסיבה לאי-שימוש", IncludeInEventLog = true)]
    // Category: מאפייני שימוש | Field Name: הסיבה לאי-שימוש | Foreign Field ID: — | Event Log: Yes
    public string? ReasonForNonUse { get; set; }

    // Planning & permitting
    [Column(TypeName = "integer")]
    [Display(Name = "יעוד")]
    [FieldSpec(Category = "תכנון ורישוי", FieldName = "יעוד", ForeignFieldId = "Tbl_YK")]
    // Category: תכנון ורישוי | Field Name: יעוד | Foreign Field ID: Tbl_YK | Event Log: No
    public int? Yeud { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "שימוש בפועל")]
    [FieldSpec(Category = "תכנון ורישוי", FieldName = "שימוש בפועל")]
    // Category: תכנון ורישוי | Field Name: שימוש בפועל | Foreign Field ID: — | Event Log: No
    public string? ActualUse { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "קוד שימוש בארנונה")]
    [FieldSpec(Category = "תכנון ורישוי", FieldName = "קוד שימוש בארנונה", ForeignFieldId = "Tbl_SugShimush")]
    // Category: תכנון ורישוי | Field Name: קוד שימוש בארנונה | Foreign Field ID: Tbl_SugShimush | Event Log: No
    public int? ArnonaCodeShimush { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "היתר בניה")]
    [FieldSpec(Category = "תכנון ורישוי", FieldName = "היתר בניה", ForeignFieldId = "Tbl_IsThere", IncludeInEventLog = true)]
    // Category: תכנון ורישוי | Field Name: היתר בניה | Foreign Field ID: Tbl_IsThere | Event Log: Yes
    public int? HeterBniya { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "טופס 4")]
    [FieldSpec(Category = "תכנון ורישוי", FieldName = "טופס 4", ForeignFieldId = "Tbl_IsThere", IncludeInEventLog = true)]
    // Category: תכנון ורישוי | Field Name: טופס 4 | Foreign Field ID: Tbl_IsThere | Event Log: Yes
    public int? Tofes4 { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "האם יש חריגת בניה")]
    [FieldSpec(Category = "תכנון ורישוי", FieldName = "האם יש חריגת בניה", ForeignFieldId = "Tbl_Y_N_Maybe", IncludeInEventLog = true)]
    // Category: תכנון ורישוי | Field Name: האם יש חריגת בניה | Foreign Field ID: Tbl_Y_N_Maybe | Event Log: Yes
    public int? HarigatBniya { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "אישור שימוש חורג")]
    [FieldSpec(Category = "תכנון ורישוי", FieldName = "אישור שימוש חורג", ForeignFieldId = "Tbl_IsThere", IncludeInEventLog = true)]
    // Category: תכנון ורישוי | Field Name: אישור שימוש חורג | Foreign Field ID: Tbl_IsThere | Event Log: Yes
    public int? IsurShimushHoreg { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "שטח החלקה (מ״ר)")]
    [FieldSpec(Category = "תכנון ורישוי", FieldName = "שטח החלקה (מ״ר)")]
    // Category: תכנון ורישוי | Field Name: שטח החלקה (מ״ר) | Foreign Field ID: — | Event Log: No
    public string? ParcelSize { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "סה\"כ זכויות בניה מאושרות (מ״ר)")]
    [FieldSpec(Category = "תכנון ורישוי", FieldName = "סה\"כ זכויות בניה מאושרות (מ״ר)")]
    // Category: תכנון ורישוי | Field Name: סה"כ זכויות בניה מאושרות (מ״ר) | Foreign Field ID: — | Event Log: No
    public string? BuildRights { get; set; }

    [Column("ShtachBanuySum", TypeName = "text")]
    [Display(Name = "סה\"כ שטח בנוי (מ״ר)")]
    [FieldSpec(Category = "תכנון ורישוי", FieldName = "סה\"כ שטח בנוי (מ״ר)")]
    // Category: תכנון ורישוי | Field Name: סה"כ שטח בנוי (מ״ר) | Foreign Field ID: — | Event Log: No
    public string? ShtachBanuySum { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "מספר קומות")]
    [FieldSpec(Category = "תכנון ורישוי", FieldName = "מספר קומות")]
    // Category: תכנון ורישוי | Field Name: מספר קומות | Foreign Field ID: — | Event Log: No
    public int? FloorSum { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "סטטוס קידום תכנון")]
    [FieldSpec(Category = "תכנון ורישוי", FieldName = "סטטוס קידום תכנון", ForeignFieldId = "Tbl_KidumTichnun", IncludeInEventLog = true)]
    // Category: תכנון ורישוי | Field Name: סטטוס קידום תכנון | Foreign Field ID: Tbl_KidumTichnun | Event Log: Yes
    public int? KidumTichnunStatus { get; set; }

    // Availability & rehabilitation readiness
    [Column(TypeName = "integer")]
    [Display(Name = "סוג הבעלות")]
    [FieldSpec(Category = "זמינות והיתכנות לשיקום", FieldName = "סוג הבעלות", ForeignFieldId = "Tbl_SugBaalut")]
    // Category: זמינות והיתכנות לשיקום | Field Name: סוג הבעלות | Foreign Field ID: Tbl_SugBaalut | Event Log: No
    public int? SugBaalut { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "מוכנות הבעלים למכור")]
    [FieldSpec(Category = "זמינות והיתכנות לשיקום", FieldName = "מוכנות הבעלים למכור", ForeignFieldId = "Tbl_Y_N_Maybe", IncludeInEventLog = true)]
    // Category: זמינות והיתכנות לשיקום | Field Name: מוכנות הבעלים למכור | Foreign Field ID: Tbl_Y_N_Maybe | Event Log: Yes
    public int? WantToSell { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "מוכנות הבעלים להשכיר")]
    [FieldSpec(Category = "זמינות והיתכנות לשיקום", FieldName = "מוכנות הבעלים להשכיר", ForeignFieldId = "Tbl_Y_N_Maybe", IncludeInEventLog = true)]
    // Category: זמינות והיתכנות לשיקום | Field Name: מוכנות הבעלים להשכיר | Foreign Field ID: Tbl_Y_N_Maybe | Event Log: Yes
    public int? WantToRent { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "תהליך העברת חזקה בפועל")]
    [FieldSpec(Category = "זמינות והיתכנות לשיקום", FieldName = "תהליך העברת חזקה בפועל", ForeignFieldId = "Tbl_HezkaMove", IncludeInEventLog = true)]
    // Category: זמינות והיתכנות לשיקום | Field Name: תהליך העברת חזקה בפועל | Foreign Field ID: Tbl_HezkaMove | Event Log: Yes
    public int? HezkaMove { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "הימצאות אנשים או חפצים בנכס")]
    [FieldSpec(Category = "זמינות והיתכנות לשיקום", FieldName = "הימצאות אנשים או חפצים בנכס", ForeignFieldId = "Tbl_WhatsInside", IncludeInEventLog = true)]
    // Category: זמינות והיתכנות לשיקום | Field Name: הימצאות אנשים או חפצים בנכס | Foreign Field ID: Tbl_WhatsInside | Event Log: Yes
    public int? WhatsInside { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "יכולת העירייה לשקם את הנכס בעצמה")]
    [FieldSpec(Category = "זמינות והיתכנות לשיקום", FieldName = "יכולת העירייה לשקם את הנכס בעצמה", ForeignFieldId = "Tbl_Y_N_Maybe", IncludeInEventLog = true)]
    // Category: זמינות והיתכנות לשיקום | Field Name: יכולת העירייה לשקם את הנכס בעצמה | Foreign Field ID: Tbl_Y_N_Maybe | Event Log: Yes
    public int? CanMuniFix { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "עמדת הבעלים")]
    [FieldSpec(Category = "זמינות והיתכנות לשיקום", FieldName = "עמדת הבעלים", IncludeInEventLog = true)]
    // Category: זמינות והיתכנות לשיקום | Field Name: עמדת הבעלים | Foreign Field ID: — | Event Log: Yes
    public string? OwnerPosition { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "עמדת העירייה")]
    [FieldSpec(Category = "זמינות והיתכנות לשיקום", FieldName = "עמדת העירייה", IncludeInEventLog = true)]
    // Category: זמינות והיתכנות לשיקום | Field Name: עמדת העירייה | Foreign Field ID: — | Event Log: Yes
    public string? MiuniPosition { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "ציון עמידה בסטנדרט")]
    [FieldSpec(Category = "זמינות והיתכנות לשיקום", FieldName = "ציון עמידה בסטנדרט", IncludeInEventLog = true)]
    // Category: זמינות והיתכנות לשיקום | Field Name: ציון עמידה בסטנדרט | Foreign Field ID: — | Event Log: Yes
    public int? StandardMark { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "כלול בפיילוט")]
    [FieldSpec(Category = "זמינות והיתכנות לשיקום", FieldName = "כלול בפיילוט", ForeignFieldId = "Tbl_Y_N_Maybe")]
    // Category: זמינות והיתכנות לשיקום | Field Name: כלול בפיילוט | Foreign Field ID: Tbl_Y_N_Maybe | Event Log: No
    public int? InPilot { get; set; }

    // Legal status
    [Column(TypeName = "integer")]
    [Display(Name = "שעבוד")]
    [FieldSpec(Category = "סטטוס משפטי", FieldName = "שעבוד", ForeignFieldId = "Tbl_Y_N_Maybe", IncludeInEventLog = true)]
    // Category: סטטוס משפטי | Field Name: שעבוד | Foreign Field ID: Tbl_Y_N_Maybe | Event Log: Yes
    public int? Shiabud { get; set; }

    [Column("OwnerUnderExec", TypeName = "integer")]
    [Display(Name = "הבעלים תחת הוצאה לפועל")]
    [FieldSpec(Category = "סטטוס משפטי", FieldName = "הבעלים תחת הוצאה לפועל", ForeignFieldId = "Tbl_Y_N_Maybe", IncludeInEventLog = true)]
    // Category: סטטוס משפטי | Field Name: הבעלים תחת הוצאה לפועל | Foreign Field ID: Tbl_Y_N_Maybe | Event Log: Yes
    public int? OwnerUnderExec { get; set; }

    [Column("LegalDespute", TypeName = "integer")]
    [Display(Name = "סכסוך משפטי בין הבעלים")]
    [FieldSpec(Category = "סטטוס משפטי", FieldName = "סכסוך משפטי בין הבעלים", ForeignFieldId = "Tbl_Y_N_Maybe", IncludeInEventLog = true)]
    // Category: סטטוס משפטי | Field Name: סכסוך משפטי בין הבעלים | Foreign Field ID: Tbl_Y_N_Maybe | Event Log: Yes
    public int? LegalDespute { get; set; }

    [Column("ArnonaDept", TypeName = DbTypes.Money)]
    [Display(Name = "יתרת חוב לארנונה")]
    [FieldSpec(Category = "סטטוס משפטי", FieldName = "יתרת חוב לארנונה", IncludeInEventLog = true)]
    // Category: סטטוס משפטי | Field Name: יתרת חוב לארנונה | Foreign Field ID: — | Event Log: Yes
    public Money? ArnonaDept { get; set; }

    // Physical status
    [Column("maintenance", TypeName = "integer")]
    [Display(Name = "תחזוקה")]
    [FieldSpec(Category = "מצב פיזי", FieldName = "תחזוקה", ForeignFieldId = "Tbl_maintenance", IncludeInEventLog = true)]
    // Category: מצב פיזי | Field Name: תחזוקה | Foreign Field ID: Tbl_maintenance | Event Log: Yes
    public int? Maintenance { get; set; }

    [Column("DangerousBldg", TypeName = "integer")]
    [Display(Name = "מבנה מסוכן")]
    [FieldSpec(Category = "מצב פיזי", FieldName = "מבנה מסוכן", ForeignFieldId = "Tbl_DangerousBldg", IncludeInEventLog = true)]
    // Category: מצב פיזי | Field Name: מבנה מסוכן | Foreign Field ID: Tbl_DangerousBldg | Event Log: Yes
    public int? DangerousBldg { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "איטום")]
    [FieldSpec(Category = "מצב פיזי", FieldName = "איטום", ForeignFieldId = "Tbl_Itum", IncludeInEventLog = true)]
    // Category: מצב פיזי | Field Name: איטום | Foreign Field ID: Tbl_Itum | Event Log: Yes
    public int? Itum { get; set; }

    // Development potential
    [Column(TypeName = "integer")]
    [Display(Name = "איכות הקרקע")]
    [FieldSpec(Category = "פוטנציאל פיתוח", FieldName = "איכות הקרקע", ForeignFieldId = "Tbl_LandQuality")]
    // Category: פוטנציאל פיתוח | Field Name: איכות הקרקע | Foreign Field ID: Tbl_LandQuality | Event Log: No
    public int? LandQuality { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "זכויות בניה לא מנוצלות")]
    [FieldSpec(Category = "פוטנציאל פיתוח", FieldName = "זכויות בניה לא מנוצלות", ForeignFieldId = "BuildRights - ShtachBanuySum =")]
    // Category: פוטנציאל פיתוח | Field Name: זכויות בניה לא מנוצלות | Foreign Field ID: BuildRights - ShtachBanuySum = | Event Log: No
    public int? BldgRightsNotUsed { get; set; }

    [Column(TypeName = "integer")]
    [Display(Name = "יעוד לשימור")]
    [FieldSpec(Category = "פוטנציאל פיתוח", FieldName = "יעוד לשימור", ForeignFieldId = "Tbl_ForShimur")]
    // Category: פוטנציאל פיתוח | Field Name: יעוד לשימור | Foreign Field ID: Tbl_ForShimur | Event Log: No
    public int? ForShimur { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "יעוד אפשרי בעתיד")]
    [FieldSpec(Category = "פוטנציאל פיתוח", FieldName = "יעוד אפשרי בעתיד")]
    // Category: פוטנציאל פיתוח | Field Name: יעוד אפשרי בעתיד | Foreign Field ID: — | Event Log: No
    public string? FutureYeud { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "שימוש אפשרי בעתיד")]
    [FieldSpec(Category = "פוטנציאל פיתוח", FieldName = "שימוש אפשרי בעתיד")]
    // Category: פוטנציאל פיתוח | Field Name: שימוש אפשרי בעתיד | Foreign Field ID: — | Event Log: No
    public string? FutureUse { get; set; }

    // Enforcement
    [Column(TypeName = "text")]
    [Display(Name = "פיקוח כללי")]
    [FieldSpec(Category = "אכיפה", FieldName = "פיקוח כללי", IncludeInEventLog = true)]
    // Category: אכיפה | Field Name: פיקוח כללי | Foreign Field ID: — | Event Log: Yes
    public string? PikuachKlali { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "פיקוח על הבניה")]
    [FieldSpec(Category = "אכיפה", FieldName = "פיקוח על הבניה", IncludeInEventLog = true)]
    // Category: אכיפה | Field Name: פיקוח על הבניה | Foreign Field ID: — | Event Log: Yes
    public string? PikuachAlBniya { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "צו מבנה מסוכן")]
    [FieldSpec(Category = "אכיפה", FieldName = "צו מבנה מסוכן", IncludeInEventLog = true)]
    // Category: אכיפה | Field Name: צו מבנה מסוכן | Foreign Field ID: — | Event Log: Yes
    public string? TzavDangerBldg { get; set; }

    [Column(TypeName = "text")]
    [Display(Name = "צו שיפוץ חזיתות")]
    [FieldSpec(Category = "אכיפה", FieldName = "צו שיפוץ חזיתות", IncludeInEventLog = true)]
    // Category: אכיפה | Field Name: צו שיפוץ חזיתות | Foreign Field ID: — | Event Log: Yes
    public string? TzavShiputzFronts { get; set; }

    // App-specific fields not defined in the CSV spec
    [Column(TypeName = "text")]
    public string Neighborhood { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string PhotoUrls { get; set; } = string.Empty;

    public ICollection<BuildingLog> Logs { get; set; } = new List<BuildingLog>();
    public ICollection<ExternalSystemSnapshot> ExternalSnapshots { get; set; } = new List<ExternalSystemSnapshot>();
}
