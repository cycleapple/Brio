using Brio.Config;
using Brio.Input;
using Brio.IPC;
using Brio.Resources;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.Web;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace Brio.UI.Windows;

public class SettingsWindow : Window
{
    private readonly ConfigurationService _configurationService;
    private readonly PenumbraService _penumbraService;
    private readonly GlamourerService _glamourerService;
    private readonly WebService _webService;
    private readonly BrioIPCService _brioIPCService;
    private readonly CustomizePlusService _customizePlusService;
    private readonly MareService _mareService;

    public SettingsWindow(
        ConfigurationService configurationService,
        PenumbraService penumbraService,
        GlamourerService glamourerService,
        WebService webService,
        CustomizePlusService customizePlusService,
        BrioIPCService brioIPCService,
        MareService mareService) : base($"{Brio.Name} 設定###brio_settings_window", ImGuiWindowFlags.NoResize)
    {
        Namespace = "brio_settings_namespace";

        _configurationService = configurationService;
        _penumbraService = penumbraService;
        _glamourerService = glamourerService;
        _webService = webService;
        _brioIPCService = brioIPCService;
        _mareService = mareService;
        _customizePlusService = customizePlusService;

        Size = new Vector2(450, 450);
    }

    private bool _isModal = false;
    private float? _libraryPadding = null;
    public void OpenAsLibraryTab()
    {
        _libraryPadding = 35;

        Flags = ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;
        IsOpen = true;

        BringToFront();

        _isModal = true;
    }

    public override void OnClose()
    {
        Flags = ImGuiWindowFlags.NoResize;
        _isModal = false;

        _libraryPadding = null;
    }

    public override void Draw()
    {
        using(ImRaii.PushId("brio_settings"))
        {
            if(_isModal)
            {
                DrawLibrarySection();

                if(ImBrio.Button("關閉", FontAwesomeIcon.Times, new Vector2(100, 0)))
                {
                    IsOpen = false;
                }
            }
            else
            {
                using(var tab = ImRaii.TabBar("###brio_settings_tabs"))
                {
                    if(tab.Success)
                    {
                        DrawGeneralTab();
                        DrawIPCTab();
                        DrawPosingTab();
                        DrawLibraryTab();
                        DrawSceneTab();
                        DrawKeysTab();
                        DrawAdvancedTab();
                    }
                }
            }
        }
    }

