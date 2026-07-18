let blazorComponentRef = null;
function registerBlazorComponent(dotNetRef) {
    blazorComponentRef = dotNetRef;
}

window.getPixelFromCoordinate = function (x, y){
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
}

window.clearMap = function () {
    if (window._edgeLayer) {
        window._edgeLayer.getSource().clear(true);
    }
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
                disableHitDetection: true,
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