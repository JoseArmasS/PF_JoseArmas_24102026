// Solo comportamiento de interfaz; la secuencia y los permisos se validan en el servidor.
(() => {
    const estacion = document.getElementById('estacion-trabajo');
    const linea = document.getElementById('linea-trabajo');
    estacion?.addEventListener('change', () => {
        linea.value = '';
        if (estacion.value) estacion.form.requestSubmit();
    });
    linea?.addEventListener('change', () => {
        if (estacion.value) linea.form.requestSubmit();
    });
    document.querySelectorAll('form[data-confirmar-ciclo]').forEach(form => {
        form.addEventListener('submit', () => {
            form.querySelector('button[type="submit"]').disabled = true;
        });
    });

    const origen = document.querySelector('[data-esperas-url]');
    const contadores = [...document.querySelectorAll('[data-visita-espera]')];
    if (!origen || !contadores.length) return;
    let estados = new Map();
    let referencia = performance.now();
    let consultando = false;
    let ultimaConsulta = -Infinity;
    let errorConsulta = false;
    contadores.forEach(contenedor => {
        const nodo = contenedor.querySelector('[data-espera-bus]');
        estados.set(contenedor.dataset.visitaEspera, {
            estado: nodo.dataset.estado,
            mensaje: nodo.querySelector('[data-espera-mensaje]').textContent,
            segundosRestantes: Number(nodo.dataset.segundos)
        });
    });
    async function consultar() {
        if (consultando || performance.now() - ultimaConsulta < 2000) return;
        consultando = true;
        ultimaConsulta = performance.now();
        try {
            const respuesta = await fetch(origen.dataset.esperasUrl, {
                cache: 'no-store', headers: { Accept: 'application/json' }, signal: AbortSignal.timeout(10000)
            });
            if (!respuesta.ok || !respuesta.headers.get('content-type')?.includes('application/json')) throw new Error('Consulta no disponible');
            const datos = await respuesta.json();
            estados = new Map(Object.entries(datos));
            referencia = performance.now();
            errorConsulta = false;
        } catch {
            errorConsulta = true;
        } finally {
            consultando = false;
            pintar();
        }
    }
    function pintar() {
        const transcurridos = (performance.now() - referencia) / 1000;
        contadores.forEach(contenedor => {
            const dato = estados.get(contenedor.dataset.visitaEspera);
            const nodo = contenedor.querySelector('[data-espera-bus]');
            const mensaje = nodo.querySelector('[data-espera-mensaje]');
            const tiempo = nodo.querySelector('[data-espera-tiempo]');
            tiempo.textContent = '';
            nodo.classList.remove('text-success', 'fw-semibold');
            if (errorConsulta) {
                mensaje.textContent = 'No se pudo actualizar el temporizador. Reintentando…';
                return;
            }
            if (!dato) {
                mensaje.textContent = 'El bus ya no está en espera. Actualiza la pantalla para ver sus movimientos.';
                return;
            }
            mensaje.textContent = dato.mensaje;
            if (dato.estado === 'Listo') nodo.classList.add('text-success', 'fw-semibold');
            if (dato.estado === 'Inicial' || dato.estado === 'Adicional') {
                const restante = Math.max(0, Math.ceil(dato.segundosRestantes - transcurridos));
                if (!restante) {
                    mensaje.textContent = 'Verificando el aviso de salida con el servidor…';
                    void consultar();
                } else {
                    tiempo.textContent = `${Math.floor(restante / 60).toString().padStart(2, '0')}:${(restante % 60).toString().padStart(2, '0')}`;
                }
            }
        });
    }
    pintar();
    setInterval(pintar, 1000);
    setInterval(consultar, 5000);
    document.addEventListener('visibilitychange', () => { if (!document.hidden) void consultar(); });
})();
