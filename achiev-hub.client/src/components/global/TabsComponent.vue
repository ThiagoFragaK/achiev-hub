<template>
    <div v-if="tabs.length > 0">
        <ul
            class="nav mt-3 tabs-bar"
            role="tablist"
            :class="{ 'nav-tabs-compact': compact }"
        >
            <li
                v-for="tab in tabs"
                :key="tab.name"
                class="nav-item"
                role="presentation"
            >
                <a
                    class="nav-link"
                    :class="{ active: activeTab === tab.name }"
                    :id="`${tab.name}-tab`"
                    href="#"
                    role="tab"
                    type="button"
                    :aria-selected="activeTab === tab.name"
                    :aria-controls="tab.name"
                    @click.prevent="setActiveTab(tab.name)"
                >
                    <LucideIcon
                        v-if="tab.icon"
                        :icon="tab.icon"
                        :size="compact ? 10 : 12"
                        class="icon-tab"
                    />
                    {{ tab.label }}
                </a>
            </li>
        </ul>
        <div class="tab-content mt-3">
            <div
                v-if="activeTab"
                :id="activeTab"
                class="tab-pane active"
                role="tabpanel"
                :aria-labelledby="`${activeTab}-tab`"
            >
                <slot :name="activeTab" />
            </div>
        </div>
    </div>
</template>

<script>
import LucideIcon from '@/components/global/LucideIcon.vue'

export default {
    name: 'TabsComponent',
    components: {
        LucideIcon,
    },
    props: {
        tabs: {
            type: Array,
            required: true,
            default: () => [],
        },
        modelValue: {
            type: String,
            default: '',
        },
        compact: {
            type: Boolean,
            default: false,
        },
    },
    emits: ['update:modelValue', 'selected'],
    computed: {
        activeTab() {
            if (this.modelValue && this.tabs.some((tab) => tab.name === this.modelValue)) {
                return this.modelValue
            }
            return this.tabs[0]?.name ?? ''
        },
    },
    mounted() {
        if (!this.modelValue && this.tabs.length > 0) {
            this.setActiveTab(this.tabs[0].name)
        }
    },
    methods: {
        setActiveTab(tabName) {
            this.$emit('update:modelValue', tabName)
            this.$emit('selected', tabName)
        },
    },
}
</script>

<style scoped>
.tabs-bar {
    gap: 0.35rem;
    flex-wrap: wrap;
}

.tabs-bar .nav-link {
    font-size: 0.77rem;
    font-weight: 500;
    padding: 0.275rem 0.6rem;
    line-height: 1.3;
    white-space: nowrap;
    color: var(--bs-secondary-color);
    background-color: transparent;
    border: 1px solid var(--bs-primary);
    border-radius: var(--bs-border-radius);
}

.tabs-bar .nav-link:hover {
    color: var(--bs-body-color);
    border-color: var(--bs-primary);
}

.tabs-bar .nav-link.active {
    color: var(--bs-light, #d7d6d0);
    background-color: var(--bs-primary);
    border-color: var(--bs-primary);
}

.icon-tab {
    vertical-align: text-bottom;
    margin-right: 4px;
}

.nav-tabs-compact .nav-link {
    padding: 0.165rem 0.44rem;
    font-size: 0.715rem;
    min-height: 26px;
    display: inline-flex;
    align-items: center;
}
</style>
