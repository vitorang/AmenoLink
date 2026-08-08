export function sanitizeInteger(value: number | null | undefined, minimumValue: number = 0): number {
    if (value === null || value === undefined || isNaN(value) || value < minimumValue)
        return minimumValue;

    return Math.floor(value);
}

export function handleInputBlur(event: FocusEvent, minimumValue: number = 0): number {
    const inputElement = event.target as HTMLInputElement;
    const numericValue = inputElement ? Number(inputElement.value) : null;
    const sanitizedValue = sanitizeInteger(numericValue, minimumValue);

    if (inputElement)
        inputElement.value = String(sanitizedValue);

    return sanitizedValue;
}
