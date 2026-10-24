(() => {
    const pasajeros = document.getElementById('Pasajeros');
    if (!pasajeros) return;
    const validar = () => {
        pasajeros.setCustomValidity('');
        if (pasajeros.validity.rangeUnderflow) {
            pasajeros.setCustomValidity('La cantidad debe ser un entero mayor o igual a cero.');
        } else if (pasajeros.validity.stepMismatch || pasajeros.validity.badInput) {
            pasajeros.setCustomValidity('Ingresa una cantidad entera de pasajeros.');
        } else if (pasajeros.validity.valueMissing) {
            pasajeros.setCustomValidity('Ingresa la cantidad de pasajeros.');
        }
    };
    pasajeros.addEventListener('input', validar);
    pasajeros.addEventListener('invalid', validar);
})();
