// Municipal Issue Tracker - Leaflet Map Interop
window.mapInterop = {
    _map: null,
    _markersLayer: null,
    _detailMap: null,

    initMap: function (elementId, lat, lng, zoom) {
        try {
            if (this._map) {
                this._map.off();
                this._map.remove();
                this._map = null;
                this._markersLayer = null;
            }
            var container = document.getElementById(elementId);
            if (!container) return;
            // Clear any previous Leaflet instance on this container
            if (container._leaflet_id) {
                container._leaflet_id = null;
                container.innerHTML = '';
            }
            this._map = L.map(elementId).setView([lat, lng], zoom);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
                maxZoom: 19
            }).addTo(this._map);
            this._markersLayer = L.layerGroup().addTo(this._map);
        } catch (e) {
            console.warn('mapInterop.initMap error:', e);
        }
    },

    setMarkers: function (markers) {
        try {
            if (!this._markersLayer || !this._map) return;
            this._markersLayer.clearLayers();

            if (!markers || !markers.length) return;

            markers.forEach(function (m) {
                var color = m.priority === 'Critical' ? '#dc3545' :
                    m.priority === 'High' ? '#fd7e14' :
                        m.priority === 'Medium' ? '#0d6efd' : '#6c757d';

                var icon = L.divIcon({
                    className: 'custom-marker',
                    html: '<div style="background:' + color + ';width:14px;height:14px;border-radius:50%;border:2px solid white;box-shadow:0 1px 3px rgba(0,0,0,.4);"></div>',
                    iconSize: [14, 14],
                    iconAnchor: [7, 7]
                });

                var marker = L.marker([m.lat, m.lng], { icon: icon });
                marker.bindPopup(
                    '<strong>' + m.title + '</strong><br/>' +
                    '<span>' + m.category + ' — ' + m.status + '</span><br/>' +
                    '<span>Priority: ' + m.priority + '</span><br/>' +
                    '<a href="/issues/' + m.id + '">View Details →</a>'
                );
                marker.addTo(this._markersLayer);
            });

            var group = L.featureGroup(this._markersLayer.getLayers());
            if (group.getLayers().length > 0) {
                this._map.fitBounds(group.getBounds().pad(0.1));
            }
        } catch (e) {
            console.warn('mapInterop.setMarkers error:', e);
        }
    },

    initDetailMap: function (elementId, lat, lng, title) {
        try {
            if (this._detailMap) {
                this._detailMap.off();
                this._detailMap.remove();
                this._detailMap = null;
            }
            var container = document.getElementById(elementId);
            if (!container) return;
            if (container._leaflet_id) {
                container._leaflet_id = null;
                container.innerHTML = '';
            }
            this._detailMap = L.map(elementId).setView([lat, lng], 16);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
                maxZoom: 19
            }).addTo(this._detailMap);
            L.marker([lat, lng]).addTo(this._detailMap).bindPopup(title).openPopup();
        } catch (e) {
            console.warn('mapInterop.initDetailMap error:', e);
        }
    },

    disposeMap: function () {
        try {
            if (this._map) { this._map.off(); this._map.remove(); this._map = null; }
            this._markersLayer = null;
        } catch (e) { }
    },

    disposeDetailMap: function () {
        try {
            if (this._detailMap) { this._detailMap.off(); this._detailMap.remove(); this._detailMap = null; }
        } catch (e) { }
    }
};
