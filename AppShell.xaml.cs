namespace CaseIH8940MS;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		// Single page — hide the bottom tab bar so it reads more like a simple iOS screen.
		Shell.SetTabBarIsVisible(this, false);
		Routing.RegisterRoute("settings", typeof(SettingsPage));
	}
}
