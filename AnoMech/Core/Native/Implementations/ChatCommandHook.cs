using System;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.Shell;

namespace AnoMech.Core.Native.Implementations;

// Every text command the game itself runs, typed or from a macro, after Dalamud has taken its own.
// Observe-only: the game's handler always runs, so whatever it would send still meets the packet
// firewall as before.
internal sealed unsafe class ChatCommandHook : IDisposable
{
    private readonly Hook<ShellCommandModule.Delegates.ExecuteCommandInner> hook;

    public event Action<string>? CommandRun;

    public ChatCommandHook(IGameInteropProvider interop)
    {
        hook = interop.HookFromAddress<ShellCommandModule.Delegates.ExecuteCommandInner>(
            ShellCommandModule.Addresses.ExecuteCommandInner.Value, Detour);
        hook.Enable();
    }

    private void Detour(ShellCommandModule* self, Utf8String* command, UIModule* uiModule)
    {
        hook.Original(self, command, uiModule);
        try
        {
            if (command != null) CommandRun?.Invoke(command->ToString());
        }
        catch (Exception e)
        {
            Plugin.Log.Error($"[ChatCommandHook] {e.Message}");
        }
    }

    public void Dispose() => hook.Dispose();
}
