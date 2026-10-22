namespace AbsoluteFriends.Visualizer;

internal static class MeadowTags
{
    internal static bool IsShown => RainMeadow.RainMeadow.rainMeadowOptions?.ShowFriends.Value == true;
}
