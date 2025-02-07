namespace StudioDrydock.AppStoreConnect.Lib.Model;

public static class EnumExtensions<Dest>
    where Dest : struct, Enum
{
    public static Dest? Convert<Src>(Src? src)
        where Src : struct, Enum
    {
        if (!src.HasValue)
        {
            return null;
        }

        var name = Enum.GetName(src.Value);

        if (name == null)
        {
            return null;
        }

        return Enum.Parse<Dest>(name);
    }
}