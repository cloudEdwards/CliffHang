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
        using UnityEngine.SceneManagement;

  
  /// <summary>
  /// The party screen shows two modes. Creating a new game or joining a game with a party code.
  /// After creating a game the session party code can be optained via the ingame menu.
  /// One speciality is that a region list is requested from the connection when entering the screen in order to create a matching session codes.
  /// </summary>

    public partial class FusionMenuUIStart : FusionMenuUIScreen
    {
        [SerializeField] private Button[] characterButtons;
        [SerializeField] private NetworkRunner runnerPrefab;

        private NetworkRunner runner;

        private Task<List<FusionMenuOnlineRegion>> _regionRequest;

        public virtual void OnBackButtonPressed() {
           Controller.Show<FusionMenuUIMain>();
        }

        public override void Show()
        {
            base.Show();

            if (_regionRequest == null)
            {
                _regionRequest = Connection.RequestAvailableOnlineRegionsAsync(ConnectionArgs);
            }

            for (int i = 0; i < characterButtons.Length; i++)
            {
                int index = i; // capture local copy for closure
                characterButtons[i].onClick.AddListener(() => OnCharacterButtonPressed(index));
            }
        }

        public virtual async void OnCharacterButtonPressed(int characterIndex)
        {
            PlayerPrefs.SetInt("SelectedCharacterIndex", characterIndex);
            PlayerPrefs.Save();

            Controller.Show<FusionMenuUIParty>();
        }

        
        /// <summary>
        /// The connect method to handle create and join.
        /// Internally the region request is awaited.
        /// </summary>
        /// <param name="creating">Create or join</param>
        /// <returns></returns>
        protected virtual async Task ConnectAsync(bool creating) {
            // Test for input errors before switching screen
            var inputRegionCode = "";
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

                // ConnectionArgs.Session = _sessionCodeField.text.ToUpper(); ;
                // ConnectionArgs.Region = Config.AvailableRegions[regionIndex];
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
    }
}
