namespace easygame;

public struct ModTask
{
    public string Name { get; set; }
    public int Order { get; set; }
    public Func<bool> Condition { get; set; }
    public Action Callback { get; set; }
}