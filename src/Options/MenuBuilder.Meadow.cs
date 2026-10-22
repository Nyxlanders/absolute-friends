namespace AbsoluteFriends.Options;

public abstract partial class MenuBuilder
{
    private protected const string DisabledInMeadowNote = "Doesn't work in Rain Meadow.";

    private protected const string PartlyDisabledInMeadowNote = "Some of these don't work in Rain Meadow.";

    private protected const string UnsyncedInMeadowNote = "Isn't synced in Rain Meadow.";

    private protected const string PartlyUnsyncedInMeadowNote = "Some of these aren't synced in Rain Meadow.";

    private const string MeadowMark = " *";

    private protected static bool IsMarkedDisabledInMeadow(ConfigurableBase? configurableBase) => configurableBase.IsOffInMeadow && MeadowOptions.IsMeadowInstalled;

    private protected static string? MeadowNoteOf(IReadOnlyCollection<ConfigurableBase> options)
    {
        return NoteOf(options.Count(IsMarkedDisabledInMeadow), options.Count, DisabledInMeadowNote, PartlyDisabledInMeadowNote)
            ?? NoteOf(options.Count(IsMarkedUnsyncedInMeadow), options.Count, UnsyncedInMeadowNote, PartlyUnsyncedInMeadowNote);
    }

    private static bool IsMarkedUnsyncedInMeadow(ConfigurableBase? configurableBase) => configurableBase.IsUnsyncedInMeadow && MeadowOptions.IsMeadowInstalled;

    private static string? NoteOf(int marked, int count, string allNote, string partlyNote) => marked == 0 ? null : marked == count ? allNote : partlyNote;

    private string DescriptionOf(ConfigurableBase configurableBase) => WithMeadowNote(Translate(configurableBase.Description ?? ""), MeadowNoteOf([configurableBase]));

    private string LabelOf(ConfigurableBase configurableBase, string text) => WithMeadowMark(Translate(text), IsMarkedDisabledInMeadow(configurableBase));

    private string WithMeadowNote(string description, string? note) => note == null
        ? description
        : description.Length == 0 ? Translate(note) : $"{description}\n{Translate(note)}";

    private static string WithMeadowMark(string label, bool marked) => marked ? label + MeadowMark : label;
}
