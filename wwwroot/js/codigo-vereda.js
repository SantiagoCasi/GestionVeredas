// Código de la vereda (RF-VER-18) en Crear/Editar.
// - Avisa al escribir si el código ya existe (GET Veredas/CodigoDisponible).
// - En Crear, la casilla "Asignar automáticamente" deshabilita el campo: el servidor asigna el siguiente libre.
// La validación definitiva la hace el servidor (y el índice único de la base).
(function () {
    const input = document.getElementById('Codigo');
    if (!input) return;

    const estado = document.getElementById('codigoEstado');
    const auto = document.getElementById('codigoAutomatico');
    const siguiente = document.getElementById('codigoSiguiente');
    const url = input.dataset.url;
    const id = input.dataset.id || '0';
    let temporizador = null;
    let pedido = 0;

    function mostrar(texto, clase) {
        estado.textContent = texto || '';
        estado.classList.remove('text-success', 'text-danger', 'text-muted');
        if (clase) estado.classList.add(clase);
    }

    async function consultar() {
        const numero = ++pedido;
        try {
            const r = await fetch(`${url}?codigo=${encodeURIComponent(input.value.trim())}&id=${encodeURIComponent(id)}`,
                { headers: { 'Accept': 'application/json' } });
            if (!r.ok) return;
            const d = await r.json();
            if (numero !== pedido) return; // llegó una respuesta vieja
            if (siguiente) siguiente.textContent = d.siguiente;
            if (!input.value.trim()) { mostrar(''); return; }
            mostrar(d.mensaje, d.valido && d.disponible ? 'text-success' : 'text-danger');
        } catch (e) {
            // Sin conexión: el servidor valida igual al guardar.
        }
    }

    function aplicarAuto() {
        if (!auto) return;
        input.disabled = auto.checked;
        input.classList.toggle('bg-light', auto.checked);
        if (auto.checked) {
            input.value = '';
            mostrar('Se asignará el siguiente número libre al guardar.', 'text-muted');
        } else {
            mostrar('');
            consultar();
        }
    }

    input.addEventListener('input', () => {
        clearTimeout(temporizador);
        temporizador = setTimeout(consultar, 300);
    });
    if (auto) auto.addEventListener('change', aplicarAuto);

    aplicarAuto();
    if (!auto || !auto.checked) consultar();
})();
