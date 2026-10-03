export function formatPercentage(value) {
    const n = Number(value)
    if (!Number.isFinite(n)) {
        return '0%'
    }

    const rounded = Math.round(n * 10) / 10
    if (Number.isInteger(rounded)) {
        return `${rounded}%`
    }

    return `${rounded.toFixed(1)}%`
}
