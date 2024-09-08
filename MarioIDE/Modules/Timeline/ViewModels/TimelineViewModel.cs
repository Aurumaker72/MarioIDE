using Caliburn.Micro;
using Gemini;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using ImGuiNET;
using MarioIDE.Core.Enums;
using MarioIDE.Core.Logic;
using MarioIDE.Core.Modules.Timeline;
using MarioIDE.Framework.ViewModels;
using MarioIDE.Logic;
using MarioIDE.Modules.Timeline.Commands;
using MarioIDE.Mupen;
using MarioSharp;
using MarioSharp.Structs.Input;
using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Button = MarioSharp.Structs.Input.Button;
using InputManager = MarioIDE.Logic.InputManager;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace MarioIDE.Modules.Timeline.ViewModels;

[PartCreationPolicy(CreationPolicy.NonShared)]
public class TimelineViewModel : GlDocumentViewModel, IProject, ICommandHandler<ExportCommandDefinition>
{
    public GameVersion? GameVersion { get; private set; }
    public ISaveSystem SaveSystem { get; private set; }
    public int SelectionStart { get; private set; } = -1;
    public int SelectionEnd { get; private set; } = -1;

    private ComposablePart _part;
    private int _scrollToFrame = -1;
    private bool _shouldPopStyle;
    private float _itemHeight;

    public TimelineViewModel()
    {
        SaveSystem = new SaveSystem();
        Register();
    }

    public TimelineViewModel(GameVersion gameVersion) : this()
    {
        GameVersion = gameVersion;
        Initialize();
    }

    private void Register()
    {
        CompositionBatch batch = new();
        _part = batch.AddExportedValue<IProject>(this);
        AppBootstrapper.Container.Compose(batch);
    }

    private void Unregister()
    {
        CompositionBatch batch = new();
        batch.RemovePart(_part);
        AppBootstrapper.Container.Compose(batch);
    }

    private void Initialize()
    {
        if (GameVersion == null) return;
        byte[] dllBytes = Unlocker.VersionBytes[GameVersion.Value];
        SaveSystem.Init(GfxType.OpenGl, dllBytes);
    }

    public void Tick()
    {
        SaveSystem?.Tick();
    }

    public override void OnRender()
    {
        if (SaveSystem != null)
        {
            DrawTimeline();
        }
    }

    public void ScrollToFrame(int frame)
    {
        _scrollToFrame = frame;
    }

    public void SetSelectedRange(int selectionStart, int selectionEnd)
    {
        SelectionStart = selectionStart;
        SelectionEnd = selectionEnd;
    }

