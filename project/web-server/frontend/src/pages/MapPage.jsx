import '@arcgis/core/assets/esri/css/main.css';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import Map from '@arcgis/core/Map.js';
import MapView from '@arcgis/core/views/MapView.js';
import Graphic from '@arcgis/core/Graphic.js';
import MapImageLayer from '@arcgis/core/layers/MapImageLayer.js';
import GraphicsLayer from '@arcgis/core/layers/GraphicsLayer.js';
import SketchViewModel from '@arcgis/core/widgets/Sketch/SketchViewModel.js';
import api from '../api/client.js';
import BuildingModal from '../components/BuildingModal.jsx';
import { STATUS_LABEL_MAP } from '../i18n.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import { formatDateTime } from '../utils/formatDate.js';
import { HAIFA_CENTER, HAIFA_GIS_MAP_SERVICES } from '../gis/gisConfig.js';
import { resolveBuildingLocation } from '../gis/resolveBuildingLocation.js';
import {
  allBuildingsPointSymbol,
  allBuildingsPolygonSymbol,
  areaResultPointSymbol,
  areaResultPolygonSymbol,
  buildingPointSymbol,
  buildingPolygonSymbol,
  selectedAreaSymbol
} from '../gis/mapSymbols.js';
import {
  createGeometryFromLocationResult,
  geometryTouchesArea,
  getGeometryTarget
} from '../gis/buildingGeometry.js';

const formatValue = (value) => {
  if (value === null || value === undefined || value === '') return '—';
  return value;
};

const EXCEL_LABEL_OVERRIDES = {
  'ID נכס לצורך מערכת זו בלבד': 'ID',
  'תמצית מצב': 'תמונת מצב',
  'תאריך עדכון תמצית מצב': 'תאריך שינוי',
  'ציון עמידה בסטנדרט': 'ציון',
  'פרטי מחזיקים': 'פרטי מחזיק',
  'האם הייתה צריכת מים ב־6 החודשים האחרונים': 'צריכת מים ב-6 החודשים האחרונים',
  'האם הייתה צריכת חשמל ב־6 החודשים האחרונים': 'צריכת חשמל ב-6 החודשים האחרונים',
  'אחוז המבנה שמוגדר ניזוק': 'אחוז המבנה שעומד ניזוק',
  'קוארדינטות אורך': 'קוארדינטות',
  'קוארדינטות רוחב': 'קוארדינטות'
};

const getLocationSummary = (locationResult) => {
  if (!locationResult || locationResult.type === 'not-found') return 'לא אותר';
  if (locationResult.source === 'coordinates') return 'קואורדינטות';
  return locationResult.label || 'GIS';
};

const sortFieldsForDisplay = (fields) => {
  if (!Array.isArray(fields)) return [];
  const fieldPriority = (name) => {
    if (name === 'שם רחוב') return 0;
    if (name === 'מספר בית') return 1;
    if (name === 'כינוי הבניין') return 2;
    if (name === 'סיווג') return 3;
    if (name === 'סטטוס שיקום') return 4;
    return 5;
  };
  return fields
    .map((field, index) => ({ field, index }))
    .sort((a, b) => {
      const aPriority = fieldPriority(a.field.fieldName);
      const bPriority = fieldPriority(b.field.fieldName);
      if (aPriority !== bPriority) return aPriority - bPriority;
      return a.index - b.index;
    })
    .map((entry) => entry.field);
};

const getExcelAwareLabel = (fieldName) => {
  if (!fieldName) return '';
  const excelName = EXCEL_LABEL_OVERRIDES[fieldName];
  if (!excelName || excelName === fieldName) return fieldName;
  if (excelName === 'ID') return excelName;
  if (excelName === 'תאריך שינוי') return excelName;
  if (excelName === 'קוארדינטות') {
    if (fieldName.includes('אורך')) return 'קוארדינטות (אורך)';
    if (fieldName.includes('רוחב')) return 'קוארדינטות (רוחב)';
    return excelName;
  }
  return `${excelName} (${fieldName})`;
};

