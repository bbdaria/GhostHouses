import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import L from 'leaflet';
import api from '../api/client.js';
import { STATUS_OPTIONS, statusToLabel } from '../i18n.js';

const DEFAULT_CENTER = [32.794, 34.989];
const DEFAULT_ZOOM = 13;
const TILE_URL = import.meta.env.VITE_MAP_TILE_URL || 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
const TILE_ATTRIBUTION =
  import.meta.env.VITE_MAP_ATTRIBUTION || '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';

const statusClass = (status) => {
  if (!status || status === 'Unknown') return 'map-marker--unknown';
  if (status === 'InExecution' || status === 'OccupancyProcess') return 'map-marker--active';
  if (status === 'PlanApprovedPreparingExecution' || status === 'PreparingRehabPlan') return 'map-marker--planned';
  return 'map-marker--attention';
};

const toBoundsQuery = (bounds) => ({
  north: bounds.getNorth(),
  south: bounds.getSouth(),
  east: bounds.getEast(),
  west: bounds.getWest()
});

function markerIcon(building, isSelected) {
  return L.divIcon({
    className: `map-marker ${statusClass(building.status)}${isSelected ? ' map-marker--selected' : ''}`,
    html: `<span>${building.id}</span>`,
    iconSize: [34, 34],
    iconAnchor: [17, 17],
    popupAnchor: [0, -18]
  });
}

function popupContent(building) {
  const container = document.createElement('div');
  const title = document.createElement('strong');
  const address = document.createElement('div');
  const status = document.createElement('div');

  title.textContent = building.nickname || `Building ${building.id}`;
  address.textContent = `${building.street || ''} ${building.houseNumber || ''}`.trim();
  status.textContent = statusToLabel(building.status);

  container.append(title, address, status);
  return container;
}

function BuildingMiniCard({ building, onFocus }) {
  if (!building) {
    return (
      <div className="map-empty-state">
        <strong>No building selected</strong>
        <span>Select a marker or draw an area to inspect buildings.</span>
      </div>
    );
  }

  return (
    <article className="map-mini-card">
      <div>
        <p className="eyebrow">Building #{building.id}</p>
        <h3>{building.nickname || `${building.street} ${building.houseNumber}`}</h3>
      </div>
      <dl>
        <dt>Address</dt>
        <dd>
          {building.street} {building.houseNumber}
        </dd>
        <dt>Status</dt>
        <dd>{statusToLabel(building.status)}</dd>
        <dt>Classification</dt>
        <dd>{building.bldSivug || '-'}</dd>
        <dt>Area</dt>
        <dd>{building.area || '-'}</dd>
      </dl>
      {building.statusSummary ? <p>{building.statusSummary}</p> : null}
      <button type="button" className="ghost" onClick={() => onFocus(building)}>
        Center on map
      </button>
    </article>
  );
}

