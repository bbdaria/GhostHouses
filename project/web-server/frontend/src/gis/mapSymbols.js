import SimpleFillSymbol from '@arcgis/core/symbols/SimpleFillSymbol.js';
import SimpleMarkerSymbol from '@arcgis/core/symbols/SimpleMarkerSymbol.js';

export const allBuildingsPointSymbol = new SimpleMarkerSymbol({
  style: 'circle',
  color: [35, 120, 190, 0.72],
  size: 8,
  outline: {
    color: [255, 255, 255, 0.9],
    width: 1
  }
});

export const allBuildingsPolygonSymbol = new SimpleFillSymbol({
  color: [35, 120, 190, 0.08],
  outline: {
    color: [35, 120, 190, 0.65],
    width: 1
  }
});

export const buildingPointSymbol = new SimpleMarkerSymbol({
  style: 'circle',
  color: [214, 62, 62, 0.9],
  size: 14,
  outline: {
    color: [255, 255, 255, 1],
    width: 2
  }
});

export const buildingPolygonSymbol = new SimpleFillSymbol({
  color: [214, 62, 62, 0.18],
  outline: {
    color: [214, 62, 62, 0.95],
    width: 2
  }
});

export const selectedAreaSymbol = new SimpleFillSymbol({
  color: [35, 120, 190, 0.16],
  outline: {
    color: [35, 120, 190, 0.95],
    width: 2
  }
});

export const areaResultPointSymbol = new SimpleMarkerSymbol({
  style: 'circle',
  color: [18, 183, 106, 0.9],
  size: 10,
  outline: {
    color: [255, 255, 255, 1],
    width: 1.5
  }
});

export const areaResultPolygonSymbol = new SimpleFillSymbol({
  color: [18, 183, 106, 0.16],
  outline: {
    color: [18, 183, 106, 0.95],
    width: 1.5
  }
});