const isDateField = (field) => {
  if (!field) return false;
  const name = field.fieldName || '';
  const column = (field.columnName || '').toLowerCase();
  if (name.includes('תאריך')) return true;
  if (column.endsWith('dt') || column.includes('date')) return true;
  if (typeof field.value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(field.value)) return true;
  return false;
};

const shouldUseTextarea = (fieldName) => {
  if (!fieldName) return false;
  return (
    fieldName.includes('פרטי') ||
    fieldName.includes('תלונות') ||
    fieldName.includes('תמצית') ||
    fieldName.includes('תקציר') ||
    fieldName.includes('הסיבה') ||
    fieldName.includes('הערות')
  );
};

const formatLogDate = (value) => {
  if (!value) return '—';
  try {
    return formatDateTime(value);
  } catch {
    return value;
  }
};

const formatStatusFieldValue = (field) => {
  if (!field || field.fieldName !== 'סטטוס שיקום') {
    return formatValue(field?.value);
  }
  const value = field.value;
  if (!value || value === '0' || value === 'Unknown' || value === 'לא ידוע') {
    return '—';
  }
  return STATUS_LABEL_MAP[value] || value;
};

const groupBuildingFields = (fields = []) =>
  fields.reduce((acc, field) => {
    const category = field.category || 'כללי';
    if (!acc[category]) acc[category] = [];
    acc[category].push(field);
    return acc;
  }, {});

const orderFieldGroups = (fieldsByCategory) => {
  const entries = Object.entries(fieldsByCategory);
  if (entries.length === 0) return [];
  const priority = (category) => {
    if (category === 'מידע כללי') return 0;
    if (category === 'פרטים מזהים') return 1;
    return 2;
  };
  return entries
    .map((entry, index) => ({ entry, index }))
    .sort((a, b) => {
      const aPriority = priority(a.entry[0]);
      const bPriority = priority(b.entry[0]);
      if (aPriority !== bPriority) return aPriority - bPriority;
      return a.index - b.index;
    })
    .map((item) => item.entry);
};

const getDefaultOpenCategories = (orderedFieldGroups) => {
  if (orderedFieldGroups.length === 0) return [];
  const categories = orderedFieldGroups.map(([category]) => category);
  const defaultCategory = categories.includes('מידע כללי') ? 'מידע כללי' : categories[0];
  return defaultCategory ? [defaultCategory] : [];
};

