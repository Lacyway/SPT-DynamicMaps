using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using DynamicMaps.Data;
using DynamicMaps.UI.Components;
using DynamicMaps.Utils;
using EFT;
using EFT.Interactive;
using UnityEngine;

namespace DynamicMaps.DynamicMarkers
{
	public class ExtractMarkerProvider : IDynamicMarkerProvider
	{
		// TODO: move to config
		private const string _extractCategory = "Extract";
		private const string _extractImagePath = "Markers/exit.png";
		private static Color _extractDefaultColor = Color.yellow;
		private static Color _extractOpenColor = Color.green;
		private static Color _extractHasRequirementsColor = Color.yellow;
		private static Color _extractClosedColor = Color.red;
		//

		private bool _showExtractStatusInRaid = true;
		public bool ShowExtractStatusInRaid
		{
			get
			{
				return _showExtractStatusInRaid;
			}

			set
			{
				if (_showExtractStatusInRaid == value)
				{
					return;
				}

				_showExtractStatusInRaid = value;

				// force update all statuses
				foreach (ExfiltrationPoint extract in _extractMarkers.Keys)
				{
					UpdateExtractStatus(extract, extract.Status);
				}
			}
		}

		private Dictionary<ExfiltrationPoint, MapMarker> _extractMarkers
			= [];

		public void OnShowInRaid(MapView map)
		{
			// get valid extracts only on first time that this is run in a raid
			if (_extractMarkers.Count == 0)
			{
				AddExtractMarkers(map);
			}

			foreach (ExfiltrationPoint extract in _extractMarkers.Keys)
			{
				// update color based on exfil status
				UpdateExtractStatus(extract, extract.Status);

				// subscribe to status changes while map is shown
				extract.OnStatusChanged += UpdateExtractStatus;
			}
		}

		public void OnHideInRaid(MapView map)
		{
			// unsubscribe from updates while map is hidden
			foreach (ExfiltrationPoint extract in _extractMarkers.Keys)
			{
				extract.OnStatusChanged -= UpdateExtractStatus;
			}
		}

		public void OnRaidEnd(MapView map)
		{
			TryRemoveMarkers();
		}

		public void OnMapChanged(MapView map, MapDef mapDef)
		{
			foreach (ExfiltrationPoint extract in _extractMarkers.Keys.ToList())
			{
				TryRemoveMarker(extract);
				TryAddMarker(map, extract);
			}
		}

		public void OnDisable(MapView map)
		{
			TryRemoveMarkers();
		}

		private void AddExtractMarkers(MapView map)
		{
			GameWorld gameWorld = Singleton<GameWorld>.Instance;
			Player player = GameUtils.GetMainPlayer();

			IEnumerable<ExfiltrationPoint> extracts;
			if (GameUtils.IsScavRaid())
			{
				extracts = gameWorld.ExfiltrationController.ScavExfiltrationPoints
								.Where(p => p.isActiveAndEnabled && p.InfiltrationMatch(player))
								.Cast<ExfiltrationPoint>();
			}
			else
			{
				extracts = gameWorld.ExfiltrationController.ExfiltrationPoints
								.Where(p => p.isActiveAndEnabled && p.InfiltrationMatch(player));
			}

			// add markers, only this single time
			foreach (ExfiltrationPoint extract in extracts)
			{
				TryAddMarker(map, extract);
			}
		}

		private void TryRemoveMarkers()
		{
			foreach (ExfiltrationPoint extract in _extractMarkers.Keys.ToList())
			{
				TryRemoveMarker(extract);
			}
		}

		private void UpdateExtractStatus(ExfiltrationPoint extract, EExfiltrationStatus status)
		{
			if (!_extractMarkers.ContainsKey(extract))
			{
				return;
			}

			MapMarker marker = _extractMarkers[extract];
			if (!_showExtractStatusInRaid)
			{
				marker.Color = _extractDefaultColor;
				return;
			}

			switch (extract.Status)
			{
				case EExfiltrationStatus.NotPresent:
					marker.Color = _extractClosedColor;
					break;
				case EExfiltrationStatus.UncompleteRequirements:
					marker.Color = _extractHasRequirementsColor;
					return;
				default:
					marker.Color = _extractOpenColor;
					break;
			}
		}

		private void TryAddMarker(MapView map, ExfiltrationPoint extract)
		{
			if (_extractMarkers.ContainsKey(extract))
			{
				return;
			}

			MapMarkerDef markerDef = new MapMarkerDef
			{
				Category = _extractCategory,
				ImagePath = _extractImagePath,
				Text = extract.Settings.Name.BSGLocalized(),
				Position = MathUtils.ConvertToMapPosition(extract.transform)
			};

			MapMarker marker = map.AddMapMarker(markerDef);
			_extractMarkers[extract] = marker;

			UpdateExtractStatus(extract, extract.Status);
		}

		private void TryRemoveMarker(ExfiltrationPoint extract)
		{
			if (!_extractMarkers.ContainsKey(extract))
			{
				return;
			}

			extract.OnStatusChanged -= UpdateExtractStatus;

			_extractMarkers[extract].ContainingMapView.RemoveMapMarker(_extractMarkers[extract]);
			_extractMarkers.Remove(extract);
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
