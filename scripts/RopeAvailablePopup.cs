using Godot;
using System;

public partial class RopeAvailablePopup : PanelContainer
{
    public void OnRopeAvailable()
    {
        GD.Print("Rope available.");
        Show();
    }

    public void OnRopeUnavailable()
    {
        GD.Print("Rope unavailable.");
        Hide();
    }
}