export default function MapPage() {
  const mapContainerRef = useRef(null);
  const viewRef = useRef(null);
  const allBuildingsLayerRef = useRef(null);
  const highlightLayerRef = useRef(null);
  const areaSelectionLayerRef = useRef(null);
  const areaResultsLayerRef = useRef(null);
  const sketchRef = useRef(null);
  const areaDrawingRef = useRef(false);
  const gisCandidatesRef = useRef(null);
  const gisCandidatesPromiseRef = useRef(null);
  const locationCacheRef = useRef(new globalThis.Map());
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const buildingId = searchParams.get('buildingId');
  const [mapReady, setMapReady] = useState(false);
  const [mapError, setMapError] = useState('');
  const [building, setBuilding] = useState(null);
  const [buildingError, setBuildingError] = useState('');
  const [locationResult, setLocationResult] = useState(null);
  const [locating, setLocating] = useState(false);
  const [allBuildingsStatus, setAllBuildingsStatus] = useState('idle');
  const [allBuildingsProgress, setAllBuildingsProgress] = useState(null);
  const [allBuildingsCount, setAllBuildingsCount] = useState(0);
  const [allBuildingsError, setAllBuildingsError] = useState('');
  const [areaMode, setAreaMode] = useState('idle');
  const [selectedAreaGeometry, setSelectedAreaGeometry] = useState(null);
  const [areaResults, setAreaResults] = useState([]);
  const [areaError, setAreaError] = useState('');
  const [areaProgress, setAreaProgress] = useState(null);
  const [areaCardsExporting, setAreaCardsExporting] = useState(false);
  const [areaCardsExportError, setAreaCardsExportError] = useState('');
  const [detailsModalOpen, setDetailsModalOpen] = useState(false);
  const [detailsBuilding, setDetailsBuilding] = useState(null);
  const [detailsError, setDetailsError] = useState('');
  const [detailsActionMessage, setDetailsActionMessage] = useState('');
  const [detailsCardExporting, setDetailsCardExporting] = useState(false);
  const [openDetailsCategories, setOpenDetailsCategories] = useState(() => new Set());
  useDocumentTitle('מפת GIS - מוקד המבנים העירוני');

  const detailsOrderedFieldGroups = useMemo(
    () => orderFieldGroups(groupBuildingFields(detailsBuilding?.fields || [])),
    [detailsBuilding]
  );

  const detailsDefaultOpenCategories = useMemo(
    () => getDefaultOpenCategories(detailsOrderedFieldGroups),
    [detailsOrderedFieldGroups]
  );

  useEffect(() => {
    if (!detailsBuilding) {
      setOpenDetailsCategories(new Set());
      return;
    }
    setOpenDetailsCategories(new Set(detailsDefaultOpenCategories));
  }, [detailsBuilding, detailsDefaultOpenCategories]);

  const loadGisCandidates = async () => {
    if (gisCandidatesRef.current) return gisCandidatesRef.current;

    if (!gisCandidatesPromiseRef.current) {
      gisCandidatesPromiseRef.current = api.fetchBuildingGisCandidates().then((candidates) => {
        gisCandidatesRef.current = candidates;
        return candidates;
      });
    }

    return gisCandidatesPromiseRef.current;
  };

  const resolveCandidateLocation = async (candidate) => {
    const cached = locationCacheRef.current.get(candidate.id);
    if (cached) return cached;

    let location;
    try {
      location = await resolveBuildingLocation(candidate.gisLocation);
    } catch {
      location = { type: 'not-found' };
    }

    const resolved = {
      location,
      geometry: createGeometryFromLocationResult(location)
    };
    locationCacheRef.current.set(candidate.id, resolved);
    return resolved;
  };

  useEffect(() => {
    if (!mapContainerRef.current) return undefined;

    const allBuildingsLayer = new GraphicsLayer({ title: 'All GhostHouses buildings' });
    const highlightLayer = new GraphicsLayer({ title: 'Selected building' });
    const areaSelectionLayer = new GraphicsLayer({ title: 'Selected area' });
    const areaResultsLayer = new GraphicsLayer({ title: 'Buildings inside selected area' });

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
        }),
        allBuildingsLayer,
        areaSelectionLayer,
        areaResultsLayer,
        highlightLayer
      ]
    });

    const view = new MapView({
      container: mapContainerRef.current,
      map,
      center: [HAIFA_CENTER.longitude, HAIFA_CENTER.latitude],
      zoom: 13
    });

    viewRef.current = view;
    allBuildingsLayerRef.current = allBuildingsLayer;
    highlightLayerRef.current = highlightLayer;
    areaSelectionLayerRef.current = areaSelectionLayer;
    areaResultsLayerRef.current = areaResultsLayer;

    const sketch = new SketchViewModel({
      view,
      layer: areaSelectionLayer,
      polygonSymbol: selectedAreaSymbol,
      updateOnGraphicClick: false
    });
    sketchRef.current = sketch;

    const sketchCreateHandle = sketch.on('create', (event) => {
      if (event.state === 'start') {
        areaDrawingRef.current = true;
        setAreaMode('drawing');
        setAreaError('');
        setAreaResults([]);
        setAreaProgress(null);
        areaSelectionLayer.removeAll();
        areaResultsLayer.removeAll();
      }

      if (event.state === 'cancel') {
        areaDrawingRef.current = false;
        setAreaMode('idle');
      }

      if (event.state === 'complete') {
        areaDrawingRef.current = false;
        const graphic = event.graphic;
        graphic.symbol = selectedAreaSymbol;
        areaSelectionLayer.removeAll();
        areaSelectionLayer.add(graphic);
        setSelectedAreaGeometry(graphic.geometry);
      }
    });

    const mapClickHandle = view.on('click', async (event) => {
      if (areaDrawingRef.current) return;

      const hit = await view.hitTest(event, {
        include: [allBuildingsLayer, areaResultsLayer]
      });
      const clickedGraphic = hit.results.find((result) => result.graphic?.attributes?.buildingId)?.graphic;
      const clickedBuildingId = clickedGraphic?.attributes?.buildingId;

      if (clickedBuildingId) {
        navigate(`/map?buildingId=${clickedBuildingId}`);
      }
    });

    setMapReady(true);
    view
      .when()
      .catch(() => setMapError('לא הצלחנו לטעון את מפת ה-GIS של עיריית חיפה.'));

    return () => {
      sketchCreateHandle.remove();
      mapClickHandle.remove();
      sketch.destroy();
      viewRef.current = null;
      allBuildingsLayerRef.current = null;
      highlightLayerRef.current = null;
      areaSelectionLayerRef.current = null;
      areaResultsLayerRef.current = null;
      sketchRef.current = null;
      view.destroy();
    };
  }, [navigate]);

  useEffect(() => {
    let cancelled = false;

    const loadAllBuildingsLayer = async () => {
      if (!mapReady || !allBuildingsLayerRef.current) return;

      setAllBuildingsStatus('loading');
      setAllBuildingsError('');
      setAllBuildingsProgress({ resolved: 0, total: 0 });
      allBuildingsLayerRef.current.removeAll();

      try {
        const candidates = await loadGisCandidates();
        const graphics = [];
        setAllBuildingsProgress({ resolved: 0, total: candidates.length });

        for (let index = 0; index < candidates.length; index += 1) {
          if (cancelled) return;

          const candidate = candidates[index];
          const resolved = await resolveCandidateLocation(candidate);

          if (resolved.geometry) {
            graphics.push(new Graphic({
              geometry: resolved.geometry,
              symbol: resolved.location.type === 'point' ? allBuildingsPointSymbol : allBuildingsPolygonSymbol,
              attributes: { buildingId: candidate.id }
            }));
          }

          if (index % 5 === 0 || index + 1 === candidates.length) {
            setAllBuildingsProgress({ resolved: index + 1, total: candidates.length });
          }
        }

        if (cancelled) return;

        allBuildingsLayerRef.current?.addMany(graphics);
        setAllBuildingsCount(graphics.length);
        setAllBuildingsStatus('complete');
      } catch (err) {
        if (!cancelled) {
          setAllBuildingsStatus('error');
          setAllBuildingsError(err.message || 'לא הצלחנו להציג את המבנים על המפה.');
        }
      }
    };

    loadAllBuildingsLayer();
    return () => {
      cancelled = true;
    };
  }, [mapReady]);

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

      highlightLayerRef.current?.removeAll();

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

      const geometry = createGeometryFromLocationResult(result);
      if (!geometry) {
        setLocating(false);
        return;
      }

      const graphic = new Graphic({
        geometry,
        symbol: result.type === 'point' ? buildingPointSymbol : buildingPolygonSymbol
      });
      highlightLayerRef.current?.add(graphic);

      try {
        await view.goTo(
          result.type === 'point'
            ? { target: geometry, zoom: 18 }
            : { target: getGeometryTarget(geometry), zoom: 17 },
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

  useEffect(() => {
    let cancelled = false;

    const runAreaSearch = async () => {
      if (!selectedAreaGeometry) return;

      setAreaMode('resolving');
      setAreaError('');
      setAreaCardsExportError('');
      setAreaResults([]);
      setAreaProgress({ resolved: 0, total: 0 });
      areaResultsLayerRef.current?.removeAll();

      try {
        const candidates = await loadGisCandidates();
        const results = [];
        setAreaProgress({ resolved: 0, total: candidates.length });

        for (let index = 0; index < candidates.length; index += 1) {
          if (cancelled) return;

          const candidate = candidates[index];
          const resolved = await resolveCandidateLocation(candidate);

          if (await geometryTouchesArea(selectedAreaGeometry, resolved.geometry)) {
            results.push({
              ...candidate,
              location: resolved.location,
              geometry: resolved.geometry
            });
          }

          if (index % 5 === 0 || index + 1 === candidates.length) {
            setAreaProgress({ resolved: index + 1, total: candidates.length });
          }
        }

        if (cancelled) return;

        const graphics = results.map((result) => new Graphic({
          geometry: result.geometry,
          symbol: result.location.type === 'point' ? areaResultPointSymbol : areaResultPolygonSymbol,
          attributes: { buildingId: result.id }
        }));

        areaResultsLayerRef.current?.addMany(graphics);
        setAreaResults(results);
        setAreaMode('complete');
      } catch (err) {
        if (!cancelled) {
          setAreaError(err.message || 'לא הצלחנו לבצע חיפוש באזור שנבחר.');
          setAreaMode('error');
        }
      }
    };

    runAreaSearch();
    return () => {
      cancelled = true;
    };
  }, [selectedAreaGeometry]);

  const startAreaSelection = (shape) => {
    const sketch = sketchRef.current;
    if (!sketch || !mapReady) return;

    setAreaMode('drawing');
    areaDrawingRef.current = true;
    setAreaError('');
    setAreaCardsExportError('');
    setAreaResults([]);
    setAreaProgress(null);
    setSelectedAreaGeometry(null);
    areaSelectionLayerRef.current?.removeAll();
    areaResultsLayerRef.current?.removeAll();

    sketch.cancel();
    sketch.create(shape);
  };

  const clearAreaSelection = () => {
    sketchRef.current?.cancel();
    areaDrawingRef.current = false;
    areaSelectionLayerRef.current?.removeAll();
    areaResultsLayerRef.current?.removeAll();
    setSelectedAreaGeometry(null);
    setAreaResults([]);
    setAreaError('');
    setAreaCardsExportError('');
    setAreaProgress(null);
    setAreaMode('idle');
  };

  const focusAreaResult = async (result) => {
    const view = viewRef.current;
    if (!view || !result.geometry) return;

    highlightLayerRef.current?.removeAll();
    highlightLayerRef.current?.add(new Graphic({
      geometry: result.geometry,
      symbol: result.location.type === 'point' ? buildingPointSymbol : buildingPolygonSymbol,
      attributes: { buildingId: result.id }
    }));

    try {
      await view.goTo(
        result.location.type === 'point'
          ? { target: result.geometry, zoom: 18 }
          : { target: getGeometryTarget(result.geometry), zoom: 17 },
        { duration: 650 }
      );
    } catch {
      // Ignore navigation interruptions caused by user interaction.
    }
  };

  const openDetailsModal = async (id) => {
    if (!id) return;

    setDetailsModalOpen(true);
    setDetailsBuilding(null);
    setDetailsError('');
    setDetailsActionMessage('');

    try {
      const data = await api.fetchBuilding(id);
      setDetailsBuilding(data);
    } catch (err) {
      setDetailsError(err.message || 'לא הצלחנו לטעון את פרטי המבנה.');
    }
  };

  const closeDetailsModal = () => {
    setDetailsModalOpen(false);
    setDetailsBuilding(null);
    setDetailsError('');
    setDetailsActionMessage('');
  };

  const toggleDetailsCategory = (category) => {
    setOpenDetailsCategories((prev) => {
      const next = new Set(prev);
      if (next.has(category)) {
        next.delete(category);
      } else {
        next.add(category);
      }
      return next;
    });
  };

  const handleCategoryToggleKeyDown = (event, toggle) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      toggle();
    }
  };

  const handleDetailsExportCard = async (selectedBuilding) => {
    if (!selectedBuilding || detailsCardExporting) return;

    setDetailsActionMessage('');
    setDetailsCardExporting(true);
    try {
      const blob = await api.exportBuildingCard(selectedBuilding.id);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `building-card-${selectedBuilding.id}.pptx`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      setDetailsActionMessage(err.message || 'שגיאה בייצוא כרטיס מבנה.');
    } finally {
      setDetailsCardExporting(false);
    }
  };

  const handleAreaExportCards = async () => {
    if (areaCardsExporting || areaResults.length === 0) return;

    const ids = areaResults.map((result) => result.id).filter((id) => id || id === 0);
    if (ids.length === 0) {
      setAreaCardsExportError('לא נמצאו מזהי מבנים לייצוא.');
      return;
    }

    setAreaCardsExportError('');
    setAreaCardsExporting(true);
    try {
      const blob = await api.exportBuildingCardsByIds(ids);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      const date = new Date().toISOString().slice(0, 10);
      link.href = url;
      link.download = `building-cards-map-area-${date}.pptx`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      setAreaCardsExportError(err.message || 'שגיאה בייצוא כרטיסי המבנים.');
    } finally {
      setAreaCardsExporting(false);
    }
  };

  const statusLabel = STATUS_LABEL_MAP[building?.status] || building?.status || '—';
  const hasAreaSelection = areaMode !== 'idle';

  return (
    <main className="app gis-app">
      <header className="page-header">
        <div>
          <h1>מפת GIS</h1>
          <p className="subtitle">מפת עיריית חיפה עם חיבור למאגר המבנים במערכת.</p>
        </div>
      </header>

      <section className="gis-layout">
        <aside className="gis-side-panel">
          <section className="gis-panel-section">
            <h2>בחירת אזור</h2>
            <p className="muted">כל המבנים שאותרו במאגר מוצגים על המפה. סמנו אזור כדי לסנן את המבנים שבתוכו.</p>
            {allBuildingsStatus === 'loading' && (
              <p className="muted">
                מציג מבנים על המפה
                {allBuildingsProgress?.total ? ` (${allBuildingsProgress.resolved}/${allBuildingsProgress.total})` : '...'}
              </p>
            )}
            {allBuildingsStatus === 'complete' && (
              <p className="success">מוצגים {allBuildingsCount} מבנים על המפה.</p>
            )}
            {allBuildingsError && <p className="error">{allBuildingsError}</p>}
            <div className="gis-action-row">
              <button
                type="button"
                className="primary"
                disabled={!mapReady || areaMode === 'drawing' || areaMode === 'resolving'}
                onClick={() => startAreaSelection('rectangle')}
              >
                מלבן
              </button>
              <button
                type="button"
                className="ghost"
                disabled={!mapReady || areaMode === 'drawing' || areaMode === 'resolving'}
                onClick={() => startAreaSelection('polygon')}
              >
                פוליגון
              </button>
              {hasAreaSelection && (
                <button type="button" className="ghost" onClick={clearAreaSelection}>
                  ניקוי
                </button>
              )}
            </div>
            {areaMode === 'drawing' && <p className="muted">שרטטו את האזור על המפה.</p>}
            {areaMode === 'resolving' && (
              <p className="muted">
                מאתר מבנים באזור
                {areaProgress?.total ? ` (${areaProgress.resolved}/${areaProgress.total})` : '...'}
              </p>
            )}
            {areaError && <p className="error">{areaError}</p>}
            {areaMode === 'complete' && areaResults.length === 0 && (
              <p className="muted">לא נמצאו מבנים מוכרים באזור שנבחר.</p>
            )}
            {areaResults.length > 0 && (
              <div className="gis-results-list">
                <div className="gis-results-header">
                  <h3>נמצאו {areaResults.length} מבנים</h3>
                  <button
                    type="button"
                    className="ghost"
                    onClick={handleAreaExportCards}
                    disabled={areaCardsExporting}
                  >
                    {areaCardsExporting ? 'מייצא...' : 'ייצוא כרטיסי מבנים'}
                  </button>
                </div>
                {areaCardsExportError && <p className="error">{areaCardsExportError}</p>}
                {areaResults.map((result) => (
                  <article key={result.id} className="gis-result-item">
                    <div>
                      <strong>{formatValue(result.nickname)}</strong>
                      <span>
                        {formatValue(result.street)} {formatValue(result.houseNumber)}
                      </span>
                      <small>{STATUS_LABEL_MAP[result.status] || result.status || '—'}</small>
                    </div>
                    <div className="gis-result-actions">
                      <button type="button" className="ghost" onClick={() => focusAreaResult(result)}>
                        מיקוד
                      </button>
                      <button type="button" className="ghost" onClick={() => openDetailsModal(result.id)}>
                        פרטים
                      </button>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </section>

          <section className="gis-panel-section">
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
                <button
                  type="button"
                  className="ghost gis-details-button"
                  onClick={() => openDetailsModal(building.id)}
                >
                  פרטי מבנה
                </button>
                {locationResult?.type === 'not-found' && <p className="error">{locationResult.message}</p>}
              </>
            )}
          </section>
        </aside>

        <div className="gis-map-shell">
          {!mapReady && !mapError && <div className="gis-map-status">טוען מפה...</div>}
          {mapError && <div className="gis-map-status error">{mapError}</div>}
          <div ref={mapContainerRef} className="gis-map" aria-label="מפת GIS עיריית חיפה" />
        </div>
      </section>

      <BuildingModal
        visible={detailsModalOpen}
        mode="view"
        building={detailsBuilding}
        createFieldValues={{}}
        createPhotoValue=""
        createFieldGroups={[]}
        createTemplateLoading={false}
        createSelectTablesLoading={false}
        editFieldValues={{}}
        editPhotoValue={detailsBuilding?.photos?.[0] ?? ''}
        streets={[]}
        selectTablesByName={{}}
        selectTablesLoading={false}
        orderedFieldGroups={detailsOrderedFieldGroups}
        isRehabStatusRequired={false}
        isEditRehabStatusRequired={false}
        isRequiredCreateColumn={() => false}
        canEdit={false}
        actionMessage={detailsActionMessage}
        duplicatePrompt=""
        editDuplicatePrompt=""
        onCreateFieldChange={() => {}}
        onCreateSubmit={() => {}}
        onDuplicateConfirm={() => {}}
        onDuplicateCancel={() => {}}
        onEditChange={() => {}}
        onEditSubmit={() => {}}
        onEditDuplicateConfirm={() => {}}
        onEditDuplicateCancel={() => {}}
        onOpenEdit={() => {}}
        onOpenLogs={() => {
          if (detailsBuilding?.id) navigate(`/logs?buildingId=${detailsBuilding.id}`);
        }}
        onDelete={() => {}}
        onExportCard={handleDetailsExportCard}
        onClose={closeDetailsModal}
        onPhotoUpload={() => {}}
        onPhotoDelete={() => {}}
        photoLoading={false}
        photoError=""
        detailError={detailsError}
        loadStreets={() => {}}
        sortFieldsForDisplay={sortFieldsForDisplay}
        getExcelAwareLabel={getExcelAwareLabel}
        isDateField={isDateField}
        shouldUseTextarea={shouldUseTextarea}
        isRequiredEditColumn={() => false}
        displayOrDash={formatValue}
        formatStatusFieldValue={formatStatusFieldValue}
        formatLogDate={formatLogDate}
        openViewCategories={openDetailsCategories}
        toggleViewCategory={toggleDetailsCategory}
        openEditCategories={new Set()}
        toggleEditCategory={() => {}}
        openCreateCategories={new Set()}
        toggleCreateCategory={() => {}}
        handleCategoryToggleKeyDown={handleCategoryToggleKeyDown}
      />
    </main>
  );
}
