let blazorComponentRef = null;
function registerBlazorComponent(dotNetRef) {
    blazorComponentRef = dotNetRef;
}

window.getPixelFromCoordinate = function (x, y) {
    return window._myOLMap.getPixelFromCoordinate([x, y]);
}

window.myMapConfig = function (olMap) {
    window._myOLMap = olMap;
    window._edgeLayer = null;
    blazorComponentRef.invokeMethodAsync('MapInitialized');
}

window.clearMap = function () {
    if (window._edgeLayer) {
        window._edgeLayer.getSource().clear(true);
    }
    if (window._polyLayer) {
        window._polyLayer.getSource().clear(true);
    }
}

window.drawPolygons = function (json) {
    return new Promise((resolve) => {
        const map = window._myOLMap;
        if (!map) {
            resolve();
            return;
        }
        if (!window._polyLayer) {
            const source = new ol.source.Vector();
            const layer = new ol.layer.Vector({
                source: source,
                style: (f) => getStyle({ fill: f.get('fill'), stroke: f.get('stroke') })
            });
            window._polyLayer = layer
            map.addLayer(window._polyLayer);
        }

        const features = new ol.format.GeoJSON().readFeatures(json, {
            dataProjection: 'EPSG:4326',      // whatever the gpkg SRID is
            featureProjection: 'EPSG:3857',   // map view projection
        });
        window._polyLayer.getSource().addFeatures(features);

        resolve();
    });
}

const cache = {};
function getStyle(s) {
    const key = (s.fill || '') + '|' + (s.stroke || '') + '|' + (s.width || 1);
    return cache[key] ??= new ol.style.Style({
        fill: new ol.style.Fill({ color: s.fill || 'rgba(255,100,0,0.4)' }),
        stroke: new ol.style.Stroke({ color: s.stroke || '#222', width: s.width || 1 }),
    });
}

window.setEdges = function (flatCoords, colors) {
    return new Promise((resolve) => {
        const map = window._myOLMap;
        if (!map) { resolve(); return; }

        if (window._edgeLayer) {
            window._edgeLayer.getSource().clear(true);
        } else {
            window._edgeLayer = new ol.layer.WebGLVector({
                source: new ol.source.Vector({ useSpatialIndex: false }),
                disableHitDetection: false,
                style: { 'stroke-color': ['get', 'color'], 'stroke-width': 2 }
            });
            map.addLayer(window._edgeLayer);
        }

        const DEG_TO_RAD = Math.PI / 180;
        const R = 6378137;
        const R_RAD = R * DEG_TO_RAD;
        // Precompute log constant
        const HALF_R = R / 2;

        const totalEdges = colors.length;

        // --- 1. Group indices by color using a plain object (faster than Map for string keys) ---
        const colorGroups = Object.create(null);
        for (let i = 0; i < totalEdges; i++) {
            const color = colors[i] || '#ffffff';
            if (colorGroups[color] === undefined) colorGroups[color] = [];
            colorGroups[color].push(i);
        }

        // --- 2. Build features: one MultiLineString per color, coords projected inline ---
        const features = [];

        for (const color in colorGroups) {
            const indices = colorGroups[color];
            const n = indices.length;
            // Pre-allocate: each line = [[x0,y0],[x1,y1]]
            const lines = new Array(n);

            for (let j = 0; j < n; j++) {
                const o = indices[j] * 4;
                const lon0 = flatCoords[o] * R_RAD;
                const lat0 = flatCoords[o + 1] * DEG_TO_RAD;
                const lon1 = flatCoords[o + 2] * R_RAD;
                const lat1 = flatCoords[o + 3] * DEG_TO_RAD;

                // Mercator Y — avoid repeated sin+log by inlining
                const sin0 = Math.sin(lat0);
                const sin1 = Math.sin(lat1);
                lines[j] = [
                    [lon0, HALF_R * Math.log((1 + sin0) / (1 - sin0))],
                    [lon1, HALF_R * Math.log((1 + sin1) / (1 - sin1))]
                ];
            }

            features.push(new ol.Feature({
                geometry: new ol.geom.MultiLineString(lines),
                color
            }));
        }

        window._edgeLayer.getSource().addFeatures(features);
        resolve();
    });
};