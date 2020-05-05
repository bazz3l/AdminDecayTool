using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Admin Decay Hammer", "Bazz3l", "0.0.4")]
    [Description("Hit a building block to start a faster decay.")]
    class AdminDecayHammer : RustPlugin
    {
        const string _usePerm = "admindecayhammer.use";
        List<ulong> _players = new List<ulong>();

        #region Config
        PluginConfig _config;

        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                DecayVariance = 100f
            };
        }

        class PluginConfig
        {
            public float DecayVariance;
        }
        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoKeyPress"]      = "To use the decay hammer, please hold shift while hitting a building block.",
                ["NoBuildingBlock"] = "No building block found.",
                ["NoDecayEntity"]   = "No decay entity found.",
                ["NoPermission"]    = "No permission.",
                ["DecayStarted"]    = "Decay will start on next decay tick.",
                ["ToggleEnabled"]   = "Decay hammer is now Enabled.",
                ["ToggleDisabled"]  = "Decay hammer Disabled."
            }, this);
        }
        #endregion

        #region Oxide
        void Init()
        {
            permission.RegisterPermission(_usePerm, this);

            _config = Config.ReadObject<PluginConfig>();
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player != null && _players.Contains(player.userID))
            {
                _players.Remove(player.userID);
            }
        }

         void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null || !_players.Contains(player.userID) || !permission.UserHasPermission(player.UserIDString, _usePerm))
            {
                return;
            }

            if (!player.serverInput.IsDown(BUTTON.SPRINT))
            {
                player.ChatMessage(Lang("NoKeyPress", player.UserIDString));
                return;
            }

            BuildingBlock block = info?.HitEntity as BuildingBlock;
            if (block == null)
            {
                player.ChatMessage(Lang("NoBuildingBlock", player.UserIDString));
                return;
            }

            DecayEntity decayEntity = block as DecayEntity;
            if (decayEntity == null)
            {
                player.ChatMessage(Lang("NoDecayEntity", player.UserIDString));
                return;
            }

            BuildingDecay(decayEntity.buildingID);

            player.ChatMessage(Lang("DecayStarted", player.UserIDString));
        }
        #endregion

        #region Core
        private void BuildingDecay(uint buildingID)
        {
            BuildingManager.Building buildManager = BuildingManager.server.GetBuilding(buildingID);
            if (buildManager == null)
            {
                return;
            }

            foreach(DecayEntity decayEnt in buildManager.decayEntities)
            {
                decayEnt.ResetUpkeepTime();
                decayEnt.decayVariance = _config.DecayVariance;
            }
        }
        #endregion

        #region Commands
        [ChatCommand("decayhammer")]
        void DecayHammerCommand(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, _usePerm))
            {
                player.ChatMessage(Lang("NoPermission"));
                return;
            }

            if (!_players.Contains(player.userID))
            {
                _players.Add(player.userID);
            }
            else
            {
                _players.Remove(player.userID);
            }

            player.ChatMessage(_players.Contains(player.userID) ? Lang("ToggleEnabled", player.UserIDString) : Lang("ToggleDisabled", player.UserIDString));
        }
        #endregion

        #region Helpers
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        #endregion
    }
}
