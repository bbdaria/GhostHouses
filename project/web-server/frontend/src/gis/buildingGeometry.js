import * as geometryEngine from '@arcgis/core/geometry/geometryEngine.js';
import Point from '@arcgis/core/geometry/Point.js';
import Polygon from '@arcgis/core/geometry/Polygon.js';
import * as projectOperator from '@arcgis/core/geometry/operators/projectOperator.js';
import * as webMercatorUtils from '@arcgis/core/geometry/support/webMercatorUtils.js';

const WEB_MERCATOR_WKIDS = new Set([3857, 102100, 102113]);

const isWgs84 = (spatialReference) =>
  spatialReference?.wkid === 4326 || spatialReference?.latestWkid === 4326;

const isWebMercator = (spatialReference) =>
  Boolean(spatialReference?.isWebMercator) ||
  WEB_MERCATOR_WKIDS.has(spatialReference?.wkid) ||
  WEB_MERCATOR_WKIDS.has(spatialReference?.latestWkid);

const sameSpatialReference = (first, second) => {
  const firstWkid = first?.latestWkid || first?.wkid;
  const secondWkid = second?.latestWkid || second?.wkid;
  return Boolean(firstWkid && secondWkid && firstWkid === secondWkid);
};

export const createGeometryFromLocationResult = (locationResult) => {
  if (!locationResult || locationResult.type === 'not-found') return null;

  if (locationResult.type === 'point') {
    return new Point({
      longitude: locationResult.longitude,
      latitude: locationResult.latitude,
      spatialReference: { wkid: 4326 }
    });
  }

  if (locationResult.type === 'polygon') {
    return new Polygon({
      ...locationResult.geometry,
      spatialReference: locationResult.geometry?.spatialReference || { wkid: 4326 }
    });
  }

  return null;
};

export const getGeometryTarget = (geometry) => geometry?.extent?.expand(1.6) || geometry;

const alignGeometryToArea = async (areaGeometry, buildingGeometry) => {
  if (sameSpatialReference(areaGeometry.spatialReference, buildingGeometry.spatialReference)) {
    return buildingGeometry;
  }

  if (isWebMercator(areaGeometry.spatialReference) && isWgs84(buildingGeometry.spatialReference)) {
    return webMercatorUtils.geographicToWebMercator(buildingGeometry);
  }

  if (isWgs84(areaGeometry.spatialReference) && isWebMercator(buildingGeometry.spatialReference)) {
    return webMercatorUtils.webMercatorToGeographic(buildingGeometry);
  }

  if (areaGeometry.spatialReference && buildingGeometry.spatialReference) {
    try {
      await projectOperator.load();
      return projectOperator.execute(buildingGeometry, areaGeometry.spatialReference) || buildingGeometry;
    } catch {
      return buildingGeometry;
    }
  }

  return buildingGeometry;
};

const safely = (check) => {
  try {
    return Boolean(check());
  } catch {
    return false;
  }
};

export const geometryTouchesArea = async (areaGeometry, buildingGeometry) => {
  if (!areaGeometry || !buildingGeometry) return false;

  const comparableBuildingGeometry = await alignGeometryToArea(areaGeometry, buildingGeometry);
  if (!comparableBuildingGeometry) return false;

  return (
    safely(() => geometryEngine.intersects(areaGeometry, comparableBuildingGeometry)) ||
    safely(() => geometryEngine.contains(areaGeometry, comparableBuildingGeometry)) ||
    safely(() => geometryEngine.within(comparableBuildingGeometry, areaGeometry)) ||
    safely(() => geometryEngine.overlaps(areaGeometry, comparableBuildingGeometry)) ||
    safely(() => geometryEngine.distance(areaGeometry, comparableBuildingGeometry, 'meters') <= 0.5) ||
    safely(() => areaGeometry.extent?.contains(comparableBuildingGeometry))
  );
};
