using Godot;
using System;

public partial class RopeAvailablePopup : PanelContainer
{
    public void OnRopeAvailable()
    {
        Show();
    }

    public void OnRopeUnavailable()
    {
        Hide();
    }
}
