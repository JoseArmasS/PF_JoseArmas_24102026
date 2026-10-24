document.querySelectorAll('.bus-occupancy-form').forEach(form => {
    form.addEventListener('submit', async event => {
        event.preventDefault();
        const boton = form.querySelector('button[type="submit"]');
        const mensaje = form.querySelector('.bus-occupancy-feedback');
        boton.disabled = true;
        mensaje.textContent = '';
        try {
            const datos = new FormData(form);
            datos.set('desdeOperacion', 'true');
            const respuesta = await fetch(form.action, { method: 'POST', body: datos });
            if (respuesta.redirected) {
                mensaje.textContent = 'Tu sesión terminó. Inicia sesión nuevamente.';
                return;
            }
            const resultado = await respuesta.json();
            if (respuesta.ok && resultado.guardado) {
                window.location.reload();
                return;
            }
            mensaje.textContent = (resultado.solicitarMasBuses ? 'Se requieren más buses. La medición no se guardó. ' : '')
                + (resultado.errores || ['No se pudo guardar la ocupación.']).join(' ');
        } catch {
            mensaje.textContent = 'No se pudo confirmar el guardado. Consulta el historial antes de volver a intentarlo.';
        } finally {
            boton.disabled = false;
        }
    });
});
