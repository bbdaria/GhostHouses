export const HAIFA_WEB_MAP_ITEM_ID = 'a8d41abb1da4429889e6e58adcb7648b';

export const HAIFA_GIS_MAP_SERVICES = {
  baseMap:
    'https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_BaseMap_2022/MapServer',
  statistics:
    'https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_Stat_Public/MapServer',
  engineering:
    'https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_Eng_Public/MapServer',
  preservation:
    'https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_Shimur_Public/MapServer',
  community:
    'https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_Community_Public/MapServer',
  traffic:
    'https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_Signs_Public/MapServer'
};

export const HAIFA_GIS_LAYERS = {
  addresses:
    'https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_Stat_Public/MapServer/0',
  regulatedParcel:
    'https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_Eng_Public/MapServer/5',
  taxParcel:
    'https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_Eng_Public/MapServer/4',
  preservationBuildings:
    'https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_Shimur_Public/MapServer/0'
};

export const HAIFA_CENTER = {
  longitude: 34.9896,
  latitude: 32.794
};

export const HAIFA_BOUNDS = {
  minLongitude: 34.94,
  maxLongitude: 35.08,
  minLatitude: 32.75,
  maxLatitude: 32.86
};
