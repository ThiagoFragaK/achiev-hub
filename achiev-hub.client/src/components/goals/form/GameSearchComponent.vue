<template>
    <SelectComponent
        v-model="selectedAppId"
        :options="options"
        :loading="loading"
        :disabled="disabled"
        :autocomplete="true"
        :searchable="true"
        :filter-locally="true"
        label="Game"
        placeholder="Search games…"
        empty-message="No eligible games match."
        @select="onSelect"
        @search="onSearch"
    />
    <p v-if="loadError" class="text-danger small mt-1 mb-0">{{ loadError }}</p>
</template>

<script>
import SelectComponent from '@/components/global/SelectComponent.vue'
import { getEligibleGames } from '@/services/goalsService'
import { formatPercentage } from '@/utils/NumberFormat'

export default {
    name: 'GameSearchComponent',
    components: {
        SelectComponent
    },
    props: {
        modelValue: {
            type: [Number, String, null],
            default: null
        },
        disabled: {
            type: Boolean,
            default: false
        }
    },
    emits: ['update:modelValue', 'select'],
    data() {
        return {
            loading: false,
            loadError: '',
            games: [],
            selectedAppId: this.modelValue,
            searchQuery: ''
        }
    },
    computed: {
        options() {
            return this.games.map((game) => ({
                value: game.appId,
                label: game.name,
                meta: formatPercentage(game.achievementsPercentage),
                game
            }))
        }
    },
    watch: {
        modelValue(value) {
            this.selectedAppId = value
        },
        selectedAppId(value) {
            if (value !== this.modelValue) {
                this.$emit('update:modelValue', value)
            }
        }
    },
    async created() {
        await this.loadGames()
        if (this.modelValue != null) {
            this.selectedAppId = Number(this.modelValue) || this.modelValue
            const match = this.options.find((o) => o.value === this.selectedAppId)
            if (match) {
                this.$emit('select', match.game)
            }
        }
    },
    methods: {
        async loadGames(q) {
            this.loading = true
            this.loadError = ''
            try {
                this.games = await getEligibleGames(q) || []
            } catch (err) {
                this.games = []
                this.loadError = err?.message || 'Failed to load games.'
            } finally {
                this.loading = false
            }
        },
        onSearch(text) {
            this.searchQuery = text
        },
        onSelect(option) {
            if (!option) {
                this.$emit('update:modelValue', null)
                this.$emit('select', null)
                return
            }
            this.$emit('update:modelValue', option.value)
            this.$emit('select', option.game || option)
        }
    }
}
</script>
