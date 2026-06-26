import '@arcgis/core/assets/esri/css/main.css';
import { useEffect, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import Map from '@arcgis/core/Map.js';
import MapView from '@arcgis/core/views/MapView.js';
import Graphic from '@arcgis/core/Graphic.js';
import Point from '@arcgis/core/geometry/Point.js';
import Polygon from '@arcgis/core/geometry/Polygon.js';
import MapImageLayer from '@arcgis/core/layers/MapImageLayer.js';
import SimpleMarkerSymbol from '@arcgis/core/symbols/SimpleMarkerSymbol.js';
import SimpleFillSymbol from '@arcgis/core/symbols/SimpleFillSymbol.js';
import api from '../api/client.js';
import { STATUS_LABEL_MAP } from '../i18n.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import { HAIFA_CENTER, HAIFA_GIS_MAP_SERVICES } from '../gis/gisConfig.js';
import { resolveBuildingLocation } from '../gis/resolveBuildingLocation.js';

const pointSymbol = new SimpleMarkerSymbol({
  style: 'circle',
  color: [214, 62, 62, 0.9],
  size: 14,
  outline: {
    color: [255, 255, 255, 1],
    width: 2
  }
});

const polygonSymbol = new SimpleFillSymbol({
  color: [214, 62, 62, 0.18],
  outline: {
    color: [214, 62, 62, 0.95],
    width: 2
  }
});

const formatValue = (value) => {
  if (value === null || value === undefined || value === '') return '—';
  return value;
};

const getLocationSummary = (locationResult) => {
  if (!locationResult || locationResult.type === 'not-found') return 'לא אותר';
  if (locationResult.source === 'coordinates') return 'קואורדינטות';
  return locationResult.label || 'GIS';
};

export default function MapPage() {
  const mapContainerRef = useRef(null);
  const viewRef = useRef(null);
  const highlightGraphicRef = useRef(null);
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const buildingId = searchParams.get('buildingId');
  const [mapReady, setMapReady] = useState(false);
  const [mapError, setMapError] = useState('');
  const [building, setBuilding] = useState(null);
  const [buildingError, setBuildingError] = useState('');
  const [locationResult, setLocationResult] = useState(null);
  const [locating, setLocating] = useState(false);
  useDocumentTitle('מפת GIS - מוקד המבנים העירוני');

  useEffect(() => {
    if (!mapContainerRef.current) return undefined;

    const map = new Map({
      basemap: null,
      layers: [
        new MapImageLayer({
          url: HAIFA_GIS_MAP_SERVICES.baseMap,
          title: 'Haifa municipality base map'
        }),
        new MapImageLayer({
          url: HAIFA_GIS_MAP_SERVICES.community,
          title: 'Haifa community GIS'
        }),
        new MapImageLayer({
          url: HAIFA_GIS_MAP_SERVICES.traffic,
          title: 'Haifa traffic GIS'
        }),
        new MapImageLayer({
          url: HAIFA_GIS_MAP_SERVICES.engineering,
          title: 'Haifa engineering GIS'
        }),
        new MapImageLayer({
          url: HAIFA_GIS_MAP_SERVICES.preservation,
          title: 'Haifa preservation GIS',
          opacity: 0.75
        })
      ]
    });

    const view = new MapView({
      container: mapContainerRef.current,
      map,
      center: [HAIFA_CENTER.longitude, HAIFA_CENTER.latitude],
      zoom: 13
    });

    viewRef.current = view;
    setMapReady(true);
    view
      .when()
      .catch(() => setMapError('לא הצלחנו לטעון את מפת ה-GIS של עיריית חיפה.'));

    return () => {
      viewRef.current = null;
      highlightGraphicRef.current = null;
      view.destroy();
    };
  }, []);

  useEffect(() => {
    let cancelled = false;

    const loadBuilding = async () => {
      setBuilding(null);
      setBuildingError('');
      setLocationResult(null);

      if (!buildingId) return;

      try {
        const data = await api.fetchBuilding(buildingId);
        if (!cancelled) {
          setBuilding(data);
        }
      } catch (err) {
        if (!cancelled) {
          setBuildingError(err.message || 'לא הצלחנו לטעון את פרטי המבנה.');
        }
      }
    };

    loadBuilding();
    return () => {
      cancelled = true;
    };
  }, [buildingId]);

  useEffect(() => {
    let cancelled = false;

    const locateBuilding = async () => {
      const view = viewRef.current;
      if (!view || !mapReady) return;

      if (highlightGraphicRef.current) {
        view.graphics.remove(highlightGraphicRef.current);
        highlightGraphicRef.current = null;
      }

      if (!building) {
        setLocating(false);
        return;
      }

      setLocating(true);
      setLocationResult(null);

      let result;
      try {
        result = await resolveBuildingLocation(building.gisLocation);
      } catch {
        result = {
          type: 'not-found',
          message: 'לא הצלחנו לאתר את המבנה במפת ה-GIS לפי הנתונים הקיימים.'
        };
      }

      if (cancelled) return;

      setLocationResult(result);
      if (result.type === 'not-found') {
        setLocating(false);
        return;
      }

      let geometry;
      if (result.type === 'point') {
        geometry = new Point({
          longitude: result.longitude,
          latitude: result.latitude
        });
      } else if (result.type === 'polygon') {
        geometry = new Polygon(result.geometry);
      }

      if (!geometry) {
        setLocating(false);
        return;
      }

      const graphic = new Graphic({
        geometry,
        symbol: result.type === 'point' ? pointSymbol : polygonSymbol
      });
      view.graphics.add(graphic);
      highlightGraphicRef.current = graphic;

      try {
        await view.goTo(
          result.type === 'point'
            ? { target: geometry, zoom: 18 }
            : { target: geometry.extent?.expand(1.8) || geometry, zoom: 17 },
          { duration: 850 }
        );
      } catch {
        // Ignore navigation interruptions, for example if the user starts panning.
      } finally {
        if (!cancelled) setLocating(false);
      }
    };

    locateBuilding();
    return () => {
      cancelled = true;
    };
  }, [building, mapReady]);

  const statusLabel = STATUS_LABEL_MAP[building?.status] || building?.status || '—';

  return (
    <main className="app gis-app">
      <header className="page-header">
        <div>
          <h1>מפת GIS</h1>
          <p className="subtitle">מפת עיריית חיפה עם חיבור למאגר המבנים במערכת.</p>
        </div>
        {buildingId && (
          <button type="button" className="ghost" onClick={() => navigate('/buildings')}>
            חזרה למאגר מבנים
          </button>
        )}
      </header>

      <section className="gis-layout">
        <aside className="gis-side-panel">
          <h2>פרטי מיקום</h2>
          {!buildingId && <p className="muted">בחרו מבנה ממאגר המבנים כדי למקד אותו על המפה.</p>}
          {buildingError && <p className="error">{buildingError}</p>}
          {building && (
            <>
              <dl className="gis-building-summary">
                <div>
                  <dt>מבנה</dt>
                  <dd>{formatValue(building.nickname)}</dd>
                </div>
                <div>
                  <dt>כתובת</dt>
                  <dd>
                    {formatValue(building.street)} {formatValue(building.houseNumber)}
                  </dd>
                </div>
                <div>
                  <dt>סטטוס</dt>
                  <dd>{statusLabel}</dd>
                </div>
                <div>
                  <dt>מקור איתור</dt>
                  <dd>{locating ? 'מאתר...' : getLocationSummary(locationResult)}</dd>
                </div>
              </dl>
              {locationResult?.type === 'not-found' && <p className="error">{locationResult.message}</p>}
              {locationResult?.type !== 'not-found' && locationResult && (
                <p className="success">המבנה סומן על גבי המפה.</p>
              )}
              <button
                type="button"
                className="ghost"
                onClick={() => navigate('/buildings')}
              >
                חזרה למאגר מבנים
              </button>
            </>
          )}
        </aside>

        <div className="gis-map-shell">
          {!mapReady && !mapError && <div className="gis-map-status">טוען מפה...</div>}
          {mapError && <div className="gis-map-status error">{mapError}</div>}
          <div ref={mapContainerRef} className="gis-map" aria-label="מפת GIS עיריית חיפה" />
        </div>
      </section>
    </main>
  );
}
