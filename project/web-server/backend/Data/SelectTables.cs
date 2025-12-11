using System.Collections.ObjectModel;

namespace WebServer.Data;

public sealed record SelectOption(int Value, string Label);

/// <summary>
/// Static registry of select-table options, keyed by SelectTableName (from Data.csv / FieldSpec).
/// </summary>
public static class SelectTables
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<SelectOption>> Tables =
        new ReadOnlyDictionary<string, IReadOnlyList<SelectOption>>(
            new Dictionary<string, IReadOnlyList<SelectOption>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tbl_Sivug"] = new[]
                {
                    new SelectOption(1, "ריק בהגדרה"),
                    new SelectOption(2, "ריק באישור ולא בטיפול"),
                    new SelectOption(3, "ריק ובהליך שיקום"),
                    new SelectOption(4, "ריק ונטוש"),
                    new SelectOption(5, "מאוכלס")
                },
                ["Tbl_StatusShikum"] = new[]
                {
                    new SelectOption(1, "מיפוי החסמים וגיבוש פתרון"),
                    new SelectOption(2, "העברת בעלות"),
                    new SelectOption(3, "חסמים המונעים פיתוח"),
                    new SelectOption(4, "הבעלים בוחן אפיק פעולה לשיקום"),
                    new SelectOption(5, "הכנת תכנית שיקום"),
                    new SelectOption(6, "תכנית מאושרת, הכנה לביצוע"),
                    new SelectOption(7, "בביצוע"),
                    new SelectOption(8, "הליך אכלוס")
                },
                ["Tbl_SugMivne"] = new[]
                {
                    new SelectOption(1, "יחידה בבניין"),
                    new SelectOption(2, "בניין"),
                    new SelectOption(3, "מבנה עזר")
                },
                ["Tbl_Y_N_Maybe"] = new[]
                {
                    new SelectOption(1, "כן"),
                    new SelectOption(2, "לא"),
                    new SelectOption(3, "לא ידוע")
                },
                ["Tbl_SugBaalut"] = new[]
                {
                    new SelectOption(1, "פרטי (בודד)"),
                    new SelectOption(2, "פרטי (מרובה)"),
                    new SelectOption(3, "פרטי (תאגיד)"),
                    new SelectOption(4, "ממשלה"),
                    new SelectOption(5, "רשות מקומית"),
                    new SelectOption(6, "ציבורי אחר"),
                    new SelectOption(7, "בעלות מעורבת (ציבור ופרטי)"),
                    new SelectOption(8, "בעלות לא ידועה")
                },
                ["Tbl_YK"] = new[]
                {
                    new SelectOption(1, "מגורים"),
                    new SelectOption(2, "מסחר"),
                    new SelectOption(3, "תעשיה"),
                    new SelectOption(4, "מעורב"),
                    new SelectOption(5, "וכו'")
                },
                ["Tbl_SugShimush"] = new[]
                {
                    new SelectOption(1, "מגורים - מתחיל ב-3"),
                    new SelectOption(2, "עסקים - עסקים/תעשיה/מלאכה מתחיל ב-6")
                },
                ["Tbl_IsThere"] = new[]
                {
                    new SelectOption(1, "קיים"),
                    new SelectOption(2, "לא קיים"),
                    new SelectOption(3, "קיים לחלק מהמבנה")
                },
                ["Tbl_KidumTichnun"] = new[]
                {
                    new SelectOption(1, "בתהליך חיפוש קונים"),
                    new SelectOption(2, "בתהליך חיפוש שוכרים"),
                    new SelectOption(3, "בתהליך קידום תב\"ע"),
                    new SelectOption(4, "בתהליך קידום היתר"),
                    new SelectOption(5, "בתהליך שיפוץ"),
                    new SelectOption(6, "לא קורה כלום")
                },
                ["Tbl_HezkaMove"] = new[]
                {
                    new SelectOption(1, "תהליך העברת חזקה בפועל - שלב 1"),
                    new SelectOption(2, "תהליך העברת חזקה בפועל - שלב 2"),
                    new SelectOption(3, "תהליך העברת חזקה בפועל - שלב 3")
                },
                ["Tbl_WhatsInside"] = new[]
                {
                    new SelectOption(1, "ריק לגמרי"),
                    new SelectOption(2, "מאוכלס ע\"י הבעלים"),
                    new SelectOption(3, "מאוכלס ע\"י צד ג' באישור הבעלים"),
                    new SelectOption(4, "מאוכלס ע\"י פולס")
                },
                ["Tbl_maintenance"] = new[]
                {
                    new SelectOption(1, "טוב/דורש השקעה מינורית"),
                    new SelectOption(2, "דורש השקעה גדולה אך בעלות סבירה"),
                    new SelectOption(3, "דורש השקעה גדולה מאוד ולא כלכלית")
                },
                ["Tbl_DangerousBldg"] = new[]
                {
                    new SelectOption(1, "יש"),
                    new SelectOption(2, "אין")
                },
                ["Tbl_SuspectedDangerBldg"] = new[]
                {
                    new SelectOption(1, "יש"),
                    new SelectOption(2, "אין")
                },
                ["Tbl_Itum"] = new[]
                {
                    new SelectOption(1, "סגור ואטום"),
                    new SelectOption(2, "פרוץ")
                },
                ["Tbl_LandQuality"] = new[]
                {
                    new SelectOption(1, "נקיה"),
                    new SelectOption(2, "זיהום/חשד לזיהום"),
                    new SelectOption(3, "לא ידוע")
                },
                ["Tbl_ForShimur"] = new[]
                {
                    new SelectOption(1, "מיועד לשימור"),
                    new SelectOption(2, "בעל ערכים אך טרם יועד לשימור"),
                    new SelectOption(3, "לא מיועד לשימור")
                },
                ["Tbl_PtorStage"] = new[]
                {
                    new SelectOption(1, "ראשונה"),
                    new SelectOption(2, "שניה"),
                    new SelectOption(3, "שלישית")
                }
            });

    public static IReadOnlyList<SelectOption> GetOptions(string? selectTableName)
    {
        if (string.IsNullOrWhiteSpace(selectTableName))
        {
            return Array.Empty<SelectOption>();
        }

        return Tables.TryGetValue(selectTableName, out var options)
            ? options
            : Array.Empty<SelectOption>();
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<SelectOption>> GetAllTables() => Tables;
}
