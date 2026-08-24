<template>
    <div :id="collapseId" ref="collapseEl" class="collapse">
        <slot />
    </div>
</template>

<script>
import { Collapse } from 'bootstrap'

export default {
    name: 'CollapseComponent',
    props: {
        collapseId: {
            type: String,
            required: true
        }
    },
    mounted() {
        this.collapseInstance = new Collapse(this.$refs.collapseEl, {
            toggle: false
        })
    },
    beforeUnmount() {
        this.collapseInstance?.dispose()
        this.collapseInstance = null
    },
    methods: {
        open() {
            this.collapseInstance?.show()
        },
        close() {
            this.collapseInstance?.hide()
        },
        toggle() {
            this.collapseInstance?.toggle()
        }
    }
}
</script>
