let blazorComponentRef = null;
function registerBlazorComponent(dotNetRef) {
    blazorComponentRef = dotNetRef;
}

window.myMapConfig = function (olMap) {
    window._myOLMap = olMap;
    window._edgeLayer = null;
    blazorComponentRef.invokeMethodAsync('MapInitialized');
}

window.setEdges = function (flatCoords, colors) {
    const map = window._myOLMap;
    if (!map) { console.error("Map not ready"); return; }

    // 1. CLEAR PREVIOUS EDGES IMMEDIATELY
    if (window._edgeLayer) {
        window._edgeLayer.getSource().clear(true);
    }

    // 2. TELL BLAZOR TO SHOW THE LOADING SPINNER
    if (blazorComponentRef) {
        blazorComponentRef.invokeMethodAsync('SetLoadingState', true);
    }

    // 3. Defer the heavy processing so the UI thread can paint the spinner
    requestAnimationFrame(() => {
        setTimeout(() => {
            const colorGroups = {};
            const totalEdges = colors.length;

            for (let i = 0; i < totalEdges; i++) {
                const coordOffset = i * 4;
                const color = colors[i] || '#ffffff';

                const startLon = flatCoords[coordOffset];
                const startLat = flatCoords[coordOffset + 1];
                const endLon = flatCoords[coordOffset + 2];
                const endLat = flatCoords[coordOffset + 3];

                if (startLon === undefined || endLon === undefined) continue;

                const transformedLine = [
                    ol.proj.fromLonLat([startLon, startLat]),
                    ol.proj.fromLonLat([endLon, endLat])
                ];

                if (!colorGroups[color]) {
                    colorGroups[color] = [];
                }
                colorGroups[color].push(transformedLine);
            }

            const features = Object.keys(colorGroups).map(color => {
                return new ol.Feature({
                    geometry: new ol.geom.MultiLineString(colorGroups[color]),
                    color: color
                });
            });

            if (!window._edgeLayer) {
                window._edgeLayer = new ol.layer.WebGLVector({
                    source: new ol.source.Vector({ useSpatialIndex: false }),
                    disableHitDetection: true,
                    style: {
                        'stroke-color': ['get', 'color'],
                        'stroke-width': 2
                    }
                });
                map.addLayer(window._edgeLayer);
            }

            const source = window._edgeLayer.getSource();
            source.addFeatures(features);

            if (blazorComponentRef) {
                blazorComponentRef.invokeMethodAsync('SetLoadingState', false);
            }
        }, 0);
    });
};