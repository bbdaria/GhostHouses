import { HAIFA_BOUNDS, HAIFA_GIS_LAYERS } from './gisConfig.js';

const hasNumber = (value) => Number.isFinite(Number(value));

const asNumber = (value) => {
  if (!hasNumber(value)) return null;
  return Number(value);
};

const isLikelyWgs84 = (longitude, latitude) =>
  hasNumber(longitude) &&
  hasNumber(latitude) &&
  Number(longitude) >= -180 &&
  Number(longitude) <= 180 &&
  Number(latitude) >= -90 &&
  Number(latitude) <= 90;

const isInsideHaifaBounds = (longitude, latitude) =>
  isLikelyWgs84(longitude, latitude) &&
  Number(longitude) >= HAIFA_BOUNDS.minLongitude &&
  Number(longitude) <= HAIFA_BOUNDS.maxLongitude &&
  Number(latitude) >= HAIFA_BOUNDS.minLatitude &&
  Number(latitude) <= HAIFA_BOUNDS.maxLatitude;

const escapeSqlString = (value) => String(value || '').replace(/'/g, "''").trim();

const parseHouseNumber = (value) => {
  const normalized = String(value || '').trim();
  const match = normalized.match(/^(\d+)\s*([א-תA-Za-z]?)$/);
  if (!match) return null;

  return {
    number: Number(match[1]),
    letter: match[2] || ''
  };
};

const queryLayer = async (url, params) => {
  const query = new URLSearchParams({
    f: 'json',
    returnGeometry: 'true',
    outFields: '*',
    outSR: '4326',
    resultRecordCount: '1',
    ...params
  });

  const response = await fetch(`${url}/query?${query}`);
  if (!response.ok) {
    throw new Error(`GIS query failed (${response.status})`);
  }

  const payload = await response.json();
  if (payload.error) {
    throw new Error(payload.error.message || 'GIS query failed');
  }

  const feature = payload.features?.[0] || null;
  if (feature?.geometry && payload.spatialReference && !feature.geometry.spatialReference) {
    feature.geometry = {
      ...feature.geometry,
      spatialReference: payload.spatialReference
    };
  }

  return feature;
};

const getFeaturePoint = (feature, label, source) => {
  if (!feature?.geometry || !hasNumber(feature.geometry.x) || !hasNumber(feature.geometry.y)) {
    return null;
  }

  return {
    type: 'point',
    source,
    label,
    longitude: Number(feature.geometry.x),
    latitude: Number(feature.geometry.y),
    attributes: feature.attributes || {}
  };
};

const queryRegulatedParcel = async ({ gushM, parcelM }) => {
  if (!hasNumber(gushM) || !hasNumber(parcelM)) return null;

  const feature = await queryLayer(HAIFA_GIS_LAYERS.regulatedParcel, {
    where: `Gush_Num = ${Math.trunc(Number(gushM))} AND Parcel = ${Math.trunc(Number(parcelM))}`
  });

  if (!feature?.geometry) return null;
  return {
    type: 'polygon',
    source: 'regulated-parcel',
    label: 'חלקה מוסדרת',
    geometry: feature.geometry,
    attributes: feature.attributes || {}
  };
};

const queryTaxParcel = async ({ gushS, parcelS }) => {
  if (!hasNumber(gushS) || !hasNumber(parcelS)) return null;

  const feature = await queryLayer(HAIFA_GIS_LAYERS.taxParcel, {
    where: `GUSH_NO = ${Math.trunc(Number(gushS))} AND PARCEL = ${Math.trunc(Number(parcelS))}`
  });

  if (!feature?.geometry) return null;
  return {
    type: 'polygon',
    source: 'tax-parcel',
    label: 'חלקת שומה',
    geometry: feature.geometry,
    attributes: feature.attributes || {}
  };
};

const queryAddressPoint = async ({ streetName, houseNumber }) => {
  const street = escapeSqlString(streetName);
  const parsedHouseNumber = parseHouseNumber(houseNumber);
  if (!street || !parsedHouseNumber) return null;

  const letterFilter = parsedHouseNumber.letter
    ? ` AND BLDG_LETTE = '${escapeSqlString(parsedHouseNumber.letter)}'`
    : '';

  const feature = await queryLayer(HAIFA_GIS_LAYERS.addresses, {
    where: `STREET_NAM = '${street}' AND BLDG_NUM = ${parsedHouseNumber.number}${letterFilter}`
  });

  return getFeaturePoint(feature, 'כתובת עירונית', 'municipality-address');
};

const queryPreservationBuilding = async ({ streetName, houseNumber }) => {
  const street = escapeSqlString(streetName);
  const number = escapeSqlString(houseNumber);
  if (!street || !number) return null;

  const feature = await queryLayer(HAIFA_GIS_LAYERS.preservationBuildings, {
    where: `street = '${street}' AND bldg_num = '${number}'`
  });

  if (!feature?.geometry) return null;
  return {
    type: 'polygon',
    source: 'preservation-building',
    label: 'מבנה לשימור לפי כתובת',
    geometry: feature.geometry,
    attributes: feature.attributes || {}
  };
};

export async function resolveBuildingLocation(gisLocation) {
  if (!gisLocation) {
    return {
      type: 'not-found',
      message: 'לא נמצאו נתוני GIS עבור המבנה.'
    };
  }

  const resolvers = [queryRegulatedParcel, queryTaxParcel, queryAddressPoint, queryPreservationBuilding];
  for (const resolver of resolvers) {
    try {
      const result = await resolver(gisLocation);
      if (result) return result;
    } catch {
      // Continue through weaker fallbacks and let the UI show a single clear result.
    }
  }

  const longitude = asNumber(gisLocation.longitude);
  const latitude = asNumber(gisLocation.latitude);
  if (isInsideHaifaBounds(longitude, latitude)) {
    return {
      type: 'point',
      source: 'coordinates',
      label: 'קואורדינטות המבנה',
      longitude,
      latitude
    };
  }

  return {
    type: 'not-found',
    message: 'לא הצלחנו לאתר את המבנה במפת ה-GIS לפי הנתונים הקיימים.'
  };
}
