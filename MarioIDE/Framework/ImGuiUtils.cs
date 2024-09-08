using ImGuiNET;
using System;
using System.Numerics;

namespace MarioIDE.Framework;

public static class ImGuiUtils
{
    public static void DrawFullscreen(Action action)
    {
        ImGui.SetNextWindowPos(new Vector2(0, 0));
        ImGui.SetNextWindowSize(new Vector2(ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y));

        ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 2);

        if (ImGui.Begin("fullscreen",
                ImGuiWindowFlags.NoNavFocus |
                ImGuiWindowFlags.NoNav |
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoScrollbar
            ))
        {
            action();
            ImGui.End();
        }

        ImGui.PopStyleVar(4);
        ImGui.PopStyleColor(1);
    }

    public static uint Color(byte r, byte g, byte b, byte a)
    {
        uint ret = a;
        ret <<= 8;
        ret += b;
        ret <<= 8;
        ret += g;
        ret <<= 8;
        ret += r;
        return ret;
    }
}