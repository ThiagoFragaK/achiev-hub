<template>
    <div ref="root" class="position-relative">
        <label v-if="label" class="form-label small text-secondary mb-1" :for="inputId">
            {{ label }}
        </label>
        <div class="input-group">
            <span v-if="autocomplete" class="input-group-text bg-body">
                <LucideIcon icon="Search" :size="16" />
            </span>
            <input
                :id="inputId"
                type="text"
                class="form-control"
                :class="{ 'border-start-0': autocomplete }"
                autocomplete="off"
                :placeholder="placeholder"
                :disabled="disabled || loading"
                :value="displayText"
                @input="onInput"
                @focus="openDropdown"
                @keydown.down.prevent="moveHighlight(1)"
                @keydown.up.prevent="moveHighlight(-1)"
                @keydown.enter.prevent="selectHighlighted"
                @keydown.esc.prevent="closeDropdown"
            />
            <button
                v-if="clearable && (modelValue != null || searchText)"
                type="button"
                class="input-group-text bg-body"
                :disabled="disabled || loading"
                aria-label="Clear selection"
                @mousedown.prevent="clear"
            >
                ×
            </button>
        </div>

        <div
            v-if="dropdownOpen"
            class="list-group position-absolute w-100 shadow-sm select-dropdown"
            role="listbox"
        >
            <div v-if="loading" class="list-group-item text-secondary small">
                Loading…
            </div>
            <template v-else>
                <button
                    v-for="(option, index) in visibleOptions"
                    :key="optionKey(option, index)"
                    type="button"
                    class="list-group-item list-group-item-action d-flex justify-content-between align-items-center"
                    :class="{ active: index === highlightIndex }"
                    role="option"
                    @mousedown.prevent="selectOption(option)"
                >
                    <span class="text-truncate me-2">{{ option.label }}</span>
                    <span v-if="option.meta != null && option.meta !== ''" class="small text-nowrap">
                        {{ option.meta }}
                    </span>
                </button>
                <div
                    v-if="visibleOptions.length === 0"
                    class="list-group-item text-secondary small"
                >
                    {{ emptyMessage }}
                </div>
            </template>
        </div>
    </div>
</template>

<script>
import LucideIcon from '@/components/global/LucideIcon.vue'

let selectIdCounter = 0

export default {
    name: 'SelectComponent',
    components: {
        LucideIcon
    },
    props: {
        modelValue: {
            type: [String, Number, null],
            default: null
        },
        options: {
            type: Array,
            default: () => []
        },
        label: {
            type: String,
            default: ''
        },
        placeholder: {
            type: String,
            default: 'Search…'
        },
        disabled: {
            type: Boolean,
            default: false
        },
        loading: {
            type: Boolean,
            default: false
        },
        autocomplete: {
            type: Boolean,
            default: false
        },
        searchable: {
            type: Boolean,
            default: true
        },
        clearable: {
            type: Boolean,
            default: true
        },
        emptyMessage: {
            type: String,
            default: 'No options match.'
        },
        filterLocally: {
            type: Boolean,
            default: true
        }
    },
    emits: ['update:modelValue', 'select', 'search'],
    data() {
        selectIdCounter += 1
        return {
            inputId: `select-input-${selectIdCounter}`,
            searchText: '',
            dropdownOpen: false,
            highlightIndex: 0
        }
    },
    computed: {
        selectedOption() {
            if (this.modelValue == null || this.modelValue === '') {
                return null
            }
            return this.options.find((o) => o.value === this.modelValue) ?? null
        },
        displayText() {
            if (this.dropdownOpen || this.searchText) {
                return this.searchText
            }
            return this.selectedOption?.label ?? ''
        },
        visibleOptions() {
            if (!this.searchable || !this.filterLocally) {
                return this.options
            }
            const q = this.searchText.trim().toLowerCase()
            if (!q) {
                return this.options
            }
            return this.options.filter((o) => String(o.label || '').toLowerCase().includes(q))
        }
    },
    watch: {
        modelValue(value) {
            if (value == null || value === '') {
                this.searchText = ''
            } else if (this.selectedOption) {
                this.searchText = ''
            }
        },
        visibleOptions() {
            this.highlightIndex = 0
        }
    },
    mounted() {
        document.addEventListener('mousedown', this.onDocumentClick)
    },
    beforeUnmount() {
        document.removeEventListener('mousedown', this.onDocumentClick)
    },
    methods: {
        optionKey(option, index) {
            return option.value != null ? String(option.value) : `opt-${index}`
        },
        openDropdown() {
            if (this.disabled || this.loading) {
                return
            }
            this.dropdownOpen = true
            if (this.selectedOption && !this.searchText) {
                this.searchText = ''
            }
        },
        closeDropdown() {
            this.dropdownOpen = false
            this.highlightIndex = 0
            if (this.selectedOption) {
                this.searchText = ''
            }
        },
        onInput(event) {
            this.searchText = event.target.value
            this.dropdownOpen = true
            this.$emit('search', this.searchText)
        },
        moveHighlight(delta) {
            if (!this.dropdownOpen || this.visibleOptions.length === 0) {
                this.openDropdown()
                return
            }
            const len = this.visibleOptions.length
            this.highlightIndex = (this.highlightIndex + delta + len) % len
        },
        selectHighlighted() {
            const option = this.visibleOptions[this.highlightIndex]
            if (option) {
                this.selectOption(option)
            }
        },
        selectOption(option) {
            this.$emit('update:modelValue', option.value)
            this.$emit('select', option)
            this.searchText = ''
            this.closeDropdown()
        },
        clear() {
            this.searchText = ''
            this.$emit('update:modelValue', null)
            this.$emit('select', null)
            this.$emit('search', '')
            this.openDropdown()
        },
        onDocumentClick(event) {
            if (this.dropdownOpen && !this.$refs.root?.contains(event.target)) {
                this.closeDropdown()
            }
        }
    }
}
</script>

<style scoped>
.select-dropdown {
    z-index: 1050;
    max-height: 16rem;
    overflow-y: auto;
}
</style>
