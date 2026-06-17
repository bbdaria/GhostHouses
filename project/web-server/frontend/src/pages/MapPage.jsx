import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import L from 'leaflet';
import api from '../api/client.js';
import { STATUS_OPTIONS, statusToLabel } from '../i18n.js';

const DEFAULT_CENTER = [32.794, 34.989];
const DEFAULT_ZOOM = 13;
const TILE_URL = import.meta.env.VITE_MAP_TILE_URL || 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
const TILE_ATTRIBUTION =
  import.meta.env.VITE_MAP_ATTRIBUTION || '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';
const GEOCODE_CITY = import.meta.env.VITE_MAP_GEOCODE_CITY || 'חיפה';
const GEOCODE_COUNTRY = import.meta.env.VITE_MAP_GEOCODE_COUNTRY || 'ישראל';
const GEOCODE_CACHE_KEY = 'ghosthouses:geocoded-buildings:v1';
const MAX_GEOCODE_PER_LOAD = 25;
const GEOCODE_DELAY_MS = 1100;
const HEBREW_LABELS = {
  pageEyebrow: 'מפה',
  pageTitle: 'מפת מבנים',
  pageSubtitle: 'ניתן להזיז ולהגדיל את המפה, לבחור אזור, ולראות את מצב המבנים הרשומים במערכת.',
  noBuildingTitle: 'לא נבחר מבנה',
  noBuildingText: 'בחרו סימון על המפה או סמנו אזור כדי לראות פרטים.',
  buildingNumber: 'מבנה',
  address: 'כתובת',
  status: 'סטטוס',
  classification: 'סיווג',
  area: 'אזור',
  centerOnMap: 'מרכז במפה',
  noCoordinates: 'למבנה אין קואורדינטות ולכן לא ניתן למרכז אותו במפה.',
  all: 'הכול',
  selectArea: 'בחירת אזור',
  clear: 'ניקוי',
  mappedLoading: 'טוען מבנים מהמפה...',
  mappedCount: 'מבנים עם מיקום במפה',
  firstCorner: 'בחרו את הפינה הראשונה של האזור.',
  oppositeCorner: 'בחרו את הפינה הנגדית של האזור.',
  selectedArea: 'אזור נבחר',
  buildings: 'מבנים',
  noSelectedArea: 'עדיין לא נבחר אזור.',
  unmappedTitle: 'מבנים ללא מיקום במפה',
  unmappedText: 'המבנים האלה קיימים במסד הנתונים, אבל חסרים להם קווי אורך/רוחב ולכן אי אפשר להציג אותם כסימון על המפה.',
  noUnmapped: 'לכל המבנים המסוננים יש מיקום.',
  needsAttention: 'דורש טיפול',
  planned: 'בתכנון',
  active: 'פעיל',
  unknown: 'לא ידוע',
  estimated: 'מיקום משוער לפי כתובת',
  geocoding: 'מאתר כתובות על המפה...',
  geocodedCount: 'מבנים מוקמו לפי כתובת',
  failedLoad: 'טעינת המבנים למפה נכשלה.',
  failedSelection: 'טעינת המבנים באזור הנבחר נכשלה.',
  geocodingPartial: 'חלק מהכתובות לא אותרו. מומלץ להזין קווי אורך/רוחב במבנה עצמו.'
};

const statusClass = (status) => {
  if (!status || status === 'Unknown') return 'map-marker--unknown';
  if (status === 'InExecution' || status === 'OccupancyProcess') return 'map-marker--active';
  if (status === 'PlanApprovedPreparingExecution' || status === 'PreparingRehabPlan') return 'map-marker--planned';
  return 'map-marker--attention';
};

const sleep = (ms) => new Promise((resolve) => window.setTimeout(resolve, ms));

const loadGeocodeCache = () => {
  try {
    const raw = localStorage.getItem(GEOCODE_CACHE_KEY);
    return raw ? JSON.parse(raw) : {};
  } catch {
    return {};
  }
};

const saveGeocodeCache = (cache) => {
  try {
    localStorage.setItem(GEOCODE_CACHE_KEY, JSON.stringify(cache));
  } catch {
    // The map still works without local storage caching.
  }
};

