namespace RibbonKit.WinForms;

/// <summary>Requests native top-level window corners from Windows 11 build 22000 or later. Windows owns the final silhouette, including maximized and snapped windows.</summary>
public enum RibbonWindowCornerPreference
{
    /// <summary>Lets Windows choose the window corners.</summary>
    SystemDefault = 0,
    /// <summary>Requests square window corners.</summary>
    Square = 1,
    /// <summary>Requests rounded window corners when appropriate.</summary>
    Rounded = 2,
    /// <summary>Requests rounded window corners with a smaller radius when appropriate.</summary>
    SmallRounded = 3
}
