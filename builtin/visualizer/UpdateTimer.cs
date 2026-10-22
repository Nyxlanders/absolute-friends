namespace AbsoluteFriends.Visualizer;

internal class UpdateTimer
{
    private int _remaining;

    public void Reset() => _remaining = 0;

    public bool Elapse()
    {
        if (_remaining > 0)
        {
            --_remaining;

            return false;
        }

        _remaining = Config.UpdateInterval.Value - 1;

        return true;
    }
}
