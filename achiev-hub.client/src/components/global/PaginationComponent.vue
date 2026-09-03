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
                v-for="page in pages"
                :key="page"
                class="page-item"
                :class="{ active: page === current }"
            >
                <a
                    class="page-link"
                    href="#"
                    :aria-current="page === current ? 'page' : undefined"
                    @click.prevent="changePage(page)"
                >
                    {{ page }}
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
            const range = []
            let start = this.current - 1
            let end = this.current + 1

            if (start < 1) {
                start = 1
                end = Math.min(3, this.totalPages)
            }

            if (end > this.totalPages) {
                end = this.totalPages
                start = Math.max(1, end - 2)
            }

            for (let i = start; i <= end; i++) {
                range.push(i)
            }
            return range
        }
    },
    methods: {
        changePage(page) {
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
