using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
    public class BTRMarkerProvider : IDynamicMarkerProvider
    {
        // TODO: move to config
        private static string _btrIconPath = "Markers/btr.png";
        private static Color _btrColor = new(54f/255f, 100f/255f, 42f/255f);
        private static Vector2 _btrSize = new(45, 45f);
        private const string _btrName = "BTR";
        private const string _btrCategory = "BTR";
        //

        private MapMarker _btrMarker;

        public void OnShowInRaid(MapView map)
        {
            if (_btrMarker != null)
            {
                return;
            }

            TryAddMarker(map);
        }

        public void OnMapChanged(MapView map, MapDef mapDef)
        {
            TryRemoveMarker();
            TryAddMarker(map);
        }

        public void OnRaidEnd(MapView map)
        {
            TryRemoveMarker();
        }

        public void OnDisable(MapView map)
        {
            TryRemoveMarker();
        }

        private void TryAddMarker(MapView map)
        {
            if (_btrMarker != null)
            {
                return;
            }

			EFT.Vehicle.BTRView btrView = GameUtils.GetBTRView();
            if (btrView == null)
            {
                return;
            }

            // try adding the marker
            _btrMarker = map.AddTransformMarker(btrView.transform, _btrName, _btrCategory, _btrColor,
                                                _btrIconPath, _btrSize);
        }

        private void TryRemoveMarker()
        {
            if (_btrMarker == null)
            {
                return;
            }

            _btrMarker.ContainingMapView.RemoveMapMarker(_btrMarker);
            _btrMarker = null;
        }

        public void OnHideInRaid(MapView map)
        {
            // do nothing
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
