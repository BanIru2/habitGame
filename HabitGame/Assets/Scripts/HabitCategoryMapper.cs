using System;

public static class HabitCategoryMapper
{
    public static string ToCategoryId(HabitCategory category)
    {
        return category switch
        {
            HabitCategory.Physical => "physical",
            HabitCategory.Biorhythm => "biorhythm",
            HabitCategory.Environment => "environment",
            HabitCategory.SelfDev => "selfdev",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
    }

    public static HabitCategory ToHabitCategory(string categoryId)
    {
        return categoryId switch
        {
            "physical" => HabitCategory.Physical,
            "biorhythm" => HabitCategory.Biorhythm,
            "environment" => HabitCategory.Environment,
            "selfdev" => HabitCategory.SelfDev,
            _ => throw new ArgumentException($"Unknown habit category id: {categoryId}", nameof(categoryId))
        };
    }

    public static bool TryToHabitCategory(string categoryId, out HabitCategory category)
    {
        switch (categoryId)
        {
            case "physical":
                category = HabitCategory.Physical;
                return true;
            case "biorhythm":
                category = HabitCategory.Biorhythm;
                return true;
            case "environment":
                category = HabitCategory.Environment;
                return true;
            case "selfdev":
                category = HabitCategory.SelfDev;
                return true;
            default:
                category = default;
                return false;
        }
    }
}
