(() => {
    const capacidad = document.getElementById('Formulario_CapacidadPasajeros');
    if (!capacidad) return;

    const validarCapacidad = () => {
        capacidad.setCustomValidity('');
        if (capacidad.validity.rangeUnderflow) {
            capacidad.setCustomValidity('La capacidad debe ser un entero mayor que cero.');
        }
    };

    capacidad.addEventListener('input', validarCapacidad);
    capacidad.addEventListener('invalid', validarCapacidad);
})();
