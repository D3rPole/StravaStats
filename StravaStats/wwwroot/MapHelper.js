let blazorComponentRef = null;
function registerBlazorComponent(dotNetRef) {
    blazorComponentRef = dotNetRef;
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

        const CHUNK_SIZE = 8000;
        const totalEdges = colors.length;
        const colorGroups = new Map();
        let cursor = 0;

        const DEG_TO_RAD = Math.PI / 180;
        const R = 6378137;

        function lonLatToMerc(lon, lat) {
            const x = lon * DEG_TO_RAD * R;
            const sinLat = Math.sin(lat * DEG_TO_RAD);
            const y = R * Math.log((1 + sinLat) / (1 - sinLat)) / 2;
            return [x, y];
        }

        function processChunk() {
            const end = Math.min(cursor + CHUNK_SIZE, totalEdges);
            for (let i = cursor; i < end; i++) {
                const o = i * 4;
                const startLon = flatCoords[o];
                if (startLon === undefined) continue;
                const color = colors[i] || '#ffffff';
                const line = [
                    lonLatToMerc(startLon, flatCoords[o + 1]),
                    lonLatToMerc(flatCoords[o + 2], flatCoords[o + 3])
                ];
                let group = colorGroups.get(color);
                if (!group) { group = []; colorGroups.set(color, group); }
                group.push(line);
            }
            cursor = end;

            if (cursor < totalEdges) {
                requestAnimationFrame(processChunk);
            } else {
                const features = [];
                for (const [color, lines] of colorGroups) {
                    features.push(new ol.Feature({
                        geometry: new ol.geom.MultiLineString(lines),
                        color
                    }));
                }
                window._edgeLayer.getSource().addFeatures(features);
                resolve();
            }
        }

        requestAnimationFrame(processChunk);
    });
};