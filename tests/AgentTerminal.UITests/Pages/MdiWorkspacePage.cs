using System;
using System.Linq;
using System.Collections.Generic;
using FlaUI.Core.AutomationElements;
using AgentTerminal.UITests.Infrastructure;

namespace AgentTerminal.UITests.Pages;

public class MdiWorkspacePage
{
    private readonly AutomationElement _container;

    public MdiWorkspacePage(AutomationElement container)
    {
        _container = container;
    }

    public AutomationElement Container => _container;

    public IReadOnlyList<MdiChildWindowPage> GetChildWindows()
    {
        var elements = _container.FindAllDescendants(cf => cf.ByAutomationId("MdiChildWindow"));
        return elements.Select(e => new MdiChildWindowPage(e)).ToList();
    }

    public MdiChildWindowPage? FindWindowByTitle(string title)
    {
        return GetChildWindows().FirstOrDefault(w => w.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
    }

    public void WaitForWindowCount(int expectedCount, TimeSpan? timeout = null)
    {
        UiaWait.UntilTrue(() => GetChildWindows().Count == expectedCount,
            timeout: timeout ?? TimeSpan.FromSeconds(5),
            message: $"Timed out waiting for child window count to be {expectedCount}. Current count: {GetChildWindows().Count}");
    }
}
