using Brio.Config;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Penumbra.Api.Helpers;
using Penumbra.Api.IpcSubscribers;
using System;
using System.Collections.Generic;

namespace Brio.IPC;

public class PenumbraService : BrioIPC
{
    public override string Name { get; } = "Penumbra";

    public override bool IsAvailable
        => CheckStatus() == IPCStatus.Available;

    public override bool AllowIntegration
        => _configurationService.Configuration.IPC.AllowPenumbraIntegration;

    public override int APIMajor => 5;
    public override int APIMinor => 0;

    public override (int Major, int Minor) GetAPIVersion()
        => _penumbraApiVersion.Invoke();

    public override IDalamudPluginInterface GetPluginInterface()
        => _pluginInterface;

    //
    //

    private readonly ConfigurationService _configurationService;
    private readonly IDalamudPluginInterface _pluginInterface;

    public delegate void PenumbraRedrawEvent(int gameObjectId);
    public event PenumbraRedrawEvent? OnPenumbraRedraw;

    private readonly EventSubscriber<nint, int> _penumbraRedrawEvent;
    private readonly EventSubscriber _penumbraInitializedSubscriber;
    private readonly EventSubscriber _penumbraDisposedSubscriber;

    private readonly SetCollectionForObject _penumbraSetCollectionForObject;
    private readonly GetCollectionForObject _penumbraGetCollectionForObject;
    private readonly SetCutsceneParentIndex _penumbraSetCutsceneParentIndex;
    private readonly GetCollections _penumbraGetCollections;
    private readonly OpenMainWindow _penumbraOpenMainWindow;
    private readonly ApiVersion _penumbraApiVersion;

    public PenumbraService(IDalamudPluginInterface pluginInterface, ConfigurationService configurationService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;

        _penumbraInitializedSubscriber = Initialized.Subscriber(pluginInterface, OnConfigurationChanged);
        _penumbraDisposedSubscriber = Disposed.Subscriber(pluginInterface, OnConfigurationChanged);
        _penumbraRedrawEvent = GameObjectRedrawn.Subscriber(pluginInterface, HandlePenumbraRedraw);

        _penumbraGetCollectionForObject = new GetCollectionForObject(_pluginInterface);
        _penumbraSetCollectionForObject = new SetCollectionForObject(_pluginInterface);
        _penumbraSetCutsceneParentIndex = new SetCutsceneParentIndex(_pluginInterface);
        _penumbraGetCollections = new GetCollections(_pluginInterface);
        _penumbraOpenMainWindow = new OpenMainWindow(pluginInterface);
        _penumbraApiVersion = new ApiVersion(_pluginInterface);

        _configurationService.OnConfigurationChanged += OnConfigurationChanged;

        OnConfigurationChanged();
    }

    public void OpenPenumbra()
    {
        if(IsAvailable == false)
            return;

        _penumbraOpenMainWindow.Invoke(Penumbra.Api.Enums.TabType.Mods);
    }

    public string GetCollectionForObject(IGameObject gameObject)
    {
        if(IsAvailable == false || gameObject is null)
            return string.Empty;

        var (_, _, collection) = _penumbraGetCollectionForObject.Invoke(gameObject.ObjectIndex);
        return collection.Name;
    }

    public Guid SetCollectionForObject(IGameObject gameObject, Guid collectionName)
    {
        if(IsAvailable == false || gameObject is null)
            return Guid.Empty;

        Brio.Log.Debug($"Setting GameObject {gameObject.ObjectIndex} collection to {collectionName}");

        var (objectValid, _, effectiveCollection) = _penumbraGetCollectionForObject.Invoke(gameObject.ObjectIndex);
        if(objectValid == false)
            return Guid.Empty;

        var (result, oldCollection) = _penumbraSetCollectionForObject.Invoke(gameObject.ObjectIndex, collectionName, true, true);
        if(result is not Penumbra.Api.Enums.PenumbraApiEc.Success and not Penumbra.Api.Enums.PenumbraApiEc.NothingChanged)
        {
            Brio.Log.Warning($"Could not set GameObject {gameObject.ObjectIndex} collection to {collectionName}: {result}");
            return Guid.Empty;
        }

        // A newly created individual assignment has no old explicit collection.
        // Preserve its previously effective collection so Reset can restore it.
        return oldCollection?.Id ?? effectiveCollection.Id;
    }

    public Dictionary<Guid, string> GetCollections()
    {
        if(IsAvailable == false)
            return null!;

        return _penumbraGetCollections.Invoke();
    }

    /// <summary>
    /// Treat a Brio-created GPose actor as an independent actor instead of a copy of
    /// the character it was initialized from. Penumbra otherwise resolves collection
    /// assignments through the copied character's identifier.
    /// </summary>
    public void DetachCutsceneActor(IGameObject gameObject)
    {
        if(IsAvailable == false || gameObject is null)
            return;

        var result = _penumbraSetCutsceneParentIndex.Invoke(gameObject.ObjectIndex, -1);
        if(result != Penumbra.Api.Enums.PenumbraApiEc.Success)
            Brio.Log.Warning($"Could not detach GameObject {gameObject.ObjectIndex} from its Penumbra cutscene parent: {result}");
    }

    private void HandlePenumbraRedraw(nint arg1, int arg2)
    {
        Brio.Log.Debug("Penumbra redraw event received.");
        OnPenumbraRedraw?.Invoke(arg2);
    }

    private void OnConfigurationChanged()
        => CheckStatus();

    public override void Dispose()
    {
        _configurationService.OnConfigurationChanged -= OnConfigurationChanged;
        _penumbraInitializedSubscriber.Dispose();
        _penumbraDisposedSubscriber.Dispose();
    }
}
