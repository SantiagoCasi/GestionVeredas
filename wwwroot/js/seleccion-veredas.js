// Filtro y selección múltiple de veredas (SPEC-002).
// Se usa en Veredas/Index (Agregar al paquete…) y en Paquetes/AgregarVeredas. No usa jQuery.
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var filas = Array.prototype.slice.call(document.querySelectorAll('.fila-vereda'));
        if (!filas.length) return;

        var texto = document.getElementById('filtroTexto');
        var estado = document.getElementById('filtroEstado');
        var medicion = document.getElementById('filtroMedicion');
        var vacio = document.getElementById('sinResultados');
        var todas = document.getElementById('seleccionarTodas');
        var barra = document.getElementById('barraSeleccion');
        var formPaquete = document.getElementById('formAgregarAPaquete');
        var paqueteDestino = document.getElementById('paqueteDestino');
        var errorPaquete = document.getElementById('errorPaquete');
        var contadores = document.querySelectorAll('.contador-seleccion');
        var botones = document.querySelectorAll('.btn-agregar-seleccion');
        var filaClicable = document.querySelector('[data-fila-clicable]') !== null;

        function casillas() {
            return Array.prototype.slice.call(document.querySelectorAll('.sel-vereda'));
        }

        function visible(fila) {
            return !fila.classList.contains('d-none');
        }

        // Filtro por texto, estado y medición. Las marcadas que quedan ocultas siguen marcadas.
        function filtrar() {
            var t = texto ? texto.value.trim().toLowerCase() : '';
            var e = estado ? estado.value : '';
            var m = medicion ? medicion.value : '';
            var visibles = 0;
            filas.forEach(function (f) {
                var ok = (!t || (f.dataset.texto || '').indexOf(t) >= 0)
                    && (!e || f.dataset.estado === e)
                    && (!m || f.dataset.medida === m);
                f.classList.toggle('d-none', !ok);
                if (ok) visibles++;
            });
            if (vacio) vacio.classList.toggle('d-none', visibles > 0);
            actualizarContador();
        }

        // Marca o desmarca solo las filas visibles y habilitadas.
        function seleccionarTodas(marcar) {
            filas.forEach(function (f) {
                if (!visible(f)) return;
                var c = f.querySelector('.sel-vereda');
                if (c && !c.disabled) c.checked = marcar;
            });
            actualizarContador();
        }

        function actualizarContador() {
            var marcadas = casillas().filter(function (c) { return c.checked && !c.disabled; }).length;
            var textoContador = marcadas === 1 ? '1 seleccionada' : marcadas + ' seleccionadas';
            contadores.forEach(function (c) { c.textContent = textoContador; });
            botones.forEach(function (b) { b.disabled = marcadas === 0; });
            if (barra) barra.classList.toggle('d-none', marcadas === 0);

            if (todas) {
                var habilitadasVisibles = filas
                    .filter(visible)
                    .map(function (f) { return f.querySelector('.sel-vereda'); })
                    .filter(function (c) { return c && !c.disabled; });
                var marcadasVisibles = habilitadasVisibles.filter(function (c) { return c.checked; }).length;
                todas.checked = habilitadasVisibles.length > 0 && marcadasVisibles === habilitadasVisibles.length;
                todas.indeterminate = marcadasVisibles > 0 && marcadasVisibles < habilitadasVisibles.length;
                todas.disabled = habilitadasVisibles.length === 0;
            }
        }

        if (texto) texto.addEventListener('input', filtrar);
        if (estado) estado.addEventListener('change', filtrar);
        if (medicion) medicion.addEventListener('change', filtrar);
        if (todas) todas.addEventListener('change', function () { seleccionarTodas(todas.checked); });

        document.addEventListener('change', function (e) {
            if (e.target.classList && e.target.classList.contains('sel-vereda')) actualizarContador();
        });

        // En "Agregar veredas" toda la fila marca la casilla.
        if (filaClicable) {
            filas.forEach(function (f) {
                f.addEventListener('click', function (e) {
                    if (e.target.closest('a, button, input, label, select')) return;
                    var c = f.querySelector('.sel-vereda');
                    if (!c || c.disabled) return;
                    c.checked = !c.checked;
                    actualizarContador();
                });
            });
        }

        // En Veredas/Index hay que elegir el paquete antes de enviar (el servidor valida igual).
        if (formPaquete && paqueteDestino) {
            formPaquete.addEventListener('submit', function (e) {
                if (!paqueteDestino.value) {
                    e.preventDefault();
                    if (errorPaquete) errorPaquete.textContent = 'Elegí el paquete al que querés agregar las veredas.';
                    paqueteDestino.focus();
                }
            });
            paqueteDestino.addEventListener('change', function () {
                if (errorPaquete) errorPaquete.textContent = '';
            });
        }

        filtrar();
    });
})();
