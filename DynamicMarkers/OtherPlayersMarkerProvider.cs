using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DynamicMaps.Data;
using DynamicMaps.Patches;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class OtherPlayersMarkerProvider : IDynamicMarkerProvider
    {
        private const string _arrowImagePath = "Markers/arrow.png";
        private const string _starImagePath = "Markers/star.png";

        // TODO: bring these all out to config
        private const string _friendlyPlayerCategory = "Friendly Player";
        private const string _friendlyPlayerImagePath = _arrowImagePath;
        private static Color _friendlyPlayerColor = Color.Lerp(Color.blue, Color.white, 0.5f);

        private const string _enemyPlayerCategory = "Enemy Player";
        private const string _enemyPlayerImagePath = _arrowImagePath;
        private static Color _enemyPlayerColor = Color.red;

        private const string _scavCategory = "Scav";
        private const string _scavImagePath = _arrowImagePath;
        private static Color _scavColor = Color.Lerp(Color.red, Color.yellow, 0.5f);

        private const string _bossCategory = "Boss";
        private const string _bossImagePath = _starImagePath;
        private static Color _bossColor = Color.Lerp(Color.red, Color.yellow, 0.5f);
        //

        private bool _showFriendlyPlayers = true;
        public bool ShowFriendlyPlayers
        {
            get
            {
                return _showFriendlyPlayers;
            }

            set
            {
                HandleSetBoolOption(ref _showFriendlyPlayers, value);
            }
        }

        private bool _showEnemyPlayers = false;
        public bool ShowEnemyPlayers
        {
            get
            {
                return _showEnemyPlayers;
            }

            set
            {
                HandleSetBoolOption(ref _showEnemyPlayers, value);
            }
        }

        private bool _showScavs = false;
        public bool ShowScavs
        {
            get
            {
                return _showScavs;
            }

            set
            {
                HandleSetBoolOption(ref _showScavs, value);
            }
        }

        private bool _showBosses = false;
        public bool ShowBosses
        {
            get
            {
                return _showBosses;
            }

            set
            {
                HandleSetBoolOption(ref _showBosses, value);
            }
        }

        private MapView _lastMapView;
        private Dictionary<Player, PlayerMapMarker> _playerMarkers = new Dictionary<Player, PlayerMapMarker>();

        public void OnShowInRaid(MapView map)
        {
            _lastMapView = map;

            TryAddMarkers();
            RemoveNonActivePlayers();

            // register to event to get all the new ones while map is showing
            Singleton<GameWorld>.Instance.OnPersonAdd += TryAddMarker;

            // register to events to get when players die
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer += OnUnregisterPlayer;
            PlayerOnDeadPatch.OnDead += TryRemoveMarker;
        }

        public void OnHideInRaid(MapView map)
        {
            // unregister from events while map isn't showing
            Singleton<GameWorld>.Instance.OnPersonAdd -= TryAddMarker;
            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
            PlayerOnDeadPatch.OnDead -= TryRemoveMarker;
        }

        public void OnRaidEnd(MapView map)
        {
			// unregister from events since map is ending
			GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld != null)
            {
                gameWorld.OnPersonAdd -= TryAddMarker;
            }

            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
            PlayerOnDeadPatch.OnDead -= TryRemoveMarker;

            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            _lastMapView = map;

            foreach (Player player in _playerMarkers.Keys.ToList())
            {
                TryRemoveMarker(player);
                TryAddMarker(player);
            }
        }

        public void OnDisable(MapView map)
        {
			// unregister from events since provider is being disabled
			GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld != null)
            {
                gameWorld.OnPersonAdd -= TryAddMarker;
            }

            GameWorldUnregisterPlayerPatch.OnUnregisterPlayer -= OnUnregisterPlayer;
            PlayerOnDeadPatch.OnDead -= TryRemoveMarker;

            TryRemoveMarkers();
        }

        private void TryRemoveMarkers()
        {
            foreach (Player player in _playerMarkers.Keys.ToList())
            {
                TryRemoveMarker(player);
            }

            _playerMarkers.Clear();
        }

        private void TryAddMarkers()
        {
            if (!GameUtils.IsInRaid())
            {
                return;
            }

			// add all players that have spawned already in raid
			GameWorld gameWorld = Singleton<GameWorld>.Instance;
            foreach (Player player in gameWorld.AllAlivePlayersList)
            {
                if (player.IsYourPlayer || _playerMarkers.ContainsKey(player))
                {
                    continue;
                }

                TryAddMarker(player);
            }
        }

        private void OnUnregisterPlayer(IPlayer iPlayer)
        {
			Player player = iPlayer as Player;
            if (player == null)
            {
                return;
            }

            TryRemoveMarker(player);
        }

        private void RemoveNonActivePlayers()
        {
			HashSet<Player> alivePlayers = new HashSet<Player>(Singleton<GameWorld>.Instance.AllAlivePlayersList);
            foreach (Player player in _playerMarkers.Keys.ToList())
            {
                if (player.HasCorpse() || !alivePlayers.Contains(player))
                {
                    TryRemoveMarker(player);
                }
            }
        }

        private void TryAddMarker(IPlayer iPlayer)
        {
			Player player = iPlayer as Player;
            if (player == null)
            {
                return;
            }

            if (_lastMapView == null || player.IsBTRShooter() || _playerMarkers.ContainsKey(player))
            {
                return;
            }

			// set category and color
			string category = _scavCategory;
			string imagePath = _scavImagePath;
			Color color = _scavColor;

            if (player.IsGroupedWithMainPlayer())
            {
                category = _friendlyPlayerCategory;
                imagePath = _friendlyPlayerImagePath;
                color = _friendlyPlayerColor;
            }
            else if (player.IsTrackedBoss())
            {
                category = _bossCategory;
                imagePath = _bossImagePath;
                color = _bossColor;
            }
            else if (player.IsPMC())
            {
                category = _enemyPlayerCategory;
                imagePath = _enemyPlayerImagePath;
                color = _enemyPlayerColor;
            }

            if (!ShouldShowCategory(category))
            {
                return;
            }

			// try adding marker
			PlayerMapMarker marker = _lastMapView.AddPlayerMarker(player, category, color, imagePath);
            _playerMarkers[player] = marker;
        }

        private void RemoveDisabledMarkers()
        {
            foreach (Player player in _playerMarkers.Keys.ToList())
            {
				PlayerMapMarker marker = _playerMarkers[player];
                if (!ShouldShowCategory(marker.Category))
                {
                    TryRemoveMarker(player);
                }
            }
        }

        private void TryRemoveMarker(Player player)
        {
            if (!_playerMarkers.ContainsKey(player))
            {
                return;
            }

            _playerMarkers[player].ContainingMapView.RemoveMapMarker(_playerMarkers[player]);
            _playerMarkers.Remove(player);
        }

        private bool ShouldShowCategory(string category)
        {
			return category switch
			{
				_friendlyPlayerCategory => _showFriendlyPlayers,
				_enemyPlayerCategory => _showEnemyPlayers,
				_bossCategory => _showBosses,
				_scavCategory => _showScavs,
				_ => false,
			};
		}

        private void HandleSetBoolOption(ref bool boolOption, bool value)
        {
            if (value == boolOption)
            {
                return;
            }

            boolOption = value;

            if (boolOption)
            {
                TryAddMarkers();
            }
            else
            {
                RemoveDisabledMarkers();
            }
        }

        public void OnShowOutOfRaid(MapView map)
        {
            // do nothing
        }

        public void OnHideOutOfRaid(MapView map)
        {
            // do nothing
        }
    }
}