    private void DrawTimeline()
    {
        IPlaybackController playbackController = IoC.Get<IPlaybackController>();

        Vector2 size = new Vector2(Width - 14, Height - 40);
        const ImGuiTableFlags flags = ImGuiTableFlags.ScrollY
                                      | ImGuiTableFlags.RowBg
                                      | ImGuiTableFlags.BordersOuter
                                      | ImGuiTableFlags.BordersV
                                      | ImGuiTableFlags.Hideable
                                      | ImGuiTableFlags.Reorderable;

        int currentFrame = SaveSystem.CurrentFrame.Frame;

        if (_scrollToFrame != -1)
        {
            ImGui.SetNextWindowScroll(new Vector2(0, _scrollToFrame * _itemHeight - size.Y / 2f));
            _scrollToFrame = -1;
        }

        if (ImGui.BeginTable("Inputs", 16, flags, size)) // 31
        {
            ImGui.TableSetupScrollFreeze(0, 1); // Make top row always visible
            ImGui.TableSetupColumn("Frame", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder, 80);
            ImGui.TableSetupColumn("X", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder, 30);
            ImGui.TableSetupColumn("Y", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder, 30);
            ImGui.TableSetupColumn("S", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder, 20);
            ImGui.TableSetupColumn("A", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder, 20);
            ImGui.TableSetupColumn("B", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder, 20);
            ImGui.TableSetupColumn("Z", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder, 20);
            ImGui.TableSetupColumn("C^", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder, 20);
            ImGui.TableSetupColumn("Cv", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder, 20);
            ImGui.TableSetupColumn("C<", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder, 20);
            ImGui.TableSetupColumn("C>", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder, 20);
            ImGui.TableSetupColumn("R", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder, 20);
            ImGui.TableSetupColumn("Level");
            ImGui.TableSetupColumn("Action");
            ImGui.TableSetupColumn("HSpeed");
            ImGui.TableSetupColumn("HSlidingSpeed");

            ImGui.TableHeadersRow();

            ImGuiListClipperPtr clipper;
            unsafe
            {
                clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            }

            clipper.Begin(SaveSystem.InputManager.GetFrameCount());

            while (clipper.Step())
            {
                for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
                {
                    ImGui.TableNextRow();

                    FrameData frameData = ((SaveSystem)SaveSystem).GetFrameData(row);

                    if (_shouldPopStyle)
                    {
                        ImGui.PopStyleColor(2);
                        _shouldPopStyle = false;
                    }

                    if (row == currentFrame)
                    {
                        ImGui.PushStyleColor(ImGuiCol.TableRowBg, new Vector4(0.2f, 0.1f, 0.1f, 1.0f));
                        ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(0.2f, 0.14f, 0.14f, 1.0f));
                        _shouldPopStyle = true;
                    }

                    ImGui.TableSetColumnIndex(0);

                    bool sel = row >= Math.Min(SelectionStart, SelectionEnd) && row <= Math.Max(SelectionStart, SelectionEnd);
                    ImGui.Selectable("##select_" + row, ref sel, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap, new Vector2(0, 22));

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && ImGui.IsItemHovered(ImGuiHoveredFlags.RectOnly))
                    {
                        playbackController.SetPlaybackState(PlaybackState.Paused);
                        SelectionStart = row;
                        SelectionEnd = row;
                    }

                    if (SelectionStart != -1 && row != SelectionEnd && ImGui.IsMouseDown(ImGuiMouseButton.Left) && ImGui.IsItemHovered())
                    {
                        SelectionEnd = row;
                    }

                    ImGui.SameLine();

                    if (ImGui.Button(row + "##1", new Vector2(78, 16)))
                    {
                        playbackController.SetCurrentFrame(this, row);
                    }

                    OsContPad input = SaveSystem.InputManager.GetFrameInput(row);

                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(input.X.ToString());

                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(input.Y.ToString());

                    ImGui.TableSetColumnIndex(3);
                    DrawCheckboxForButton(ref input, Button.Start, row);

                    ImGui.TableSetColumnIndex(4);
                    DrawCheckboxForButton(ref input, Button.A, row);

                    ImGui.TableSetColumnIndex(5);
                    DrawCheckboxForButton(ref input, Button.B, row);

                    ImGui.TableSetColumnIndex(6);
                    DrawCheckboxForButton(ref input, Button.Z, row);

                    ImGui.TableSetColumnIndex(7);
                    DrawCheckboxForButton(ref input, Button.CUp, row);

                    ImGui.TableSetColumnIndex(8);
                    DrawCheckboxForButton(ref input, Button.CDown, row);

                    ImGui.TableSetColumnIndex(9);
                    DrawCheckboxForButton(ref input, Button.CLeft, row);

                    ImGui.TableSetColumnIndex(10);
                    DrawCheckboxForButton(ref input, Button.CRight, row);

                    ImGui.TableSetColumnIndex(11);
                    DrawCheckboxForButton(ref input, Button.R, row);

                    ImGui.TableSetColumnIndex(12);

                    ImGui.Text(Maps.GetBestMap(frameData.Level, frameData.Area));

                    ImGui.TableSetColumnIndex(13);
                    ImGui.Text(Actions.Dictionary.TryGetValue(frameData.Action, out string action) ? action : "UNKNOWN-ACTION");

                    ImGui.TableSetColumnIndex(14);
                    ImGui.Text(frameData.HSpeed.ToString(CultureInfo.InvariantCulture));

                    ImGui.TableSetColumnIndex(15);
                    ImGui.Text(frameData.HSlidingSpeed.ToString(CultureInfo.InvariantCulture));

                    SaveSystem.InputManager.SetFrameInput(row, input);
                }

                if (_shouldPopStyle)
                {
                    ImGui.PopStyleColor(2);
                    _shouldPopStyle = false;
                }
            }

