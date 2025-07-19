namespace Fusion.Menu {
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;
#if FUSION_ENABLE_TEXTMESHPRO
  using InputField = TMPro.TMP_InputField;
#else 
  using InputField = UnityEngine.UI.InputField;
#endif
  using UnityEngine;
  using UnityEngine.UI;
    using Fusion.Sockets;

    /// <summary>
    /// The party screen shows two modes. Creating a new game or joining a game with a party code.
    /// After creating a game the session party code can be optained via the ingame menu.
    /// One speciality is that a region list is requested from the connection when entering the screen in order to create a matching session codes.
    /// </summary>
    public partial class FusionMenuUIParty : FusionMenuUIScreen {

    [SerializeField] private NetworkRunner runnerPrefab;


    /// <summary>
    /// The session code input field.
    /// </summary>
    [InlineHelp, SerializeField] protected InputField _sessionCodeField;
    /// <summary>
    /// The create game button.
    /// </summary>
    [InlineHelp, SerializeField] protected Button _createButton;
    /// <summary>
    /// The join game button.
    /// </summary>
    [InlineHelp, SerializeField] protected Button _joinButton;
    /// <summary>
    /// The quick join game button.
    /// </summary>
    [InlineHelp, SerializeField] protected Button _playButton;

    /// <summary>
    /// The back button.
    /// </summary>
    [InlineHelp, SerializeField] protected Button _backButton;

    /// <summary>
    /// The task of requesting the regions.
    /// </summary>
    protected Task<List<FusionMenuOnlineRegion>> _regionRequest;

    partial void AwakeUser();
    partial void InitUser();
    partial void ShowUser();
    partial void HideUser();

    /// <summary>
    /// The Unity awake method. Calls partial method <see cref="AwakeUser"/> to be implemented on the SDK side.
    /// </summary>
    public override void Awake() {
      base.Awake();
      AwakeUser();
    }

    /// <summary>
    /// The screen init method. Calls partial method <see cref="InitUser"/> to be implemented on the SDK side.
    /// </summary>
    public override void Init() {
      base.Init();
      InitUser();
    }

    /// <summary>
    /// The screen show method. Calls partial method <see cref="ShowUser"/> to be implemented on the SDK side.
    /// When entering this screen an async request to retrieve the available regions is started.
    /// </summary>
    public override void Show() {
      base.Show();

      if (Config.CodeGenerator == null) {
        Debug.LogError("Add a CodeGenerator to the FusionMenuConfig");
      }

      _sessionCodeField.SetTextWithoutNotify("".PadLeft(Config.CodeGenerator.Length, '-'));
      _sessionCodeField.characterLimit = Config.CodeGenerator.Length;

      if (_regionRequest == null || _regionRequest.IsFaulted) {
        // Request the regions already when entering the party menu
        _regionRequest = Connection.RequestAvailableOnlineRegionsAsync(ConnectionArgs);
      }

      ShowUser();
    }

    /// <summary>
    /// The screen hide method. Calls partial method <see cref="HideUser"/> to be implemented on the SDK side.
    /// </summary>
    public override void Hide()
    {
      base.Hide();
      HideUser();
    }

    /// <summary>
    /// Is called when the <see cref="_createButton"/> is pressed using SendMessage() from the UI object.
    /// </summary>
    protected virtual async void OnCreateButtonPressed()
    {
      Controller.Show<FusionMenuUILoading>();
      Controller.Get<FusionMenuUILoading>().SetStatusText("Creating session...");

      await ConnectAsync(true);
    }

    /// <summary>
    /// Is called when the <see cref="_joinButton"/> is pressed using SendMessage() from the UI object.
    /// </summary>
    // protected virtual async void OnJoinButtonPressed() {
    //   await ConnectAsync(false);
    // }

    protected virtual async void OnJoinButtonPressed()
    {
      Controller.Show<FusionMenuUILoading>();
      Controller.Get<FusionMenuUILoading>().SetStatusText("Searching for sessions...");

      await ConnectAsync(false);
    }

    /// <summary>
    /// Is called when the <see cref="_playButton"/> is pressed using SendMessage() from the UI object.
    /// Intitiates the connection and expects the connection object to set further screen states.
    /// </summary>
    protected virtual async void OnPlayButtonPressed() {
      ConnectionArgs.Session = null;
      ConnectionArgs.Creating = false;
      ConnectionArgs.Region = ConnectionArgs.PreferredRegion;

      Controller.Show<FusionMenuUILoading>();

      var result = await Connection.ConnectAsync(ConnectionArgs);

      await FusionMenuUIMain.HandleConnectionResult(result, this.Controller);
    }

    /// <summary>
    /// Is called when the <see cref="_backButton"/> is pressed using SendMessage() from the UI object.
    /// </summary>
    public virtual void OnBackButtonPressed()
    {
      Controller.Show<FusionMenuUIMain>();
    }

    /// <summary>
    /// The connect method to handle create and join.
    /// Internally the region request is awaited.
    /// </summary>
    /// <param name="creating">Create or join</param>
    /// <returns></returns>
    protected virtual async Task ConnectAsync(bool creating) {
      // Test for input errors before switching screen
      var inputRegionCode = _sessionCodeField.text.ToUpper();
      if (creating == false && Config.CodeGenerator.IsValid(inputRegionCode) == false) {
        await Controller.PopupAsync($"The session code '{inputRegionCode}' is not a valid session code. Please enter {Config.CodeGenerator.Length} characters or digits.", "Invalid Session Code");
        return;
      }

      if (_regionRequest.IsCompleted == false) {
        // Goto loading screen
        Controller.Show<FusionMenuUILoading>();
        Controller.Get<FusionMenuUILoading>().SetStatusText("Fetching Regions");

        try {
          // TODO: Disconnect button not usable during this time
          await _regionRequest;
        } catch (Exception e) {
          Debug.LogException(e);
          // Error is handled in next section
        }
      }

      if (_regionRequest.IsCompletedSuccessfully == false && _regionRequest.Result.Count == 0) {
        await Controller.PopupAsync($"Failed to request regions.", "Connection Failed");
        Controller.Show<FusionMenuUIMain>();
        return;
      }

      if (creating) {
        var regionIndex = -1;
        if (string.IsNullOrEmpty(ConnectionArgs.PreferredRegion)) {
          // Select a best region now
          regionIndex = FindBestAvailableOnlineRegionIndex(_regionRequest.Result);
        } else {
          regionIndex = _regionRequest.Result.FindIndex(r => r.Code == ConnectionArgs.PreferredRegion);
        }

        if (regionIndex == -1) {
          await Controller.PopupAsync($"Selected region is not available.", "Connection Failed");
          Controller.Show<FusionMenuUIMain>();
          return;
        }

        ConnectionArgs.Session = Config.CodeGenerator.EncodeRegion(Config.CodeGenerator.Create(), regionIndex);
        ConnectionArgs.Region = _regionRequest.Result[regionIndex].Code;
      } else {
        var regionIndex = Config.CodeGenerator.DecodeRegion(inputRegionCode);
        if (regionIndex < 0 || regionIndex > Config.AvailableRegions.Count) {
          await Controller.PopupAsync($"The session code '{inputRegionCode}' is not a valid session code (cannot decode the region).", "Invalid Session Code");
          return;
        }

        ConnectionArgs.Session = _sessionCodeField.text.ToUpper(); ;
        ConnectionArgs.Region = Config.AvailableRegions[regionIndex];
      }

      ConnectionArgs.Creating = creating;

      Controller.Show<FusionMenuUILoading>();

      var result = await Connection.ConnectAsync(ConnectionArgs);

      await FusionMenuUIMain.HandleConnectionResult(result, this.Controller);
    }

    /// <summary>
    /// Find the region with the lowest ping.
    /// </summary>
    /// <param name="regions">Region list</param>
    /// <returns>The index of the region with the lowest ping</returns>
    protected static int FindBestAvailableOnlineRegionIndex(List<FusionMenuOnlineRegion> regions) {
      var lowestPing = int.MaxValue;
      var index = -1;
      for (int i = 0; regions != null && i < regions.Count; i++) {
        if (regions[i].Ping < lowestPing) {
          lowestPing = regions[i].Ping;
          index = i;
        }
      }

      return index;
    }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            throw new NotImplementedException();
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            throw new NotImplementedException();
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            // throw new NotImplementedException();
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            throw new NotImplementedException();
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            throw new NotImplementedException();
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            throw new NotImplementedException();
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            // throw new NotImplementedException();
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            // throw new NotImplementedException();
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            throw new NotImplementedException();
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            throw new NotImplementedException();
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            throw new NotImplementedException();
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            throw new NotImplementedException();
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
            throw new NotImplementedException();
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
            throw new NotImplementedException();
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            throw new NotImplementedException();
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            throw new NotImplementedException();
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            throw new NotImplementedException();
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            throw new NotImplementedException();
        }
    }
}
