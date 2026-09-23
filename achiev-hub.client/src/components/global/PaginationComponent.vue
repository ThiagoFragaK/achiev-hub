<template>
    <nav class="mt-3" aria-label="Pagination">
        <ul class="pagination justify-content-center">
            <li class="page-item" :class="{ disabled: current === 1 }">
                <a
                    class="page-link d-flex align-items-center justify-content-center"
                    href="#"
                    aria-label="Previous page"
                    @click.prevent="changePage(current - 1)"
                >
                    <LucideIcon icon="ChevronsLeft" :size="18" />
                </a>
            </li>

            <li
                v-for="(item, index) in pages"
                :key="`${item}-${index}`"
                class="page-item"
                :class="{ active: item === current, disabled: item === 'ellipsis' }"
            >
                <span v-if="item === 'ellipsis'" class="page-link">…</span>
                <a
                    v-else
                    class="page-link"
                    href="#"
                    :aria-current="item === current ? 'page' : undefined"
                    @click.prevent="changePage(item)"
                >
                    {{ item }}
                </a>
            </li>

            <li class="page-item" :class="{ disabled: current === totalPages }">
                <a
                    class="page-link d-flex align-items-center justify-content-center"
                    href="#"
                    aria-label="Next page"
                    @click.prevent="changePage(current + 1)"
                >
                    <LucideIcon icon="ChevronsRight" :size="18" />
                </a>
            </li>
        </ul>
    </nav>
</template>

<script>
import LucideIcon from '@/components/global/LucideIcon.vue'

export default {
    name: 'PaginationComponent',
    components: {
        LucideIcon
    },
    props: {
        currentPage: {
            type: Number,
            required: true
        },
        totalPages: {
            type: Number,
            required: true
        },
        perPage: {
            type: Number,
            required: true
        },
        totalItems: {
            type: Number,
            required: true
        }
    },
    emits: ['change-page'],
    data() {
        return {
            current: this.currentPage
        }
    },
    watch: {
        currentPage(newVal) {
            this.current = newVal
        }
    },
    computed: {
        pages() {
            const total = Math.max(1, this.totalPages)
            const current = Math.min(Math.max(1, this.current), total)

            if (total <= 5) {
                return Array.from({ length: total }, (_, i) => i + 1)
            }

            const items = [1]
            const start = Math.max(2, current - 1)
            const end = Math.min(total - 1, current + 1)

            if (start > 2) {
                items.push('ellipsis')
            }

            for (let page = start; page <= end; page += 1) {
                items.push(page)
            }

            if (end < total - 1) {
                items.push('ellipsis')
            }

            items.push(total)
            return items
        }
    },
    methods: {
        changePage(page) {
            if (page === 'ellipsis') {
                return
            }
            if (this.isValidPage(page)) {
                this.current = page
                this.$emit('change-page', page)
            }
        },
        isValidPage(page) {
            return page >= 1 && page <= this.totalPages && page !== this.current
        }
    }
}
</script>
