using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Waypoint;

/// <summary>
/// Quick functions: the chat lines the player sends most, one button each, handed to the client control bridge as if
/// typed into the chat box. The player's name is typed once and used by every party line, and a line typed by hand is
/// sent as it is. A release install has no bridge, so every button copies its line instead.
/// </summary>
public partial class QuickTab : UserControl
{
    Shared _shared = null!;

    public QuickTab() => InitializeComponent();

    internal void Attach(Shared shared)
    {
        _shared = shared;
        Build();
        L.Current.PropertyChanged += (_, _) => Build();
    }

    /// <summary>One card per group, one button per line; the panel is built again when the language changes.</summary>
    void Build()
    {
        GroupList.Children.Clear();
        foreach (var (group, actions) in QuickActions.Groups)
        {
            var card = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            card.Children.Add(new TextBlock
            {
                Text = L.Text(group),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6),
            });
            var row = new WrapPanel();
            foreach (var action in actions)
            {
                var button = new Button
                {
                    Content = L.Text(action.Label),
                    MinWidth = 96,
                    Margin = new Thickness(0, 0, 8, 8),
                };
                button.Click += (_, _) => _ = Send(QuickActions.Line(action, Player.Text), action.TakesPlayer);
                row.Children.Add(button);
            }
            card.Children.Add(row);
            GroupList.Children.Add(card);
        }
    }

    void SendCustomClicked(object sender, RoutedEventArgs e)
    {
        if (QuickActions.Custom(CustomLine.Text) is not { } line)
        {
            State.Text = L.Text("Type a line that starts with a slash.");
            return;
        }
        _ = SendLine(line);
    }

    async Task Send(string? line, bool needsPlayer)
    {
        if (line is null)
        {
            State.Text = needsPlayer ? L.Text("Type the player name first.") : L.Text("Nothing to send.");
            return;
        }
        await SendLine(line);
    }

    /// <summary>One line to the client, or to the clipboard on a release install that has no bridge.</summary>
    async Task SendLine(string line)
    {
        try
        {
            if (Repo.IsInstall)
            {
                Clipboard.SetText(line);
                State.Text = L.Text("Copied {0}; paste it into the chat with Ctrl+V.", line);
                return;
            }
            var (_, replies) = ClientBridge.Replies(await _shared.Bridge.SendAsync(line));
            State.Text = replies.Count == 0
                ? L.Text("Sent {0}.", line)
                : L.Text("Sent {0}. {1}", line, string.Join(" ", replies));
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
        {
            State.Text = L.Text("Could not send: {0}", e.Message);
        }
    }
}
