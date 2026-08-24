<template>
    <div class="card h-100">
        <div class="card-body d-flex align-items-center justify-content-center">
            <SemiGauge :value="value" :label="label" />
        </div>
    </div>
</template>

<script>
import SemiGauge from '@/components/charts/SemiGauge.vue'

export default {
    name: 'UsersAverageSemiGauge',
    components: {
        SemiGauge
    },
    props: {
        value: {
            type: Number,
            required: false,
            default: 67
        },
        label: {
            type: String,
            required: false,
            default: 'Users Average'
        }
    }
}
</script>
