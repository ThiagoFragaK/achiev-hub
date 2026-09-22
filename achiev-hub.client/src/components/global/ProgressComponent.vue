<template>
    <div 
        class="progress" 
        role="progressbar" 
        :aria-label="label" 
        :aria-valuenow="value" 
        :aria-valuemin="0" 
        :aria-valuemax="100"
    >
        <div 
            class="progress-bar"
            :class="'bg-' + color"
            :style="{ width: value + '%' }"
        >
            {{ label }}
        </div>
    </div>
</template>

<script>
export default {
    name: 'ProgressComponent',
    props: {
        value: {
            type: Number,
            required: true
        },
        label: {
            type: String,
            required: false
        },
        color: {
            type: String,
            required: false,
            default: 'primary'
        }
    }
}
</script>