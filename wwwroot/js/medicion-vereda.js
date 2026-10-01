// Medición de la vereda (SPEC-001): renglones de pozos en vivo, total, cordón,
// bloqueo del envío con errores y alta rápida de tipos de suelo.
// Aplica las mismas reglas y mensajes que Services/MedicionService.cs. Es solo una vista previa:
// lo que se guarda es lo que calcula el servidor. No usa jQuery ni eval.
(function () {
    'use strict';

    var MEDIDA_MAXIMA = 9999.99;
    var LARGO_MAXIMO_FORMULA = 500; // Veredas.Medicion y MedicionCordon son nvarchar(500)
    var TOTAL_MAXIMO = { 2: '99999999.99', 3: '9999999.999' };

    // ---- Mensajes (iguales a MedicionService) ----
    var msj = {
        vacio: function (n) { return 'El término ' + n + ' está vacío: revisá que no haya dos «+» seguidos ni un «+» al principio o al final.'; },
        espacio: function (n, t) { return 'El término ' + n + ' («' + t + '») tiene un espacio dentro de una medida. Separá las medidas con «*».'; },
        caracter: function (n, t, c) { return 'El término ' + n + ' («' + t + '») tiene un carácter que no se permite: «' + c + '». Usá solo números, «+», «*» (o «x») y paréntesis.'; },
        parentesis: function (n, t) { return 'El término ' + n + ' («' + t + '») tiene los paréntesis mal puestos. Cada término puede ir entre paréntesis, así: (2*3).'; },
        medidaVacia: function (n, t) { return 'El término ' + n + ' («' + t + '») tiene una medida vacía: revisá que no haya dos «*» seguidos ni un «*» al principio o al final.'; },
        numero: function (n, t, m) { return 'El término ' + n + ' («' + t + '») tiene una medida que no es un número válido: «' + m + '». Usá la coma o el punto solo para los decimales (2,5 o 2.5).'; },
        cero: function (n, t) { return 'El término ' + n + ' («' + t + '») tiene una medida en cero. Las medidas tienen que ser mayores a 0.'; },
        grande: function (n, t) { return 'El término ' + n + ' («' + t + '») tiene una medida mayor a 9.999,99 m.'; },
        cantidad: function (n, t, k, e) { return 'El término ' + n + ' («' + t + '») tiene ' + k + ' ' + (k === 1 ? 'medida' : 'medidas') + ' y lleva ' + e + '.'; },
        totalGrande: 'El total es demasiado grande. Revisá la medición.',
        largo: 'La medición puede tener hasta 500 caracteres.',
        pozoSinTipo: function (n) { return 'Elegí el tipo de suelo del pozo ' + n + '.'; },
        revisa: 'Revisá la medición y los tipos de suelo marcados en rojo antes de guardar.',
        yaEstaba: 'Ese tipo de suelo ya estaba en la lista: quedó elegido.',
        sesion: 'No se pudo agregar el tipo de suelo porque se venció la sesión. Iniciá sesión en otra pestaña y volvé a probar; lo que cargaste acá no se pierde.',
        otroError: 'No se pudo agregar el tipo de suelo. Probá de nuevo.'
    };

    // ---- Números exactos con BigInt: { m: mantisa, e: cantidad de decimales } ----
    function aExacto(texto) {
        var t = texto.replace(',', '.');
        var partes = t.split('.');
        var enteros = partes[0] || '0';
        var decimales = partes.length > 1 ? partes[1] : '';
        return { m: BigInt(enteros + decimales), e: decimales.length };
    }

    // Redondea m / 10^e a d decimales, alejándose de cero en el punto medio. Devuelve la mantisa a escala d.
    function redondear(m, e, d) {
        if (e <= d) return m * (10n ** BigInt(d - e));
        var divisor = 10n ** BigInt(e - d);
        var cociente = m / divisor;
        var resto = m % divisor;
        if (resto * 2n >= divisor) cociente += 1n;
        return cociente;
    }

    function aNumero(m, d) { return Number(m) / Math.pow(10, d); }

    // Mismo algoritmo que MedicionService.Calcular.
    function calcular(texto, medidasPorTermino) {
        if (medidasPorTermino !== 2 && medidasPorTermino !== 3) throw new RangeError('Las medidas por término son 2 o 3.');
        var vacio = { total: null, normalizado: null, terminos: [], errores: [] };
        if (texto == null || texto.trim() === '') return vacio;

        var d = medidasPorTermino === 2 ? 2 : 3;
        var errores = [];
        var calculados = [];
        var partes = texto.split('+');

        for (var i = 0; i < partes.length; i++) {
            var n = i + 1;
            var t = partes[i].trim();
            var r = revisarTermino(n, t, medidasPorTermino);
            if (r.error) errores.push(r.error); else calculados.push({ orden: n, medidas: r.medidas, valores: r.valores });
        }
        if (errores.length) return { total: null, normalizado: null, terminos: [], errores: errores };

        var terminos = [];
        var totalM = 0n;
        calculados.forEach(function (c) {
            var m = 1n, e = 0;
            c.valores.forEach(function (v) { m *= v.m; e += v.e; });
            var sub = redondear(m, e, d);
            totalM += sub;
            terminos.push({ orden: c.orden, medidas: c.medidas, subtotalM: sub, subtotal: aNumero(sub, d) });
        });

        var maximo = aExacto(TOTAL_MAXIMO[medidasPorTermino]);
        if (totalM > redondear(maximo.m, maximo.e, d)) {
            return { total: null, normalizado: null, terminos: [], errores: [msj.totalGrande] };
        }

        var normalizado = terminos.map(function (x) { return '(' + x.medidas + ')'; }).join('+');
        if (normalizado.length > LARGO_MAXIMO_FORMULA) {
            return { total: null, normalizado: null, terminos: [], errores: [msj.largo] };
        }

        return {
            total: aNumero(totalM, d),
            normalizado: normalizado,
            terminos: terminos,
            errores: []
        };
    }

    function revisarTermino(n, t, esperadas) {
        if (t.length === 0) return { error: msj.vacio(n) };
        if (/[0-9.,]\s+[0-9.,]/.test(t)) return { error: msj.espacio(n, t) };

        var limpio = t.replace(/\s+/g, '').replace(/[xX]/g, '*');
        for (var i = 0; i < limpio.length; i++) {
            if (!/[0-9,.*()]/.test(limpio[i])) return { error: msj.caracter(n, t, limpio[i]) };
        }

        if (limpio.indexOf('(') >= 0 || limpio.indexOf(')') >= 0) {
            var envuelve = limpio.length >= 2 && limpio[0] === '(' && limpio[limpio.length - 1] === ')';
            var interior = envuelve ? limpio.substring(1, limpio.length - 1) : '';
            if (!envuelve || interior.indexOf('(') >= 0 || interior.indexOf(')') >= 0) return { error: msj.parentesis(n, t) };
            limpio = interior;
        }

        var textos = limpio.split('*');
        if (textos.some(function (m) { return m.length === 0; })) return { error: msj.medidaVacia(n, t) };

        var valores = [];
        for (var j = 0; j < textos.length; j++) {
            var m = textos[j];
            if (!/^[0-9]+([.,][0-9]+)?$/.test(m) && !/^[.,][0-9]+$/.test(m)) return { error: msj.numero(n, t, m) };
            valores.push(aExacto(m));
        }

        if (valores.some(function (v) { return v.m === 0n; })) return { error: msj.cero(n, t) };
        var max = aExacto(String(MEDIDA_MAXIMA));
        if (valores.some(function (v) { return comparar(v, max) > 0; })) return { error: msj.grande(n, t) };
        if (valores.length !== esperadas) return { error: msj.cantidad(n, t, valores.length, esperadas) };

        return { medidas: limpio, valores: valores };
    }

    function comparar(a, b) {
        var e = Math.max(a.e, b.e);
        var am = a.m * (10n ** BigInt(e - a.e));
        var bm = b.m * (10n ** BigInt(e - b.e));
        return am > bm ? 1 : am < bm ? -1 : 0;
    }

    function formatear(valor, d, unidad) {
        if (valor == null) return '—';
        return valor.toLocaleString('es-AR', { minimumFractionDigits: d, maximumFractionDigits: d }) + ' ' + unidad;
    }

    // Para las pruebas con node (no se usa en el navegador).
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = { calcular: calcular, formatear: formatear };
        return;
    }

    // ---- Formulario ----
    function iniciar(caja) {
        var form = caja.closest('form');
        var inputMedicion = caja.querySelector('.medicion-input');
        var erroresMedicion = caja.querySelector('.medicion-errores');
        var contRoturas = caja.querySelector('.roturas');
        var salidaTotal = caja.querySelector('.medicion-total');
        var chkCordon = caja.querySelector('.cordon-check');
        var bloqueCordon = caja.querySelector('.cordon-bloque');
        var inputCordon = caja.querySelector('.cordon-input');
        var erroresCordon = caja.querySelector('.cordon-errores');
        var salidaCordon = caja.querySelector('.cordon-total');
        var aviso = caja.querySelector('.medicion-aviso');
        var plantilla = caja.querySelector('.plantilla-tipos');
        var modal = document.getElementById('modalTipoSuelo');

        // Errores del servidor por renglón (se muestran hasta que el usuario cambia la medición).
        var erroresServidor = {};
        contRoturas.querySelectorAll('[data-valmsg-for]').forEach(function (s) {
            var m = /tiposRotura\[(\d+)\]/.exec(s.getAttribute('data-valmsg-for') || '');
            if (m && s.textContent.trim()) erroresServidor[m[1]] = s.textContent.trim();
        });

        // Errores del servidor de la fórmula y del cordón (p. ej., «La cantidad de pozos no coincide…»):
        // se siguen mostrando junto con los del cálculo en vivo hasta que el usuario cambia el campo.
        function textos(contenedor) {
            return Array.prototype.map.call(contenedor.children, function (d) { return d.textContent.trim(); })
                .filter(function (t) { return t.length > 0; });
        }
        var erroresServidorMedicion = textos(erroresMedicion);
        var erroresServidorCordon = textos(erroresCordon);

        function unir(servidor, vivos) {
            var todos = vivos.slice();
            servidor.forEach(function (e) { if (todos.indexOf(e) < 0) todos.push(e); });
            return todos;
        }

        function valoresActuales() {
            return Array.prototype.map.call(contRoturas.querySelectorAll('select.rotura-tipo'), function (s) { return s.value; });
        }

        function crearSelect(indice, valor) {
            var sel = plantilla.content.querySelector('select').cloneNode(true);
            sel.name = 'tiposRotura[' + indice + ']';
            sel.id = 'tiposRotura_' + indice;
            sel.value = valor || '';
            if (sel.value !== (valor || '')) sel.value = '';
            return sel;
        }

        function crearRenglon(i, termino, error, valor) {
            var fila = document.createElement('div');
            fila.className = 'rotura-fila row g-2 align-items-start py-1 border-bottom';
            fila.dataset.indice = String(i);

            var colPozo = document.createElement('div');
            colPozo.className = 'col-auto fw-semibold pt-1';
            colPozo.textContent = 'Pozo ' + (i + 1);
            fila.appendChild(colPozo);

            var colMedidas = document.createElement('div');
            colMedidas.className = 'col-12 col-md-4 pt-1 rotura-medidas';
            if (error) {
                colMedidas.classList.add('text-danger', 'small');
                colMedidas.textContent = error;
            } else if (termino) {
                colMedidas.textContent = termino.medidas.split('*').join(' × ') + ' = ' + formatear(termino.subtotal, 2, 'm²');
            }
            fila.appendChild(colMedidas);

            var colTipo = document.createElement('div');
            colTipo.className = 'col';
            var sel = crearSelect(i, valor);
            sel.setAttribute('aria-label', 'Tipo de suelo del pozo ' + (i + 1));
            colTipo.appendChild(sel);
            var msg = document.createElement('span');
            msg.className = 'text-danger small d-block rotura-error';
            msg.setAttribute('data-valmsg-for', 'tiposRotura[' + i + ']');
            if (erroresServidor[String(i)]) msg.textContent = erroresServidor[String(i)];
            colTipo.appendChild(msg);
            var info = document.createElement('span');
            info.className = 'text-success small d-block rotura-info';
            colTipo.appendChild(info);
            fila.appendChild(colTipo);

            var colBoton = document.createElement('div');
            colBoton.className = 'col-auto';
            var btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'btn btn-sm btn-outline-secondary rotura-nuevo';
            btn.dataset.destino = String(i);
            btn.textContent = '+ Nuevo';
            colBoton.appendChild(btn);
            fila.appendChild(colBoton);

            return fila;
        }

        function mostrarErrores(contenedor, errores) {
            contenedor.innerHTML = '';
            errores.forEach(function (e) {
                var div = document.createElement('div');
                div.textContent = e;
                contenedor.appendChild(div);
            });
        }

        // Vuelve a armar un renglón por término, conservando el tipo elegido en cada posición.
        function actualizarMedicion() {
            var valores = valoresActuales();
            var texto = inputMedicion.value;
            var r = calcular(texto, 2);
            contRoturas.innerHTML = '';

            if (r.errores.length) {
                mostrarErrores(erroresMedicion, unir(erroresServidorMedicion, r.errores));
                // Un renglón por término, con su error si lo tiene, para que no cambie la cantidad de desplegables.
                var partes = texto.split('+');
                partes.forEach(function (p, i) {
                    var errorTermino = r.errores.filter(function (e) { return e.indexOf('El término ' + (i + 1) + ' ') === 0; })[0] || null;
                    var parcial = errorTermino ? null : calcular(p, 2);
                    var termino = parcial && parcial.terminos.length ? parcial.terminos[0] : null;
                    contRoturas.appendChild(crearRenglon(i, termino, errorTermino, valores[i]));
                });
                salidaTotal.textContent = '—';
                return r;
            }

            mostrarErrores(erroresMedicion, erroresServidorMedicion);
            r.terminos.forEach(function (t, i) {
                contRoturas.appendChild(crearRenglon(i, t, null, valores[i]));
            });
            salidaTotal.textContent = r.total == null ? '—' : formatear(r.total, 2, 'm²');
            return r;
        }

        function actualizarCordon() {
            bloqueCordon.classList.toggle('d-none', !chkCordon.checked);
            var r = calcular(inputCordon.value, 3);
            mostrarErrores(erroresCordon, unir(erroresServidorCordon, r.errores));
            salidaCordon.textContent = r.errores.length || r.total == null ? '—' : formatear(r.total, 3, 'm³');
            return r;
        }

        inputMedicion.addEventListener('input', function () {
            erroresServidor = {};
            erroresServidorMedicion = [];
            aviso.classList.add('d-none');
            actualizarMedicion();
        });
        inputCordon.addEventListener('input', function () {
            erroresServidorCordon = [];
            actualizarCordon();
        });
        // Al ocultar el cordón no se borra el valor: el servidor lo descarta si la casilla llega sin marcar.
        chkCordon.addEventListener('change', actualizarCordon);

        contRoturas.addEventListener('change', function (e) {
            if (!e.target.matches('select.rotura-tipo')) return;
            var fila = e.target.closest('.rotura-fila');
            if (e.target.value) {
                fila.querySelector('.rotura-error').textContent = '';
                delete erroresServidor[fila.dataset.indice];
            }
            fila.querySelector('.rotura-info').textContent = '';
        });

        // Bloqueo del envío: así no se pierden las fotos elegidas si el servidor rechaza el formulario.
        form.addEventListener('submit', function (e) {
            var r = actualizarMedicion();
            var c = chkCordon.checked ? actualizarCordon() : { errores: [] };
            var primero = null;
            var hayError = r.errores.length > 0 || c.errores.length > 0;

            if (r.errores.length) primero = inputMedicion;
            else if (c.errores.length) primero = inputCordon;

            contRoturas.querySelectorAll('.rotura-fila').forEach(function (fila) {
                var sel = fila.querySelector('select.rotura-tipo');
                if (!sel.value) {
                    hayError = true;
                    fila.querySelector('.rotura-error').textContent = msj.pozoSinTipo(Number(fila.dataset.indice) + 1);
                    if (!primero) primero = sel;
                }
            });

            if (hayError) {
                e.preventDefault();
                aviso.textContent = msj.revisa;
                aviso.classList.remove('d-none');
                if (primero) primero.focus();
            }
        });

        // ---- Alta rápida de tipo de suelo ----
        var destino = null;
        if (modal) {
            var inTipo = modal.querySelector('#tsRapidoTipo');
            var inMedida = modal.querySelector('#tsRapidoMedida');
            var zonaErrores = modal.querySelector('.ts-rapido-errores');
            var btnAgregar = modal.querySelector('.ts-rapido-agregar');
            var url = modal.getAttribute('data-url');

            contRoturas.addEventListener('click', function (e) {
                var btn = e.target.closest('.rotura-nuevo');
                if (!btn) return;
                destino = btn.dataset.destino;
                mostrarErrores(zonaErrores, []);
                bootstrap.Modal.getOrCreateInstance(modal).show();
            });

            modal.addEventListener('shown.bs.modal', function () { inTipo.focus(); });

            function agregarOpcion(select, id, texto) {
                if (select.querySelector('option[value="' + id + '"]')) return;
                var op = document.createElement('option');
                op.value = id;
                op.textContent = texto;
                var siguiente = null;
                Array.prototype.some.call(select.options, function (o) {
                    if (o.value && o.textContent.localeCompare(texto, 'es') > 0) { siguiente = o; return true; }
                    return false;
                });
                select.insertBefore(op, siguiente);
            }

            btnAgregar.addEventListener('click', function () {
                var datos = new FormData();
                datos.append('Tipo', inTipo.value);
                datos.append('Medida', inMedida.value);
                var token = form.querySelector('input[name="__RequestVerificationToken"]');

                btnAgregar.disabled = true;
                mostrarErrores(zonaErrores, []);

                fetch(url, {
                    method: 'POST',
                    body: datos,
                    headers: { 'RequestVerificationToken': token ? token.value : '' },
                    credentials: 'same-origin'
                }).then(function (resp) {
                    var tipo = resp.headers.get('Content-Type') || '';
                    if (resp.redirected || (resp.ok && tipo.indexOf('application/json') < 0)) {
                        mostrarErrores(zonaErrores, [msj.sesion]);
                        return null;
                    }
                    if (tipo.indexOf('application/json') < 0) {
                        mostrarErrores(zonaErrores, [msj.otroError]);
                        return null;
                    }
                    return resp.json().then(function (json) {
                        if (resp.status === 400) {
                            mostrarErrores(zonaErrores, (json && json.errores) || [msj.otroError]);
                            return;
                        }
                        if (!resp.ok) {
                            mostrarErrores(zonaErrores, [msj.otroError]);
                            return;
                        }
                        var id = String(json.id);
                        agregarOpcion(plantilla.content.querySelector('select'), id, json.texto);
                        contRoturas.querySelectorAll('select.rotura-tipo').forEach(function (s) { agregarOpcion(s, id, json.texto); });

                        var fila = contRoturas.querySelector('.rotura-fila[data-indice="' + destino + '"]');
                        if (fila) {
                            var sel = fila.querySelector('select.rotura-tipo');
                            sel.value = id;
                            fila.querySelector('.rotura-error').textContent = '';
                            fila.querySelector('.rotura-info').textContent = json.existente ? msj.yaEstaba : '';
                        }
                        inTipo.value = '';
                        inMedida.value = '';
                        bootstrap.Modal.getOrCreateInstance(modal).hide();
                    });
                }).catch(function () {
                    mostrarErrores(zonaErrores, [msj.otroError]);
                }).finally(function () {
                    btnAgregar.disabled = false;
                });
            });
        }

        actualizarMedicion();
        actualizarCordon();
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.medicion-vereda').forEach(iniciar);
    });
})();