const buildAddressQuery = (building) =>
  [building.street, building.houseNumber, GEOCODE_CITY, GEOCODE_COUNTRY]
    .filter((part) => part !== null && part !== undefined && String(part).trim() !== '')
    .join(' ');

const geocodeAddress = async (building) => {
  const query = buildAddressQuery(building);
  if (!query) {
    return null;
  }

  const params = new URLSearchParams({
    format: 'jsonv2',
    limit: '1',
    countrycodes: 'il',
    q: query
  });

  const response = await fetch(`https://nominatim.openstreetmap.org/search?${params.toString()}`, {
    headers: {
      Accept: 'application/json'
    }
  });

  if (!response.ok) {
    return null;
  }

  const results = await response.json();
  const first = Array.isArray(results) ? results[0] : null;
  if (!first?.lat || !first?.lon) {
    return null;
  }

  const latitude = Number(first.lat);
  const longitude = Number(first.lon);
  if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) {
    return null;
  }

  return {
    latitude,
    longitude,
    displayName: first.display_name || query
  };
};

function markerIcon(building, isSelected) {
  return L.divIcon({
    className: `map-marker ${statusClass(building.status)}${building.isGeocoded ? ' map-marker--estimated' : ''}${
      isSelected ? ' map-marker--selected' : ''
    }`,
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
  const source = document.createElement('small');

  title.textContent = building.nickname || `${HEBREW_LABELS.buildingNumber} ${building.id}`;
  address.textContent = `${building.street || ''} ${building.houseNumber || ''}`.trim();
  status.textContent = statusToLabel(building.status);
  source.textContent = building.isGeocoded ? HEBREW_LABELS.estimated : '';

  container.append(title, address, status);
  if (building.isGeocoded) {
    container.append(source);
  }
  return container;
}

function BuildingMiniCard({ building, onFocus }) {
  if (!building) {
    return (
      <div className="map-empty-state">
        <strong>{HEBREW_LABELS.noBuildingTitle}</strong>
        <span>{HEBREW_LABELS.noBuildingText}</span>
      </div>
    );
  }

  return (
    <article className="map-mini-card">
      <div>
        <p className="eyebrow">
          {HEBREW_LABELS.buildingNumber} #{building.id}
        </p>
        <h3>{building.nickname || `${building.street} ${building.houseNumber}`}</h3>
      </div>
      <dl>
        <dt>{HEBREW_LABELS.address}</dt>
        <dd>
          {building.street} {building.houseNumber}
        </dd>
        <dt>{HEBREW_LABELS.status}</dt>
        <dd>{statusToLabel(building.status)}</dd>
        <dt>{HEBREW_LABELS.classification}</dt>
        <dd>{building.bldSivug || '-'}</dd>
        <dt>{HEBREW_LABELS.area}</dt>
        <dd>{building.area || '-'}</dd>
        {building.isGeocoded ? (
          <>
            <dt>{HEBREW_LABELS.estimated}</dt>
            <dd>{building.geocodedAddress || '-'}</dd>
          </>
        ) : null}
      </dl>
      {building.statusSummary ? <p>{building.statusSummary}</p> : null}
      {building.isMapped ? (
        <button type="button" className="ghost" onClick={() => onFocus(building)}>
          {HEBREW_LABELS.centerOnMap}
        </button>
      ) : (
        <p className="muted">{HEBREW_LABELS.noCoordinates}</p>
      )}
    </article>
  );
}

export default function MapPage() {
  const mapElementRef = useRef(null);
  const mapRef = useRef(null);
  const markersRef = useRef(null);
  const selectionRef = useRef(null);
  const selectionStartRef = useRef(null);
  const didFitMarkersRef = useRef(false);

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
  const [geocodedById, setGeocodedById] = useState(() => loadGeocodeCache());
  const [geocodeFailedIds, setGeocodeFailedIds] = useState(() => new Set());
  const [geocoding, setGeocoding] = useState(false);
  const [geocodingMessage, setGeocodingMessage] = useState('');
  const mappedBuildings = useMemo(() => buildings.filter((building) => building.isMapped), [buildings]);
  const geocodedBuildings = useMemo(
    () =>
      buildings
        .filter((building) => !building.isMapped && geocodedById[building.id])
        .map((building) => ({
          ...building,
          latitude: geocodedById[building.id].latitude,
          longitude: geocodedById[building.id].longitude,
          isMapped: true,
          isGeocoded: true,
          geocodedAddress: geocodedById[building.id].displayName
        })),
    [buildings, geocodedById]
  );
  const visibleBuildings = useMemo(() => [...mappedBuildings, ...geocodedBuildings], [mappedBuildings, geocodedBuildings]);
  const unmappedBuildings = useMemo(
    () => buildings.filter((building) => !building.isMapped && !geocodedById[building.id]),
    [buildings, geocodedById]
  );

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
    async () => {
      setLoading(true);
      setError('');
      try {
        const data = await api.fetchMapBuildings(null, { ...filters, includeUnmapped: true });
        setBuildings(data);
      } catch (err) {
        setError(err.message || HEBREW_LABELS.failedLoad);
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

    loadBuildings();

    return () => {
    };
  }, [loadBuildings, mapReady]);

  useEffect(() => {
    const layer = markersRef.current;
    if (!layer) {
      return;
    }

    layer.clearLayers();
    visibleBuildings.forEach((building) => {
      const marker = L.marker([building.latitude, building.longitude], {
        icon: markerIcon(building, selectedBuilding?.id === building.id)
      });

      marker.bindPopup(popupContent(building));
      marker.on('click', () => setSelectedBuilding(building));
      marker.addTo(layer);
    });
  }, [selectedBuilding?.id, visibleBuildings]);

  useEffect(() => {
    const map = mapRef.current;
    if (!mapReady || !map || didFitMarkersRef.current || visibleBuildings.length === 0) {
      return;
    }

    const bounds = L.latLngBounds(visibleBuildings.map((building) => [building.latitude, building.longitude]));
    map.fitBounds(bounds.pad(0.15), { maxZoom: 16 });
    didFitMarkersRef.current = true;
  }, [visibleBuildings, mapReady]);

  useEffect(() => {
    if (unmappedBuildings.length === 0 || geocoding) {
      return undefined;
    }

    let cancelled = false;

    const run = async () => {
      const nextCache = { ...loadGeocodeCache(), ...geocodedById };
      const nextFailedIds = new Set(geocodeFailedIds);
      const candidates = unmappedBuildings
        .filter((building) => buildAddressQuery(building))
        .filter((building) => !nextFailedIds.has(building.id))
        .slice(0, MAX_GEOCODE_PER_LOAD);

      if (candidates.length === 0) {
        return;
      }

      setGeocoding(true);
      setGeocodingMessage(HEBREW_LABELS.geocoding);
      let misses = 0;
      let changed = false;

      for (const building of candidates) {
        if (cancelled) {
          return;
        }

        if (nextCache[building.id]) {
          continue;
        }

        const result = await geocodeAddress(building);
        if (cancelled) {
          return;
        }

        if (result) {
          nextCache[building.id] = result;
          changed = true;
        } else {
          misses += 1;
          nextFailedIds.add(building.id);
          changed = true;
        }

        await sleep(GEOCODE_DELAY_MS);
      }

      if (!cancelled) {
        if (changed) {
          saveGeocodeCache(nextCache);
          setGeocodedById({ ...nextCache });
          setGeocodeFailedIds(new Set(nextFailedIds));
        }
        setGeocoding(false);
        setGeocodingMessage(misses > 0 ? HEBREW_LABELS.geocodingPartial : '');
      }
    };

    run();

    return () => {
      cancelled = true;
    };
  }, [geocodeFailedIds, geocodedById, geocoding, unmappedBuildings]);

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
        setSelectionHint(HEBREW_LABELS.oppositeCorner);
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
        const selected = visibleBuildings.filter((building) => bounds.contains([building.latitude, building.longitude]));
        setSelectedBuildings(selected);
        setSelectedBuilding(selected[0] || null);
      } catch (err) {
        setError(err.message || HEBREW_LABELS.failedSelection);
      } finally {
        setSelectionLoading(false);
      }
    };

    map.on('click', handleClick);
    map.getContainer().classList.toggle('map-selecting', selectionMode);
    setSelectionHint(selectionMode ? HEBREW_LABELS.firstCorner : '');

    return () => {
      map.off('click', handleClick);
      map.getContainer().classList.remove('map-selecting');
    };
  }, [mapReady, selectionMode, visibleBuildings]);

  const visibleCountLabel = useMemo(() => {
    if (loading) return HEBREW_LABELS.mappedLoading;
    return `${visibleBuildings.length} ${HEBREW_LABELS.mappedCount}`;
  }, [visibleBuildings.length, loading]);

  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    didFitMarkersRef.current = false;
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
    if (!building.isMapped || !Number.isFinite(building.latitude) || !Number.isFinite(building.longitude)) {
      return;
    }
    map.setView([building.latitude, building.longitude], Math.max(map.getZoom(), 17));
  };

  return (
    <main className="map-app">
      <div className="page-header">
        <div>
          <p className="eyebrow">{HEBREW_LABELS.pageEyebrow}</p>
          <h1>{HEBREW_LABELS.pageTitle}</h1>
          <p className="subtitle">{HEBREW_LABELS.pageSubtitle}</p>
        </div>
        <div className="map-toolbar">
          <label>
            {HEBREW_LABELS.status}
            <select name="status" value={filters.status} onChange={handleFilterChange}>
              <option value="">{HEBREW_LABELS.all}</option>
              {STATUS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            {HEBREW_LABELS.classification}
            <select name="bldSivug" value={filters.bldSivug} onChange={handleFilterChange}>
              <option value="">{HEBREW_LABELS.all}</option>
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
            {HEBREW_LABELS.selectArea}
          </button>
          <button type="button" className="ghost" onClick={clearSelection}>
            {HEBREW_LABELS.clear}
          </button>
        </div>
      </div>

      {error ? <p className="error">{error}</p> : null}

      <section className="map-layout">
        <div className="map-canvas-panel">
          <div className="map-status-bar">
            <span>{visibleCountLabel}</span>
            {selectionHint ? <strong>{selectionHint}</strong> : null}
            {geocoding ? <strong>{geocodingMessage}</strong> : null}
          </div>
          <div ref={mapElementRef} className="map-canvas" />
        </div>

        <aside className="map-side-panel">
          <BuildingMiniCard building={selectedBuilding} onFocus={focusBuilding} />

          <div className="map-selection-panel">
            <div className="panel-header">
              <div>
                <p className="eyebrow">{HEBREW_LABELS.selectedArea}</p>
                <h3>{selectionLoading ? HEBREW_LABELS.mappedLoading : `${selectedBuildings.length} ${HEBREW_LABELS.buildings}`}</h3>
              </div>
            </div>
            {selectedBuildings.length === 0 ? (
              <p className="muted">{HEBREW_LABELS.noSelectedArea}</p>
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

          <div className="map-selection-panel">
            <div className="panel-header">
              <div>
                <p className="eyebrow">{HEBREW_LABELS.unmappedTitle}</p>
                <h3>{unmappedBuildings.length}</h3>
              </div>
            </div>
            <p className="muted">{unmappedBuildings.length > 0 ? HEBREW_LABELS.unmappedText : HEBREW_LABELS.noUnmapped}</p>
            {geocodedBuildings.length > 0 ? (
              <p className="success">
                {geocodedBuildings.length} {HEBREW_LABELS.geocodedCount}
              </p>
            ) : null}
            {geocodingMessage && !geocoding ? <p className="muted">{geocodingMessage}</p> : null}
            {unmappedBuildings.length > 0 ? (
              <ul className="map-selection-list">
                {unmappedBuildings.slice(0, 25).map((building) => (
                  <li key={building.id}>
                    <button type="button" onClick={() => setSelectedBuilding(building)}>
                      <strong>
                        {building.street} {building.houseNumber}
                      </strong>
                      <span>{statusToLabel(building.status)}</span>
                    </button>
                  </li>
                ))}
              </ul>
            ) : null}
          </div>

          <div className="map-legend">
            <span>
              <i className="map-dot map-dot--attention" /> {HEBREW_LABELS.needsAttention}
            </span>
            <span>
              <i className="map-dot map-dot--planned" /> {HEBREW_LABELS.planned}
            </span>
            <span>
              <i className="map-dot map-dot--active" /> {HEBREW_LABELS.active}
            </span>
            <span>
              <i className="map-dot map-dot--unknown" /> {HEBREW_LABELS.unknown}
            </span>
            <span>
              <i className="map-dot map-dot--estimated" /> {HEBREW_LABELS.estimated}
            </span>
          </div>
        </aside>
      </section>
    </main>
  );
}
