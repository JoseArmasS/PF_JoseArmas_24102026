// Prototipo aislado. No llama a servicios, no persiste datos ni controla operaciones reales.
(() => {
    const prepare = document.getElementById('cycle-prepare');
    if (!prepare) return;
    const byId = id => document.getElementById(`cycle-${id}`);
    const station = position => `Estación ${'ABCDE'[position]}`;
    let visits = [], current = 0, travelling = false, closed = false;
    const addHistory = text => {
        const entry = document.createElement('li');
        entry.textContent = text;
        byId('history').append(entry);
    };
    const syncDirection = () => {
        const origin = Number(byId('origin').value);
        byId('direction').options[0].disabled = origin === 4;
        byId('direction').options[1].disabled = origin === 0;
        if (origin === 0) byId('direction').value = '1';
        if (origin === 4) byId('direction').value = '-1';
        byId('station-title').textContent = station(origin);
    };
    const render = () => {
        const last = current === visits.length - 1;
        const state = closed ? 'Ciclo cerrado' : travelling ? 'En trayecto' : last ? 'Ciclo completo' : 'En estación';
        const destination = travelling ? visits[current + 1] : visits[current];
        byId('state').textContent = state;
        byId('station-title').textContent = station(destination);
        byId('station-help').textContent = travelling ? 'Vista del operador de la estación que recibirá el bus.' : 'Vista del operador que está atendiendo el bus.';
        byId('context').textContent = travelling
            ? `${station(visits[current])} → ${station(destination)} · Línea 1`
            : `${station(visits[current])} · Línea 1 · ${current} de ${visits.length - 1} tramos completados`;
        byId('occupancy').hidden = travelling || closed || last;
        byId('next').hidden = closed;
        byId('next').textContent = travelling ? `Simular llegada a ${station(destination)}`
            : last ? 'Simular cierre del ciclo' : `Simular salida hacia ${station(visits[current + 1])}`;
        byId('row-state').textContent = state;
        byId('row-destination').textContent = closed || last ? 'Finalizado en origen' : station(visits[current + 1]);
        byId('feedback').textContent = closed ? 'Ciclo de ejemplo cerrado. Puedes preparar otro.'
            : last ? 'Se recorrieron ambos sentidos y se regresó al punto inicial.' : '';
        byId('start').disabled = !closed;
        prepare.querySelectorAll('select').forEach(select => { select.disabled = !closed; });
    };
    byId('origin').addEventListener('change', syncDirection);
    syncDirection();
    prepare.addEventListener('submit', event => {
        event.preventDefault();
        const origin = Number(byId('origin').value);
        let direction = Number(byId('direction').value), position = origin;
        visits = [origin];
        // Itinerario ilustrativo sobre cinco estaciones ficticias, sin reglas de producción.
        for (let step = 0; step < 8; step++) {
            if (position === 0) direction = 1;
            if (position === 4) direction = -1;
            position += direction;
            visits.push(position);
        }
        current = 0; travelling = false; closed = false;
        byId('history').replaceChildren();
        addHistory(`Inicio de ejemplo en ${station(origin)}.`);
        byId('route').textContent = visits.map(station).join(' → ');
        byId('buses').replaceChildren();
        const row = document.createElement('tr');
        const busCell = document.createElement('th');
        busCell.scope = 'row'; busCell.textContent = `Bus ${byId('bus').value}`;
        row.append(busCell);
        ['row-state', 'row-destination'].forEach(id => {
            const cell = document.createElement('td'); cell.id = `cycle-${id}`; row.append(cell);
        });
        const actionCell = document.createElement('td');
        const action = document.createElement('button');
        action.type = 'button'; action.className = 'btn btn-sm btn-outline-secondary'; action.textContent = 'Atender';
        action.addEventListener('click', () => {
            byId('attention').hidden = !byId('attention').hidden;
            action.setAttribute('aria-expanded', String(!byId('attention').hidden));
        });
        action.setAttribute('aria-controls', 'cycle-attention'); action.setAttribute('aria-expanded', 'true');
        actionCell.append(action); row.append(actionCell); byId('buses').append(row);
        byId('attention').hidden = false;
        byId('attention-title').textContent = `Atender bus ${byId('bus').value}`;
        byId('count').textContent = '1 operación de ejemplo';
        byId('occupancy-value').textContent = 'Sin medición en esta visita.';
        render();
    });
    byId('next').addEventListener('click', () => {
        if (!travelling && current === visits.length - 1) {
            closed = true; addHistory('Cierre del ciclo de ejemplo.');
        } else if (travelling) {
            current++; travelling = false;
            addHistory(`Llegada de ejemplo a ${station(visits[current])}.`);
            byId('occupancy-value').textContent = 'Sin medición en esta visita.';
        } else {
            travelling = true; addHistory(`Salida de ejemplo de ${station(visits[current])}.`);
        }
        render();
    });
    byId('occupancy').addEventListener('submit', event => {
        event.preventDefault();
        const passengers = Number(byId('passengers').value);
        byId('occupancy-value').textContent = `${passengers} pasajeros · ${(passengers / 60 * 100).toFixed(1)} % · ejemplo`;
        addHistory(`Medición de ejemplo en ${station(visits[current])}: ${passengers} pasajeros.`);
    });
})();
