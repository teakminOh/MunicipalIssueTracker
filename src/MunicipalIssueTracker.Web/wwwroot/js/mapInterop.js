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

            var self = this;
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
                marker.addTo(self._markersLayer);
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
    },

    // --- Form Map Picker ---
    _formMap: null,
    _formMarker: null,
    _dotnetRef: null,

    initFormMap: function (elementId, lat, lng, dotnetRef) {
        try {
            if (this._formMap) {
                this._formMap.off();
                this._formMap.remove();
                this._formMap = null;
            }
            this._dotnetRef = dotnetRef;
            var container = document.getElementById(elementId);
            if (!container) return;
            if (container._leaflet_id) {
                container._leaflet_id = null;
                container.innerHTML = '';
            }
            this._formMap = L.map(elementId).setView([lat, lng], 15);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
                maxZoom: 19
            }).addTo(this._formMap);
            this._formMarker = L.marker([lat, lng], { draggable: true }).addTo(this._formMap);

            var self = this;
            this._formMap.on('click', function (e) {
                self._formMarker.setLatLng(e.latlng);
                if (self._dotnetRef) {
                    self._dotnetRef.invokeMethodAsync('OnMapClicked', e.latlng.lat, e.latlng.lng);
                }
            });
            this._formMarker.on('dragend', function (e) {
                var pos = e.target.getLatLng();
                if (self._dotnetRef) {
                    self._dotnetRef.invokeMethodAsync('OnMapClicked', pos.lat, pos.lng);
                }
            });
        } catch (e) {
            console.warn('mapInterop.initFormMap error:', e);
        }
    },

    updateFormMarker: function (lat, lng) {
        try {
            if (this._formMarker && this._formMap) {
                this._formMarker.setLatLng([lat, lng]);
                this._formMap.setView([lat, lng], this._formMap.getZoom());
            }
        } catch (e) { }
    },

    reverseGeocode: async function (lat, lng) {
        try {
            var resp = await fetch(
                'https://nominatim.openstreetmap.org/reverse?format=json&lat=' + lat + '&lon=' + lng + '&zoom=18&addressdetails=1',
                { headers: { 'Accept-Language': 'sk', 'User-Agent': 'MunicipalIssueTracker/1.0' } }
            );
            var data = await resp.json();
            return data.display_name || '';
        } catch (e) {
            return '';
        }
    },

    searchAddresses: async function (query) {
        try {
            var resp = await fetch(
                'https://nominatim.openstreetmap.org/search?format=json&q=' + encodeURIComponent(query) +
                '&viewbox=19.35,49.45,19.60,49.35&bounded=1&limit=5&addressdetails=1',
                { headers: { 'Accept-Language': 'sk', 'User-Agent': 'MunicipalIssueTracker/1.0' } }
            );
            var data = await resp.json();
            return data.map(function (item) {
                return { displayName: item.display_name, lat: parseFloat(item.lat), lng: parseFloat(item.lon) };
            });
        } catch (e) {
            return [];
        }
    },

    disposeFormMap: function () {
        try {
            if (this._formMap) { this._formMap.off(); this._formMap.remove(); this._formMap = null; }
            this._formMarker = null;
            this._dotnetRef = null;
        } catch (e) { }
    }
};
