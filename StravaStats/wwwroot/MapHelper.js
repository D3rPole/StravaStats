let blazorComponentRef = null;

function registerBlazorComponent(dotNetRef) {
    blazorComponentRef = dotNetRef;
}

window.myMapConfig = function (olMap) {
    window._myOLMap = olMap;
    window._edgeLayer = null;
    blazorComponentRef.invokeMethodAsync('MapInitialized');
}

// Keep a cache of styles outside the function scope
var styleCache = {};

window.setEdges = function (edges) {
    styleCache = {}
    const map = window._myOLMap;
    if (!map) { console.error("Map not ready"); return; }

    const features = edges.map(e => new ol.Feature({
        geometry: new ol.geom.LineString([e.start, e.end])
            .transform('EPSG:4326', 'EPSG:3857'),
        color: e.color
    }));

    if (!window._edgeLayer) {
        window._edgeLayer = new ol.layer.Vector({
            source: new ol.source.Vector(),
            // Optimised style function using the cache
            style: function (feature) {
                const color = feature.get('color') || '#ffffff';

                // If this color hasn't been used yet, create and cache it
                if (!styleCache[color]) {
                    styleCache[color] = new ol.style.Style({
                        stroke: new ol.style.Stroke({
                            color: color,
                            width: 4
                        })
                    });
                }
                return styleCache[color];
            }
        });
        map.addLayer(window._edgeLayer);
    }

    window._edgeLayer.getSource().clear();
    window._edgeLayer.getSource().addFeatures(features);
}