export default function MapPage() {
  const mapElementRef = useRef(null);
  const mapRef = useRef(null);
  const markersRef = useRef(null);
  const selectionRef = useRef(null);
  const selectionStartRef = useRef(null);

  const [mapReady, setMapReady] = useState(false);
  const [buildings, setBuildings] = useState([]);
  const [selectedBuilding, setSelectedBuilding] = useState(null);
  const [selectedBuildings, setSelectedBuildings] = useState([]);
  const [loading, setLoading] = useState(false);
  const [selectionLoading, setSelectionLoading] = useState(false);
  const [error, setError] = useState('');
  const [selectionMode, setSelectionMode] = useState(false);
  const [selectionHint, setSelectionHint] = useState('');
  const [filters, setFilters] = useState({ status: '', bldSivug: '' });
  const [sivugOptions, setSivugOptions] = useState([]);

  useEffect(() => {
    let mounted = true;
    const loadSivugOptions = async () => {
      try {
        const options = await api.fetchSelectTable('Tbl_Sivug');
        if (mounted) {
          setSivugOptions(options || []);
        }
      } catch {
        if (mounted) {
          setSivugOptions([]);
        }
      }
    };

    loadSivugOptions();
    return () => {
      mounted = false;
    };
  }, []);

  useEffect(() => {
    if (!mapElementRef.current || mapRef.current) {
      return undefined;
    }

    const map = L.map(mapElementRef.current, {
      zoomControl: true,
      preferCanvas: true
    }).setView(DEFAULT_CENTER, DEFAULT_ZOOM);

    L.tileLayer(TILE_URL, {
      maxZoom: 19,
      attribution: TILE_ATTRIBUTION
    }).addTo(map);

    markersRef.current = L.layerGroup().addTo(map);
    selectionRef.current = L.layerGroup().addTo(map);
    mapRef.current = map;
    setMapReady(true);

    return () => {
      map.remove();
      mapRef.current = null;
      markersRef.current = null;
      selectionRef.current = null;
    };
  }, []);

  const loadBuildings = useCallback(
    async (bounds) => {
      setLoading(true);
      setError('');
      try {
        const data = await api.fetchMapBuildings(toBoundsQuery(bounds), filters);
        setBuildings(data);
      } catch (err) {
        setError(err.message || 'Failed to load map buildings.');
      } finally {
        setLoading(false);
      }
    },
    [filters]
  );

  useEffect(() => {
    const map = mapRef.current;
    if (!mapReady || !map) {
      return undefined;
    }

    const handleMoveEnd = () => {
      loadBuildings(map.getBounds());
    };

    map.on('moveend', handleMoveEnd);
    handleMoveEnd();

    return () => {
      map.off('moveend', handleMoveEnd);
    };
  }, [loadBuildings, mapReady]);

  useEffect(() => {
    const layer = markersRef.current;
    if (!layer) {
      return;
    }

    layer.clearLayers();
    buildings.forEach((building) => {
      const marker = L.marker([building.latitude, building.longitude], {
        icon: markerIcon(building, selectedBuilding?.id === building.id)
      });

      marker.bindPopup(popupContent(building));
      marker.on('click', () => setSelectedBuilding(building));
      marker.addTo(layer);
    });
  }, [buildings, selectedBuilding?.id]);

  useEffect(() => {
    const map = mapRef.current;
    if (!mapReady || !map) {
      return undefined;
    }

    const handleClick = async (event) => {
      if (!selectionMode) {
        return;
      }

      if (!selectionStartRef.current) {
        selectionStartRef.current = event.latlng;
        setSelectionHint('Select the opposite corner of the area.');
        return;
      }

      const bounds = L.latLngBounds(selectionStartRef.current, event.latlng);
      selectionStartRef.current = null;
      selectionRef.current?.clearLayers();
      L.rectangle(bounds, {
        color: '#38bdf8',
        weight: 2,
        fillColor: '#38bdf8',
        fillOpacity: 0.12
      }).addTo(selectionRef.current);

      setSelectionLoading(true);
      setSelectionHint('');
      try {
        const data = await api.fetchMapBuildings(toBoundsQuery(bounds), filters);
        setSelectedBuildings(data);
        setSelectedBuilding(data[0] || null);
      } catch (err) {
        setError(err.message || 'Failed to load selected buildings.');
      } finally {
        setSelectionLoading(false);
      }
    };

    map.on('click', handleClick);
    map.getContainer().classList.toggle('map-selecting', selectionMode);
    setSelectionHint(selectionMode ? 'Select the first corner of the area.' : '');

    return () => {
      map.off('click', handleClick);
      map.getContainer().classList.remove('map-selecting');
    };
  }, [filters, mapReady, selectionMode]);

  const visibleCountLabel = useMemo(() => {
    if (loading) return 'Loading map buildings...';
    return `${buildings.length} mapped buildings in view`;
  }, [buildings.length, loading]);

  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters((current) => ({ ...current, [name]: value }));
  };

  const clearSelection = () => {
    selectionRef.current?.clearLayers();
    selectionStartRef.current = null;
    setSelectedBuildings([]);
    setSelectionMode(false);
    setSelectionHint('');
  };

  const focusBuilding = (building) => {
    const map = mapRef.current;
    if (!map || !building) {
      return;
    }

    setSelectedBuilding(building);
    map.setView([building.latitude, building.longitude], Math.max(map.getZoom(), 17));
  };

  return (
    <main className="map-app">
      <div className="page-header">
        <div>
          <p className="eyebrow">Map</p>
          <h1>Building locations</h1>
          <p className="subtitle">Pan the map to inspect mapped abandoned buildings and select an area to review details.</p>
        </div>
        <div className="map-toolbar">
          <label>
            Status
            <select name="status" value={filters.status} onChange={handleFilterChange}>
              <option value="">All</option>
              {STATUS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            Classification
            <select name="bldSivug" value={filters.bldSivug} onChange={handleFilterChange}>
              <option value="">All</option>
              {sivugOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <button
            type="button"
            className={selectionMode ? 'primary' : 'ghost'}
            onClick={() => {
              selectionStartRef.current = null;
              setSelectionMode((current) => !current);
            }}
          >
            Select area
          </button>
          <button type="button" className="ghost" onClick={clearSelection}>
            Clear
          </button>
        </div>
      </div>

      {error ? <p className="error">{error}</p> : null}

      <section className="map-layout">
        <div className="map-canvas-panel">
          <div className="map-status-bar">
            <span>{visibleCountLabel}</span>
            {selectionHint ? <strong>{selectionHint}</strong> : null}
          </div>
          <div ref={mapElementRef} className="map-canvas" />
        </div>

        <aside className="map-side-panel">
          <BuildingMiniCard building={selectedBuilding} onFocus={focusBuilding} />

          <div className="map-selection-panel">
            <div className="panel-header">
              <div>
                <p className="eyebrow">Selected area</p>
                <h3>{selectionLoading ? 'Loading...' : `${selectedBuildings.length} buildings`}</h3>
              </div>
            </div>
            {selectedBuildings.length === 0 ? (
              <p className="muted">No selected area yet.</p>
            ) : (
              <ul className="map-selection-list">
                {selectedBuildings.map((building) => (
                  <li key={building.id}>
                    <button type="button" onClick={() => focusBuilding(building)}>
                      <strong>
                        {building.street} {building.houseNumber}
                      </strong>
                      <span>{statusToLabel(building.status)}</span>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>

          <div className="map-legend">
            <span>
              <i className="map-dot map-dot--attention" /> Needs attention
            </span>
            <span>
              <i className="map-dot map-dot--planned" /> Planned
            </span>
            <span>
              <i className="map-dot map-dot--active" /> Active
            </span>
            <span>
              <i className="map-dot map-dot--unknown" /> Unknown
            </span>
          </div>
        </aside>
      </section>
    </main>
  );
}