    private void DrawGeneralTab()
    {
        using(var tab = ImRaii.TabItem("一般"))
        {
            if(tab.Success)
            {
                if(ImGui.CollapsingHeader("視窗", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    DrawOpenBrioSetting();
                    DrawHideSettings();
                }

                if(ImGui.CollapsingHeader("資料庫", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    bool useLibraryWhenImporting = _configurationService.Configuration.UseLibraryWhenImporting;
                    if(ImGui.Checkbox("匯入檔案時使用資料庫", ref useLibraryWhenImporting))
                    {
                        _configurationService.Configuration.UseLibraryWhenImporting = useLibraryWhenImporting;
                        _configurationService.ApplyChange();
                    }

                    bool returnToLastLocation = _configurationService.Configuration.Library.ReturnLibraryToLastLocation;
                    if(ImGui.Checkbox("開啟資料庫時回到上次位置", ref returnToLastLocation))
                    {
                        _configurationService.Configuration.Library.ReturnLibraryToLastLocation = returnToLastLocation;
                        _configurationService.ApplyChange();
                    }

                    bool useFilenameAsActorName = _configurationService.Configuration.Library.UseFilenameAsActorName;
                    if(ImGui.Checkbox("使用角色檔名作為角色名稱", ref useFilenameAsActorName))
                    {
                        _configurationService.Configuration.Library.UseFilenameAsActorName = useFilenameAsActorName;
                        _configurationService.ApplyChange();
                    }
                }

                DrawNPCAppearanceHack();

                if(ImGui.CollapsingHeader("顯示", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    DrawDisplaySettings();
                }
            }
        }
    }

    private void DrawOpenBrioSetting()
    {
        var selectedBrioOpenBehavior = _configurationService.Configuration.Interface.OpenBrioBehavior;
        const string label = "開啟 Brio";
        ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label).X);
        using(var combo = ImRaii.Combo(label, selectedBrioOpenBehavior.ToString()))
        {
            if(combo.Success)
            {
                foreach(var openBrioBehavior in Enum.GetValues<OpenBrioBehavior>())
                {
                    if(ImGui.Selectable($"{openBrioBehavior}", openBrioBehavior == selectedBrioOpenBehavior))
                    {
                        _configurationService.Configuration.Interface.OpenBrioBehavior = openBrioBehavior;
                        _configurationService.ApplyChange();
                    }
                }
            }
        }
    }

    private void DrawHideSettings()
    {
        bool showInGPose = _configurationService.Configuration.Interface.ShowInGPose;
        if(ImGui.Checkbox("在團體動作中顯示", ref showInGPose))
        {
            _configurationService.Configuration.Interface.ShowInGPose = showInGPose;
            _configurationService.ApplyChange();
        }

        bool showInCutscene = _configurationService.Configuration.Interface.ShowInCutscene;
        if(ImGui.Checkbox("在過場動畫中顯示", ref showInCutscene))
        {
            _configurationService.Configuration.Interface.ShowInCutscene = showInCutscene;
            _configurationService.ApplyChange();
        }

        bool showWhenUIHidden = _configurationService.Configuration.Interface.ShowWhenUIHidden;
        if(ImGui.Checkbox("遊戲介面隱藏時顯示", ref showWhenUIHidden))
        {
            _configurationService.Configuration.Interface.ShowWhenUIHidden = showWhenUIHidden;
            _configurationService.ApplyChange();
        }
    }

    private void DrawDisplaySettings()
    {
        bool censorActorNames = _configurationService.Configuration.Interface.CensorActorNames;
        if(ImGui.Checkbox("隱藏角色名稱", ref censorActorNames))
        {
            _configurationService.Configuration.Interface.CensorActorNames = censorActorNames;
            _configurationService.ApplyChange();
        }

        bool enableBrioColor = _configurationService.Configuration.Appearance.EnableBrioColor;
        if(ImGui.Checkbox("啟用 Brio 主題色", ref enableBrioColor))
        {
            _configurationService.Configuration.Appearance.EnableBrioColor = enableBrioColor;
            _configurationService.ApplyChange();
        }

        bool enableBrioScale = _configurationService.Configuration.Appearance.EnableBrioScale;
        if(ImGui.Checkbox("啟用 Brio 文字縮放", ref enableBrioScale))
        {
            _configurationService.Configuration.Appearance.EnableBrioScale = enableBrioScale;
            _configurationService.ApplyChange();
        }
    }

    private void DrawSceneTab()
    {
        using(var tab = ImRaii.TabItem("自動儲存"))
        {
            if(tab.Success)
            {
                DrawImportScene();
            }
        }
    }

    private void DrawIPCTab()
    {
        using(var tab = ImRaii.TabItem("IPC"))
        {
            if(tab.Success)
            {
                DrawBrioIPC();
                DrawThirdPartyIPC();
            }
        }
    }

    private void DrawThirdPartyIPC()
    {
        if(ImGui.CollapsingHeader("Third-Party", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool enableCustomizePlus = _configurationService.Configuration.IPC.AllowCustomizePlusIntegration;
            if(ImGui.Checkbox("Allow Customize+ Integration", ref enableCustomizePlus))
            {
                _configurationService.Configuration.IPC.AllowCustomizePlusIntegration = enableCustomizePlus;
                _configurationService.ApplyChange();
                _customizePlusService.CheckStatus(true);
            }

            var customizePlusStatus = _customizePlusService.CheckStatus();
            using(ImRaii.Disabled(!enableCustomizePlus))
            {
                ImGui.Text($"Customize+ Status: {customizePlusStatus}");
                ImGui.SameLine();
                if(ImBrio.FontIconButton("refresh_Customize", FontAwesomeIcon.Sync, "Refresh Customize+ Status"))
                {
                    _customizePlusService.CheckStatus(true);
                }
            }
        }

        if(ImGui.CollapsingHeader("Third-Party [Penumbra Based]", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var penumbraStatus = _penumbraService.CheckStatus();
            var penumbraUnavailable = penumbraStatus is IPCStatus.None or IPCStatus.NotInstalled or IPCStatus.VersionMismatch or IPCStatus.Error;

            if(penumbraUnavailable)
            {
                using(ImRaii.PushColor(ImGuiCol.Text, UIConstants.GizmoRed))
                    ImGui.Text("Please Install Penumbra");
            }

            using(ImRaii.Disabled(penumbraUnavailable))
            {
                bool enablePenumbra = _configurationService.Configuration.IPC.AllowPenumbraIntegration;
                if(ImGui.Checkbox("Allow Penumbra Integration", ref enablePenumbra))
                {
                    _configurationService.Configuration.IPC.AllowPenumbraIntegration = enablePenumbra;
                    _configurationService.ApplyChange();
                    _penumbraService.CheckStatus(true);
                }

                ImGui.Text($"Penumbra Status: {penumbraStatus}");
                ImGui.SameLine();
                if(ImBrio.FontIconButton("refresh_penumbra", FontAwesomeIcon.Sync, "Refresh Penumbra Status"))
                {
                    _penumbraService.CheckStatus(true);
                }

                using(ImRaii.Disabled(!enablePenumbra))
                {
                    bool enableGlamourer = _configurationService.Configuration.IPC.AllowGlamourerIntegration;
                    if(ImGui.Checkbox("Allow Glamourer Integration", ref enableGlamourer))
                    {
                        _configurationService.Configuration.IPC.AllowGlamourerIntegration = enableGlamourer;
                        _configurationService.ApplyChange();
                        _glamourerService.CheckStatus(true);
                    }

                    var glamourerStatus = _glamourerService.CheckStatus();
                    using(ImRaii.Disabled(!enableGlamourer))
                    {
                        ImGui.Text($"Glamourer Status: {glamourerStatus}");
                        ImGui.SameLine();
                        if(ImBrio.FontIconButton("refresh_glamourer", FontAwesomeIcon.Sync, "Refresh Glamourer Status"))
                        {
                            _glamourerService.CheckStatus(true);
                        }
                    }

                    bool enableMare = _configurationService.Configuration.IPC.AllowMareIntegration;

                    if(ImGui.Checkbox("Allow Mare Synchronos Integration", ref enableMare))
                    {
                        _configurationService.Configuration.IPC.AllowMareIntegration = enableMare;
                        _configurationService.ApplyChange();
                        _mareService.CheckStatus(true);
                    }

                    var mareStatus = _mareService.CheckStatus();
                    using(ImRaii.Disabled(!enableMare))
                    {
                        ImGui.Text($"Mare Synchronos Status: {mareStatus}");
                        ImGui.SameLine();
                        if(ImBrio.FontIconButton("refresh_mare", FontAwesomeIcon.Sync, "Refresh Mare Synchronos Status"))
                        {
                            _mareService.CheckStatus(true);
                        }
                    }

                    _glamourerService.Disabled = !enablePenumbra;
                    _mareService.Disabled = _glamourerService.Disabled;
                }
            }
        }
    }

    private void DrawImportScene()
    {
        if(ImGui.CollapsingHeader("一般", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var enabled = _configurationService.Configuration.AutoSave.AutoSaveSystemEnabled;
            if(ImGui.Checkbox("啟用自動儲存", ref enabled))
            {
                _configurationService.Configuration.AutoSave.AutoSaveSystemEnabled = enabled;
                _configurationService.ApplyChange();
            }

            using(ImRaii.Disabled(!enabled))
            {
                var saveInterval = _configurationService.Configuration.AutoSave.AutoSaveInterval;
                if(ImGui.SliderInt("自動儲存間隔", ref saveInterval, 15, 500, "%d 秒"))
                {
                    _configurationService.Configuration.AutoSave.AutoSaveInterval = saveInterval;
                    _configurationService.ApplyChange();
                }

                var maxSaves = _configurationService.Configuration.AutoSave.MaxAutoSaves;
                if(ImGui.SliderInt("自動存檔數量上限", ref maxSaves, 3, 30))
                {
                    _configurationService.Configuration.AutoSave.MaxAutoSaves = maxSaves;
                    _configurationService.ApplyChange();
                }

                //bool applyModelTransform = _configurationService.Configuration.Import.ApplyModelTransform;
                //if(ImGui.Checkbox("Apply Model Transform on Import", ref applyModelTransform))
                //{
                //    _configurationService.Configuration.Import.ApplyModelTransform = applyModelTransform;
                //    _configurationService.ApplyChange();
                //}

                //var positionTransformType = _configurationService.Configuration.Import.PositionTransformType;
                //ImGui.SetNextItemWidth(200);
                //using(var combo = ImRaii.Combo("Position", positionTransformType.ToString()))
                //{
                //    if(combo.Success)
                //    {
                //        foreach(var poseImportTransformType in Enum.GetValues<ScenePoseTransformType>())
                //        {
                //            if(ImGui.Selectable($"{poseImportTransformType}", poseImportTransformType == positionTransformType))
                //            {
                //                _configurationService.Configuration.Import.PositionTransformType = poseImportTransformType;
                //                _configurationService.ApplyChange();
                //            }
                //        }
                //    }
                //}

                //var rotationTransformType = _configurationService.Configuration.Import.RotationTransformType;
                //ImGui.SetNextItemWidth(200);
                //using(var combo = ImRaii.Combo("Rotation", rotationTransformType.ToString()))
                //{
                //    if(combo.Success)
                //    {
                //        foreach(var poseImportTransformType in Enum.GetValues<ScenePoseTransformType>())
                //        {
                //            if(ImGui.Selectable($"{poseImportTransformType}", poseImportTransformType == rotationTransformType))
                //            {
                //                _configurationService.Configuration.Import.RotationTransformType = poseImportTransformType;
                //                _configurationService.ApplyChange();
                //            }
                //        }
                //    }
                //}

                //var scaleTransformType = _configurationService.Configuration.Import.ScaleTransformType;
                //ImGui.SetNextItemWidth(200);
                //using(var combo = ImRaii.Combo("Scale", scaleTransformType.ToString()))
                //{
                //    if(combo.Success)
                //    {
                //        foreach(var poseImportTransformType in Enum.GetValues<ScenePoseTransformType>())
                //        {
                //            if(ImGui.Selectable($"{poseImportTransformType}", poseImportTransformType == scaleTransformType))
                //            {
                //                _configurationService.Configuration.Import.ScaleTransformType = poseImportTransformType;
                //                _configurationService.ApplyChange();
                //            }
                //        }
                //    }
                //}
            }
        }
    }

    private void DrawBrioIPC()
    {
        if(ImGui.CollapsingHeader("Brio", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool enableBrioIpc = _configurationService.Configuration.IPC.EnableBrioIPC;
            if(ImGui.Checkbox("Enable Brio IPC", ref enableBrioIpc))
            {
                _configurationService.Configuration.IPC.EnableBrioIPC = enableBrioIpc;
                _configurationService.ApplyChange();
            }
            ImGui.Text($"Brio IPC Status: {(_brioIPCService.IsIPCEnabled ? "Active" : "Inactive")}");

            bool enableWebApi = _configurationService.Configuration.IPC.AllowWebAPI;
            if(ImGui.Checkbox("Enable Brio API", ref enableWebApi))
            {
                _configurationService.Configuration.IPC.AllowWebAPI = enableWebApi;
                _configurationService.ApplyChange();
            }

            ImGui.Text($"Brio API Status: {(_webService.IsRunning ? "Active" : "Inactive")}");
        }
    }

    private void DrawNPCAppearanceHack()
    {
        if(ImGui.CollapsingHeader("Appearance", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var allowNPCHackBehavior = _configurationService.Configuration.Appearance.ApplyNPCHack;
            const string label = "Allow NPC Appearance on Players";
            ImGui.SetNextItemWidth(-ImGui.CalcTextSize(label).X);
            using(var combo = ImRaii.Combo(label, allowNPCHackBehavior.ToString()))
            {
                if(combo.Success)
                {
                    foreach(var npcHack in Enum.GetValues<ApplyNPCHack>())
                    {
                        if(ImGui.Selectable($"{npcHack}", npcHack == allowNPCHackBehavior))
                        {
                            _configurationService.Configuration.Appearance.ApplyNPCHack = npcHack;
                            _configurationService.ApplyChange();
                        }
                    }
                }
            }

            bool enableTinting = _configurationService.Configuration.Appearance.EnableTinting;
            if(ImGui.Checkbox("Enable Tinting", ref enableTinting))
            {
                _configurationService.Configuration.Appearance.EnableTinting = enableTinting;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawPosingTab()
    {
        using(var tab = ImRaii.TabItem("姿勢"))
        {
            if(tab.Success)
            {
                DrawPosingGeneralSection();
                DrawGPoseSection();
                DrawOverlaySection();
            }
        }
    }

    private void DrawGPoseSection()
    {
        if(ImGui.CollapsingHeader("團體動作", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool enableMouseHook = _configurationService.Configuration.Posing.DisableGPoseMouseSelect;
            if(ImGui.Checkbox("停用團體動作的滑鼠選取", ref enableMouseHook))
            {
                _configurationService.Configuration.Posing.DisableGPoseMouseSelect = enableMouseHook;
                _configurationService.ApplyChange();
            }

            bool enableBrioTargetChange = _configurationService.Configuration.Posing.BrioTargetChangesWithGPose;
            if(ImGui.Checkbox("Brio 目標跟隨團體動作目標", ref enableBrioTargetChange))
            {
                _configurationService.Configuration.Posing.BrioTargetChangesWithGPose = enableBrioTargetChange;
                _configurationService.ApplyChange();
            }

            bool enableGPoseTargetChange = _configurationService.Configuration.Posing.GPoseTargetChangesWithBrio;
            if(ImGui.Checkbox("團體動作目標跟隨 Brio 目標", ref enableGPoseTargetChange))
            {
                _configurationService.Configuration.Posing.GPoseTargetChangesWithBrio = enableGPoseTargetChange;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawOverlaySection()
    {
        if(ImGui.CollapsingHeader("姿勢浮層", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool defaultsOn = _configurationService.Configuration.Posing.OverlayDefaultsOn;
            if(ImGui.Checkbox("預設開啟浮層", ref defaultsOn))
            {
                _configurationService.Configuration.Posing.OverlayDefaultsOn = defaultsOn;
                _configurationService.ApplyChange();
            }

            bool standout = _configurationService.Configuration.Posing.ModelTransformStandout;
            if(ImGui.Checkbox("醒目顯示「模型變換」骨骼", ref standout))
            {
                _configurationService.Configuration.Posing.ModelTransformStandout = standout;
                _configurationService.ApplyChange();
            }

            if(standout == false)
                ImGui.BeginDisabled();

            Vector4 modelTransformCircleStandOut = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.ModelTransformCircleStandOutColor);
            if(ImGui.ColorEdit4("「模型變換」骨骼醒目色彩", ref modelTransformCircleStandOut, ImGuiColorEditFlags.NoInputs))
            {
                _configurationService.Configuration.Posing.ModelTransformCircleStandOutColor = ImGui.ColorConvertFloat4ToU32(modelTransformCircleStandOut);
                _configurationService.ApplyChange();
            }

            if(standout == false)
                ImGui.EndDisabled();

            bool allowGizmoAxisFlip = _configurationService.Configuration.Posing.AllowGizmoAxisFlip;
            if(ImGui.Checkbox("允許翻轉操作軸", ref allowGizmoAxisFlip))
            {
                _configurationService.Configuration.Posing.AllowGizmoAxisFlip = allowGizmoAxisFlip;
                _configurationService.ApplyChange();
            }

            bool hideGizmoWhenAdvancedPosingOpen = _configurationService.Configuration.Posing.HideGizmoWhenAdvancedPosingOpen;
            if(ImGui.Checkbox("開啟進階姿勢時隱藏操作軸", ref hideGizmoWhenAdvancedPosingOpen))
            {
                _configurationService.Configuration.Posing.HideGizmoWhenAdvancedPosingOpen = hideGizmoWhenAdvancedPosingOpen;
                _configurationService.ApplyChange();
            }

            bool hideToolbarWhenAdvancedPosingOpen = _configurationService.Configuration.Posing.HideToolbarWhenAdvandedPosingOpen;
            if(ImGui.Checkbox("開啟進階姿勢時隱藏工具列", ref hideToolbarWhenAdvancedPosingOpen))
            {
                _configurationService.Configuration.Posing.HideToolbarWhenAdvandedPosingOpen = hideToolbarWhenAdvancedPosingOpen;
                _configurationService.ApplyChange();
            }

            bool showSkeletonLines = _configurationService.Configuration.Posing.ShowSkeletonLines;
            if(ImGui.Checkbox("顯示骨架線", ref showSkeletonLines))
            {
                _configurationService.Configuration.Posing.ShowSkeletonLines = showSkeletonLines;
                _configurationService.ApplyChange();
            }

            bool hideSkeletonWhenGizmoActive = _configurationService.Configuration.Posing.HideSkeletonWhenGizmoActive;
            if(ImGui.Checkbox("操作軸使用中時隱藏骨架", ref hideSkeletonWhenGizmoActive))
            {
                _configurationService.Configuration.Posing.HideSkeletonWhenGizmoActive = hideSkeletonWhenGizmoActive;
                _configurationService.ApplyChange();
            }

            float lineThickness = _configurationService.Configuration.Posing.SkeletonLineThickness;
            if(ImGui.DragFloat("線條粗細", ref lineThickness, 0.01f, 0.01f, 20f))
            {
                _configurationService.Configuration.Posing.SkeletonLineThickness = lineThickness;
                _configurationService.ApplyChange();
            }

            float circleSize = _configurationService.Configuration.Posing.BoneCircleSize;
            if(ImGui.DragFloat("圓圈大小", ref circleSize, 0.01f, 0.01f, 20f))
            {
                _configurationService.Configuration.Posing.BoneCircleSize = circleSize;
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleNormalColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleNormalColor);
            if(ImGui.ColorEdit4("骨骼圓圈一般色彩", ref boneCircleNormalColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleNormalColor = ImGui.ColorConvertFloat4ToU32(boneCircleNormalColor);
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleInactiveColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleInactiveColor);
            if(ImGui.ColorEdit4("骨骼圓圈停用色彩", ref boneCircleInactiveColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleInactiveColor = ImGui.ColorConvertFloat4ToU32(boneCircleInactiveColor);
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleHoveredColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleHoveredColor);
            if(ImGui.ColorEdit4("骨骼圓圈游標停留色彩", ref boneCircleHoveredColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleHoveredColor = ImGui.ColorConvertFloat4ToU32(boneCircleHoveredColor);
                _configurationService.ApplyChange();
            }

            Vector4 boneCircleSelectedColor = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.BoneCircleSelectedColor);
            if(ImGui.ColorEdit4("骨骼圓圈選取色彩", ref boneCircleSelectedColor, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.BoneCircleSelectedColor = ImGui.ColorConvertFloat4ToU32(boneCircleSelectedColor);
                _configurationService.ApplyChange();
            }

            Vector4 skeletonLineActive = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.SkeletonLineActiveColor);
            if(ImGui.ColorEdit4("骨架線啟用色彩", ref skeletonLineActive, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.SkeletonLineActiveColor = ImGui.ColorConvertFloat4ToU32(skeletonLineActive);
                _configurationService.ApplyChange();
            }

            Vector4 skeletonLineInactive = ImGui.ColorConvertU32ToFloat4(_configurationService.Configuration.Posing.SkeletonLineInactiveColor);
            if(ImGui.ColorEdit4("骨架線停用色彩", ref skeletonLineInactive, ImGuiColorEditFlags.NoInputs))
            {

                _configurationService.Configuration.Posing.SkeletonLineInactiveColor = ImGui.ColorConvertFloat4ToU32(skeletonLineInactive);
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawPosingGeneralSection()
    {
        if(ImGui.CollapsingHeader("一般", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var undoStackSize = _configurationService.Configuration.Posing.UndoStackSize;
            if(ImGui.DragInt("復原紀錄", ref undoStackSize, 1, 0, 100))
            {
                _configurationService.Configuration.Posing.UndoStackSize = undoStackSize;
                _configurationService.ApplyChange();
            }
        }
    }

    bool resetSettings = false;
    private void DrawAdvancedTab()
    {
        using(var tab = ImRaii.TabItem("進階"))
        {
            if(tab.Success)
            {
                DrawEnvironmentSection();

                if(ImGui.CollapsingHeader("Brio", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Checkbox("啟用「將設定重設為預設值」按鈕", ref resetSettings);

                    using(ImRaii.Disabled(!resetSettings))
                    {
                        if(ImGui.Button("將設定重設為預設值", new(170, 0)))
                        {
                            _configurationService.Reset();
                            resetSettings = false;
                        }
                    }
                }

            }
        }
    }

    private void DrawEnvironmentSection()
    {
        if(ImGui.CollapsingHeader("環境", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var resetTimeOnGPoseExit = _configurationService.Configuration.Environment.ResetTimeOnGPoseExit;
            if(ImGui.Checkbox("離開團體動作時重設時間", ref resetTimeOnGPoseExit))
            {
                _configurationService.Configuration.Environment.ResetTimeOnGPoseExit = resetTimeOnGPoseExit;
                _configurationService.ApplyChange();
            }

            var resetWeatherOnGPoseExit = _configurationService.Configuration.Environment.ResetWeatherOnGPoseExit;
            if(ImGui.Checkbox("離開團體動作時重設天氣", ref resetWeatherOnGPoseExit))
            {
                _configurationService.Configuration.Environment.ResetWeatherOnGPoseExit = resetWeatherOnGPoseExit;
                _configurationService.ApplyChange();
            }

            var resetWaterOnGPoseExit = _configurationService.Configuration.Environment.ResetWaterOnGPoseExit;
            if(ImGui.Checkbox("離開團體動作時重設水面", ref resetWaterOnGPoseExit))
            {
                _configurationService.Configuration.Environment.ResetWaterOnGPoseExit = resetWaterOnGPoseExit;
                _configurationService.ApplyChange();
            }
        }
    }

    private void DrawLibraryTab()
    {
        using(var tab = ImRaii.TabItem("資料庫"))
        {
            if(tab.Success)
            {
                DrawLibrarySection();
            }
        }
    }

    private void DrawLibrarySection()
    {
        LibrarySourcesEditor.Draw(null, _configurationService, _configurationService.Configuration.Library, _libraryPadding);
    }

    private void DrawKeysTab()
    {
        using(var tab = ImRaii.TabItem("按鍵綁定"))
        {
            if(!tab.Success)
                return;

            bool enableKeyHandlingOnKeyMod = _configurationService.Configuration.Input.EnableKeyHandlingOnKeyMod;
            if(ImGui.Checkbox("移動自由攝影機時攔截空白鍵、Shift、Ctrl 與 Alt 輸入", ref enableKeyHandlingOnKeyMod))
            {
                _configurationService.Configuration.Input.EnableKeyHandlingOnKeyMod = enableKeyHandlingOnKeyMod;
                _configurationService.ApplyChange();
            }

            ImGui.Separator();

            bool enableKeybinds = _configurationService.Configuration.Input.EnableKeybinds;
            if(ImGui.Checkbox("啟用鍵盤快捷鍵", ref enableKeybinds))
            {
                _configurationService.Configuration.Input.EnableKeybinds = enableKeybinds;
                _configurationService.ApplyChange();
            }

            if(enableKeybinds == false)
            {
                ImGui.BeginDisabled();
            }

            bool showPrompts = _configurationService.Configuration.Input.ShowPromptsInGPose;
            if(ImGui.Checkbox("在團體動作中顯示按鍵提示", ref showPrompts))
            {
                _configurationService.Configuration.Input.ShowPromptsInGPose = showPrompts;
                _configurationService.ApplyChange();
            }

            if(ImGui.CollapsingHeader("介面", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawKeyBind(KeyBindEvents.Interface_ToggleBrioWindow);
                DrawKeyBind(KeyBindEvents.Interface_IncrementSmallModifier);
                DrawKeyBind(KeyBindEvents.Interface_IncrementLargeModifier);
            }

            if(ImGui.CollapsingHeader("姿勢", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawKeyBind(KeyBindEvents.Posing_DisableGizmo);
                DrawKeyBind(KeyBindEvents.Posing_DisableSkeleton);
                DrawKeyBind(KeyBindEvents.Posing_HideOverlay);
                DrawKeyBind(KeyBindEvents.Posing_ToggleOverlay);
                DrawKeyBind(KeyBindEvents.Posing_Undo);
                DrawKeyBind(KeyBindEvents.Posing_Redo);
                DrawKeyBind(KeyBindEvents.Posing_Translate);
                DrawKeyBind(KeyBindEvents.Posing_Rotate);
                DrawKeyBind(KeyBindEvents.Posing_Scale);
            }

            if(enableKeybinds == false)
            {
                ImGui.EndDisabled();
            }
        }
    }

    private void DrawKeyBind(KeyBindEvents evt)
    {
        string evtText = Localize.Get($"keys.{evt}") ?? evt.ToString();

        if(KeybindEditor.KeySelector(evtText, evt, _configurationService.Configuration.Input))
        {
            _configurationService.ApplyChange();
        }
    }
}
