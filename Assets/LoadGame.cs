/// <summary>
/// Navigation item intended to work with NavigationStack.cs
/// </summary>

public class LoadGame : NavAction
{
    public City city;

    public override void DoAction()
    {
        city.InitMap();
        NavigationStack.Instance.CloseMenu();
    }
}
