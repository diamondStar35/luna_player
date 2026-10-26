using LunaPlayer.Actions;
using WxSharp;

namespace LunaPlayer.UI;

internal readonly record struct ChapterMenuEntry(int Index, string Label);

internal sealed record ChapterMenuComponents(
    Menu Menu,
    MenuItem Root,
    MenuItem Stop,
    MenuItem Previous,
    MenuItem Next,
    MenuItem Audiobook);

internal static partial class MainMenuBuilder
{
    internal static ChapterMenuComponents BuildChaptersMenu(
        Menu player, IReadOnlyDictionary<ActionId, int> commandIds, ShortcutManager shortcuts)
    {
        var menu = new Menu();
        // This fixed tail is never rebuilt: deleting permanent command ids makes wx reuse reserved ids.
        menu.AppendSeparator();
        var audiobook = menu.AppendCheckItem(commandIds[ActionId.ToggleAudiobookMode],
            // Translators: Checkable Chapters menu command that treats chapters as separate playlist items.
            Label(Tr("Audiobook mode"), ActionId.ToggleAudiobookMode, shortcuts));
        var stop = menu.AppendCheckItem(commandIds[ActionId.ToggleChapterStop],
            // Translators: Checkable Chapters menu command that pauses once at the end of the current chapter.
            Label(Tr("Stop playback at the end of current chapter"), ActionId.ToggleChapterStop, shortcuts));
        var previous = menu.Append(commandIds[ActionId.PreviousChapter],
            // Translators: Chapters menu command that jumps to the preceding chapter.
            Label(Tr("Previous chapter"), ActionId.PreviousChapter, shortcuts));
        var next = menu.Append(commandIds[ActionId.NextChapter],
            // Translators: Chapters menu command that jumps to the following chapter.
            Label(Tr("Next chapter"), ActionId.NextChapter, shortcuts));
        // Translators: Player submenu listing chapters in the current source file and their durations.
        var root = player.AppendSubMenu(menu, Tr("Chapters"));
        return new ChapterMenuComponents(menu, root, stop, previous, next, audiobook);
    }
}
