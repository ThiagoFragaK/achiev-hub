/** Single-select filter options for global unlock percentage. */
export const GLOBAL_PERCENTAGE_FILTER_OPTIONS = [
    { value: 'all', label: 'All rarity' },
    { value: 'under1', label: 'Under 1%' },
    { value: 'under5', label: 'Under 5%' },
    { value: 'under10', label: 'Under 10%' },
    { value: 'under30', label: 'Under 30%' },
    { value: 'under50', label: 'Under 50%' },
    { value: 'above50', label: 'Above 50%' }
]

/**
 * Formats a Steam global achievement percentage for display.
 * @param {unknown} value
 * @returns {string}
 */
export function formatGlobalPercentage(value) {
    if (value === null || value === undefined || value === '') {
        return '—'
    }
    const number = Number(value)
    if (Number.isNaN(number)) {
        return '—'
    }
    return `${number.toFixed(1)}%`
}

/**
 * CSS class for rarity color by global unlock percentage.
 * @param {unknown} value
 * @returns {string}
 */
export function globalPercentageColorClass(value) {
    if (value === null || value === undefined || value === '') {
        return ''
    }
    const number = Number(value)
    if (Number.isNaN(number)) {
        return ''
    }
    if (number < 1) return 'global-pct--under-1'
    if (number < 5) return 'global-pct--under-5'
    if (number < 10) return 'global-pct--under-10'
    if (number < 30) return 'global-pct--under-30'
    if (number < 50) return 'global-pct--under-50'
    return 'global-pct--above-50'
}