            _itemHeight = clipper.ItemsHeight;
            ImGui.EndTable();
        }

        ImGui.Dummy(new Vector2(0, 4));

        if (ImGui.Button("<<", Vector2.One * 16))
        {
            //Rewind
            playbackController.SetPlaybackState(PlaybackState.PlayingBackward);
        }

        ImGui.SameLine(0, 2);

        if (ImGui.Button("<", Vector2.One * 16) || KeyboardState.IsKeyDown(Keys.Up) || KeyboardState.IsKeyPressed(Keys.Left))
        {
            //Step back
            playbackController.SetPlaybackState(PlaybackState.Paused);
            playbackController.StepBackward(this);
        }

        ImGui.SameLine(0, 2);

        if (ImGui.Button("||", Vector2.One * 16))
        {
            //Pause
            playbackController.SetPlaybackState(PlaybackState.Paused);
        }

        ImGui.SameLine(0, 2);

        if (ImGui.Button(">", Vector2.One * 16) || KeyboardState.IsKeyDown(Keys.Down) || KeyboardState.IsKeyPressed(Keys.Right))
        {
            //Step forward
            playbackController.SetPlaybackState(PlaybackState.Paused);
            playbackController.StepForward(this);
        }

        ImGui.SameLine(0, 2);

        if (ImGui.Button(">>", Vector2.One * 16))
        {
            //Play
            playbackController.SetPlaybackState(PlaybackState.PlayingForward);
        }
        
        ImGui.SameLine(0, 2);
        ImGui.Text($"{currentFrame}/{SaveSystem.InputManager.GetFrameCount()}");

        ImGui.SameLine(0, 2);
        bool vsync = Module.MaxFPS == 30;
        ImGui.Checkbox("VSync", ref vsync);
        Module.MaxFPS = vsync ? 30 : 480;
    }

    private static void DrawCheckboxForButton(ref OsContPad input, Button button, int row)
    {
        bool state = input.GetButtonState(button);
        ImGui.Checkbox($"##{button}-{row}", ref state);
        input.SetButton(button, state);
    }

    protected override Task DoNew()
    {
        return Task.CompletedTask;
    }

    protected override Task DoLoad(string filePath)
    {
        if (Path.GetExtension(filePath).Equals(".m64", StringComparison.InvariantCultureIgnoreCase))
        {
            IM64 m64 = MupenParser.Parse(filePath);
            Debug.WriteLine("Loading M64 with region code: " + m64.RegionCode);

            GameVersion = m64.RegionCode == 69 ? MarioIDE.GameVersion.US : MarioIDE.GameVersion.JP;
            ((InputManager)((SaveSystem)SaveSystem).InputManager).LoadFrom(m64.ControllerInputs);

            Initialize();
        }
        else if (Path.GetExtension(filePath).Equals(".mide", StringComparison.InvariantCultureIgnoreCase))
        {
            // TODO: load .mide file
        }

        return Task.CompletedTask;
    }

    protected override Task DoSave(string filePath)
    {
        return Task.CompletedTask;
    }

    void ICommandHandler<ExportCommandDefinition>.Update(Command command)
    {
        command.Enabled = true;
    }

    async Task ICommandHandler<ExportCommandDefinition>.Run(Command command)
    {
        await DoExportAs(this);
    }

    public override Task TryCloseAsync(bool? dialogResult = null)
    {
        SaveSystem?.Dispose();
        SaveSystem = null;
        Unregister();
        return base.TryCloseAsync(dialogResult);
    }

    private static async Task DoExportAs(IPersistedDocument persistedDocument)
    {
        SaveFileDialog dialog = new()
        {
            FileName = Path.GetFileNameWithoutExtension(persistedDocument.FileName)!
        };

        string filter = string.Empty;
        const string fileExtension = ".m64";

        EditorFileType fileType = IoC.GetAll<IEditorProvider>()
            .SelectMany(x => x.FileTypes)
            .SingleOrDefault(x => x.FileExtension == fileExtension);

        if (fileType != null)
        {
            filter = fileType.Name + "|*" + fileType.FileExtension;
        }

        dialog.Filter = filter;

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        string filePath = dialog.FileName;
        await persistedDocument.Save(filePath);
    }
}