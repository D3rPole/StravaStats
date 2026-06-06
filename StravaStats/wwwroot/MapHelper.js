window.myMapConfig = function (olMap) {
    window._myOLMap = olMap;
}

window.setEdges = function (edges) {
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
            style: f => new ol.style.Style({
                stroke: new ol.style.Stroke({ color: f.get('color'), width: 4 })
            })
        });
        map.addLayer(window._edgeLayer);
    }

    window._edgeLayer.getSource().clear();
    window._edgeLayer.getSource().addFeatures(features);
}