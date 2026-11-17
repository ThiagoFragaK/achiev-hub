/**
 * Client-side pagination helpers for table state shaped like:
 * { currentPage, perPage, totalPages, totalCount }
 */

export function createPagination({ currentPage = 1, perPage = 15 } = {}) {
    return {
        currentPage,
        perPage,
        totalPages: 1,
        totalCount: 0
    }
}

/**
 * Applies an API paged payload `{ data, pagination }` onto table pagination state.
 * Returns the data array (never null).
 */
export function applyPagedResult(pagination, result) {
    const data = result?.data ?? []
    const api = result?.pagination

    pagination.currentPage = api?.currentPage ?? pagination.currentPage ?? 1
    pagination.totalPages = Math.max(1, api?.lastPage ?? 1)
    pagination.totalCount = api?.totalCount ?? data.length

    return data
}

export function resetPagination(pagination) {
    pagination.totalCount = 0
    pagination.totalPages = 1
}

/**
 * Sets the current page. Returns false when the page did not change.
 */
export function setPage(pagination, page) {
    const next = Math.max(1, Number(page) || 1)
    if (pagination.currentPage === next) {
        return false
    }

    pagination.currentPage = next
    return true
}
