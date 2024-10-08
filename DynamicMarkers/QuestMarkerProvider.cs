using System.Collections.Generic;
using DynamicMaps.Data;
using DynamicMaps.DynamicMarkers;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;

namespace DynamicMaps
{
    public class QuestMarkerProvider : IDynamicMarkerProvider
    {
        private List<MapMarker> _questMarkers = new();

        public void OnShowInRaid(MapView map)
        {
            if (GameUtils.IsScavRaid())
            {
                return;
            }

            AddQuestObjectiveMarkers(map);
        }

        public void OnHideInRaid(MapView map)
        {
            // TODO: don't just be lazy and try to update markers
            TryRemoveMarkers();
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            if (!GameUtils.IsInRaid())
            {
                return;
            }

            TryRemoveMarkers();
            AddQuestObjectiveMarkers(map);
        }

        public void OnRaidEnd(MapView map)
        {
            QuestUtils.DiscardQuestData();
            TryRemoveMarkers();
        }

        public void OnDisable(MapView map)
        {
            TryRemoveMarkers();
        }

        private void AddQuestObjectiveMarkers(MapView map)
        {
            QuestUtils.TryCaptureQuestData();

			EFT.Player player = GameUtils.GetMainPlayer();

			IEnumerable<MapMarkerDef> markerDefs = QuestUtils.GetMarkerDefsForPlayer(player);
            foreach (MapMarkerDef markerDef in markerDefs)
            {
				MapMarker marker = map.AddMapMarker(markerDef);
                _questMarkers.Add(marker);
            }
        }

        private void TryRemoveMarkers()
        {
            foreach (MapMarker marker in _questMarkers)
            {
                marker.ContainingMapView.RemoveMapMarker(marker);
            }
            _questMarkers.Clear();
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
