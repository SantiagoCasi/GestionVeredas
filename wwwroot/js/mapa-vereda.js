// Minimapa para ubicar veredas en Colón (Buenos Aires) usando OpenStreetMap.
// Todo es gratuito y sin clave: mapa con Leaflet, búsqueda de direcciones con Nominatim
// y búsqueda de esquinas (entre calles) con Overpass.
//
// En formularios (Create/Edit): toma Calle, Altura, EntreCalle1 y EntreCalle2
// y guarda "latitud,longitud" en el campo oculto #Ubicacion.
// En Detalles: <div class="mapa-solo-lectura" data-ubicacion="lat,lng"> muestra un mapa fijo.
(function () {
    const CENTRO_COLON = [-33.8833, -61.1000];
    const LIMITES = { norte: -33.84, sur: -33.93, este: -61.05, oeste: -61.16 };
    const TILES = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
    const ATRIBUCION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>';

    const $ = id => document.getElementById(id);
    const valor = id => ($(id)?.value || '').trim();

    function crearMapa(div, centro, zoom) {
        const mapa = L.map(div, {
            maxBounds: [[LIMITES.sur - 0.05, LIMITES.oeste - 0.05], [LIMITES.norte + 0.05, LIMITES.este + 0.05]]
        }).setView(centro, zoom);
        L.tileLayer(TILES, { maxZoom: 19, attribution: ATRIBUCION }).addTo(mapa);
        return mapa;
    }

    function parsear(texto) {
        const partes = (texto || '').split(',');
        if (partes.length !== 2) return null;
        const lat = parseFloat(partes[0]), lng = parseFloat(partes[1]);
        return isNaN(lat) || isNaN(lng) ? null : [lat, lng];
    }

    function dentroDeColon(lat, lng) {
        return lat <= LIMITES.norte && lat >= LIMITES.sur && lng <= LIMITES.este && lng >= LIMITES.oeste;
    }

    // ---------- Búsquedas ----------

    // Dirección con altura (ej. "Calle 12 1115").
    async function buscarDireccion(calle, altura) {
        const params = new URLSearchParams({
            format: 'jsonv2',
            street: `${altura} ${calle}`.trim(),
            city: 'Colón',
            state: 'Buenos Aires',
            country: 'Argentina',
            viewbox: `${LIMITES.oeste},${LIMITES.norte},${LIMITES.este},${LIMITES.sur}`,
            bounded: '1',
            limit: '5',
            'accept-language': 'es'
        });
        const r = await fetch(`https://nominatim.openstreetmap.org/search?${params}`);
        if (!r.ok) return null;
        const datos = await r.json();
        const hit = datos.find(d => dentroDeColon(+d.lat, +d.lon));
        return hit ? { punto: [+hit.lat, +hit.lon], exacto: hit.addresstype === 'building' || hit.type === 'house', texto: hit.display_name } : null;
    }

    // Arma una expresión para buscar una calle por nombre. Si es un número ("12" o "Calle 12"),
    // busca calles cuyo nombre termine en ese número.
    function regexCalle(nombre) {
        const numero = nombre.match(/(\d+)\s*$/);
        if (numero) return `(^|[^0-9])${numero[1]}$`;
        return nombre.replace(/[.*+?^${}()|[\]\\"]/g, '\\$&');
    }

    // Esquina entre dos calles, usando los datos de OpenStreetMap.
    // Si las calles se cruzan en más de un punto, devuelve el más cercano a "referencia" (si se pasa).
    async function buscarEsquina(calleA, calleB, referencia) {
        const bbox = `${LIMITES.sur},${LIMITES.oeste},${LIMITES.norte},${LIMITES.este}`;
        const consulta = `[out:json][timeout:15];
            way["highway"]["name"~"${regexCalle(calleA)}",i](${bbox})->.a;
            way["highway"]["name"~"${regexCalle(calleB)}",i](${bbox})->.b;
            node(w.a)(w.b);
            out;`;
        const r = await fetch('https://overpass-api.de/api/interpreter', {
            method: 'POST',
            body: new URLSearchParams({ data: consulta })
        });
        if (!r.ok) return null;
        const datos = await r.json();
        const nodos = (datos.elements || []).filter(n => typeof n.lat === 'number');
        if (!nodos.length) return null;
        if (referencia) {
            const dist = n => Math.pow(n.lat - referencia[0], 2) + Math.pow(n.lon - referencia[1], 2);
            nodos.sort((x, y) => dist(x) - dist(y));
        }
        return [nodos[0].lat, nodos[0].lon];
    }

    // Solo la calle (poco preciso, se usa como último recurso).
    async function buscarCalle(calle) {
        const params = new URLSearchParams({
            format: 'jsonv2', street: calle, city: 'Colón', state: 'Buenos Aires', country: 'Argentina',
            viewbox: `${LIMITES.oeste},${LIMITES.norte},${LIMITES.este},${LIMITES.sur}`, bounded: '1', limit: '1'
        });
        const r = await fetch(`https://nominatim.openstreetmap.org/search?${params}`);
        if (!r.ok) return null;
        const d = (await r.json())[0];
        return d ? [+d.lat, +d.lon] : null;
    }

    // Pin verde = ubicación confiable; pin naranja = aproximada (hay que revisarla).
    function iconoPin(aproximado) {
        const color = aproximado ? '#f08c00' : '#2f6fba';
        return L.divIcon({
            className: '',
            html: `<div style="width:24px;height:24px;background:${color};border:2px solid #fff;border-radius:50% 50% 50% 0;transform:rotate(-45deg);box-shadow:0 2px 5px rgba(0,0,0,.35)"></div>`,
            iconSize: [24, 24],
            iconAnchor: [12, 29]
        });
    }

    // ---------- Formulario (Create / Edit) ----------

    function iniciarFormulario() {
        const div = $('mapaVereda');
        if (!div || typeof L === 'undefined') return;

        const campo = $('Ubicacion');
        const guardada = parsear(campo.value);
        const mapa = crearMapa(div, guardada || CENTRO_COLON, guardada ? 18 : 14);
        const pin = L.marker(guardada || CENTRO_COLON, { draggable: true, title: 'Ubicación de la vereda', icon: iconoPin(false) });
        let ajustadoAMano = false;

        const estado = (texto, error) => {
            const el = $('mapaEstado');
            el.textContent = texto;
            el.classList.toggle('text-danger', !!error);
        };

        // Link a Street View de Google (gratis, sin clave) para revisar el punto elegido.
        const actualizarStreetView = () => {
            const link = $('linkStreetView');
            if (!link) return;
            const p = parsear(campo.value);
            link.classList.toggle('d-none', !p);
            if (p) link.href = `https://www.google.com/maps/@?api=1&map_action=pano&viewpoint=${p[0]},${p[1]}`;
        };

        const poner = (p, centrar, aproximado) => {
            pin.setIcon(iconoPin(!!aproximado));
            pin.setLatLng(p).addTo(mapa);
            campo.value = `${p[0].toFixed(6)},${p[1].toFixed(6)}`;
            actualizarStreetView();
            if (centrar) mapa.setView(p, 18);
        };

        actualizarStreetView();
        if (guardada) {
            pin.addTo(mapa);
            estado('Ubicación guardada. Podés mover el pin o buscar de nuevo.');
        }

        pin.on('dragend', () => {
            const p = pin.getLatLng();
            poner([p.lat, p.lng], false);
            ajustadoAMano = true;
            estado('Ubicación ajustada a mano.');
        });

        mapa.on('click', e => {
            poner([e.latlng.lat, e.latlng.lng], false);
            ajustadoAMano = true;
            estado('Ubicación marcada a mano.');
        });

        async function buscar() {
            const calle = valor('Calle'), altura = valor('Altura');
            const e1 = valor('EntreCalle1'), e2 = valor('EntreCalle2');
            if (!calle) { estado('Primero completá la calle.', true); return; }

            estado('Buscando…');
            try {
                // 1) Calle + altura. Si OSM no tiene esa altura cargada devuelve un punto cualquiera
                //    de la calle (puede quedar a varias cuadras), así que solo se confía si es exacto.
                let aprox = null;
                if (altura) {
                    const r = await buscarDireccion(calle, altura);
                    if (r && r.exacto) {
                        poner(r.punto, true, false);
                        ajustadoAMano = false;
                        estado('Dirección exacta encontrada. Podés mover el pin para ajustar.');
                        return;
                    }
                    aprox = r;
                }

                // 2) Entre calles: punto medio entre las dos esquinas (o la esquina que se encuentre).
                //    Si hay un resultado aproximado, sirve de referencia cuando las calles se cruzan más de una vez.
                if (e1 || e2) {
                    const ref = aprox ? aprox.punto : null;
                    const [p1, p2] = await Promise.all([
                        e1 ? buscarEsquina(calle, e1, ref) : null,
                        e2 ? buscarEsquina(calle, e2, ref) : null
                    ]);
                    if (p1 && p2) {
                        poner([(p1[0] + p2[0]) / 2, (p1[1] + p2[1]) / 2], true, false);
                        ajustadoAMano = false;
                        estado('Ubicada en la mitad de la cuadra entre las dos esquinas. Mové el pin a la vereda exacta.');
                        return;
                    }
                    if (p1 || p2) {
                        poner(p1 || p2, true, true);
                        ajustadoAMano = false;
                        estado('Se encontró una sola esquina (pin naranja: aproximado). Mové el pin a la vereda exacta.', true);
                        return;
                    }
                }

                // 2b) Resultado aproximado de la altura (sin esquinas que lo mejoren)
                if (aprox) {
                    poner(aprox.punto, true, true);
                    ajustadoAMano = false;
                    estado('Ubicación aproximada (pin naranja): puede estar a varias cuadras. Completá "entre calles" o movelo a mano.', true);
                    return;
                }

                // 3) Solo la calle
                const p = await buscarCalle(calle);
                if (p) {
                    poner(p, true, true);
                    estado('Solo se encontró la calle (pin naranja: aproximado). Hacé clic en el lugar exacto.', true);
                    return;
                }
                estado('No se encontró esa dirección en Colón. Marcá el lugar haciendo clic en el mapa.', true);
            } catch (e) {
                estado('No se pudo buscar (¿sin conexión?). Podés marcar el lugar haciendo clic en el mapa.', true);
            }
        }

        $('btnBuscarMapa')?.addEventListener('click', buscar);
        $('btnQuitarUbicacion')?.addEventListener('click', () => {
            mapa.removeLayer(pin);
            campo.value = '';
            actualizarStreetView();
            ajustadoAMano = false;
            estado('Sin ubicación.');
        });

        // Si todavía no hay ubicación, busca sola cuando se termina de escribir la dirección.
        ['Calle', 'Altura', 'EntreCalle1', 'EntreCalle2'].forEach(id =>
            $(id)?.addEventListener('change', () => { if (!ajustadoAMano && !parsear(campo.value)) buscar(); }));

        // Si el mapa está en una columna angosta, recalcula el tamaño al terminar de cargar.
        setTimeout(() => mapa.invalidateSize(), 200);
    }

    // ---------- Solo lectura (Detalles) ----------

    function iniciarSoloLectura() {
        if (typeof L === 'undefined') return;
        document.querySelectorAll('.mapa-solo-lectura').forEach(div => {
            const p = parsear(div.dataset.ubicacion);
            if (!p) return;
            const mapa = crearMapa(div, p, 17);
            L.marker(p).addTo(mapa);
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        iniciarFormulario();
        iniciarSoloLectura();
    });
})();
