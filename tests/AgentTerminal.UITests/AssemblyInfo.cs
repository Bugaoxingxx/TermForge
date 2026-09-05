using Xunit;

// UI 自动化测试必须单线程顺序执行，避免多窗口并发导致桌面焦点抢占与 UIA 树竞争
[assembly: CollectionBehavior(DisableTestParallelization = true)]
